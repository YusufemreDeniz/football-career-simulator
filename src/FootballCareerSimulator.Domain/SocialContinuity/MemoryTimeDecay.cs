using FootballCareerSimulator.Domain.WorldCalendar;

namespace FootballCareerSimulator.Domain.SocialContinuity;

/// <summary>
/// Basit deterministik Memory time decay (ilk dikey kesit).
/// Formül dengeleme için açık bırakılmıştı; burada kategori bazlı MVP oranı sabittir.
/// </summary>
public static class MemoryTimeDecay
{
    public const int RuleVersion = 1;

    public static int PeriodDays(MemoryCategory category) =>
        category switch
        {
            MemoryCategory.Selection or MemoryCategory.MatchPerformance => 7,
            MemoryCategory.Promise
                or MemoryCategory.Trust
                or MemoryCategory.Relationship => 14,
            _ => 28,
        };

    public static int LossPerPeriod(MemoryCategory category) =>
        category switch
        {
            MemoryCategory.Selection or MemoryCategory.MatchPerformance => 8,
            MemoryCategory.Promise
                or MemoryCategory.Trust
                or MemoryCategory.Relationship => 5,
            _ => 3,
        };

    public static int InfluenceFloor(MemoryCategory category) =>
        category switch
        {
            MemoryCategory.Career
                or MemoryCategory.ClubHistory
                or MemoryCategory.Transfer => 20,
            MemoryCategory.Promise
                or MemoryCategory.Trust
                or MemoryCategory.Relationship => 10,
            _ => MemoryRecord.MinImportance,
        };

    public static int ComputeCurrentInfluence(
        MemoryCategory category,
        int baseImportance,
        GameDate lastReinforcedOn,
        GameDate asOf,
        int reinforcementCount = 0)
    {
        if (baseImportance is < MemoryRecord.MinImportance or > MemoryRecord.MaxImportance)
        {
            throw new SocialContinuityInvariantViolationException(
                $"Base importance must be between {MemoryRecord.MinImportance} and {MemoryRecord.MaxImportance}.");
        }

        if (reinforcementCount < 0)
        {
            throw new SocialContinuityInvariantViolationException(
                "Reinforcement count cannot be negative.");
        }

        if (asOf.DayNumber < lastReinforcedOn.DayNumber)
        {
            throw new SocialContinuityInvariantViolationException(
                "Decay as-of date cannot be before last reinforcement.");
        }

        var peak = Math.Min(
            MemoryRecord.MaxImportance,
            baseImportance + (reinforcementCount * MemoryRecord.InfluenceBonusPerReinforcement));
        var days = asOf.DayNumber - lastReinforcedOn.DayNumber;
        var periods = days / PeriodDays(category);
        var lost = periods * LossPerPeriod(category);
        var floor = InfluenceFloor(category);
        return Math.Clamp(peak - lost, floor, MemoryRecord.MaxImportance);
    }
}
