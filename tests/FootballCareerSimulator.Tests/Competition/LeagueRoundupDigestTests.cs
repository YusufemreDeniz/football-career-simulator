using FootballCareerSimulator.Application.Competition.Queries;
using Xunit;

namespace FootballCareerSimulator.Tests.Competition;

public class LeagueRoundupDigestTests
{
    [Fact]
    public void LeaderUnchanged_ReportsLeaderAndCalmHeadline()
    {
        var digest = LeagueRoundupDigest.Compose(
            beforeLeaderName: "Atlas",
            afterLeaderName: "Atlas",
            afterLeaderPoints: 18,
            beforeManagedRank: 4,
            afterManagedRank: 4,
            relegationZone: new[] { "Delta", "Epsilon" },
            managedClubName: "Home",
            otherScorelines: new[] { "Atlas 2-0 Delta", "Epsilon 1-1 Vega" },
            nextOpponentName: "Vega");

        Assert.Equal(LeagueRoundupDigest.Brand, digest.BrandTitle);
        Assert.Equal("Lig akşamı — tablo güncellendi.", digest.Headline);
        Assert.Contains(digest.BeatLines, l => l == "Lider: Atlas (18p)");
    }

    [Fact]
    public void LeaderChanged_FlagsZirveHeadline()
    {
        var digest = LeagueRoundupDigest.Compose(
            beforeLeaderName: "Atlas",
            afterLeaderName: "Vega",
            afterLeaderPoints: 19,
            beforeManagedRank: 3,
            afterManagedRank: 2,
            relegationZone: Array.Empty<string>(),
            managedClubName: "Home",
            otherScorelines: Array.Empty<string>(),
            nextOpponentName: null);

        Assert.Equal("Zirve el değiştirdi — Vega.", digest.Headline);
        Assert.Contains(digest.BeatLines, l => l == "Lider değişti: Vega (19p)");
    }

    [Fact]
    public void ManagedRankClimbed_ReportsDirection()
    {
        var digest = LeagueRoundupDigest.Compose(
            beforeLeaderName: "Atlas",
            afterLeaderName: "Atlas",
            afterLeaderPoints: 18,
            beforeManagedRank: 5,
            afterManagedRank: 3,
            relegationZone: Array.Empty<string>(),
            managedClubName: "Home",
            otherScorelines: Array.Empty<string>(),
            nextOpponentName: null);

        Assert.Equal("Hafta lehine bitti — sıra yükseldi.", digest.Headline);
        Assert.Contains(digest.BeatLines, l => l == "Sıran 5. → 3. (yükseldi)");
    }

    [Fact]
    public void ManagedRankDropped_ReportsDecline()
    {
        var digest = LeagueRoundupDigest.Compose(
            beforeLeaderName: "Atlas",
            afterLeaderName: "Atlas",
            afterLeaderPoints: 18,
            beforeManagedRank: 2,
            afterManagedRank: 4,
            relegationZone: Array.Empty<string>(),
            managedClubName: "Home",
            otherScorelines: Array.Empty<string>(),
            nextOpponentName: null);

        Assert.Equal("Hafta aleyhe bitti — sıra geriledi.", digest.Headline);
        Assert.Contains(digest.BeatLines, l => l == "Sıran 2. → 4. (geriledi)");
    }

    [Fact]
    public void ManagedRankTop_ReportsSummit()
    {
        var digest = LeagueRoundupDigest.Compose(
            beforeLeaderName: null,
            afterLeaderName: null,
            afterLeaderPoints: null,
            beforeManagedRank: 2,
            afterManagedRank: 1,
            relegationZone: Array.Empty<string>(),
            managedClubName: "Home",
            otherScorelines: Array.Empty<string>(),
            nextOpponentName: null);

        Assert.Equal("Tablo senin — zirvedesin.", digest.Headline);
        Assert.Contains(digest.BeatLines, l => l == "Zirvedesin.");
    }

    [Fact]
    public void ManagedInRelegationZone_FlagsDanger()
    {
        var digest = LeagueRoundupDigest.Compose(
            beforeLeaderName: "Atlas",
            afterLeaderName: "Atlas",
            afterLeaderPoints: 18,
            beforeManagedRank: 7,
            afterManagedRank: 7,
            relegationZone: new[] { "Home", "Delta" },
            managedClubName: "Home",
            otherScorelines: Array.Empty<string>(),
            nextOpponentName: null);

        Assert.Contains(digest.BeatLines, l => l == "Küme hattındasın — Home, Delta arasında.");
    }

    [Fact]
    public void RelegationZoneWithoutManaged_ListsClubs()
    {
        var digest = LeagueRoundupDigest.Compose(
            beforeLeaderName: "Atlas",
            afterLeaderName: "Atlas",
            afterLeaderPoints: 18,
            beforeManagedRank: 4,
            afterManagedRank: 4,
            relegationZone: new[] { "Delta", "Epsilon" },
            managedClubName: "Home",
            otherScorelines: Array.Empty<string>(),
            nextOpponentName: null);

        Assert.Contains(digest.BeatLines, l => l == "Küme hattı: Delta, Epsilon");
    }

    [Fact]
    public void NextOpponentResult_FoundInScorelines()
    {
        var digest = LeagueRoundupDigest.Compose(
            beforeLeaderName: "Atlas",
            afterLeaderName: "Atlas",
            afterLeaderPoints: 18,
            beforeManagedRank: 4,
            afterManagedRank: 4,
            relegationZone: Array.Empty<string>(),
            managedClubName: "Home",
            otherScorelines: new[] { "Atlas 2-0 Delta", "Vega 2-1 Epsilon" },
            nextOpponentName: "Vega");

        Assert.Contains(digest.BeatLines, l => l == "Sıradaki rakip Vega: Vega 2-1 Epsilon");
    }

    [Fact]
    public void NextOpponentResult_NotPlayed_SkipsLine()
    {
        var digest = LeagueRoundupDigest.Compose(
            beforeLeaderName: "Atlas",
            afterLeaderName: "Atlas",
            afterLeaderPoints: 18,
            beforeManagedRank: 4,
            afterManagedRank: 4,
            relegationZone: Array.Empty<string>(),
            managedClubName: "Home",
            otherScorelines: new[] { "Atlas 2-0 Delta" },
            nextOpponentName: "Vega");

        Assert.DoesNotContain(digest.BeatLines, l => l.StartsWith("Sıradaki rakip", StringComparison.Ordinal));
    }

    [Fact]
    public void LeaderChange_OpponentResult_BothLinesPresent()
    {
        var digest = LeagueRoundupDigest.Compose(
            beforeLeaderName: "Delta",
            afterLeaderName: "Vega",
            afterLeaderPoints: 21,
            beforeManagedRank: 3,
            afterManagedRank: 2,
            relegationZone: new[] { "Omega" },
            managedClubName: "Home",
            otherScorelines: new[] { "Vega 3-1 Delta", "Home 2-0 Omega" },
            nextOpponentName: "Omega");

        Assert.Contains(digest.BeatLines, l => l == "Lider değişti: Vega (21p)");
        Assert.Contains(digest.BeatLines, l => l == "Sıradaki rakip Omega: Home 2-0 Omega");
    }
}
