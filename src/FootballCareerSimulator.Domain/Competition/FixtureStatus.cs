namespace FootballCareerSimulator.Domain.Competition;

/// <summary>
/// docs/03_DOMAIN_MODEL.md Bölüm 12.4 — Planned → PreparationOpen → Ready → ResultAccepted → Archived.
/// </summary>
public enum FixtureStatus
{
    Planned = 0,
    PreparationOpen = 1,
    Ready = 2,
    ResultAccepted = 3,
    Archived = 4,
}
