namespace FootballCareerSimulator.Domain.Competition;

/// <summary>
/// docs/03_DOMAIN_MODEL.md Bölüm 12.3 — Preseason → ActiveSeason → Completed → Archived.
/// </summary>
public enum SeasonStatus
{
    Preseason = 0,
    Active = 1,
    Completed = 2,
    Archived = 3,
}
