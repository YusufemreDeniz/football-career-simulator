using FootballCareerSimulator.Application.CareerWorld;
using FootballCareerSimulator.Application.ClubGovernance.Composition;
using FootballCareerSimulator.Application.ContractRegistration.Infrastructure;
using FootballCareerSimulator.Application.PlayerCareer.Infrastructure;
using FootballCareerSimulator.Domain.ClubGovernance;
using FootballCareerSimulator.Domain.Competition;
using FootballCareerSimulator.Domain.WorldCalendar;
using FootballCareerSimulator.Simulation.CareerWorld;

namespace FootballCareerSimulator.Tests.CareerWorld;

public sealed class ProductionCareerWorldBootstrapTests
{
    private static readonly GameDate OpeningDay = ProductionCareerWorldConstraints.DefaultOpeningDate;

    [Fact]
    public void SameSeed_ProducesIdenticalWorldIdentityAndContent()
    {
        var first = ProductionCareerWorldBootstrap.Create(42, OpeningDay);
        var second = ProductionCareerWorldBootstrap.Create(42, OpeningDay);

        Assert.Equal(first.WorldId, second.WorldId);
        Assert.Equal(Fingerprint(first), Fingerprint(second));
    }

    [Fact]
    public void DifferentSeeds_ProduceDifferentWorldIdentityAndContent()
    {
        var first = ProductionCareerWorldBootstrap.Create(42, OpeningDay);
        var second = ProductionCareerWorldBootstrap.Create(99, OpeningDay);

        Assert.NotEqual(first.WorldId, second.WorldId);
        Assert.NotEqual(Fingerprint(first), Fingerprint(second));
    }

    [Fact]
    public void Generate_CreatesTwentyClubsAndTargetPlayerPopulation()
    {
        var world = ProductionCareerWorldBootstrap.Create(741852, OpeningDay);

        Assert.Equal(ProductionCareerWorldConstraints.CountryCount, 1);
        Assert.Equal(ProductionCareerWorldConstraints.ClubCount, world.Clubs.Count);
        Assert.Equal(ProductionCareerWorldConstraints.TargetActivePlayerCount, world.Players.Count);
        Assert.Equal(ProductionCareerWorldConstraints.ContractedPlayerCount, world.ContractedPlayerCount);
        Assert.Equal(ProductionCareerWorldConstraints.FreeAgentCount, world.FreeAgents.Count);
        Assert.Equal(
            CompetitionMvpConstraints.TotalFixturesFor(ProductionCareerWorldConstraints.ClubCount),
            world.Fixtures.Count);
        Assert.Equal(ProductionCareerWorldConstraints.ClubCount, world.Managers.Count);
        Assert.Equal(ProductionCareerWorldConstraints.CountryDisplayName, world.Country.DisplayName);
        Assert.Equal(MvpLeagueIdentity.DisplayName, world.LeagueName);
        Assert.Equal(OpeningDay, world.WorldDate);
    }

    [Fact]
    public void Generate_AssignsUniquePersistentIdentities()
    {
        var world = ProductionCareerWorldBootstrap.Create(17, OpeningDay);

        Assert.Equal(world.Clubs.Count, world.Clubs.Select(club => club.Id.Value).Distinct().Count());
        Assert.Equal(world.Clubs.Count, world.Clubs.Select(club => club.DisplayName).Distinct().Count());
        Assert.Equal(world.Clubs.Count, world.Clubs.Select(club => club.Code.Value).Distinct().Count());
        Assert.Equal(world.Players.Count, world.Players.Select(player => player.Id.Value).Distinct().Count());
        Assert.Equal(world.Managers.Count, world.Managers.Select(manager => manager.ManagerId.Value).Distinct().Count());
        Assert.Equal(world.Fixtures.Count, world.Fixtures.Select(fixture => fixture.Id.Value).Distinct().Count());
        Assert.All(world.Clubs, club => Assert.IsType<Club>(club));
        Assert.All(world.Players, player => Assert.IsType<Domain.PlayerCareer.PlayerCareer>(player));
        Assert.DoesNotContain("Spike1Placeholder", world.Clubs[0].GetType().FullName);
        Assert.DoesNotContain("Spike1Placeholder", world.Players[0].GetType().FullName);
    }

    [Fact]
    public void Registry_RejectsDuplicateClubIdentities()
    {
        var first = Club.Create(new Domain.Shared.ClubId(1), "Twin City", new ClubCode("TWN"), 70);
        var duplicateName = Club.Create(new Domain.Shared.ClubId(2), "Twin City", new ClubCode("TWC"), 64);

        Assert.Throws<ClubGovernanceInvariantViolationException>(() =>
            LeagueClubRegistry.Rehydrate([first, duplicateName]));
    }

    [Fact]
    public void HydratePeople_LoadsUniquePlayersIntoProductionStores()
    {
        var world = ProductionCareerWorldBootstrap.Create(5, OpeningDay);
        var players = new InMemoryPlayerCareerStore();
        var freeAgents = new InMemoryFreeAgentStore();

        ProductionCareerWorldBootstrap.HydratePeople(world, players, freeAgents);

        Assert.Equal(world.Players.Count, players.Careers.Count);
        Assert.Equal(world.FreeAgents.Count, freeAgents.FreeAgents.Count);
        Assert.Equal(
            world.Players.Select(player => player.Id.Value).Distinct().Count(),
            players.Careers.Select(player => player.Id.Value).Distinct().Count());
    }

    [Fact]
    public void NewCareerComposition_ExposesGeneratedWorldToQueries()
    {
        var world = ProductionCareerWorldBootstrap.Create(8, OpeningDay);
        var clubs = ClubGovernanceModule.Create(world.ClubRegistry);
        var summary = ProductionCareerWorldBootstrap.ToSummary(world);

        Assert.Equal(ProductionCareerWorldConstraints.ClubCount, clubs.Queries.GetAllClubs().Count);
        Assert.Equal(world.Clubs[0].DisplayName, clubs.Queries.GetAllClubs()[0].DisplayName);
        Assert.Equal(world.RootSeed, summary.RootSeed);
        Assert.Equal(world.Country.DisplayName, summary.CountryName);
        Assert.Contains(world.WorldDate.ToDisplayDateString(), summary.OpeningDateDisplay, StringComparison.Ordinal);
        Assert.False(string.IsNullOrWhiteSpace(summary.LeagueName));
    }

    [Fact]
    public void TwentyClubSeason_CanStartAndPlanFixtures()
    {
        var world = ProductionCareerWorldBootstrap.Create(11, OpeningDay);
        var season = CompetitionSeason.Create(world.CompetitionId, new SeasonId(1), OpeningDay);
        foreach (var club in world.Clubs)
        {
            season.RegisterParticipant(club.Id);
        }

        season.StartActiveSeason(OpeningDay);
        season.PlanLeagueFixtures(OpeningDay.AddDays(31), new FixtureId(1));

        Assert.Equal(ProductionCareerWorldConstraints.ClubCount, season.Participants.Count);
        Assert.Equal(world.Fixtures.Count, season.Fixtures.Count);
    }

    private static string Fingerprint(ProductionCareerWorld world) =>
        string.Join(
            '|',
            world.WorldId,
            world.RootSeed,
            string.Join(',', world.Clubs.Select(club => $"{club.Id.Value}:{club.DisplayName}:{club.Code.Value}:{club.SportiveStrength}")),
            string.Join(',', world.Players.Select(player => $"{player.Id.Value}:{player.BirthYear}:{player.CurrentAbility}:{player.PotentialAbility}")),
            string.Join(',', world.Managers.Select(manager => $"{manager.ManagerId.Value}:{manager.DisplayName}:{manager.ClubId.Value}")));
}
