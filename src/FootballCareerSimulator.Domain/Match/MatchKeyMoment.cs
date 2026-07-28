namespace FootballCareerSimulator.Domain.Match;

/// <summary>
/// MVP maç önemli anı (gol); tam timeline değil.
/// </summary>
public sealed record MatchKeyMoment(
    int Minute,
    bool IsHomeGoal,
    int ScorerSlotIndex);
