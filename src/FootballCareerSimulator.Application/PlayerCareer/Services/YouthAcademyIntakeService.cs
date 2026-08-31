using FootballCareerSimulator.Application.ClubGovernance.Ports;
using FootballCareerSimulator.Application.Competition.Ports;
using FootballCareerSimulator.Application.Interaction.Ports;
using FootballCareerSimulator.Application.ManagerCareer.Ports;
using FootballCareerSimulator.Application.PlayerCareer.Queries;
using FootballCareerSimulator.Application.WorldCalendar.Ports;
using FootballCareerSimulator.Domain.Competition;
using FootballCareerSimulator.Domain.Interaction;
using FootballCareerSimulator.Domain.PlayerCareer;
using FootballCareerSimulator.Domain.Shared;
using FootballCareerSimulator.Simulation.PlayerCareer;
using FootballCareerSimulator.Simulation.TeamPreparation;

namespace FootballCareerSimulator.Application.PlayerCareer.Services;

/// <summary>
/// Sezonluk akademi havuzunu mevcut dünya seed'inden yeniden üretir. Yalnızca kararlar
/// mevcut DecisionRequest deposuna yazılır; bu nedenle aday verisi için yeni save şeması gerekmez.
/// </summary>
public sealed class YouthAcademyIntakeService
{
    private readonly IClubRegistryStore _clubs;
    private readonly ILeagueCompetitionStore _competition;
    private readonly IManagerCareerStore _managerCareer;
    private readonly IWorldTimelineStore _timeline;
    private readonly IDecisionRequestStore _decisions;

    public YouthAcademyIntakeService(
        IClubRegistryStore clubs,
        ILeagueCompetitionStore competition,
        IManagerCareerStore managerCareer,
        IWorldTimelineStore timeline,
        IDecisionRequestStore decisions)
    {
        _clubs = clubs ?? throw new ArgumentNullException(nameof(clubs));
        _competition = competition ?? throw new ArgumentNullException(nameof(competition));
        _managerCareer = managerCareer ?? throw new ArgumentNullException(nameof(managerCareer));
        _timeline = timeline ?? throw new ArgumentNullException(nameof(timeline));
        _decisions = decisions ?? throw new ArgumentNullException(nameof(decisions));
    }

    public YouthAcademyIntakeReadModel? GetManagedClubIntake()
    {
        var employment = _managerCareer.Career.ActiveEmployment;
        var season = _competition.League.CurrentSeason;
        return employment is null || season is null
            ? null
            : GetIntake(employment.ClubId, season.SeasonId);
    }

    public YouthAcademyIntakeReadModel GetIntake(ClubId clubId, SeasonId seasonId)
    {
        var club = _clubs.Registry.GetClubOrThrow(clubId);
        var season = _competition.League.Seasons
            .SingleOrDefault(candidate => candidate.SeasonId == seasonId)
            ?? throw new YouthAcademyIntakeException($"Season {seasonId.Value} was not found.");
        var isRevealed = _timeline.Timeline.CurrentDate >= season.PreseasonStartDate;
        if (!isRevealed)
        {
            return new YouthAcademyIntakeReadModel(
                clubId.Value,
                seasonId.Value,
                season.PreseasonStartDate.DayNumber,
                IsRevealed: false,
                Array.Empty<YouthAcademyCandidateReadModel>());
        }

        var generated = MvpYouthAcademyIntakeGenerator.Generate(
            clubId,
            seasonId,
            _timeline.Timeline.RootSeed,
            club.SportiveStrength,
            _timeline.Timeline.RngVersion);

        return new YouthAcademyIntakeReadModel(
            clubId.Value,
            seasonId.Value,
            season.PreseasonStartDate.DayNumber,
            IsRevealed: true,
            generated.Select(candidate => ToReadModel(clubId, candidate)).ToArray());
    }

    public YouthAcademyCandidateReadModel AcceptManagedCandidate(long candidatePlayerId) =>
        DecideManagedCandidate(candidatePlayerId, YouthAcademyCandidateDecisionStatus.Accepted);

    public YouthAcademyCandidateReadModel RejectManagedCandidate(long candidatePlayerId) =>
        DecideManagedCandidate(candidatePlayerId, YouthAcademyCandidateDecisionStatus.Rejected);

    private YouthAcademyCandidateReadModel DecideManagedCandidate(
        long candidatePlayerId,
        YouthAcademyCandidateDecisionStatus decision)
    {
        if (decision == YouthAcademyCandidateDecisionStatus.Pending)
        {
            throw new ArgumentOutOfRangeException(nameof(decision), decision, "A final academy decision is required.");
        }

        var intake = GetManagedClubIntake()
            ?? throw new YouthAcademyIntakeException(
                "An employed manager and a current season are required for an academy decision.");
        var candidate = intake.Candidates.SingleOrDefault(item => item.PlayerId == candidatePlayerId)
            ?? throw new YouthAcademyIntakeException(
                $"Candidate {candidatePlayerId} does not belong to the current managed-club intake.");

        if (candidate.DecisionStatus != YouthAcademyCandidateDecisionStatus.Pending)
        {
            if (candidate.DecisionStatus == decision)
            {
                return candidate;
            }

            throw new YouthAcademyIntakeException(
                $"Candidate {candidatePlayerId} already has a {candidate.DecisionStatus} decision.");
        }

        var playerId = new PlayerId(candidatePlayerId);
        var nextId = _decisions.Requests.Count == 0
            ? 1L
            : checked(_decisions.Requests.Max(request => request.DecisionRequestId.Value) + 1L);
        var day = _timeline.Timeline.CurrentDate;
        var request = DecisionRequest.OpenYouthAcademyCandidateRequest(
                new DecisionRequestId(nextId),
                _managerCareer.Career.ManagerId,
                playerId,
                new ClubId(intake.ClubId),
                day,
                day,
                isHardBlocker: false)
            .Answer(
                decision == YouthAcademyCandidateDecisionStatus.Accepted
                    ? DecisionRequest.OptionAcceptYouthAcademyCandidate
                    : DecisionRequest.OptionRejectYouthAcademyCandidate,
                day);
        _decisions.Upsert(request);

        return GetManagedClubIntake()!.Candidates.Single(item => item.PlayerId == candidatePlayerId);
    }

    private YouthAcademyCandidateReadModel ToReadModel(
        ClubId clubId,
        MvpYouthAcademyCandidate candidate)
    {
        var request = _decisions.Requests
            .Where(item => item.Kind == DecisionRequestKind.YouthAcademyCandidate)
            .Where(item => item.ClubId == clubId && item.SubjectPlayerId == candidate.PlayerId)
            .OrderByDescending(item => item.DecisionRequestId.Value)
            .FirstOrDefault();
        var status = request?.SelectedOptionCode switch
        {
            DecisionRequest.OptionAcceptYouthAcademyCandidate =>
                YouthAcademyCandidateDecisionStatus.Accepted,
            DecisionRequest.OptionRejectYouthAcademyCandidate =>
                YouthAcademyCandidateDecisionStatus.Rejected,
            _ => YouthAcademyCandidateDecisionStatus.Pending,
        };

        return new YouthAcademyCandidateReadModel(
            candidate.PlayerId.Value,
            candidate.CandidateIndex,
            candidate.DisplayName,
            candidate.PositionRole.ToPositionCode(),
            candidate.PositionRole.ToTurkishName(),
            candidate.Age,
            candidate.CurrentAbility,
            candidate.PotentialAbility,
            candidate.DevelopmentProfile,
            status,
            request?.DecisionRequestId.Value,
            request?.ResolvedOn?.DayNumber);
    }
}

public sealed class YouthAcademyIntakeException : Exception
{
    public YouthAcademyIntakeException(string message)
        : base(message)
    {
    }
}
