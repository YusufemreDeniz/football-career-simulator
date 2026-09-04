namespace FootballCareerSimulator.Application.CareerWorld;

using FootballCareerSimulator.Application.ContractRegistration.Ports;
using FootballCareerSimulator.Application.PlayerCareer.Ports;
using FootballCareerSimulator.Domain.WorldCalendar;
using FootballCareerSimulator.Simulation.CareerWorld;

public static class ProductionCareerWorldBootstrap
{
    public static ProductionCareerWorld Create(int rootSeed, GameDate? startingDate = null) =>
        ProductionCareerWorldGenerator.Generate(
            rootSeed,
            startingDate ?? ProductionCareerWorldConstraints.DefaultOpeningDate);

    public static ProductionCareerWorldSummary ToSummary(ProductionCareerWorld world)
    {
        ArgumentNullException.ThrowIfNull(world);
        return new ProductionCareerWorldSummary(
            world.RootSeed,
            world.Country.DisplayName,
            world.LeagueName,
            world.Clubs.Count,
            world.Players.Count,
            world.ContractedPlayerCount,
            world.FreeAgents.Count,
            world.Fixtures.Count,
            world.WorldDate.ToDisplayDateString());
    }

    public static void HydratePeople(
        ProductionCareerWorld world,
        IPlayerCareerStore playerStore,
        IFreeAgentStore freeAgentStore)
    {
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(playerStore);
        ArgumentNullException.ThrowIfNull(freeAgentStore);

        playerStore.ReplaceAll(world.Players);
        freeAgentStore.ReplaceAll(world.FreeAgents);
    }
}
