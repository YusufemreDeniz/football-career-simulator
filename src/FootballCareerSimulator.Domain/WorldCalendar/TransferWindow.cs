namespace FootballCareerSimulator.Domain.WorldCalendar;

/// <summary>
/// Transfer penceresi (World &amp; Calendar authority). Yaz/kış otomatik türetimi yok.
/// </summary>
public sealed class TransferWindow
{
    private TransferWindow(
        TransferWindowStatus status,
        GameDate? openedOn,
        GameDate? closesOn)
    {
        Status = status;
        OpenedOn = openedOn;
        ClosesOn = closesOn;
    }

    public TransferWindowStatus Status { get; }

    public GameDate? OpenedOn { get; }

    public GameDate? ClosesOn { get; }

    public bool IsOpen => Status == TransferWindowStatus.Open;

    public static TransferWindow Closed() =>
        new(TransferWindowStatus.Closed, openedOn: null, closesOn: null);

    public static TransferWindow Open(GameDate openedOn, GameDate? closesOn = null)
    {
        if (closesOn is { } end && end.IsBefore(openedOn))
        {
            throw new WorldCalendarInvariantViolationException(
                "Transfer window close date cannot be before open date.");
        }

        return new TransferWindow(TransferWindowStatus.Open, openedOn, closesOn);
    }

    public static TransferWindow Rehydrate(
        TransferWindowStatus status,
        GameDate? openedOn,
        GameDate? closesOn)
    {
        if (!Enum.IsDefined(status))
        {
            throw new WorldCalendarInvariantViolationException(
                $"Unknown transfer window status: {status}.");
        }

        return status switch
        {
            TransferWindowStatus.Closed => Closed(),
            TransferWindowStatus.Open => Open(
                openedOn ?? throw new WorldCalendarInvariantViolationException(
                    "Open transfer window requires OpenedOn."),
                closesOn),
            _ => throw new WorldCalendarInvariantViolationException(
                $"Unknown transfer window status: {status}."),
        };
    }
}
