using FootballCareerSimulator.Application.Competition.Queries;
using FootballCareerSimulator.Domain.Competition;

namespace FootballCareerSimulator.Tests.Competition;

public sealed class OpponentDossierDigestTests
{
    [Theory]
    [InlineData(true, "evine geliyor")]
    [InlineData(false, "deplasmanındasın")]
    public void Headline_UsesVenueContext(bool managedIsHome, string expected)
    {
        var digest = Compose(managedIsHome: managedIsHome);

        Assert.Equal(OpponentDossierDigest.Brand, digest.BrandTitle);
        Assert.Contains("Rakip FK", digest.Headline, StringComparison.Ordinal);
        Assert.Contains(expected, digest.Headline, StringComparison.Ordinal);
        Assert.Equal(4, digest.DetailLines.Count);
    }

    [Fact]
    public void Standing_FormatsRankPointsAndSignedGoalDifference()
    {
        var standings = new[]
        {
            Standing(clubId: 3, played: 5, points: 12, goalsFor: 8, goalsAgainst: 4),
            Standing(clubId: OpponentId, played: 5, points: 10, goalsFor: 7, goalsAgainst: 5),
            Standing(clubId: ManagedId, played: 5, points: 8, goalsFor: 4, goalsAgainst: 5),
        };

        var digest = Compose(standings: standings);

        Assert.Equal("Lig: 2/3 · 10 puan · averaj +2", digest.StandingLine);
    }

    [Fact]
    public void Standing_WithoutPlayedMatch_DoesNotPresentSyntheticRank()
    {
        var digest = Compose(
            standings:
            [
                Standing(ManagedId),
                Standing(OpponentId),
            ]);

        Assert.Equal("Lig: henüz sonuç verisi oluşmadı.", digest.StandingLine);
    }

    [Fact]
    public void Form_IsChronologicalAndFromOpponentPerspective()
    {
        var fixtures = new[]
        {
            Result(1, day: 3, home: OpponentId, away: 9, homeGoals: 2, awayGoals: 0),
            Result(2, day: 6, home: 8, away: OpponentId, homeGoals: 1, awayGoals: 1),
            Result(3, day: 9, home: 7, away: OpponentId, homeGoals: 3, awayGoals: 1),
        };

        var digest = Compose(fixtures: fixtures);

        Assert.Equal("Form (eski→yeni): G-B-M · 4/9 puan", digest.FormLine);
    }

    [Fact]
    public void Form_IgnoresPlannedAndUnrelatedFixtures()
    {
        var fixtures = new[]
        {
            Fixture(1, day: 3, home: OpponentId, away: 9, status: FixtureStatus.Planned),
            Result(2, day: 6, home: 8, away: 7, homeGoals: 1, awayGoals: 0),
        };

        var digest = Compose(fixtures: fixtures);

        Assert.Equal("Form: henüz tamamlanmış maç yok.", digest.FormLine);
    }

    [Fact]
    public void Form_KeepsOnlyLatestFiveResults()
    {
        var fixtures = Enumerable.Range(1, 6)
            .Select(index => Result(
                index,
                day: index,
                home: OpponentId,
                away: 10 + index,
                homeGoals: index == 1 ? 0 : 1,
                awayGoals: index == 1 ? 2 : 0))
            .ToArray();

        var digest = Compose(fixtures: fixtures);

        Assert.Equal("Form (eski→yeni): G-G-G-G-G · 15/15 puan", digest.FormLine);
    }

    [Theory]
    [InlineData(50, 60, "rakip belirgin üstün")]
    [InlineData(50, 54, "rakip az farkla güçlü")]
    [InlineData(50, 51, "dengeli eşleşme")]
    [InlineData(60, 54, "az farkla üstünsün")]
    [InlineData(60, 50, "belirgin üstünlüğün var")]
    public void Strength_UsesRelativeBands(
        int managedStrength,
        int opponentStrength,
        string expected)
    {
        var digest = Compose(
            managedStrength: managedStrength,
            opponentStrength: opponentStrength);

        Assert.Contains(expected, digest.StrengthLine, StringComparison.Ordinal);
    }

    [Fact]
    public void Threat_WinningStreakHasPriority()
    {
        var fixtures = new[]
        {
            Result(1, 1, OpponentId, 8, 1, 0),
            Result(2, 2, 9, OpponentId, 0, 2),
            Result(3, 3, OpponentId, 10, 3, 1),
        };

        var digest = Compose(fixtures: fixtures, managedIsHome: false);

        Assert.Contains("üç maçlık galibiyet serisi", digest.ThreatLine, StringComparison.Ordinal);
        Assert.Contains("deplasmanda erken baskı", digest.ThreatLine, StringComparison.Ordinal);
    }

    [Fact]
    public void Threat_ProductiveAttackUsesStandingEvidence()
    {
        var digest = Compose(
            standings:
            [
                Standing(ManagedId, played: 4, points: 5, goalsFor: 4, goalsAgainst: 5),
                Standing(OpponentId, played: 4, points: 7, goalsFor: 7, goalsAgainst: 4),
            ]);

        Assert.Contains("üretken hücum", digest.ThreatLine, StringComparison.Ordinal);
    }

    [Fact]
    public void Threat_StrongerOpponentUsesVenueAdvice()
    {
        var digest = Compose(
            managedStrength: 50,
            opponentStrength: 60,
            managedIsHome: false);

        Assert.Contains("kadro kalitesi", digest.ThreatLine, StringComparison.Ordinal);
        Assert.Contains("alanı daralt", digest.ThreatLine, StringComparison.Ordinal);
    }

    [Fact]
    public void Threat_TopZoneUsesRankWhenOtherSignalsAreQuiet()
    {
        var digest = Compose(
            standings:
            [
                Standing(OpponentId, played: 4, points: 8, goalsFor: 4, goalsAgainst: 4),
                Standing(3, played: 4, points: 7, goalsFor: 4, goalsAgainst: 4),
                Standing(4, played: 4, points: 6, goalsFor: 4, goalsAgainst: 4),
                Standing(ManagedId, played: 4, points: 5, goalsFor: 4, goalsAgainst: 4),
            ]);

        Assert.Contains("zirve temposu", digest.ThreatLine, StringComparison.Ordinal);
    }

    [Fact]
    public void Threat_DefensiveRecordIsSurfaced()
    {
        var digest = Compose(
            standings:
            [
                Standing(3, played: 5, points: 10, goalsFor: 5, goalsAgainst: 4),
                Standing(4, played: 5, points: 9, goalsFor: 5, goalsAgainst: 4),
                Standing(5, played: 5, points: 8, goalsFor: 5, goalsAgainst: 4),
                Standing(OpponentId, played: 5, points: 7, goalsFor: 4, goalsAgainst: 3),
                Standing(ManagedId, played: 5, points: 6, goalsFor: 4, goalsAgainst: 5),
            ]);

        Assert.Contains("savunma direnci", digest.ThreatLine, StringComparison.Ordinal);
    }

    [Fact]
    public void Threat_NeutralProfileDoesNotInventAnEdge()
    {
        var digest = Compose();

        Assert.Contains("belirgin bir uç yok", digest.ThreatLine, StringComparison.Ordinal);
    }

    private const long ManagedId = 1;
    private const long OpponentId = 2;

    private static OpponentDossierDigest Compose(
        bool managedIsHome = true,
        int managedStrength = 60,
        int opponentStrength = 60,
        IReadOnlyList<StandingEntryReadModel>? standings = null,
        IReadOnlyList<FixtureReadModel>? fixtures = null) =>
        OpponentDossierDigest.Compose(
            OpponentId,
            "Rakip FK",
            managedIsHome,
            managedStrength,
            opponentStrength,
            standings ??
            [
                Standing(ManagedId),
                Standing(OpponentId),
            ],
            fixtures ?? Array.Empty<FixtureReadModel>());

    private static StandingEntryReadModel Standing(
        long clubId,
        int played = 0,
        int points = 0,
        int goalsFor = 0,
        int goalsAgainst = 0) =>
        new(
            clubId,
            played,
            Won: 0,
            Drawn: 0,
            Lost: 0,
            goalsFor,
            goalsAgainst,
            points,
            GoalDifference: goalsFor - goalsAgainst);

    private static FixtureReadModel Result(
        long id,
        int day,
        long home,
        long away,
        int homeGoals,
        int awayGoals) =>
        Fixture(id, day, home, away, FixtureStatus.ResultAccepted, homeGoals, awayGoals);

    private static FixtureReadModel Fixture(
        long id,
        int day,
        long home,
        long away,
        FixtureStatus status,
        int? homeGoals = null,
        int? awayGoals = null) =>
        new(
            id,
            SeasonId: 1,
            home,
            away,
            Round: day,
            ScheduledDayNumber: day,
            ScheduledIsoDate: $"2026-08-{day:00}",
            status.ToString(),
            homeGoals,
            awayGoals);
}
