namespace FootballCareerSimulator.Application.Continental;

/// <summary>
/// Kıtasal turnuvanın kayda yazılan özü. Turnuva motoru dünya tohumundan yeniden kurulur ve
/// yönetilen kulübün oynadığı maç sonuçları sırayla yeniden uygulanır; yapay zeka maçları tohumdan
/// deterministik olarak yeniden üretilir.
/// </summary>
public sealed record ContinentalTournamentRecord(
    int SeasonStartYear,
    long ManagedClubId,
    int ManagedClubStrength,
    IReadOnlyList<ContinentalManagedResult> ManagedResults)
{
    public ContinentalTournamentRecord WithResult(ContinentalManagedResult result) =>
        this with { ManagedResults = ManagedResults.Append(result).ToArray() };
}

public sealed record ContinentalManagedResult(long FixtureId, int HomeGoals, int AwayGoals);

public interface IContinentalTournamentStore
{
    ContinentalTournamentRecord? Record { get; }

    void Replace(ContinentalTournamentRecord? record);
}

public sealed class InMemoryContinentalTournamentStore : IContinentalTournamentStore
{
    public ContinentalTournamentRecord? Record { get; private set; }

    public void Replace(ContinentalTournamentRecord? record) => Record = record;
}
