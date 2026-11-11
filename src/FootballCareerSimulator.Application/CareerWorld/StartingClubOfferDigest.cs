namespace FootballCareerSimulator.Application.CareerWorld;

public sealed record StartingClubOfferDigest(
    long OfferId,
    long ClubId,
    string DisplayName,
    string Code,
    int SportiveStrength,
    string LeagueLevelSummary,
    string BoardExpectation,
    int TransferBudget,
    int SquadSize,
    int AverageAge,
    string SquadSummary,
    string WhyOffered,
    string MediaInterest,
    string ProfileSignal);
