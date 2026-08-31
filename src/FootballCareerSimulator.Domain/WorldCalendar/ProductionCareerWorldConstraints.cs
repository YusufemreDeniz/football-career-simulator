namespace FootballCareerSimulator.Domain.WorldCalendar;

/// <summary>
/// Production kariyer dünyasının MVP sayısal sınırları
/// (docs/02_MVP_SCOPE.md Bölüm 17, D-022). Spike1Placeholder ölçeğini kopyalamaz.
/// </summary>
public static class ProductionCareerWorldConstraints
{
    public const int CountryCount = 1;

    public const int LeagueCount = 1;

    public const int ClubCount = 20;

    public const int ContractedPlayersPerClub = 23;

    public const int FreeAgentsPerClub = 2;

    public const int PlayersPerClub = ContractedPlayersPerClub + FreeAgentsPerClub;

    public const int ContractedPlayerCount = ClubCount * ContractedPlayersPerClub;

    public const int FreeAgentCount = ClubCount * FreeAgentsPerClub;

    public const int TargetActivePlayerCount = ContractedPlayerCount + FreeAgentCount;

    public const string CountryDisplayName = "Valoria";

    public const string CountryCode = "VAL";

    public const long DefaultCountryId = 1;

    public static GameDate DefaultOpeningDate { get; } = GameDate.FromCalendarDate(2026, 7, 1);
}
