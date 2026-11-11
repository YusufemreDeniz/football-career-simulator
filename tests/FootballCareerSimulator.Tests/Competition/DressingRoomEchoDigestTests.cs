using FootballCareerSimulator.Application.Competition.Queries;
using FootballCareerSimulator.Domain.Competition;

namespace FootballCareerSimulator.Tests.Competition;

public sealed class DressingRoomEchoDigestTests
{
    private static readonly IReadOnlyDictionary<long, string> Clubs =
        new Dictionary<long, string> { [1] = "Biz", [2] = "Rakip A", [3] = "Rakip B" };

    [Fact]
    public void NoCompletedManagedMatch_ReturnsNull()
    {
        var digest = DressingRoomEchoDigest.Compose(
            [Fixture(1, 10, 1, 2, FixtureStatus.Planned, null, null)],
            managedClubId: 1,
            managedSinceDayNumber: 1,
            Clubs);

        Assert.Null(digest);
    }

    [Fact]
    public void LatestManagedResult_BecomesPersistentRoomEcho()
    {
        var digest = DressingRoomEchoDigest.Compose(
            [
                Fixture(1, 10, 1, 2, FixtureStatus.ResultAccepted, 1, 0),
                Fixture(2, 17, 3, 1, FixtureStatus.ResultAccepted, 1, 3),
            ],
            managedClubId: 1,
            managedSinceDayNumber: 1,
            Clubs);

        Assert.NotNull(digest);
        Assert.Equal(2, digest!.FixtureId);
        Assert.Contains("Rakip B", digest.Headline, StringComparison.Ordinal);
        Assert.Contains("3-1 galibiyet", digest.Headline, StringComparison.Ordinal);
        Assert.Contains("sallanarak", digest.VoiceLine, StringComparison.Ordinal);
        Assert.Equal("Form (eski→yeni): G-G · 6/6 puan", digest.MomentumLine);
        Assert.Equal(DressingRoomEchoDigest.MomentumMixed, digest.MomentumCode);
        Assert.Equal(0, digest.MomentumLength);
    }

    [Fact]
    public void HeavyLoss_LeavesSilentDressingRoom()
    {
        var digest = DressingRoomEchoDigest.Compose(
            [Fixture(3, 20, 1, 2, FixtureStatus.ResultAccepted, 0, 4)],
            managedClubId: 1,
            managedSinceDayNumber: 1,
            Clubs);

        Assert.NotNull(digest);
        Assert.Contains("0-4 mağlubiyet", digest!.Headline, StringComparison.Ordinal);
        Assert.Contains("sessizlik", digest.VoiceLine, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void OpponentResult_DoesNotLeakIntoManagedRoom()
    {
        var digest = DressingRoomEchoDigest.Compose(
            [Fixture(4, 30, 2, 3, FixtureStatus.ResultAccepted, 5, 0)],
            managedClubId: 1,
            managedSinceDayNumber: 1,
            Clubs);

        Assert.Null(digest);
    }

    [Fact]
    public void ThreeLatestWins_SurfaceWinningStreak()
    {
        var digest = DressingRoomEchoDigest.Compose(
            [
                Fixture(1, 10, 1, 2, FixtureStatus.ResultAccepted, 1, 0),
                Fixture(2, 17, 3, 1, FixtureStatus.ResultAccepted, 0, 2),
                Fixture(3, 24, 1, 3, FixtureStatus.ResultAccepted, 3, 1),
            ],
            managedClubId: 1,
            managedSinceDayNumber: 1,
            Clubs);

        Assert.NotNull(digest);
        Assert.Equal("Form: G-G-G · 3 maçlık galibiyet serisi", digest!.MomentumLine);
        Assert.Equal(DressingRoomEchoDigest.MomentumWinningStreak, digest.MomentumCode);
        Assert.Equal(3, digest.MomentumLength);
    }

    [Fact]
    public void ThreeLatestLosses_SurfaceDressingRoomAlarm()
    {
        var digest = DressingRoomEchoDigest.Compose(
            [
                Fixture(1, 10, 1, 2, FixtureStatus.ResultAccepted, 0, 1),
                Fixture(2, 17, 3, 1, FixtureStatus.ResultAccepted, 2, 0),
                Fixture(3, 24, 1, 3, FixtureStatus.ResultAccepted, 1, 4),
            ],
            managedClubId: 1,
            managedSinceDayNumber: 1,
            Clubs);

        Assert.NotNull(digest);
        Assert.Equal("Form: M-M-M · 3 maçlık mağlubiyet serisi", digest!.MomentumLine);
        Assert.Equal(DressingRoomEchoDigest.MomentumLosingStreak, digest.MomentumCode);
        Assert.Equal(3, digest.MomentumLength);
    }

    [Fact]
    public void LongWinningRun_CountsBeyondTheFiveMatchDisplayWindow()
    {
        var digest = DressingRoomEchoDigest.Compose(
            [
                Fixture(1, 10, 1, 2, FixtureStatus.ResultAccepted, 1, 0),
                Fixture(2, 11, 3, 1, FixtureStatus.ResultAccepted, 0, 2),
                Fixture(3, 12, 1, 3, FixtureStatus.ResultAccepted, 3, 1),
                Fixture(4, 13, 2, 1, FixtureStatus.ResultAccepted, 0, 1),
                Fixture(5, 14, 1, 2, FixtureStatus.ResultAccepted, 2, 0),
                Fixture(6, 15, 3, 1, FixtureStatus.ResultAccepted, 1, 2),
            ],
            managedClubId: 1,
            managedSinceDayNumber: 1,
            Clubs);

        Assert.NotNull(digest);
        Assert.Equal(6, digest!.MomentumLength);
        Assert.Equal("Form: G-G-G-G-G · 6 maçlık galibiyet serisi", digest.MomentumLine);
    }

    [Fact]
    public void MatchBeforeEmployment_DoesNotBecomeManagersRoomEcho()
    {
        var digest = DressingRoomEchoDigest.Compose(
            [Fixture(5, 12, 1, 2, FixtureStatus.ResultAccepted, 4, 0)],
            managedClubId: 1,
            managedSinceDayNumber: 13,
            Clubs);

        Assert.Null(digest);
    }

    private static FixtureReadModel Fixture(
        long id,
        int day,
        long home,
        long away,
        FixtureStatus status,
        int? homeGoals,
        int? awayGoals) =>
        new(id, 1, home, away, Round: (int)id, day, $"2026-08-{day:D2}", status.ToString(), homeGoals, awayGoals);
}
