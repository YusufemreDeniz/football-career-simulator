using FootballCareerSimulator.Application.CareerHub.Queries;
using FootballCareerSimulator.Simulation.TeamPreparation;

namespace FootballCareerSimulator.Tests.CareerHub;

public sealed class ScoutTransferDigestTests
{
    [Fact]
    public void Compose_PrioritizesWeakestPositionGroupAndNarrowsWatchedCandidateRange()
    {
        var profiles = Enumerable.Range(0, 25)
            .Select(slot => new MvpSquadPlayerProfile(
                $"Oyuncu {slot}",
                slot < 2
                    ? MvpSquadPositionGroup.Goalkeeper
                    : slot < 10
                        ? MvpSquadPositionGroup.Defender
                        : slot < 18
                            ? MvpSquadPositionGroup.Midfielder
                            : MvpSquadPositionGroup.Forward))
            .ToArray();
        var ratings = Enumerable.Range(0, 25).ToDictionary(slot => slot, _ => 76);
        var candidates = new[]
        {
            new ScoutCandidateSource(2001, 2, "Rakip", "İzlenen Kaleci", MvpSquadPositionGroup.Goalkeeper, "KL", 78, 23, 86, 90, true),
            new ScoutCandidateSource(3001, 3, "Rakip 2", "Yeni Kaleci", MvpSquadPositionGroup.Goalkeeper, "KL", 79, 25, 83, null, false),
            new ScoutCandidateSource(3002, 3, "Rakip 2", "Stoper", MvpSquadPositionGroup.Defender, "STP", 90, 25, 92, null, false),
        };

        var digest = ScoutTransferDigest.Compose("Kulüp", 100, profiles, ratings, candidates);

        Assert.Equal("KL", digest.NeedPositionCode);
        Assert.Equal(2, digest.Candidates.Count);
        Assert.Equal("İzlenen Kaleci", digest.Candidates[0].DisplayName);
        Assert.True(digest.Candidates[0].KnowledgePercent > digest.Candidates[1].KnowledgePercent);
        Assert.True(
            digest.Candidates[0].EstimatedAbilityHigh - digest.Candidates[0].EstimatedAbilityLow
            < digest.Candidates[1].EstimatedAbilityHigh - digest.Candidates[1].EstimatedAbilityLow);
    }
}
