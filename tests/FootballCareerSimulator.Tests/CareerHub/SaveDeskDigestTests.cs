using FootballCareerSimulator.Application.CareerHub.Queries;

namespace FootballCareerSimulator.Tests.CareerHub;

public sealed class SaveDeskDigestTests
{
    [Fact]
    public void NoSave_AsksForFirstCheckpoint()
    {
        var digest = SaveDeskDigest.Compose(
            savePath: @"C:\saves\career_save.db",
            saveExists: false,
            saveLastWriteUtc: null,
            currentDayNumber: 12,
            currentIsoDate: "2026-08-12",
            managerDisplayName: "Yusuf",
            clubDisplayName: "Home FC",
            seasonId: 1,
            seasonStatus: "Active",
            acceptedFixtureCount: 4,
            totalFixtureCount: 30);

        Assert.False(digest.SaveExists);
        Assert.Contains("Henüz kayıt yok", digest.Headline, StringComparison.Ordinal);
        Assert.Contains(digest.BeatLines, b => b.Contains("Home FC", StringComparison.Ordinal));
        Assert.Contains(digest.BeatLines, b => b.Contains("Diskte kayıt yok", StringComparison.Ordinal));
        Assert.Contains("Öneri:", digest.ToDisplayText(), StringComparison.Ordinal);
        Assert.Contains("İlk Kaydet", digest.AdviceLine, StringComparison.Ordinal);
    }

    [Fact]
    public void ExistingSave_ShowsDiskStampAndOverwriteHint()
    {
        var stamp = new DateTimeOffset(2026, 8, 10, 14, 30, 0, TimeSpan.Zero);
        var digest = SaveDeskDigest.Compose(
            @"C:\saves\career_save.db",
            saveExists: true,
            saveLastWriteUtc: stamp,
            currentDayNumber: 20,
            currentIsoDate: "2026-08-20",
            managerDisplayName: "Yusuf",
            clubDisplayName: null,
            seasonId: null,
            seasonStatus: null,
            acceptedFixtureCount: 0,
            totalFixtureCount: 0);

        Assert.True(digest.SaveExists);
        Assert.Contains("Kayıt mevcut", digest.Headline, StringComparison.Ordinal);
        Assert.Contains(digest.BeatLines, b => b.Contains("işsiz", StringComparison.Ordinal));
        Assert.Contains(digest.BeatLines, b => b.StartsWith("Diskteki kayıt:", StringComparison.Ordinal));
        Assert.Contains("üzerine yazabilir", digest.Headline, StringComparison.OrdinalIgnoreCase);
    }
}
