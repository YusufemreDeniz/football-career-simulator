namespace FootballCareerSimulator.Domain.Transfer;

/// <summary>
/// Transfer Process durumları (player negotiation / financial / completion henüz yok).
/// </summary>
public enum TransferProcessStatus
{
    UnderEvaluation = 1,
    Withdrawn = 2,
    Failed = 3,
    Archived = 4,
    SportingApprovalPending = 5,
    SportingApproved = 6,
    Rejected = 7,
    ClubNegotiation = 8,
    ClubAgreementReached = 9,
}
