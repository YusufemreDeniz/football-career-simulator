using FootballCareerSimulator.Domain.ClubGovernance;
using FootballCareerSimulator.Domain.Competition;
using FootballCareerSimulator.Domain.ContractRegistration;
using FootballCareerSimulator.Domain.ManagerCareer;
using FootballCareerSimulator.Domain.PlayerCareer;
using FootballCareerSimulator.Domain.Shared;
using FootballCareerSimulator.Domain.TeamPreparation;
using FootballCareerSimulator.Domain.WorldCalendar;
using FootballCareerSimulator.Simulation.WorldCalendar;
using PlayerCareerAggregate = FootballCareerSimulator.Domain.PlayerCareer.PlayerCareer;

namespace FootballCareerSimulator.Simulation.CareerWorld;

/// <summary>
/// Production kariyer dünyasını seed'den üretir. Spike1Placeholder.WorldFactory içerik veya
/// kimlik modeli kullanılmaz; Club, PlayerCareer, Fixture ve ManagerId production tipleridir.
/// </summary>
public static class ProductionCareerWorldGenerator
{
    private static readonly string[] ClubNames =
    [
        "Valoria City",
        "Harbour Athletic",
        "Northgate United",
        "Riverside FC",
        "Eastmill Wanderers",
        "Crown Hill",
        "Ashford Town",
        "Silverport",
        "Redcliff Rovers",
        "Lakeside",
        "Ironbridge",
        "Westmere",
        "Oakenshaw",
        "Stormhaven",
        "Goldfield",
        "Mapleford",
        "Blackwater",
        "Highmere",
        "Southreach",
        "Kingsmead",
        "Whitecliff",
        "Dunford",
        "Cinderwell",
        "Port Calder",
        "Greenford Athletic",
        "Ravenshore",
        "Hillcrest",
        "Stoneharbor",
        "Emberlyn",
        "Fairhaven",
    ];

    private static readonly string[] ManagerFirstNames =
    [
        "Emre", "Can", "Leyla", "Baran", "Selin", "Kerem", "Deniz", "Aylin",
        "Mert", "Elif", "Arda", "Pinar", "Ozan", "Defne", "Tolga", "Ece",
        "Burak", "Naz", "Hakan", "Irem",
    ];

    private static readonly string[] ManagerLastNames =
    [
        "Yilmaz", "Demir", "Kaya", "Celik", "Aydin", "Sahin", "Arslan", "Dogan",
        "Kurt", "Polat", "Aslan", "Gunes", "Acar", "Tekin", "Bulut", "Aksoy",
        "Erdogan", "Tas", "Koc", "Ozturk",
    ];

    public static ProductionCareerWorld Generate(int rootSeed, GameDate worldDate)
    {
        var random = new SimulationRandomContext(rootSeed);
        var country = Country.Create(
            new CountryId(ProductionCareerWorldConstraints.DefaultCountryId),
            ProductionCareerWorldConstraints.CountryDisplayName,
            ProductionCareerWorldConstraints.CountryCode);
        var competitionId = new CompetitionId(MvpLeagueIdentity.DefaultCompetitionId);
        var clubs = CreateClubs(random);
        var registry = LeagueClubRegistry.Rehydrate(clubs);
        var people = CreatePlayers(clubs, worldDate, random);
        var managers = CreateManagers(clubs, random);
        var firstMatchday = worldDate.AddDays(31);
        var fixtures = LeagueFixtureGenerator.GenerateDoubleRoundRobin(
            competitionId,
            new SeasonId(1),
            clubs.Select(club => club.Id).ToArray(),
            firstMatchday,
            CompetitionMvpConstraints.DefaultDaysBetweenRounds,
            new FixtureId(1));

        var world = new ProductionCareerWorld(
            DeterministicGuidFactory.Create(rootSeed, sequence: 1),
            rootSeed,
            country,
            competitionId,
            MvpLeagueIdentity.DisplayName,
            worldDate,
            registry,
            people.Players,
            people.FreeAgents,
            fixtures,
            managers);

        Validate(world);
        return world;
    }

    private static IReadOnlyList<Club> CreateClubs(SimulationRandomContext random)
    {
        var available = ClubNames.ToList();
        var chosen = new List<string>(ProductionCareerWorldConstraints.ClubCount);
        while (chosen.Count < ProductionCareerWorldConstraints.ClubCount)
        {
            if (available.Count == 0)
            {
                throw new ProductionCareerWorldInvariantViolationException(
                    "Club name catalog is smaller than the required club count.");
            }

            var index = random.NextInt(0, available.Count);
            chosen.Add(available[index]);
            available.RemoveAt(index);
        }

        var usedCodes = new HashSet<string>(StringComparer.Ordinal);
        var clubs = new List<Club>(chosen.Count);
        for (var i = 0; i < chosen.Count; i++)
        {
            var strength = 52 + random.NextInt(0, 44);
            clubs.Add(Club.Create(
                new ClubId(i + 1),
                chosen[i],
                new ClubCode(AllocateClubCode(chosen[i], usedCodes, random)),
                strength));
        }

        return clubs;
    }

    private static string AllocateClubCode(
        string displayName,
        HashSet<string> usedCodes,
        SimulationRandomContext random)
    {
        var letters = new string(displayName.Where(char.IsLetter).ToArray());
        var stem = letters.Length >= 3
            ? letters[..3]
            : (letters + "XXX")[..3];
        stem = stem.ToUpperInvariant();

        if (usedCodes.Add(stem))
        {
            return stem;
        }

        for (var attempt = 0; attempt < 80; attempt++)
        {
            var candidate = $"{stem.AsSpan(0, 2)}{(char)('A' + random.NextInt(0, 26))}";
            if (usedCodes.Add(candidate))
            {
                return candidate;
            }
        }

        throw new ProductionCareerWorldInvariantViolationException(
            $"Unable to allocate a unique club code for '{displayName}'.");
    }

    private static (IReadOnlyList<PlayerCareerAggregate> Players, IReadOnlyList<PlayerFreeAgency> FreeAgents)
        CreatePlayers(
            IReadOnlyList<Club> clubs,
            GameDate worldDate,
            SimulationRandomContext random)
    {
        var players = new List<PlayerCareerAggregate>(ProductionCareerWorldConstraints.TargetActivePlayerCount);
        var freeAgents = new List<PlayerFreeAgency>(ProductionCareerWorldConstraints.FreeAgentCount);
        var usedIds = new HashSet<long>();

        foreach (var club in clubs)
        {
            for (var slot = MatchSelection.MinSquadSlot; slot <= MatchSelection.MaxSquadSlot; slot++)
            {
                var currentAbility = Math.Clamp(
                    club.SportiveStrength - 18 + random.NextInt(0, 21),
                    PlayerCareerAggregate.MinAbility,
                    PlayerCareerAggregate.MaxAbility);
                var potentialAbility = Math.Min(
                    PlayerCareerAggregate.MaxAbility,
                    currentAbility + random.NextInt(5, 19));
                var age = random.NextInt(18, 35);
                var player = PlayerCareerAggregate.CreateForSlot(
                    club.Id,
                    slot,
                    currentAbility,
                    potentialAbility,
                    worldDate.Year - age);

                if (!usedIds.Add(player.Id.Value))
                {
                    throw new ProductionCareerWorldInvariantViolationException(
                        $"Duplicate player id {player.Id.Value}.");
                }

                players.Add(player);

                if (slot >= ProductionCareerWorldConstraints.ContractedPlayersPerClub)
                {
                    freeAgents.Add(PlayerFreeAgency.Release(player.Id, club.Id, worldDate));
                }
            }
        }

        return (players, freeAgents);
    }

    private static IReadOnlyList<ProductionWorldManagerIdentity> CreateManagers(
        IReadOnlyList<Club> clubs,
        SimulationRandomContext random)
    {
        var usedNames = new HashSet<string>(StringComparer.Ordinal);
        var managers = new List<ProductionWorldManagerIdentity>(clubs.Count);
        foreach (var club in clubs)
        {
            string name;
            var guard = 0;
            do
            {
                var first = ManagerFirstNames[random.NextInt(0, ManagerFirstNames.Length)];
                var last = ManagerLastNames[random.NextInt(0, ManagerLastNames.Length)];
                name = $"{first} {last}";
                guard++;
                if (guard > 200)
                {
                    throw new ProductionCareerWorldInvariantViolationException(
                        "Unable to allocate unique manager names.");
                }
            }
            while (!usedNames.Add(name));

            managers.Add(new ProductionWorldManagerIdentity(
                new ManagerId(100 + club.Id.Value),
                name,
                club.Id));
        }

        return managers;
    }

    private static void Validate(ProductionCareerWorld world)
    {
        if (world.Clubs.Count != ProductionCareerWorldConstraints.ClubCount)
        {
            throw new ProductionCareerWorldInvariantViolationException(
                $"Expected {ProductionCareerWorldConstraints.ClubCount} clubs, found {world.Clubs.Count}.");
        }

        if (world.Players.Count != ProductionCareerWorldConstraints.TargetActivePlayerCount)
        {
            throw new ProductionCareerWorldInvariantViolationException(
                $"Expected {ProductionCareerWorldConstraints.TargetActivePlayerCount} players, found {world.Players.Count}.");
        }

        if (world.FreeAgents.Count != ProductionCareerWorldConstraints.FreeAgentCount)
        {
            throw new ProductionCareerWorldInvariantViolationException(
                $"Expected {ProductionCareerWorldConstraints.FreeAgentCount} free agents, found {world.FreeAgents.Count}.");
        }

        if (world.Managers.Count != world.Clubs.Count)
        {
            throw new ProductionCareerWorldInvariantViolationException(
                "Every club must have exactly one starting manager identity.");
        }

        if (world.Players.Select(player => player.Id.Value).Distinct().Count() != world.Players.Count)
        {
            throw new ProductionCareerWorldInvariantViolationException("Duplicate player identities are not allowed.");
        }

        if (world.Managers.Select(manager => manager.ManagerId.Value).Distinct().Count() != world.Managers.Count)
        {
            throw new ProductionCareerWorldInvariantViolationException("Duplicate manager identities are not allowed.");
        }

        if (world.Managers.Select(manager => manager.ClubId.Value).Distinct().Count() != world.Managers.Count)
        {
            throw new ProductionCareerWorldInvariantViolationException(
                "A club cannot have two starting manager identities.");
        }

        var expectedFixtures = CompetitionMvpConstraints.TotalFixturesFor(world.Clubs.Count);
        if (world.Fixtures.Count != expectedFixtures)
        {
            throw new ProductionCareerWorldInvariantViolationException(
                $"Expected {expectedFixtures} fixtures, found {world.Fixtures.Count}.");
        }
    }
}
