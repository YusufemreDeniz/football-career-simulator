namespace FootballCareerSimulator.Application.PlayerCareer.Queries;

public enum YouthAcademyCandidateDecisionStatus
{
    Pending = 1,
    Accepted = 2,
    Rejected = 3,
}

public sealed record YouthAcademyCandidateReadModel(
    long PlayerId,
    int CandidateIndex,
    string DisplayName,
    string PositionCode,
    string PositionName,
    int Age,
    int CurrentAbility,
    int PotentialAbility,
    string DevelopmentProfile,
    YouthAcademyCandidateDecisionStatus DecisionStatus,
    long? DecisionRequestId,
    int? DecidedOnDayNumber);

public sealed record YouthAcademyIntakeReadModel(
    long ClubId,
    long SeasonId,
    int RevealDayNumber,
    bool IsRevealed,
    IReadOnlyList<YouthAcademyCandidateReadModel> Candidates)
{
    public int PendingCount => Candidates.Count(candidate =>
        candidate.DecisionStatus == YouthAcademyCandidateDecisionStatus.Pending);

    public int AcceptedCount => Candidates.Count(candidate =>
        candidate.DecisionStatus == YouthAcademyCandidateDecisionStatus.Accepted);

    public bool IsComplete => IsRevealed && PendingCount == 0;
}

public sealed record YouthAcademyPlayerReadModel(
    long PlayerId,
    long ClubId,
    long IntakeSeasonId,
    string DisplayName,
    string PositionCode,
    string PositionName,
    int Age,
    int CurrentAbility,
    int PotentialAbility,
    int CompletedAcademySeasons,
    Domain.PlayerCareer.YouthAcademyLifecycleStatus Status,
    bool HasCareerSlot,
    int? FirstTeamSlot,
    int? ContractEndDayNumber,
    int? WeeklyWage);

public sealed record YouthAcademyLifecycleReadModel(
    long ClubId,
    IReadOnlyList<YouthAcademyPlayerReadModel> Players)
{
    public int DevelopingCount => Players.Count(player =>
        player.Status == Domain.PlayerCareer.YouthAcademyLifecycleStatus.Developing);

    public int PromotionEligibleCount => Players.Count(player =>
        player.Status == Domain.PlayerCareer.YouthAcademyLifecycleStatus.PromotionEligible);

    public int PromotedCount => Players.Count(player =>
        player.Status == Domain.PlayerCareer.YouthAcademyLifecycleStatus.PromotedToFirstTeam);
}

public sealed record YouthAcademyPromotionResult(
    long PlayerId,
    long ClubId,
    int SquadSlot,
    int WeeklyWage,
    int ContractEndDayNumber,
    int CurrentAbility,
    int PotentialAbility);
