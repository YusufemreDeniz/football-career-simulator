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

/// <summary>
/// Kıtasal turnuva özetinin tek yazma kapısı (Presentation store'a doğrudan yazmaz).
/// </summary>
public sealed class ContinentalTournamentService
{
    private readonly IContinentalTournamentStore _store;

    public ContinentalTournamentService(IContinentalTournamentStore store)
    {
        _store = store ?? throw new ArgumentNullException(nameof(store));
    }

    public ContinentalTournamentRecord? Current => _store.Record;

    /// <summary>
    /// Aynı sezon ve kulüp için kayıt varsa onu döndürür; yoksa sonuçsuz yeni bir sezon kaydı başlatır.
    /// </summary>
    public ContinentalTournamentRecord ResumeOrStart(int seasonStartYear, long managedClubId, int managedClubStrength)
    {
        var record = _store.Record;
        if (record is not null
            && record.SeasonStartYear == seasonStartYear
            && record.ManagedClubId == managedClubId)
        {
            return record;
        }

        var started = new ContinentalTournamentRecord(
            seasonStartYear,
            managedClubId,
            managedClubStrength,
            Array.Empty<ContinentalManagedResult>());
        _store.Replace(started);
        return started;
    }

    public void RecordManagedResult(long fixtureId, int homeGoals, int awayGoals)
    {
        var record = _store.Record
            ?? throw new InvalidOperationException("Kıtasal turnuva kaydı başlatılmadan sonuç yazılamaz.");
        _store.Replace(record.WithResult(new ContinentalManagedResult(fixtureId, homeGoals, awayGoals)));
    }
}

public sealed class InMemoryContinentalTournamentStore : IContinentalTournamentStore
{
    public ContinentalTournamentRecord? Record { get; private set; }

    public void Replace(ContinentalTournamentRecord? record) => Record = record;
}
