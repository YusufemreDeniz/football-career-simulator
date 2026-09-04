using FootballCareerSimulator.Application.Competition.Queries;

namespace FootballCareerSimulator.Tests.Competition;

public sealed class MatchHalfTimeDigestTests
{
    [Fact]
    public void Trailing_UrgesAttack()
    {
        var digest = MatchHalfTimeDigest.Compose("Home", "Away", 0, 2, managedIsHome: true);

        Assert.True(digest.HasManagedMatch);
        Assert.Contains("Geridesin", digest.Headline, StringComparison.Ordinal);
        Assert.Contains("Hücum", digest.AdviceLine, StringComparison.Ordinal);
    }

    [Fact]
    public void Leading_UrgesManageGame()
    {
        var digest = MatchHalfTimeDigest.Compose("Home", "Away", 2, 0, managedIsHome: true);

        Assert.Contains("Öndesin", digest.Headline, StringComparison.Ordinal);
        Assert.Contains("Savunma", digest.AdviceLine, StringComparison.Ordinal);
    }

    [Fact]
    public void FormatDecisionKeyMoment_MapsApproachLabels()
    {
        Assert.Equal(
            "46' Karar · Hücuma geçtin",
            MatchHalfTimeDigest.FormatDecisionKeyMoment("Devre arasında hücuma geçtin."));
        Assert.Equal(
            "46' Karar · Savunmaya çektin",
            MatchHalfTimeDigest.FormatDecisionKeyMoment("Devre arasında savunmaya çektin."));
        Assert.Equal(
            "46' Karar · Aynı plan",
            MatchHalfTimeDigest.FormatDecisionKeyMoment("Devre arasında aynı planla devam ettin."));
        Assert.Null(MatchHalfTimeDigest.FormatDecisionKeyMoment("Devre arasında Ali↔Can."));
    }

    [Fact]
    public void Compose_WithFirstHalfMoments_AddsMomentSectionToBeats()
    {
        var digest = MatchHalfTimeDigest.Compose(
            "Home",
            "Away",
            1,
            1,
            managedIsHome: true,
            firstHalfMomentLines: new[]
            {
                "23' Gol · Kaya (asist: Demir)",
                "31' Kırmızı · Rıza",
            });

        Assert.Contains("İlk yarı anları:", digest.BeatLines);
        Assert.Contains("23' Gol · Kaya (asist: Demir)", digest.BeatLines);
        Assert.Contains("31' Kırmızı · Rıza", digest.BeatLines);
        Assert.Contains(
            digest.BeatLines,
            line => line.Contains("İkinci yarı için yaklaşım seç", StringComparison.Ordinal));
    }

    [Fact]
    public void Compose_WithoutMoments_HasNoMomentSection()
    {
        var digest = MatchHalfTimeDigest.Compose("Home", "Away", 0, 2, managedIsHome: true);

        Assert.DoesNotContain(digest.BeatLines, line => line == "İlk yarı anları:");
        Assert.Equal(
            2,
            digest.BeatLines.Count);
    }

    [Fact]
    public void Compose_WithMoreThanFourMoments_KeepsOnlyFirstFour()
    {
        var lines = Enumerable.Range(1, 6)
            .Select(index => $"{index}' An")
            .ToArray();
        var digest = MatchHalfTimeDigest.Compose(
            "Home",
            "Away",
            2,
            0,
            managedIsHome: true,
            firstHalfMomentLines: lines);

        Assert.Equal(
            lines.Take(4),
            digest.BeatLines.Where(line => line.EndsWith("' An", StringComparison.Ordinal)));
    }
}
