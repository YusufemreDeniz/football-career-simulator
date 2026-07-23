namespace FootballCareerSimulator.Domain.Transfer;

/// <summary>
/// Sportif ihtiyaç kaynağı (iskelet — Target/Process yok).
/// </summary>
public enum TransferNeedKind
{
    PositionGap = 1,
    SquadDepth = 2,
    Aging = 3,
    InjuryCover = 4,
    ExpiringContract = 5,
    TacticalRequirement = 6,
}
