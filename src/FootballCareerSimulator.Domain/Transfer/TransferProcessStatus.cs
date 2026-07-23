namespace FootballCareerSimulator.Domain.Transfer;

/// <summary>
/// Transfer Process iskelet durumları (müzakere / approval / completion yok).
/// </summary>
public enum TransferProcessStatus
{
    UnderEvaluation = 1,
    Withdrawn = 2,
    Failed = 3,
    Archived = 4,
}
