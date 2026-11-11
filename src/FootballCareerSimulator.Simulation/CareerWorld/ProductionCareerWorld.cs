using FootballCareerSimulator.Domain.ClubGovernance;
using FootballCareerSimulator.Domain.Competition;
using FootballCareerSimulator.Domain.ContractRegistration;
using FootballCareerSimulator.Domain.ManagerCareer;
using FootballCareerSimulator.Domain.PlayerCareer;
using FootballCareerSimulator.Domain.Shared;
using FootballCareerSimulator.Domain.WorldCalendar;
using PlayerCareerAggregate = FootballCareerSimulator.Domain.PlayerCareer.PlayerCareer;

namespace FootballCareerSimulator.Simulation.CareerWorld;

/// <summary>
/// Production kariyer dünyasının bootstrap sonucu. Aggregate değildir; Simulation üretim çıktısıdır.
/// Spike1Placeholder World modelini temsil etmez.
/// </summary>
public sealed class ProductionCareerWorld
{
    public ProductionCareerWorld(
        Guid worldId,
        int rootSeed,
        Country country,
        CompetitionId competitionId,
        string leagueName,
        GameDate worldDate,
        LeagueClubRegistry clubRegistry,
        IReadOnlyList<PlayerCareerAggregate> players,
        IReadOnlyList<PlayerFreeAgency> freeAgents,
        IReadOnlyList<Fixture> fixtures,
        IReadOnlyList<ProductionWorldManagerIdentity> managers)
    {
        if (worldId == Guid.Empty)
        {
            throw new ProductionCareerWorldInvariantViolationException("World identity cannot be empty.");
        }

        WorldId = worldId;
        RootSeed = rootSeed;
        Country = country ?? throw new ArgumentNullException(nameof(country));
        CompetitionId = competitionId;
        LeagueName = string.IsNullOrWhiteSpace(leagueName)
            ? throw new ProductionCareerWorldInvariantViolationException("League name cannot be empty.")
            : leagueName.Trim();
        WorldDate = worldDate;
        ClubRegistry = clubRegistry ?? throw new ArgumentNullException(nameof(clubRegistry));
        Players = players ?? throw new ArgumentNullException(nameof(players));
        FreeAgents = freeAgents ?? throw new ArgumentNullException(nameof(freeAgents));
        Fixtures = fixtures ?? throw new ArgumentNullException(nameof(fixtures));
        Managers = managers ?? throw new ArgumentNullException(nameof(managers));
    }

    public Guid WorldId { get; }

    public int RootSeed { get; }

    public Country Country { get; }

    public CompetitionId CompetitionId { get; }

    public string LeagueName { get; }

    public GameDate WorldDate { get; }

    public LeagueClubRegistry ClubRegistry { get; }

    public IReadOnlyList<Club> Clubs => ClubRegistry.Clubs;

    public IReadOnlyList<PlayerCareerAggregate> Players { get; }

    public IReadOnlyList<PlayerFreeAgency> FreeAgents { get; }

    public IReadOnlyList<Fixture> Fixtures { get; }

    public IReadOnlyList<ProductionWorldManagerIdentity> Managers { get; }

    public int ContractedPlayerCount => Players.Count - FreeAgents.Count;
}

public sealed record ProductionWorldManagerIdentity(
    ManagerId ManagerId,
    string DisplayName,
    ClubId ClubId);
