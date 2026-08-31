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
