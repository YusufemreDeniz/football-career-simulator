using FootballCareerSimulator.Application.ContractRegistration.Services;
using FootballCareerSimulator.Application.PlayerCareer.Ports;
using FootballCareerSimulator.Application.TeamPreparation.Services;
using FootballCareerSimulator.Application.TrainingPhysicalState.Ports;
using FootballCareerSimulator.Application.WorldCalendar.Ports;
using FootballCareerSimulator.Domain.Shared;
using FootballCareerSimulator.Domain.TrainingPhysicalState;
using FootballCareerSimulator.Domain.WorldCalendar;
using FootballCareerSimulator.Simulation.PlayerCareer;

namespace FootballCareerSimulator.Application.PlayerCareer.Services;

/// <summary>
/// Sezon sonunda yaşlanma, emeklilik, genç oyuncu üretimi ve bağlı context senkronunu tek kapıda yürütür.
/// </summary>
public sealed class SeasonPlayerLifecycleService
{
    private readonly IPlayerCareerStore _store;
    private readonly PlayerCareerDevelopmentService _development;
    private readonly ContractRegistrationService _contracts;
    private readonly ClubSquadService _squads;
    private readonly ITrainingPhysicalStateStore _training;
    private readonly IWorldTimelineStore _timeline;
    private readonly IYouthAcademySuccessorProvider? _academySuccessors;

    public SeasonPlayerLifecycleService(
        IPlayerCareerStore store,
        PlayerCareerDevelopmentService development,
        ContractRegistrationService contracts,
        ClubSquadService squads,
        ITrainingPhysicalStateStore training,
        IWorldTimelineStore timeline,
        IYouthAcademySuccessorProvider? academySuccessors = null)
    {
        _store = store ?? throw new ArgumentNullException(nameof(store));
        _development = development ?? throw new ArgumentNullException(nameof(development));
        _contracts = contracts ?? throw new ArgumentNullException(nameof(contracts));
        _squads = squads ?? throw new ArgumentNullException(nameof(squads));
        _training = training ?? throw new ArgumentNullException(nameof(training));
        _timeline = timeline ?? throw new ArgumentNullException(nameof(timeline));
        _academySuccessors = academySuccessors;
    }

    public SeasonPlayerLifecycleResult ApplySeasonRollover(GameDate day)
    {
        var agedCount = _development.ApplyDueAging(day);
        var expiry = _contracts.ExpireDueContracts(day);
        var candidates = _store.Careers
            .Where(career => !career.IsRetired)
            .Select(career => (
                Career: career,
                Evaluation: MvpRetirementEvaluator.Evaluate(
                    career,
                    day,
                    _timeline.Timeline.RootSeed)))
            .Where(candidate => candidate.Evaluation.Decision == RetirementEvaluationDecision.Retire)
            .OrderBy(candidate => candidate.Career.OriginClubId.Value)
            .ThenBy(candidate => candidate.Career.SlotIndex)
            .ThenBy(candidate => candidate.Career.Id.Value)
            .ToArray();

        if (candidates.Length == 0)
        {
            var continuityOnly = _contracts.RestorePopulationContinuity(day);
            var continuityClubIds = expiry.AffectedClubIds
                .Concat(continuityOnly.AffectedClubIds)
                .Distinct()
                .OrderBy(value => value)
                .ToArray();
            SyncClubs(
                continuityClubIds,
                day,
                Array.Empty<(
                    Domain.PlayerCareer.PlayerCareer Retired,
                    Domain.PlayerCareer.PlayerCareer Successor,
                    bool FromAcademy)>());
            return new SeasonPlayerLifecycleResult(
                agedCount,
                0,
                0,
                continuityOnly.RenewedPlayerCount,
                continuityOnly.RetainedFreeAgentCount,
                continuityClubIds);
        }

        var reservedAcademyIds = new HashSet<Domain.PlayerCareer.PlayerId>();
        var transitions = candidates.Select(candidate =>
        {
            var career = candidate.Career;
            var generation = _store.Careers
                .Where(existing => existing.OriginClubId == career.OriginClubId)
                .Where(existing => existing.SlotIndex == career.SlotIndex)
                .Max(existing => existing.Generation) + 1;
            var academySuccessor = _academySuccessors?.CreateSuccessor(
                career.OriginClubId,
                career.SlotIndex,
                generation,
                day,
                reservedAcademyIds);
            if (academySuccessor is not null)
            {
                reservedAcademyIds.Add(academySuccessor.Id);
            }
            var successor = academySuccessor ?? MvpGeneratedPlayerFactory.CreateSuccessor(
                    career.OriginClubId,
                    career.SlotIndex,
                    generation,
                    day,
                    _timeline.Timeline.RootSeed);
            return (
                Retired: career.Retire(day, candidate.Evaluation.Reason!.Value),
                Successor: successor,
                FromAcademy: academySuccessor is not null);
        }).ToArray();

        foreach (var transition in transitions)
        {
            _store.Upsert(transition.Retired);
            _contracts.RetirePlayer(transition.Retired.Id, day);
            _store.Upsert(transition.Successor);
        }

        var continuity = _contracts.RestorePopulationContinuity(day);
        var affectedClubIds = transitions
            .Select(transition => transition.Retired.OriginClubId.Value)
            .Concat(expiry.AffectedClubIds)
            .Concat(continuity.AffectedClubIds)
            .Distinct()
            .OrderBy(value => value)
            .ToArray();

        SyncClubs(affectedClubIds, day, transitions);

        return new SeasonPlayerLifecycleResult(
            agedCount,
            transitions.Length,
            transitions.Length,
            continuity.RenewedPlayerCount,
            continuity.RetainedFreeAgentCount,
            affectedClubIds,
            transitions.Count(transition => transition.FromAcademy));
    }

    private void SyncClubs(
        IReadOnlyList<long> affectedClubIds,
        GameDate day,
        IReadOnlyList<(Domain.PlayerCareer.PlayerCareer Retired, Domain.PlayerCareer.PlayerCareer Successor, bool FromAcademy)> transitions)
    {
        foreach (var clubIdValue in affectedClubIds)
        {
            var clubId = new ClubId(clubIdValue);
            _contracts.EnsureClubContracts(clubId, day);
            _squads.SyncFromActiveContracts(clubId, day);

            var physicalBySlot = _training.PhysicalStates
                .Where(state => state.ClubId == clubId)
                .ToDictionary(state => state.SlotIndex);
            foreach (var transition in transitions.Where(item => item.Retired.OriginClubId == clubId))
            {
                physicalBySlot[transition.Retired.SlotIndex] =
                    PlayerPhysicalState.CreateRested(clubId, transition.Retired.SlotIndex);
            }

            _training.ReplacePhysicalStatesForClub(clubId, physicalBySlot.Values);
        }

    }
}

public sealed record SeasonPlayerLifecycleResult(
    int AgedPlayerCount,
    int RetiredPlayerCount,
    int GeneratedPlayerCount,
    int RenewedContractCount,
    int ActiveFreeAgentCount,
    IReadOnlyList<long> AffectedClubIds,
    int PromotedAcademyPlayerCount = 0)
{
    public static SeasonPlayerLifecycleResult Empty { get; } =
        new(0, 0, 0, 0, 0, Array.Empty<long>());
}
