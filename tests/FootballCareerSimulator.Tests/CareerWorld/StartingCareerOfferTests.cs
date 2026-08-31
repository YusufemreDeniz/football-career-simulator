using FootballCareerSimulator.Application.CareerWorld;
using FootballCareerSimulator.Application.ClubGovernance.Composition;
using FootballCareerSimulator.Application.ManagerCareer.Composition;
using FootballCareerSimulator.Application.WorldCalendar.Composition;
using FootballCareerSimulator.Domain.ManagerCareer;
using FootballCareerSimulator.Domain.Shared;
using FootballCareerSimulator.Domain.WorldCalendar;
using FootballCareerSimulator.Simulation.CareerWorld;

namespace FootballCareerSimulator.Tests.CareerWorld;

public sealed class StartingCareerOfferTests
{
    private const int Seed = 741852;
    private static readonly GameDate OpeningDay = ProductionCareerWorldConstraints.DefaultOpeningDate;

    [Fact]
    public void Preview_GuaranteesAtLeastOneLimitedUniqueOffer()
    {
        foreach (var background in StartingBackgroundCatalog.All)
        {
            var offers = StartingCareerOfferService.Preview(Seed, background, OpeningDay);

            Assert.InRange(offers.Count, 1, StartingBackgroundCatalog.OfferCount);
            Assert.True(offers.Count < ProductionCareerWorldConstraints.ClubCount);
            Assert.Equal(offers.Count, offers.Select(offer => offer.ClubId).Distinct().Count());
            Assert.Equal(offers.Count, offers.Select(offer => offer.OfferId).Distinct().Count());
            Assert.All(offers, offer =>
            {
                Assert.InRange(offer.ClubId, 1, ProductionCareerWorldConstraints.ClubCount);
                Assert.True(offer.SquadSize > 0);
                Assert.True(offer.AverageAge > 0);
                Assert.False(string.IsNullOrWhiteSpace(offer.LeagueLevelSummary));
                Assert.False(string.IsNullOrWhiteSpace(offer.BoardExpectation));
            });
        }
    }

    [Fact]
    public void Preview_DoesNotExposeEveryClub()
    {
        var world = ProductionCareerWorldBootstrap.Create(Seed, OpeningDay);
        var offered = StartingBackgroundCatalog.All
            .SelectMany(background => StartingCareerOfferService.Preview(world, background))
            .Select(offer => offer.ClubId)
            .ToHashSet();

        Assert.True(offered.Count < world.Clubs.Count);
    }

    [Fact]
    public void SameSeedAndBackground_ProducesIdenticalOffers()
    {
        var first = StartingCareerOfferService.Preview(Seed, StartingBackground.AmateurHeadCoach, OpeningDay);
        var second = StartingCareerOfferService.Preview(Seed, StartingBackground.AmateurHeadCoach, OpeningDay);

        Assert.Equal(
            first.Select(offer => (offer.OfferId, offer.ClubId, offer.SportiveStrength)),
            second.Select(offer => (offer.OfferId, offer.ClubId, offer.SportiveStrength)));
    }

    [Fact]
    public void DifferentBackgrounds_ChangeOfferRange()
    {
        var amateur = StartingCareerOfferService.Preview(Seed, StartingBackground.AmateurHeadCoach, OpeningDay);
        var assistant = StartingCareerOfferService.Preview(Seed, StartingBackground.AssistantCoach, OpeningDay);

        Assert.NotEqual(
            amateur.Select(offer => offer.ClubId).OrderBy(id => id),
            assistant.Select(offer => offer.ClubId).OrderBy(id => id));
        Assert.True(amateur.Average(offer => offer.SportiveStrength) < assistant.Average(offer => offer.SportiveStrength));
    }

    [Fact]
    public void ActivateAcceptedOffer_CreatesRealClubEmploymentViaJobOffer()
    {
        var offers = StartingCareerOfferService.Preview(Seed, StartingBackground.TacticalSpecialist, OpeningDay);
        var chosen = Assert.Single(offers.Take(1));
        var world = ProductionCareerWorldBootstrap.Create(Seed, OpeningDay);
        var club = world.ClubRegistry.GetClubOrThrow(new ClubId(chosen.ClubId));

        var career = StartingCareerOfferService.ActivateAcceptedOffer(
            new ManagerId(1),
            "Yusuf Deniz",
            club.Id,
            StartingBackground.TacticalSpecialist,
            OpeningDay,
            club.SportiveStrength);

        Assert.True(career.IsEmployed);
        Assert.Equal(ManagerEmploymentStatus.Employed, career.EmploymentStatus);
        Assert.NotNull(career.ActiveEmployment);
        Assert.Equal(club.Id, career.ActiveEmployment!.ClubId);
        Assert.Equal(OpeningDay, career.ActiveEmployment.StartedAt);
        Assert.Equal(SeasonExpectation.FromSportiveStrength(club.SportiveStrength), career.ActiveEmployment.SeasonExpectation);
        Assert.Equal(
            StartingBackgroundCatalog.InitialBoardConfidence(StartingBackground.TacticalSpecialist),
            career.ActiveEmployment.BoardConfidence.Value);
        Assert.Equal(StartingBackground.TacticalSpecialist, career.StartingBackground);
        Assert.Equal(
            StartingBackgroundCatalog.InitialReputation(StartingBackground.TacticalSpecialist),
            career.Reputation.Value);
        Assert.Null(career.PendingJobOffer);
        Assert.Empty(career.EmploymentHistory);
    }

    [Fact]
    public void ActivateAcceptedOffer_EnforcesOneActiveEmploymentPerManager()
    {
        var offers = StartingCareerOfferService.Preview(Seed, StartingBackground.YouthAcademyCoach, OpeningDay);
        var first = offers[0];
        var world = ProductionCareerWorldBootstrap.Create(Seed, OpeningDay);
        var club = world.ClubRegistry.GetClubOrThrow(new ClubId(first.ClubId));
        var career = StartingCareerOfferService.ActivateAcceptedOffer(
            new ManagerId(1),
            "TD",
            club.Id,
            StartingBackground.YouthAcademyCoach,
            OpeningDay,
            club.SportiveStrength);

        Assert.Throws<ManagerCareerInvariantViolationException>(() =>
            career.ReceiveJobOffer(JobOffer.CreateOffered(new JobOfferId(99), new ClubId(first.ClubId == 1 ? 2 : 1), OpeningDay)));
        Assert.Throws<ManagerCareerInvariantViolationException>(() =>
            career.AcceptPendingJobOffer(OpeningDay, SeasonExpectationTier.MidTable));
    }

    [Fact]
    public void CreateFromAcceptedStartingOffer_RejectsClubOutsideOfferSet()
    {
        var world = WorldCalendarModule.Create(OpeningDay, rootSeed: Seed);
        var clubs = ClubGovernanceModule.Create(ProductionCareerWorldBootstrap.Create(Seed, OpeningDay).ClubRegistry);
        var offers = StartingCareerOfferService.Preview(Seed, StartingBackground.AmateurHeadCoach, OpeningDay);
        var outsider = Enumerable.Range(1, ProductionCareerWorldConstraints.ClubCount)
            .Select(id => (long)id)
            .First(id => offers.All(offer => offer.ClubId != id));

        Assert.Throws<ManagerCareerInvariantViolationException>(() =>
            ManagerCareerModule.CreateFromAcceptedStartingOffer(
                OpeningDay,
                clubs.Store,
                world.TimelineStore,
                StartingBackground.AmateurHeadCoach,
                Seed,
                outsider));
    }

    [Fact]
    public void CreateFromAcceptedStartingOffer_AssignsSingleActiveManagerToClub()
    {
        var production = ProductionCareerWorldBootstrap.Create(Seed, OpeningDay);
        var world = WorldCalendarModule.Create(OpeningDay, rootSeed: Seed);
        var clubs = ClubGovernanceModule.Create(production.ClubRegistry);
        var offers = StartingCareerOfferService.Preview(Seed, StartingBackground.RecentlyRetiredPlayer, OpeningDay);
        var chosen = offers[0];

        var module = ManagerCareerModule.CreateFromAcceptedStartingOffer(
            OpeningDay,
            clubs.Store,
            world.TimelineStore,
            StartingBackground.RecentlyRetiredPlayer,
            Seed,
            chosen.ClubId,
            displayName: "Yusuf Deniz",
            clubSportiveStrength: chosen.SportiveStrength);

        var career = module.Queries.GetCareer();
        Assert.Equal("Yusuf Deniz", career.DisplayName);
        Assert.Equal("Employed", career.EmploymentStatus);
        Assert.Equal(chosen.ClubId, career.EmployedClubId);
        Assert.Equal(StartingBackground.RecentlyRetiredPlayer.ToString(), career.StartingBackground);
        Assert.Equal(1, production.Managers.Count(manager => manager.ClubId.Value == chosen.ClubId));
        Assert.Equal(production.Clubs.Count, production.Managers.Select(manager => manager.ClubId.Value).Distinct().Count());
    }

    [Fact]
    public void Rehydrate_RecoversStartingBackgroundFromReasonCode()
    {
        var career = ManagerCareer.CreateAwaitingInitialEmployment(
            new ManagerId(1),
            "TD",
            StartingBackground.LowerLeagueYouthManager);

        var loaded = ManagerCareer.Rehydrate(
            career.ManagerId,
            career.DisplayName,
            activeEmployment: null,
            ManagerEmploymentStatus.Unemployed,
            terminationReason: null,
            lastClubId: null,
            dismissedDueToFixtureId: null,
            dismissedAt: null,
            lastReputationReasonCode: career.LastReputationReasonCode);

        Assert.Equal(StartingBackground.LowerLeagueYouthManager, loaded.StartingBackground);
    }

    [Fact]
    public void Generate_UsesSeededRandomNotGlobalRng()
    {
        var world = ProductionCareerWorldGenerator.Generate(Seed, OpeningDay);
        var first = StartingJobOfferGenerator.Generate(world, StartingBackground.AssistantCoach);
        var second = StartingJobOfferGenerator.Generate(world, StartingBackground.AssistantCoach);

        Assert.Equal(
            first.Select(offer => (offer.OfferId.Value, offer.ClubId.Value)),
            second.Select(offer => (offer.OfferId.Value, offer.ClubId.Value)));
    }
}
