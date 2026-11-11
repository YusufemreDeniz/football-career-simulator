using FootballCareerSimulator.Application.Competition.Commands;
using FootballCareerSimulator.Application.Competition.Queries;
using FootballCareerSimulator.Domain.Match;

namespace FootballCareerSimulator.Tests.Competition;

public sealed class CrowdReactionDigestTests
{
    [Fact]
    public void HomeGoalFromLevel_RoarsForTheLead()
    {
        var digest = CrowdReactionDigest.Compose(
            managedIsHome: true,
            [Moment(MatchKeyMomentKind.Goal, 18, isHomeSide: true)]);

        var beat = Assert.Single(digest.MomentBeats);
        Assert.Equal(CrowdReactionDigest.Brand, digest.BrandTitle);
        Assert.Equal(18, beat.Minute);
        Assert.Contains("stat koptu", beat.Line, StringComparison.Ordinal);
    }

    [Fact]
    public void AwayEqualizer_LiftsAwayEndAndSilencesStadium()
    {
        var digest = CrowdReactionDigest.Compose(
            managedIsHome: false,
            [
                Moment(MatchKeyMomentKind.Goal, 12, isHomeSide: true),
                Moment(MatchKeyMomentKind.Goal, 37, isHomeSide: false),
            ]);

        Assert.Contains(
            digest.MomentBeats,
            beat => beat.Minute == 37
                && beat.Line.Contains("stat bir an sustu", StringComparison.Ordinal));
    }

    [Fact]
    public void HomeConcedesLead_CrowdPressureTurnsToBench()
    {
        var digest = CrowdReactionDigest.Compose(
            managedIsHome: true,
            [Moment(MatchKeyMomentKind.Goal, 29, isHomeSide: false)]);

        Assert.Contains("baskı kulübeye döndü", Assert.Single(digest.MomentBeats).Line);
    }

    [Theory]
    [InlineData(true, true, "hakeme çevirdi")]
    [InlineData(true, false, "galibiyet kokusunu")]
    [InlineData(false, true, "baskıyı katladı")]
    [InlineData(false, false, "deplasman köşesi cesaretlendi")]
    public void RedCard_UsesVenueAndManagedSideContext(
        bool managedIsHome,
        bool cardIsManagedSide,
        string expected)
    {
        var cardIsHomeSide = cardIsManagedSide == managedIsHome;
        var digest = CrowdReactionDigest.Compose(
            managedIsHome,
            [Moment(MatchKeyMomentKind.RedCard, 64, cardIsHomeSide)]);

        Assert.Contains(expected, Assert.Single(digest.MomentBeats).Line);
    }

    [Fact]
    public void YellowCard_DoesNotCreateCrowdBeat()
    {
        var digest = CrowdReactionDigest.Compose(
            managedIsHome: true,
            [Moment(MatchKeyMomentKind.YellowCard, 21, isHomeSide: true)]);

        Assert.Empty(digest.MomentBeats);
    }

    [Theory]
    [InlineData(true, 2, 0, "üstünlüğü korumanı")]
    [InlineData(true, 0, 1, "sabır daralıyor")]
    [InlineData(true, 0, 0, "kıvılcım bekleniyor")]
    [InlineData(false, 0, 1, "Deplasman köşesi ayakta")]
    [InlineData(false, 1, 0, "skoru sahiplenmiş")]
    [InlineData(false, 1, 1, "deplasman köşesi direniyor")]
    public void HalfTimeBeat_UsesScoreAndVenueContext(
        bool managedIsHome,
        int homeGoals,
        int awayGoals,
        string expected)
    {
        var moments = Enumerable
            .Range(0, homeGoals)
            .Select(index => Moment(MatchKeyMomentKind.Goal, 10 + index, isHomeSide: true))
            .Concat(Enumerable
                .Range(0, awayGoals)
                .Select(index => Moment(MatchKeyMomentKind.Goal, 30 + index, isHomeSide: false)))
            .ToArray();

        var digest = CrowdReactionDigest.Compose(managedIsHome, moments);

        Assert.StartsWith("45' Tribün ·", digest.HalfTimeBeat, StringComparison.Ordinal);
        Assert.Contains(expected, digest.HalfTimeBeat, StringComparison.Ordinal);
    }

    [Fact]
    public void NoMoments_StillCreatesNilNilHalfTimeBeat()
    {
        var digest = CrowdReactionDigest.Compose(managedIsHome: true, keyMoments: null);

        Assert.Empty(digest.MomentBeats);
        Assert.Contains("kıvılcım bekleniyor", digest.HalfTimeBeat, StringComparison.Ordinal);
    }

    [Fact]
    public void SecondHalfGoals_DoNotRewriteHalfTimeCrowdState()
    {
        var digest = CrowdReactionDigest.Compose(
            managedIsHome: true,
            [Moment(MatchKeyMomentKind.Goal, 70, isHomeSide: false)]);

        Assert.Contains("kıvılcım bekleniyor", digest.HalfTimeBeat, StringComparison.Ordinal);
    }

    [Fact]
    public void Compose_IsDeterministicForSameMomentSet()
    {
        MatchKeyMomentReadModel[] moments =
        [
            Moment(MatchKeyMomentKind.Goal, 14, isHomeSide: false),
            Moment(MatchKeyMomentKind.RedCard, 67, isHomeSide: true),
        ];

        var first = CrowdReactionDigest.Compose(managedIsHome: true, moments);
        var second = CrowdReactionDigest.Compose(managedIsHome: true, moments);

        Assert.Equal(first.BrandTitle, second.BrandTitle);
        Assert.Equal(first.HalfTimeBeat, second.HalfTimeBeat);
        Assert.True(first.MomentBeats.SequenceEqual(second.MomentBeats));
    }

    private static MatchKeyMomentReadModel Moment(
        MatchKeyMomentKind kind,
        int minute,
        bool isHomeSide) =>
        new(
            kind.ToString(),
            minute,
            isHomeSide,
            PrimarySlotIndex: 1);
}
