namespace FootballCareerSimulator.Domain.Match;

/// <summary>
/// MVP maç önemli anı (gol / kart / sakatlık); tam match timeline değil.
/// </summary>
public enum MatchKeyMomentKind
{
    Goal = 0,
    YellowCard = 1,
    RedCard = 2,
    Injury = 3,
}

public sealed record MatchKeyMoment(
    MatchKeyMomentKind Kind,
    int Minute,
    bool IsHomeSide,
    int PrimarySlotIndex,
    int? AssistSlotIndex = null);
