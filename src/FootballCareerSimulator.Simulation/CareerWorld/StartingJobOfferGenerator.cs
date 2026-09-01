using FootballCareerSimulator.Domain.ClubGovernance;
using FootballCareerSimulator.Domain.ManagerCareer;
using FootballCareerSimulator.Domain.Shared;
using FootballCareerSimulator.Domain.WorldCalendar;

namespace FootballCareerSimulator.Simulation.CareerWorld;

public sealed record StartingJobOffer(JobOfferId OfferId, ClubId ClubId, int SportiveStrength);

/// <summary>
/// Starting Background + dünya state'inden sınırlı, deterministik ilk Job Offer seti üretir.
/// Boş set geçersizdir; ligin tamamı asla dönmez.
/// </summary>
public static class StartingJobOfferGenerator
{
    public const long OfferIdOffset = 10_000;

    public static JobOfferId OfferIdFor(ClubId clubId) =>
        new(OfferIdOffset + clubId.Value);

    public static IReadOnlyList<StartingJobOffer> Generate(
        ProductionCareerWorld world,
        StartingBackground background)
    {
        ArgumentNullException.ThrowIfNull(world);

        var (minStrength, maxStrength) = StartingBackgroundCatalog.ClubStrengthBand(background);
        var clubs = world.Clubs;
        if (clubs.Count == 0)
        {
            throw new ProductionCareerWorldInvariantViolationException(
                "A starting job offer set cannot be produced without clubs.");
        }

        var inBand = clubs
            .Where(club => club.SportiveStrength >= minStrength && club.SportiveStrength <= maxStrength)
            .OrderBy(club => club.Id.Value)
            .ToList();

        if (inBand.Count == 0)
        {
            var midpoint = (minStrength + maxStrength) / 2;
            inBand = clubs
                .OrderBy(club => Math.Abs(club.SportiveStrength - midpoint))
                .ThenBy(club => club.Id.Value)
                .Take(1)
                .ToList();
        }

        var random = new SimulationRandomContext(MixSeed(world.RootSeed, (int)background));
        Shuffle(inBand, random);

        var take = Math.Min(StartingBackgroundCatalog.OfferCount, inBand.Count);
        var selected = inBand.Take(take).OrderBy(club => club.Id.Value).ToArray();
        if (selected.Length == 0)
        {
            throw new ProductionCareerWorldInvariantViolationException(
                "Starting job offer generation must guarantee at least one offer.");
        }

        if (selected.Length >= clubs.Count && clubs.Count > StartingBackgroundCatalog.OfferCount)
        {
            throw new ProductionCareerWorldInvariantViolationException(
                "Starting job offers cannot expose the entire league.");
        }

        return selected
            .Select(club => new StartingJobOffer(OfferIdFor(club.Id), club.Id, club.SportiveStrength))
            .ToArray();
    }

    private static int MixSeed(int rootSeed, int background)
    {
        unchecked
        {
            return (rootSeed * 397) ^ (background * 1_000_003) ^ unchecked((int)0x5EED0FFE);
        }
    }

    private static void Shuffle(List<Club> clubs, SimulationRandomContext random)
    {
        for (var index = clubs.Count - 1; index > 0; index--)
        {
            var swap = random.NextInt(0, index + 1);
            (clubs[index], clubs[swap]) = (clubs[swap], clubs[index]);
        }
    }
}
