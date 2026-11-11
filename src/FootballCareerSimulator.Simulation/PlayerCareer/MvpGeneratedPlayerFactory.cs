using FootballCareerSimulator.Domain.PlayerCareer;
using FootballCareerSimulator.Domain.Shared;
using FootballCareerSimulator.Domain.WorldCalendar;

namespace FootballCareerSimulator.Simulation.PlayerCareer;

public static class MvpGeneratedPlayerFactory
{
    public static Domain.PlayerCareer.PlayerCareer CreateSuccessor(
        ClubId clubId,
        int slotIndex,
        int generation,
        GameDate day,
        int rootSeed)
    {
        var seed = unchecked(
            (rootSeed * 397)
            ^ ((int)clubId.Value * 7919)
            ^ (slotIndex * 101)
            ^ (generation * 104729));
        var random = new SimulationRandomContext(seed);
        var currentAbility = random.NextInt(48, 64);
        var potentialAbility = Math.Min(
            Domain.PlayerCareer.PlayerCareer.MaxAbility,
            currentAbility + random.NextInt(8, 22));
        var age = random.NextInt(17, 21);

        return Domain.PlayerCareer.PlayerCareer.CreateGeneratedForSlot(
            clubId,
            slotIndex,
            currentAbility,
            potentialAbility,
            day.Year - age,
            generation);
    }
}
