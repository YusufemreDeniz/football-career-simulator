using FootballCareerSimulator.Simulation.Continental;

namespace FootballCareerSimulator.Application.CareerHub.Queries;

public sealed record ContinentalStandingLine(
    int Rank,
    long ClubId,
    string ClubName,
    string Country,
    int Played,
    int Won,
    int Drawn,
    int Lost,
    int GoalsFor,
    int GoalsAgainst,
    int GoalDifference,
    int Points,
    bool IsManagedClub)
{
    public string ToTableLine() =>
        $"{(Rank > 0 ? Rank.ToString() : "-"),-2}  {ClubName,-24} {Played,2}  {Won,2}  {Drawn,2}  {Lost,2}  {GoalsFor,2}:{GoalsAgainst,2}  {(GoalDifference >= 0 ? $"+{GoalDifference}" : GoalDifference.ToString()),3}  {Points,2}p"
        + (IsManagedClub ? "  ◄" : string.Empty);
}

public sealed record ContinentalFixtureLine(
    long FixtureId,
    string StageName,
    int RoundNumber,
    int ScheduledDayNumber,
    long HomeClubId,
    long AwayClubId,
    string HomeClubName,
    string AwayClubName,
    int? HomeScore,
    int? AwayScore,
    bool IsPlayed,
    bool IsManagedMatch)
{
    public string DisplayScoreOrVersus =>
        IsPlayed
            ? $"{HomeScore} - {AwayScore}"
            : "vs";

    public string ToListText() =>
        IsPlayed
            ? $"{HomeClubName} {HomeScore} - {AwayScore} {AwayClubName} · ({StageName})"
            : $"{HomeClubName} vs {AwayClubName} · Gün {ScheduledDayNumber} ({StageName})";
}

public sealed record ContinentalGroupReadModel(
    ContinentalGroupCode GroupCode,
    string GroupName,
    IReadOnlyList<ContinentalStandingLine> Standings,
    IReadOnlyList<ContinentalFixtureLine> Fixtures);

public sealed record ContinentalTournamentDigest(
    bool IsActive,
    string TournamentTitle,
    string StageName,
    ContinentalTournamentStage Stage,
    ContinentalGroupCode? ManagedGroupCode,
    ContinentalGroupReadModel? ManagedGroup,
    IReadOnlyList<ContinentalGroupReadModel> AllGroups,
    ContinentalFixtureLine? UpcomingManagedFixture,
    IReadOnlyList<ContinentalFixtureLine> RecentResults,
    int TotalPrizeMoneyWon,
    string PrizeSummaryText,
    string? ChampionClubName,
    bool IsManagedClubChampion)
{
    public static ContinentalTournamentDigest Clear() =>
        new(
            IsActive: false,
            TournamentTitle: "Avrupa Şampiyonlar Kupası",
            StageName: "Başlamadı",
            Stage: ContinentalTournamentStage.GroupStage,
            ManagedGroupCode: null,
            ManagedGroup: null,
            AllGroups: Array.Empty<ContinentalGroupReadModel>(),
            UpcomingManagedFixture: null,
            RecentResults: Array.Empty<ContinentalFixtureLine>(),
            TotalPrizeMoneyWon: 0,
            PrizeSummaryText: "Henüz ödül kazanılmadı.",
            ChampionClubName: null,
            IsManagedClubChampion: false);

    public static ContinentalTournamentDigest Compose(
        ContinentalTournamentEngine engine,
        int currentDayNumber)
    {
        ArgumentNullException.ThrowIfNull(engine);

        var managedGroupCode = engine.GetManagedClubGroupCode();
        var groups = new[] { ContinentalGroupCode.A, ContinentalGroupCode.B, ContinentalGroupCode.C, ContinentalGroupCode.D };

        var allGroups = groups.Select(grp =>
        {
            var standings = engine.GetGroupStandings(grp)
                .Select((entry, index) => new ContinentalStandingLine(
                    index + 1,
                    entry.ClubId,
                    entry.ClubName,
                    entry.Country,
                    entry.Played,
                    entry.Won,
                    entry.Drawn,
                    entry.Lost,
                    entry.GoalsFor,
                    entry.GoalsAgainst,
                    entry.GoalDifference,
                    entry.Points,
                    entry.ClubId == engine.ManagedClubId))
                .ToList();

            var fixtures = engine.AllFixtures
                .Where(f => f.GroupCode == grp)
                .Select(f => new ContinentalFixtureLine(
                    f.FixtureId,
                    f.StageName,
                    f.RoundNumber,
                    f.ScheduledDayNumber,
                    f.HomeClubId,
                    f.AwayClubId,
                    f.HomeClubName,
                    f.AwayClubName,
                    f.HomeScore,
                    f.AwayScore,
                    f.IsPlayed,
                    f.HomeClubId == engine.ManagedClubId || f.AwayClubId == engine.ManagedClubId))
                .ToList();

            return new ContinentalGroupReadModel(
                grp,
                $"Grup {grp}",
                standings,
                fixtures);
        }).ToList();

        var managedGroup = managedGroupCode is ContinentalGroupCode mg
            ? allGroups.FirstOrDefault(g => g.GroupCode == mg)
            : null;

        var nextFixture = engine.GetNextManagedFixture(currentDayNumber);
        var upcomingLine = nextFixture is not null
            ? new ContinentalFixtureLine(
                nextFixture.FixtureId,
                nextFixture.StageName,
                nextFixture.RoundNumber,
                nextFixture.ScheduledDayNumber,
                nextFixture.HomeClubId,
                nextFixture.AwayClubId,
                nextFixture.HomeClubName,
                nextFixture.AwayClubName,
                nextFixture.HomeScore,
                nextFixture.AwayScore,
                nextFixture.IsPlayed,
                true)
            : null;

        var recentResults = engine.AllFixtures
            .Where(f => f.IsPlayed)
            .OrderByDescending(f => f.ScheduledDayNumber)
            .ThenByDescending(f => f.FixtureId)
            .Take(8)
            .Select(f => new ContinentalFixtureLine(
                f.FixtureId,
                f.StageName,
                f.RoundNumber,
                f.ScheduledDayNumber,
                f.HomeClubId,
                f.AwayClubId,
                f.HomeClubName,
                f.AwayClubName,
                f.HomeScore,
                f.AwayScore,
                true,
                f.HomeClubId == engine.ManagedClubId || f.AwayClubId == engine.ManagedClubId))
            .ToList();

        var stageLabel = engine.CurrentStage switch
        {
            ContinentalTournamentStage.GroupStage => $"Grup Aşaması · {engine.CurrentRoundNumber}. Maç Günü",
            ContinentalTournamentStage.QuarterFinals => "Çeyrek Final",
            ContinentalTournamentStage.SemiFinals => "Yarı Final",
            ContinentalTournamentStage.Final => "BÜYÜK FİNAL",
            _ => "Tamamlandı",
        };

        var prizeText = engine.TotalPrizeMoneyWonByManagedClub > 0
            ? $"Avrupa Geliri: +{engine.TotalPrizeMoneyWonByManagedClub:N0} TRY transfer kasasına eklendi."
            : "Avrupa Galibiyeti: 3.500.000 TRY · Beraberlik: 1.200.000 TRY";

        return new ContinentalTournamentDigest(
            IsActive: true,
            TournamentTitle: "Avrupa Şampiyonlar Kupası",
            StageName: stageLabel,
            Stage: engine.CurrentStage,
            ManagedGroupCode: managedGroupCode,
            ManagedGroup: managedGroup,
            AllGroups: allGroups,
            UpcomingManagedFixture: upcomingLine,
            RecentResults: recentResults,
            TotalPrizeMoneyWon: engine.TotalPrizeMoneyWonByManagedClub,
            PrizeSummaryText: prizeText,
            ChampionClubName: engine.ChampionClubName,
            IsManagedClubChampion: engine.ChampionClubId == engine.ManagedClubId);
    }
}
