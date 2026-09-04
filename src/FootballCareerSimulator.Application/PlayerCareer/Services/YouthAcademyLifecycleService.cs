using FootballCareerSimulator.Application.ClubGovernance.Ports;
using FootballCareerSimulator.Application.Competition.Ports;
using FootballCareerSimulator.Application.ContractRegistration.Ports;
using FootballCareerSimulator.Application.Interaction.Ports;
using FootballCareerSimulator.Application.ManagerCareer.Ports;
using FootballCareerSimulator.Application.PlayerCareer.Ports;
using FootballCareerSimulator.Application.PlayerCareer.Queries;
using FootballCareerSimulator.Application.TeamPreparation.Ports;
using FootballCareerSimulator.Application.WorldCalendar.Ports;
using FootballCareerSimulator.Domain.ContractRegistration;
using FootballCareerSimulator.Domain.Interaction;
using FootballCareerSimulator.Domain.PlayerCareer;
using FootballCareerSimulator.Domain.Shared;
using FootballCareerSimulator.Domain.TeamPreparation;
using FootballCareerSimulator.Domain.WorldCalendar;
using FootballCareerSimulator.Simulation.PlayerCareer;
using FootballCareerSimulator.Simulation.TeamPreparation;
using PlayerCareerAggregate = FootballCareerSimulator.Domain.PlayerCareer.PlayerCareer;

namespace FootballCareerSimulator.Application.PlayerCareer.Services;

/// <summary>
/// Kabul kararlarından akademi durumunu yeniden kurar; terfide mevcut kariyer, sözleşme ve
/// kadro depolarını tek bir uygulama işlemi olarak günceller.
/// </summary>
public sealed class YouthAcademyLifecycleService : IYouthAcademySuccessorProvider
{
    private readonly IClubRegistryStore _clubs;
    private readonly ILeagueCompetitionStore _competition;
    private readonly IManagerCareerStore _managerCareer;
    private readonly IWorldTimelineStore _timeline;
    private readonly IDecisionRequestStore _decisions;
    private readonly IPlayerCareerStore _careers;
    private readonly IContractStore _contracts;
    private readonly IClubSquadStore _squads;

    public YouthAcademyLifecycleService(
        IClubRegistryStore clubs,
        ILeagueCompetitionStore competition,
        IManagerCareerStore managerCareer,
        IWorldTimelineStore timeline,
        IDecisionRequestStore decisions,
        IPlayerCareerStore careers,
        IContractStore contracts,
        IClubSquadStore squads)
    {
        _clubs = clubs ?? throw new ArgumentNullException(nameof(clubs));
        _competition = competition ?? throw new ArgumentNullException(nameof(competition));
        _managerCareer = managerCareer ?? throw new ArgumentNullException(nameof(managerCareer));
        _timeline = timeline ?? throw new ArgumentNullException(nameof(timeline));
        _decisions = decisions ?? throw new ArgumentNullException(nameof(decisions));
        _careers = careers ?? throw new ArgumentNullException(nameof(careers));
        _contracts = contracts ?? throw new ArgumentNullException(nameof(contracts));
        _squads = squads ?? throw new ArgumentNullException(nameof(squads));
    }

    public YouthAcademyLifecycleReadModel? GetManagedAcademy()
    {
        var employment = _managerCareer.Career.ActiveEmployment;
        return employment is null ? null : GetAcademy(employment.ClubId);
    }

    public YouthAcademyLifecycleReadModel GetAcademy(ClubId clubId)
    {
        var club = _clubs.Registry.GetClubOrThrow(clubId);
        var seasons = _competition.League.Seasons
            .Where(season => season.PreseasonStartDate <= _timeline.Timeline.CurrentDate)
            .OrderBy(season => season.PreseasonStartDate.DayNumber)
            .ThenBy(season => season.SeasonId.Value)
            .ToArray();
        var acceptedIds = _decisions.Requests
            .Where(request => request.Kind == DecisionRequestKind.YouthAcademyCandidate)
            .Where(request => request.ClubId == clubId)
            .Where(request => request.SelectedOptionCode == DecisionRequest.OptionAcceptYouthAcademyCandidate)
            .Select(request => request.SubjectPlayerId.Value)
            .ToHashSet();

        var players = new List<YouthAcademyPlayerReadModel>();
        for (var intakeIndex = 0; intakeIndex < seasons.Length; intakeIndex++)
        {
            var intakeSeason = seasons[intakeIndex];
            var completedAcademySeasons = seasons.Length - intakeIndex - 1;
            var candidates = MvpYouthAcademyIntakeGenerator.Generate(
                clubId,
                intakeSeason.SeasonId,
                _timeline.Timeline.RootSeed,
                club.SportiveStrength,
                _timeline.Timeline.RngVersion);
            foreach (var candidate in candidates.Where(item => acceptedIds.Contains(item.PlayerId.Value)))
            {
                var projection = MvpYouthAcademyDevelopmentProjector.Project(
                    candidate,
                    completedAcademySeasons,
                    _timeline.Timeline.RootSeed,
                    _timeline.Timeline.RngVersion);
                var career = _careers.Careers.FirstOrDefault(item => item.Id == candidate.PlayerId);
                var contract = _contracts.GetByPlayer(candidate.PlayerId);
                var status = career is not null
                    ? YouthAcademyLifecycleStatus.PromotedToFirstTeam
                    : projection.IsPromotionEligible
                        ? YouthAcademyLifecycleStatus.PromotionEligible
                        : YouthAcademyLifecycleStatus.Developing;
                players.Add(new YouthAcademyPlayerReadModel(
                    candidate.PlayerId.Value,
                    clubId.Value,
                    intakeSeason.SeasonId.Value,
                    candidate.DisplayName,
                    candidate.PositionRole.ToPositionCode(),
                    candidate.PositionRole.ToTurkishName(),
                    projection.Age,
                    career?.CurrentAbility ?? projection.CurrentAbility,
                    career?.PotentialAbility ?? projection.PotentialAbility,
                    projection.CompletedAcademySeasons,
                    status,
                    HasCareerSlot: career is not null,
                    career?.SlotIndex,
                    contract?.EndDate.DayNumber,
                    contract?.WeeklyWage));
            }
        }

        return new YouthAcademyLifecycleReadModel(
            clubId.Value,
            players.OrderByDescending(player => player.Status)
                .ThenByDescending(player => player.PotentialAbility)
                .ThenBy(player => player.PlayerId)
                .ToArray());
    }

    public YouthAcademyPromotionResult PromoteManagedCandidate(long candidatePlayerId)
    {
        var employment = _managerCareer.Career.ActiveEmployment
            ?? throw new YouthAcademyLifecycleException("An employed manager is required for academy promotion.");
        var candidate = GetAcademy(employment.ClubId).Players
            .SingleOrDefault(player => player.PlayerId == candidatePlayerId)
            ?? throw new YouthAcademyLifecycleException(
                $"Accepted academy player {candidatePlayerId} was not found at the managed club.");
        if (candidate.Status == YouthAcademyLifecycleStatus.PromotedToFirstTeam)
        {
            var existing = _contracts.GetByPlayer(new PlayerId(candidatePlayerId))!;
            return new YouthAcademyPromotionResult(
                candidate.PlayerId,
                candidate.ClubId,
                candidate.FirstTeamSlot!.Value,
                existing.WeeklyWage,
                existing.EndDate.DayNumber,
                candidate.CurrentAbility,
                candidate.PotentialAbility);
        }

        if (candidate.Status != YouthAcademyLifecycleStatus.PromotionEligible)
        {
            throw new YouthAcademyLifecycleException(
                $"Academy player {candidatePlayerId} is not ready for first-team promotion.");
        }

        var clubId = employment.ClubId;
        var activeCareerSlots = _careers.Careers
            .Where(career => career.OriginClubId == clubId && !career.IsRetired)
            .Select(career => career.SlotIndex)
            .ToHashSet();
        var squad = _squads.Get(clubId) ?? ClubSquad.Empty(clubId);
        var slot = Enumerable.Range(MatchSelection.MinSquadSlot, ClubSquad.MaxMembers)
            .FirstOrDefault(value => !activeCareerSlots.Contains(value) && !squad.ContainsSlot(value), -1);
        if (slot < 0)
        {
            throw new YouthAcademyLifecycleException(
                "No first-team career slot is available; complete a senior-player lifecycle transition first.");
        }

        var day = _timeline.Timeline.CurrentDate;
        var birthYear = day.Year - candidate.Age;
        var career = PlayerCareerAggregate.Rehydrate(
            new PlayerId(candidate.PlayerId),
            clubId,
            slot,
            candidate.CurrentAbility,
            candidate.PotentialAbility,
            developmentPoints: 0,
            lastDevelopedOn: null,
            birthYear,
            lastAgedCalendarYear: null,
            generation: 1);
        var endDate = GameDate.FromCalendarDate(day.Year + 3, day.Month, day.Day);
        var weeklyWage = Math.Max(500, candidate.CurrentAbility * 80);
        var contract = PlayerContract.Activate(career.Id, clubId, day, endDate, weeklyWage);
        var nextSquad = squad.EnsureMember(career.Id, slot, day);

        _careers.Upsert(career);
        _contracts.Upsert(contract);
        _squads.Upsert(nextSquad);

        return new YouthAcademyPromotionResult(
            career.Id.Value,
            clubId.Value,
            slot,
            weeklyWage,
            endDate.DayNumber,
            career.CurrentAbility,
            career.PotentialAbility);
    }

    public PlayerCareerAggregate? CreateSuccessor(
        ClubId clubId,
        int slotIndex,
        int generation,
        GameDate day,
        IReadOnlySet<PlayerId> excludedPlayerIds)
    {
        ArgumentNullException.ThrowIfNull(excludedPlayerIds);
        var candidate = GetAcademy(clubId).Players
            .Where(player => player.Status == YouthAcademyLifecycleStatus.PromotionEligible)
            .Where(player => !excludedPlayerIds.Contains(new PlayerId(player.PlayerId)))
            .OrderByDescending(player => player.PotentialAbility)
            .ThenByDescending(player => player.CurrentAbility)
            .ThenBy(player => player.PlayerId)
            .FirstOrDefault();
        if (candidate is null)
        {
            return null;
        }

        return PlayerCareerAggregate.Rehydrate(
            new PlayerId(candidate.PlayerId),
            clubId,
            slotIndex,
            candidate.CurrentAbility,
            candidate.PotentialAbility,
            developmentPoints: 0,
            lastDevelopedOn: null,
            birthYear: day.Year - candidate.Age,
            lastAgedCalendarYear: null,
            generation: generation);
    }
}

public sealed class YouthAcademyLifecycleException : Exception
{
    public YouthAcademyLifecycleException(string message)
        : base(message)
    {
    }
}
