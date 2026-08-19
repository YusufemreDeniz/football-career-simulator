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
