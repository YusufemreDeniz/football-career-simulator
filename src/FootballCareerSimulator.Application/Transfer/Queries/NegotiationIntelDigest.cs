using FootballCareerSimulator.Domain.Shared;

namespace FootballCareerSimulator.Application.Transfer.Queries;

/// <summary>
/// Transfer görüşmesi sırasında menajere rakip kulübün müzakere hafızasını hissettiren önsezi özeti (Yenilik 9: Pazarlık Belleği).
/// </summary>
public sealed record NegotiationIntelDigest(
    ClubId ObservingClubId,
    long ManagerId,
    string MarketReputationSummary,
    IReadOnlyList<string> StyleIndicators,
    int OpeningBidAggressionScore,
    int DeadlineDesperationScore,
    int WalkawayTendencyScore,
    int PersistenceIndexScore);
