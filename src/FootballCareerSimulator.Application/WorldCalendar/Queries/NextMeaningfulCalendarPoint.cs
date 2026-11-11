namespace FootballCareerSimulator.Application.WorldCalendar.Queries;

/// <summary>
/// Kariyer hub'ının "Devam" hedefi: boş günleri tek tek oynatmadan bir sonraki anlamlı takvim noktası.
/// </summary>
public sealed record NextMeaningfulCalendarPoint(
    int CurrentDayNumber,
    int TargetDayNumber,
    string ReasonCode,
    string PlayerFacingReason,
    bool AlreadyAtPoint)
{
    public int DaysToAdvance => Math.Max(0, TargetDayNumber - CurrentDayNumber);
}

public static class NextMeaningfulCalendarPointResolver
{
    public const int MaxLookaheadDays = 60;
    public const string ReasonAlreadyBlocked = "AlreadyBlocked";
    public const string ReasonPendingDecision = "PendingDecision";
    public const string ReasonDueFixture = "DueFixture";
    public const string ReasonUpcomingFixture = "UpcomingFixture";
    public const string ReasonTransferWindow = "TransferWindow";
    public const string ReasonCalmStep = "CalmStep";

    public static NextMeaningfulCalendarPoint Resolve(
        int currentDayNumber,
        bool hasHardBlocker,
        bool hasPendingDecision,
        IReadOnlyList<int> plannedFixtureDayNumbers,
        IReadOnlyList<int> transferWindowBoundaryDayNumbers)
    {
        ArgumentNullException.ThrowIfNull(plannedFixtureDayNumbers);
        ArgumentNullException.ThrowIfNull(transferWindowBoundaryDayNumbers);

        if (hasHardBlocker)
        {
            return new(
                currentDayNumber,
                currentDayNumber,
                ReasonAlreadyBlocked,
                "Önce bugünkü engeli çöz; takvim orada duruyor.",
                AlreadyAtPoint: true);
        }

        if (hasPendingDecision)
        {
            return new(
                currentDayNumber,
                currentDayNumber,
                ReasonPendingDecision,
                "Masada zorunlu bir karar var; atlanamaz.",
                AlreadyAtPoint: true);
        }

        var dueToday = plannedFixtureDayNumbers.Any(day => day <= currentDayNumber);
        if (dueToday)
        {
            return new(
                currentDayNumber,
                currentDayNumber,
                ReasonDueFixture,
                "Vadesi gelmiş maç var; önce Maç Gününe Git.",
                AlreadyAtPoint: true);
        }

        var horizon = currentDayNumber + MaxLookaheadDays;
        var nextFixture = plannedFixtureDayNumbers
            .Where(day => day > currentDayNumber && day <= horizon)
            .OrderBy(day => day)
            .Cast<int?>()
            .FirstOrDefault();
        var nextWindow = transferWindowBoundaryDayNumbers
            .Where(day => day > currentDayNumber && day <= horizon)
            .OrderBy(day => day)
            .Cast<int?>()
            .FirstOrDefault();

        if (nextFixture is int fixtureDay && (nextWindow is not int windowDay || fixtureDay <= windowDay))
        {
            return new(
                currentDayNumber,
                fixtureDay,
                ReasonUpcomingFixture,
                "Sıradaki maç gününe ilerleniyor.",
                AlreadyAtPoint: false);
        }

        if (nextWindow is int windowOnly)
        {
            return new(
                currentDayNumber,
                windowOnly,
                ReasonTransferWindow,
                "Transfer penceresi değişimine ilerleniyor.",
                AlreadyAtPoint: false);
        }

        return new(
            currentDayNumber,
            currentDayNumber + 1,
            ReasonCalmStep,
            "Yakında maç yok; bir gün ilerleniyor.",
            AlreadyAtPoint: false);
    }
}
