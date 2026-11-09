using FootballCareerSimulator.Domain.Match;
using FootballCareerSimulator.Simulation.DataPacks;
using FootballCareerSimulator.Simulation.Match;

namespace FootballCareerSimulator.Simulation.Continental;

public enum ContinentalGroupCode
{
    A = 1,
    B = 2,
    C = 3,
    D = 4,
}

public enum ContinentalTournamentStage
{
    GroupStage = 1,
    QuarterFinals = 2,
    SemiFinals = 3,
    Final = 4,
    Completed = 5,
}

public sealed record ContinentalClubParticipant(
    long ClubId,
    string ClubName,
    string Country,
    int SportiveStrength,
    bool IsDomestic,
    ContinentalGroupCode GroupCode);

public sealed class ContinentalStandingEntry
{
    public long ClubId { get; }
    public string ClubName { get; }
    public string Country { get; }
    public bool IsDomestic { get; }
    public int Played { get; set; }
    public int Won { get; set; }
    public int Drawn { get; set; }
    public int Lost { get; set; }
    public int GoalsFor { get; set; }
    public int GoalsAgainst { get; set; }
    public int GoalDifference => GoalsFor - GoalsAgainst;
    public int Points => (Won * 3) + Drawn;

    public ContinentalStandingEntry(long clubId, string clubName, string country, bool isDomestic)
    {
        ClubId = clubId;
        ClubName = clubName;
        Country = country;
        IsDomestic = isDomestic;
    }

    public void RecordResult(int goalsScored, int goalsConceded)
    {
        Played++;
        GoalsFor += goalsScored;
        GoalsAgainst += goalsConceded;

        if (goalsScored > goalsConceded)
        {
            Won++;
        }
        else if (goalsScored == goalsConceded)
        {
            Drawn++;
        }
        else
        {
            Lost++;
        }
    }
}

public sealed class ContinentalFixture
{
    public long FixtureId { get; }
    public ContinentalTournamentStage Stage { get; }
    public string StageName { get; }
    public int RoundNumber { get; }
    public int ScheduledDayNumber { get; set; }
    public long HomeClubId { get; }
    public long AwayClubId { get; }
    public string HomeClubName { get; }
    public string AwayClubName { get; }
    public int? HomeScore { get; set; }
    public int? AwayScore { get; set; }
    public bool IsPlayed => HomeScore.HasValue && AwayScore.HasValue;
    public bool IsKnockout { get; }
    public ContinentalGroupCode? GroupCode { get; }
    public IReadOnlyList<MatchKeyMoment> KeyMoments { get; set; } = Array.Empty<MatchKeyMoment>();

    public ContinentalFixture(
        long fixtureId,
        ContinentalTournamentStage stage,
        string stageName,
        int roundNumber,
        int scheduledDayNumber,
        long homeClubId,
        long awayClubId,
        string homeClubName,
        string awayClubName,
        bool isKnockout,
        ContinentalGroupCode? groupCode = null)
    {
        FixtureId = fixtureId;
        Stage = stage;
        StageName = stageName;
        RoundNumber = roundNumber;
        ScheduledDayNumber = scheduledDayNumber;
        HomeClubId = homeClubId;
        AwayClubId = awayClubId;
        HomeClubName = homeClubName;
        AwayClubName = awayClubName;
        IsKnockout = isKnockout;
        GroupCode = groupCode;
    }

    public void ApplyResult(int homeScore, int awayScore, IReadOnlyList<MatchKeyMoment> moments)
    {
        HomeScore = homeScore;
        AwayScore = awayScore;
        KeyMoments = moments ?? Array.Empty<MatchKeyMoment>();
    }

    public string ToScoreLine() =>
        IsPlayed
            ? $"{HomeClubName} {HomeScore} - {AwayScore} {AwayClubName}"
            : $"{HomeClubName} vs {AwayClubName} (Grup {GroupCode})";
}

public sealed class ContinentalTournamentEngine
{
    public const int GroupStageRounds = 6;
    public const int WinPrizeMoney = 3_500_000;
    public const int DrawPrizeMoney = 1_200_000;
    public const int KnockoutAdvancementPrize = 5_000_000;
    public const int TrophyPrizeMoney = 15_000_000;

    public int WorldSeed { get; }
    public long ManagedClubId { get; }
    public ContinentalTournamentStage CurrentStage { get; private set; } = ContinentalTournamentStage.GroupStage;
    public int CurrentRoundNumber { get; private set; } = 1;
    public long? ChampionClubId { get; private set; }
    public string? ChampionClubName { get; private set; }
    public int TotalPrizeMoneyWonByManagedClub { get; private set; }

    private readonly List<ContinentalClubParticipant> _participants = new();
    private readonly Dictionary<ContinentalGroupCode, List<ContinentalStandingEntry>> _groupStandings = new();
    private readonly List<ContinentalFixture> _fixtures = new();
    private long _nextFixtureId = 900_001;

    public IReadOnlyList<ContinentalClubParticipant> Participants => _participants;
    public IReadOnlyList<ContinentalFixture> AllFixtures => _fixtures;

    public ContinentalTournamentEngine(
        int worldSeed,
        long managedClubId,
        string managedClubName,
        int managedClubStrength,
        IReadOnlyList<(long ClubId, string Name, int Strength)> otherDomesticClubs,
        int startDayNumber = 10)
    {
        WorldSeed = worldSeed;
        ManagedClubId = managedClubId;

        InitializeTournament(
            managedClubName,
            managedClubStrength,
            otherDomesticClubs,
            startDayNumber);
    }

    private void InitializeTournament(
        string managedClubName,
        int managedClubStrength,
        IReadOnlyList<(long ClubId, string Name, int Strength)> otherDomesticClubs,
        int startDayNumber)
    {
        // 1. Dört Türk Temsilcisi: Menajerin kulübü + en güçlü 3 yerli rakip
        var domestic = new List<(long ClubId, string Name, string Country, int Strength)>
        {
            (ManagedClubId, managedClubName, "Türkiye", managedClubStrength)
        };

        foreach (var other in otherDomesticClubs.Where(c => c.ClubId != ManagedClubId).Take(3))
        {
            domestic.Add((other.ClubId, other.Name, "Türkiye", other.Strength));
        }

        // Eğer 4'e tamamlanamazsa varsayılan Süper Lig kulüpleriyle tamamla
        if (domestic.Count < 4)
        {
            var fallbacks = new[]
            {
                (1L, "Galatasaray", "Türkiye", 80),
                (2L, "Fenerbahçe", "Türkiye", 80),
                (3L, "Beşiktaş", "Türkiye", 78),
                (4L, "Trabzonspor", "Türkiye", 76),
            };
            foreach (var fb in fallbacks)
            {
                if (domestic.All(d => d.ClubId != fb.Item1))
                {
                    domestic.Add((fb.Item1, fb.Item2, fb.Item3, fb.Item4));
                    if (domestic.Count == 4) break;
                }
            }
        }

        // 2. 12 Elit Avrupa Devi (Torbalardan dengeli seçim)
        var eliteClubs = EuropeanElite202627DataPack.AllClubs.Take(12).ToList();

        // 3. Grupların Oluşturulması (A, B, C, D)
        var groups = new[] { ContinentalGroupCode.A, ContinentalGroupCode.B, ContinentalGroupCode.C, ContinentalGroupCode.D };
        for (var i = 0; i < 4; i++)
        {
            var grp = groups[i];
            _groupStandings[grp] = new List<ContinentalStandingEntry>();

            // 1 Yerli Takım
            var dom = domestic[i];
            var domParticipant = new ContinentalClubParticipant(dom.ClubId, dom.Name, dom.Country, dom.Strength, true, grp);
            _participants.Add(domParticipant);
            _groupStandings[grp].Add(new ContinentalStandingEntry(dom.ClubId, dom.Name, dom.Country, true));

            // 3 Avrupa Devi
            for (var j = 0; j < 3; j++)
            {
                var euro = eliteClubs[(i * 3) + j];
                var euroParticipant = new ContinentalClubParticipant(euro.ClubId, euro.OfficialName, euro.Country, euro.SportiveStrength, false, grp);
                _participants.Add(euroParticipant);
                _groupStandings[grp].Add(new ContinentalStandingEntry(euro.ClubId, euro.OfficialName, euro.Country, false));
            }
        }

        // 4. Grup Fikstürlerinin Planlanması (Her grupta 6 maç günü)
        // Takvimde hafta ortası günlerine (ör. her 14 günde bir Çarşamba akşamı) yerleştirilir.
        ScheduleGroupStageFixtures(startDayNumber);
    }

    private void ScheduleGroupStageFixtures(int startDayNumber)
    {
        var groups = new[] { ContinentalGroupCode.A, ContinentalGroupCode.B, ContinentalGroupCode.C, ContinentalGroupCode.D };

        // 4 takımlı round-robin (6 tur)
        // Takımlar: 0, 1, 2, 3
        int[][] roundPairs =
        [
            [0, 1,  2, 3], // Tur 1
            [2, 0,  3, 1], // Tur 2
            [0, 3,  1, 2], // Tur 3
            [1, 0,  3, 2], // Tur 4 (Rövanş)
            [0, 2,  1, 3], // Tur 5 (Rövanş)
            [3, 0,  2, 1], // Tur 6 (Rövanş)
        ];

        for (var round = 0; round < GroupStageRounds; round++)
        {
            var matchDayNumber = startDayNumber + (round * 14) + 3; // Hafta ortası
            var pairs = roundPairs[round];

            foreach (var grp in groups)
            {
                var groupTeams = _participants.Where(p => p.GroupCode == grp).ToArray();

                // Maç 1
                var home1 = groupTeams[pairs[0]];
                var away1 = groupTeams[pairs[1]];
                _fixtures.Add(new ContinentalFixture(
                    _nextFixtureId++,
                    ContinentalTournamentStage.GroupStage,
                    $"Grup {grp} · {round + 1}. Maç Günü",
                    round + 1,
                    matchDayNumber,
                    home1.ClubId,
                    away1.ClubId,
                    home1.ClubName,
                    away1.ClubName,
                    isKnockout: false,
                    groupCode: grp));

                // Maç 2
                var home2 = groupTeams[pairs[2]];
                var away2 = groupTeams[pairs[3]];
                _fixtures.Add(new ContinentalFixture(
                    _nextFixtureId++,
                    ContinentalTournamentStage.GroupStage,
                    $"Grup {grp} · {round + 1}. Maç Günü",
                    round + 1,
                    matchDayNumber,
                    home2.ClubId,
                    away2.ClubId,
                    home2.ClubName,
                    away2.ClubName,
                    isKnockout: false,
                    groupCode: grp));
            }
        }
    }

    public ContinentalFixture? GetNextDueFixture(int currentDayNumber) =>
        _fixtures
            .Where(f => !f.IsPlayed && f.ScheduledDayNumber <= currentDayNumber)
            .OrderBy(f => f.ScheduledDayNumber)
            .ThenBy(f => f.FixtureId)
            .FirstOrDefault();

    public ContinentalFixture? GetNextManagedFixture(int currentDayNumber) =>
        _fixtures
            .Where(f => !f.IsPlayed && (f.HomeClubId == ManagedClubId || f.AwayClubId == ManagedClubId))
            .OrderBy(f => f.ScheduledDayNumber)
            .ThenBy(f => f.FixtureId)
            .FirstOrDefault();

    public IReadOnlyList<ContinentalStandingEntry> GetGroupStandings(ContinentalGroupCode groupCode) =>
        _groupStandings.TryGetValue(groupCode, out var list)
            ? list.OrderByDescending(e => e.Points)
                  .ThenByDescending(e => e.GoalDifference)
                  .ThenByDescending(e => e.GoalsFor)
                  .ToArray()
            : Array.Empty<ContinentalStandingEntry>();

    public ContinentalGroupCode? GetManagedClubGroupCode() =>
        _participants.FirstOrDefault(p => p.ClubId == ManagedClubId)?.GroupCode;

    /// <summary>
    /// Eleme maçında beraberlik olamaz: güçlü taraf (eşitlikte ev sahibi) penaltılarla tek gol farkla tur atlar.
    /// Hem yapay zeka hem yönetilen kulüp maçlarında aynı kural uygulanır.
    /// </summary>
    public (int HomeGoals, int AwayGoals, bool DecidedOnPenalties) ApplyKnockoutTieBreak(
        ContinentalFixture fixture,
        int homeGoals,
        int awayGoals)
    {
        ArgumentNullException.ThrowIfNull(fixture);
        if (!fixture.IsKnockout || homeGoals != awayGoals)
        {
            return (homeGoals, awayGoals, false);
        }

        return GetClubStrength(fixture.HomeClubId) >= GetClubStrength(fixture.AwayClubId)
            ? (homeGoals + 1, awayGoals, true)
            : (homeGoals, awayGoals + 1, true);
    }

    /// <summary>
    /// Menajerin kulübünün bir Avrupa maçı sonucunu kaydeder ve puan durumunu/primini günceller.
    /// Eleme maçındaki beraberlik <see cref="ApplyKnockoutTieBreak"/> ile çözülür.
    /// </summary>
    /// <returns>Bu sonuçla yönetilen kulübe kazandırılan prim (transfer bütçesine yazılacak tutar).</returns>
    public int RecordManagedMatchResult(long fixtureId, int homeScore, int awayScore, IReadOnlyList<MatchKeyMoment>? moments = null)
    {
        var fixture = _fixtures.FirstOrDefault(f => f.FixtureId == fixtureId)
            ?? throw new InvalidOperationException($"Avrupa fikstürü bulunamadı: {fixtureId}");

        if (fixture.IsPlayed)
        {
            return 0;
        }

        (homeScore, awayScore, _) = ApplyKnockoutTieBreak(fixture, homeScore, awayScore);
        var prizeBefore = TotalPrizeMoneyWonByManagedClub;

        fixture.ApplyResult(homeScore, awayScore, moments ?? Array.Empty<MatchKeyMoment>());

        if (fixture.GroupCode is ContinentalGroupCode grp)
        {
            var standings = _groupStandings[grp];
            var homeEntry = standings.FirstOrDefault(e => e.ClubId == fixture.HomeClubId);
            var awayEntry = standings.FirstOrDefault(e => e.ClubId == fixture.AwayClubId);

            homeEntry?.RecordResult(homeScore, awayScore);
            awayEntry?.RecordResult(awayScore, homeScore);

            // Prim hesapla
            var isHome = fixture.HomeClubId == ManagedClubId;
            var managedGoals = isHome ? homeScore : awayScore;
            var opponentGoals = isHome ? awayScore : homeScore;

            if (managedGoals > opponentGoals)
            {
                TotalPrizeMoneyWonByManagedClub += WinPrizeMoney;
            }
            else if (managedGoals == opponentGoals)
            {
                TotalPrizeMoneyWonByManagedClub += DrawPrizeMoney;
            }
        }
        else if (fixture.IsKnockout)
        {
            var isHome = fixture.HomeClubId == ManagedClubId;
            var managedGoals = isHome ? homeScore : awayScore;
            var opponentGoals = isHome ? awayScore : homeScore;

            if (managedGoals > opponentGoals)
            {
                TotalPrizeMoneyWonByManagedClub += KnockoutAdvancementPrize;
                if (fixture.Stage == ContinentalTournamentStage.Final)
                {
                    ChampionClubId = ManagedClubId;
                    ChampionClubName = isHome ? fixture.HomeClubName : fixture.AwayClubName;
                    TotalPrizeMoneyWonByManagedClub += TrophyPrizeMoney;
                    CurrentStage = ContinentalTournamentStage.Completed;
                }
            }
            else if (fixture.Stage == ContinentalTournamentStage.Final)
            {
                ChampionClubId = isHome ? fixture.AwayClubId : fixture.HomeClubId;
                ChampionClubName = isHome ? fixture.AwayClubName : fixture.HomeClubName;
                CurrentStage = ContinentalTournamentStage.Completed;
            }
        }

        // Aynı gündeki diğer yapay zeka maçlarını simüle et ve biten aşamanın ardından turu ilerlet
        AdvanceTo(fixture.ScheduledDayNumber);

        return TotalPrizeMoneyWonByManagedClub - prizeBefore;
    }

    /// <summary>
    /// Verilen güne kadar yapay zeka maçlarını oynatır ve tamamlanan aşamaların sonraki turunu planlar.
    /// Yönetilen kulüp elendikten sonra da turnuvanın kalan turları bu çağrıyla ilerler.
    /// Sonuçlar dünya tohumu ve fikstür kimliğinden türetildiği için çağrı zamanından bağımsız ve deterministiktir.
    /// </summary>
    public void AdvanceTo(int dayNumber)
    {
        while (true)
        {
            SimulatePendingAiMatches(dayNumber);
            if (!CheckAndAdvanceTournamentStage())
            {
                return;
            }
        }
    }

    /// <summary>
    /// Belirtilen güne kadar olan henüz oynanmamış yapay zeka Avrupa maçlarını simüle eder.
    /// </summary>
    public int SimulatePendingAiMatches(int upToDayNumber)
    {
        var pending = _fixtures
            .Where(f => !f.IsPlayed
                        && f.ScheduledDayNumber <= upToDayNumber
                        && f.HomeClubId != ManagedClubId
                        && f.AwayClubId != ManagedClubId)
            .ToList();

        var simulatedCount = 0;
        foreach (var fixture in pending)
        {
            SimulateSingleAiFixture(fixture);
            simulatedCount++;
        }

        return simulatedCount;
    }

    private void SimulateSingleAiFixture(ContinentalFixture fixture)
    {
        var homeStrength = GetClubStrength(fixture.HomeClubId);
        var awayStrength = GetClubStrength(fixture.AwayClubId);

        var outcome = MvpFixtureMatchSimulator.SimulateWithKeyMoments(
            WorldSeed,
            fixture.FixtureId,
            homeStrength,
            awayStrength);

        var (homeGoals, awayGoals, _) = ApplyKnockoutTieBreak(
            fixture,
            outcome.Score.HomeGoals,
            outcome.Score.AwayGoals);

        fixture.ApplyResult(homeGoals, awayGoals, outcome.KeyMoments);

        if (fixture.GroupCode is ContinentalGroupCode grp && _groupStandings.TryGetValue(grp, out var standings))
        {
            var homeEntry = standings.FirstOrDefault(e => e.ClubId == fixture.HomeClubId);
            var awayEntry = standings.FirstOrDefault(e => e.ClubId == fixture.AwayClubId);
            homeEntry?.RecordResult(homeGoals, awayGoals);
            awayEntry?.RecordResult(awayGoals, homeGoals);
        }

        if (fixture.Stage == ContinentalTournamentStage.Final)
        {
            var isHomeWinner = homeGoals > awayGoals;
            ChampionClubId = isHomeWinner ? fixture.HomeClubId : fixture.AwayClubId;
            ChampionClubName = isHomeWinner ? fixture.HomeClubName : fixture.AwayClubName;
            CurrentStage = ContinentalTournamentStage.Completed;
        }
    }

    public int GetClubStrength(long clubId)
    {
        var participant = _participants.FirstOrDefault(p => p.ClubId == clubId);
        if (participant is not null)
        {
            return participant.SportiveStrength;
        }

        if (EuropeanElite202627DataPack.TryGetClub(clubId, out var elite))
        {
            return elite.SportiveStrength;
        }

        return 75;
    }

    /// <summary>
    /// Mevcut aşamanın tüm maçları oynandıysa sonraki turu, aşamanın son maç gününden 14 gün sonraya planlar.
    /// Planlama günü çağrı zamanına değil fikstür takvimine bağlıdır; böylece kayıttan yeniden kurulum aynı sonucu verir.
    /// </summary>
    private bool CheckAndAdvanceTournamentStage()
    {
        if (CurrentStage is ContinentalTournamentStage.Final or ContinentalTournamentStage.Completed)
        {
            return false;
        }

        var stageFixtures = _fixtures.Where(f => f.Stage == CurrentStage).ToList();
        if (stageFixtures.Count == 0 || !stageFixtures.All(f => f.IsPlayed))
        {
            return false;
        }

        var nextDay = stageFixtures.Max(f => f.ScheduledDayNumber) + 14;
        var stageBefore = CurrentStage;
        switch (CurrentStage)
        {
            case ContinentalTournamentStage.GroupStage:
                ScheduleQuarterFinals(nextDay);
                break;
            case ContinentalTournamentStage.QuarterFinals:
                ScheduleSemiFinals(nextDay);
                break;
            case ContinentalTournamentStage.SemiFinals:
                ScheduleFinal(nextDay);
                break;
        }

        return CurrentStage != stageBefore;
    }

    private void ScheduleQuarterFinals(int matchDayNumber)
    {
        CurrentStage = ContinentalTournamentStage.QuarterFinals;
        CurrentRoundNumber = 7;

        var a = GetGroupStandings(ContinentalGroupCode.A);
        var b = GetGroupStandings(ContinentalGroupCode.B);
        var c = GetGroupStandings(ContinentalGroupCode.C);
        var d = GetGroupStandings(ContinentalGroupCode.D);

        if (a.Count < 2 || b.Count < 2 || c.Count < 2 || d.Count < 2) return;

        // Çeyrek Final 1: A1 vs B2
        AddKnockoutFixture(ContinentalTournamentStage.QuarterFinals, "Çeyrek Final (1/4)", 7, matchDayNumber, a[0], b[1]);
        // Çeyrek Final 2: B1 vs A2
        AddKnockoutFixture(ContinentalTournamentStage.QuarterFinals, "Çeyrek Final (2/4)", 7, matchDayNumber, b[0], a[1]);
        // Çeyrek Final 3: C1 vs D2
        AddKnockoutFixture(ContinentalTournamentStage.QuarterFinals, "Çeyrek Final (3/4)", 7, matchDayNumber, c[0], d[1]);
        // Çeyrek Final 4: D1 vs C2
        AddKnockoutFixture(ContinentalTournamentStage.QuarterFinals, "Çeyrek Final (4/4)", 7, matchDayNumber, d[0], c[1]);
    }

    private void ScheduleSemiFinals(int matchDayNumber)
    {
        CurrentStage = ContinentalTournamentStage.SemiFinals;
        CurrentRoundNumber = 8;

        var qf = _fixtures.Where(f => f.Stage == ContinentalTournamentStage.QuarterFinals).ToList();
        if (qf.Count < 4) return;

        var winner1 = GetWinner(qf[0]);
        var winner2 = GetWinner(qf[2]);
        var winner3 = GetWinner(qf[1]);
        var winner4 = GetWinner(qf[3]);

        // Yarı Final 1
        AddKnockoutFixture(ContinentalTournamentStage.SemiFinals, "Yarı Final (1/2)", 8, matchDayNumber, winner1, winner2);
        // Yarı Final 2
        AddKnockoutFixture(ContinentalTournamentStage.SemiFinals, "Yarı Final (2/2)", 8, matchDayNumber, winner3, winner4);
    }

    private void ScheduleFinal(int matchDayNumber)
    {
        CurrentStage = ContinentalTournamentStage.Final;
        CurrentRoundNumber = 9;

        var sf = _fixtures.Where(f => f.Stage == ContinentalTournamentStage.SemiFinals).ToList();
        if (sf.Count < 2) return;

        var final1 = GetWinner(sf[0]);
        var final2 = GetWinner(sf[1]);

        AddKnockoutFixture(ContinentalTournamentStage.Final, "BÜYÜK FİNAL", 9, matchDayNumber, final1, final2);
    }

    private void AddKnockoutFixture(
        ContinentalTournamentStage stage,
        string stageName,
        int roundNumber,
        int scheduledDayNumber,
        (long ClubId, string Name) home,
        (long ClubId, string Name) away)
    {
        _fixtures.Add(new ContinentalFixture(
            _nextFixtureId++,
            stage,
            stageName,
            roundNumber,
            scheduledDayNumber,
            home.ClubId,
            away.ClubId,
            home.Name,
            away.Name,
            isKnockout: true));
    }

    private void AddKnockoutFixture(
        ContinentalTournamentStage stage,
        string stageName,
        int roundNumber,
        int scheduledDayNumber,
        ContinentalStandingEntry home,
        ContinentalStandingEntry away) =>
        AddKnockoutFixture(stage, stageName, roundNumber, scheduledDayNumber, (home.ClubId, home.ClubName), (away.ClubId, away.ClubName));

    private static (long ClubId, string Name) GetWinner(ContinentalFixture fixture)
    {
        // Eleme sonuçları ApplyKnockoutTieBreak sonrası kaydedildiği için beraberlik kalmaz.
        var homeWon = (fixture.HomeScore ?? 0) > (fixture.AwayScore ?? 0);
        return homeWon
            ? (fixture.HomeClubId, fixture.HomeClubName)
            : (fixture.AwayClubId, fixture.AwayClubName);
    }
}
