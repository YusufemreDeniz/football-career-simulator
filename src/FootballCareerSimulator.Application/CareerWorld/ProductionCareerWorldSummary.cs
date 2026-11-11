namespace FootballCareerSimulator.Application.CareerWorld;

public sealed record ProductionCareerWorldSummary(
    int RootSeed,
    string CountryName,
    string LeagueName,
    int ClubCount,
    int ActivePlayerCount,
    int ContractedPlayerCount,
    int FreeAgentCount,
    int FixtureCount,
    string OpeningDateDisplay);
