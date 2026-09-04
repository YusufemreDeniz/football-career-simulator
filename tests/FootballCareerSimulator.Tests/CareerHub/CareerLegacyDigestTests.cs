using FootballCareerSimulator.Application.CareerHub.Queries;

namespace FootballCareerSimulator.Tests.CareerHub;

public sealed class CareerLegacyDigestTests
{
    [Fact]
    public void Compose_AggregatesMultipleSeasonsAndChoosesNextMilestone()
    {
        var seasons = new[]
        {
            new CareerSeasonLegacySource(1, "Archived", 3, 18, 34, 20, 8, 6, 68, 60, 31),
            new CareerSeasonLegacySource(2, "Active", 1, 18, 20, 12, 5, 3, 41, 38, 18),
        };

        var digest = CareerLegacyDigest.Compose(
            "Teknik Direktör",
            "Kulüp",
            tenureDays: 500,
            reputation: 64,
            boardConfidence: 72,
            developedPlayerCount: 9,
            averageSquadAge: 25,
            expiringContractCount: 3,
            seasons);

        Assert.True(digest.HasCareer);
        Assert.Contains("54 maç", digest.RecordLine, StringComparison.Ordinal);
        Assert.Contains("32G", digest.RecordLine, StringComparison.Ordinal);
        Assert.Contains("50 galibiyet", digest.NextMilestoneLine, StringComparison.Ordinal);
        Assert.Equal(2, digest.Seasons.Count);
        Assert.Contains("1/18. sıra", digest.Seasons[0].Finish, StringComparison.Ordinal);
    }

    [Fact]
    public void WithoutActiveEmployment_KeepsCompletedClubHistoryVisible()
    {
        var digest = CareerLegacyDigest.WithoutActiveEmployment(
            "Teknik Direktör",
            reputation: 58,
            [new CareerEmploymentLegacySource(
                "Eski Kulüp",
                StartedDayNumber: 100,
                EndedDayNumber: 500,
                EndReason: "Dismissed",
                BoardConfidence: 27)]);

        Assert.True(digest.HasCareer);
        Assert.Contains("işsiz", digest.Headline, StringComparison.Ordinal);
        var employment = Assert.Single(digest.Employments);
        Assert.Contains("Eski Kulüp", employment.ToDisplayText(), StringComparison.Ordinal);
        Assert.Contains("Görevden alındı", employment.Outcome, StringComparison.Ordinal);
    }
}
