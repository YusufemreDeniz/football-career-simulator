namespace FootballCareerSimulator.Application.CareerWorld;

using FootballCareerSimulator.Domain.ManagerCareer;
using FootballCareerSimulator.Domain.Shared;
using FootballCareerSimulator.Domain.WorldCalendar;
using FootballCareerSimulator.Simulation.CareerWorld;

public static class StartingCareerOfferService
{
    public static IReadOnlyList<StartingClubOfferDigest> Preview(
        int rootSeed,
        StartingBackground background,
        GameDate? startingDate = null) =>
        Preview(ProductionCareerWorldBootstrap.Create(rootSeed, startingDate), background);

    public static IReadOnlyList<StartingClubOfferDigest> Preview(
        ProductionCareerWorld world,
        StartingBackground background)
    {
        ArgumentNullException.ThrowIfNull(world);
        var offers = StartingJobOfferGenerator.Generate(world, background);
        return offers.Select(offer => ToDigest(world, offer, background)).ToArray();
    }

    public static bool ContainsClub(IReadOnlyList<StartingClubOfferDigest> offers, long clubId)
    {
        ArgumentNullException.ThrowIfNull(offers);
        return offers.Any(offer => offer.ClubId == clubId);
    }

    public static ManagerCareer ActivateAcceptedOffer(
        ManagerId managerId,
        string displayName,
        ClubId acceptedClubId,
        StartingBackground background,
        GameDate startedAt,
        int clubSportiveStrength)
    {
        var awaiting = ManagerCareer.CreateAwaitingInitialEmployment(managerId, displayName, background);
        var offer = JobOffer.CreateOffered(
            StartingJobOfferGenerator.OfferIdFor(acceptedClubId),
            acceptedClubId,
            startedAt);
        var received = awaiting.ReceiveJobOffer(offer);
        var accepted = received.Career.AcceptPendingJobOffer(
            startedAt,
            SeasonExpectation.FromSportiveStrength(clubSportiveStrength),
            StartingBackgroundCatalog.InitialBoardConfidence(background));
        return accepted.Career;
    }

    private static StartingClubOfferDigest ToDigest(
        ProductionCareerWorld world,
        StartingJobOffer offer,
        StartingBackground background)
    {
        var club = world.ClubRegistry.GetClubOrThrow(offer.ClubId);
        var freeAgentIds = world.FreeAgents.Select(agent => agent.PlayerId.Value).ToHashSet();
        var squad = world.Players
            .Where(player =>
                player.OriginClubId == club.Id
                && !player.IsRetired
                && !freeAgentIds.Contains(player.Id.Value))
            .ToArray();
        var averageAge = squad.Length == 0
            ? 0
            : (int)Math.Round(squad.Average(player => player.AgeYears(world.WorldDate)));
        var expectation = SeasonExpectation.FromSportiveStrength(club.SportiveStrength);
        var leagueLevel = LeagueLevelLabel(expectation);
        return new StartingClubOfferDigest(
            offer.OfferId.Value,
            club.Id.Value,
            club.DisplayName,
            club.Code.Value,
            club.SportiveStrength,
            leagueLevel,
            $"{leagueLevel} · yönetim güveni {StartingBackgroundCatalog.InitialBoardConfidence(background)}",
            club.AvailableTransferFunds,
            squad.Length,
            averageAge,
            $"{squad.Length} kadrolu oyuncu, yaş ort. {averageAge}",
            StartingBackgroundCatalog.OfferFit(background),
            StartingBackgroundCatalog.MediaInterest(background),
            StartingBackgroundCatalog.ProfileSignal(background));
    }

    private static string LeagueLevelLabel(SeasonExpectationTier expectation) =>
        expectation switch
        {
            SeasonExpectationTier.TitleChallenge => "Şampiyonluk yarışı",
            SeasonExpectationTier.UpperHalf => "Üst yarı adayı",
            SeasonExpectationTier.MidTable => "Orta sıra",
            SeasonExpectationTier.LowerHalf => "Alt yarı",
            SeasonExpectationTier.Survival => "Kümede kalma mücadelesi",
            _ => "Belirsiz seviye",
        };
}
