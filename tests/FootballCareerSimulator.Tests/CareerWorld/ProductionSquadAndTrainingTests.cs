using FootballCareerSimulator.Application.CareerWorld;
using FootballCareerSimulator.Domain.TrainingPhysicalState;
using FootballCareerSimulator.Domain.WorldCalendar;
using FootballCareerSimulator.Simulation.TeamPreparation;
using FootballCareerSimulator.Simulation.TrainingPhysicalState;

namespace FootballCareerSimulator.Tests.CareerWorld;

public sealed class ProductionSquadAndTrainingTests
{
    private const int Seed = 741852;
    private static readonly GameDate Opening = ProductionCareerWorldConstraints.DefaultOpeningDate;

    [Fact]
    public void ProductionClub_GetsSeededNamesNotSuperLigPack()
    {
        var world = ProductionCareerWorldBootstrap.Create(Seed, Opening);
        var club = world.Clubs[0];
        var profiles = MvpSquadRosterGenerator.GeneratePlayerProfiles(
            club.Id,
            Seed,
            club.DisplayName);

        Assert.Equal(MvpSquadRosterGenerator.SquadSize, profiles.Count);
        Assert.Equal(profiles.Count, profiles.Select(player => player.DisplayName).Distinct().Count());
        Assert.All(profiles, profile =>
        {
            Assert.False(string.IsNullOrWhiteSpace(profile.DisplayName));
            Assert.Contains(' ', profile.DisplayName);
            Assert.False(string.IsNullOrWhiteSpace(profile.PositionCode));
        });
        Assert.Contains(profiles, profile => profile.PositionCode == "KL");
    }

    [Fact]
    public void ProductionWorld_HasTwentyThreeContractedPlayersPerClub()
    {
        var world = ProductionCareerWorldBootstrap.Create(Seed, Opening);
        var club = world.Clubs[1];
        var freeIds = world.FreeAgents.Select(agent => agent.PlayerId.Value).ToHashSet();
        var squad = world.Players
            .Where(player => player.OriginClubId == club.Id && !freeIds.Contains(player.Id.Value))
            .ToArray();

        Assert.Equal(ProductionCareerWorldConstraints.ContractedPlayersPerClub, squad.Length);
        Assert.All(squad, player =>
        {
            Assert.InRange(player.AgeYears(Opening), 18, 34);
            Assert.InRange(player.CurrentAbility, 40, 99);
        });
    }

    [Fact]
    public void ApplyToPlayer_TacticalReducesFatigueRelativeToFitness()
    {
        var clubId = new Domain.Shared.ClubId(3);
        var rested = PlayerPhysicalState.CreateRested(clubId, 0);
        var fitness = MvpTrainingLoadApplier.ApplyToPlayer(
            rested,
            WeeklyTrainingPlan.Set(clubId, TrainingFocus.Fitness, TrainingIntensity.Medium, RestApproach.Normal, Opening));
        var tactical = MvpTrainingLoadApplier.ApplyToPlayer(
            rested,
            WeeklyTrainingPlan.Set(clubId, TrainingFocus.Tactical, TrainingIntensity.Medium, RestApproach.Normal, Opening));

        Assert.True(tactical.Fatigue < fitness.Fatigue);
        Assert.True(tactical.Fitness < fitness.Fitness);
        Assert.True(tactical.Fitness >= rested.Fitness);
    }
}
