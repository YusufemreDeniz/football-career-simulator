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
            new ScoutCandidateSource(2001, 2, "Rakip", "İzlenen Kaleci", MvpSquadPositionGroup.Goalkeeper, "KL", 78, 23, 86, 78, 72, 90, true),
            new ScoutCandidateSource(3001, 3, "Rakip 2", "Yeni Kaleci", MvpSquadPositionGroup.Goalkeeper, "KL", 79, 25, 83, 78, 75, null, false),
            new ScoutCandidateSource(3002, 3, "Rakip 2", "Stoper", MvpSquadPositionGroup.Defender, "STP", 90, 25, 92, 78, 82, null, false),
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

    [Fact]
    public void Valuation_UsesAbilityPotentialAgeAndClubContext()
    {
        var star = ScoutTransferValuationModel.Evaluate(85, 88, 27, 80, 75, 76, 50_000_000);
        var veteran = ScoutTransferValuationModel.Evaluate(75, 75, 34, 68, 75, 72, 8_000_000);

        Assert.True(star.MarketValue > veteran.MarketValue);
        Assert.True(star.SuggestedWeeklyWage > veteran.SuggestedWeeklyWage);
        Assert.Equal("Kilit oyuncu", star.RecommendedSquadRole);
        Assert.True(star.InterestPercent > veteran.InterestPercent);
        Assert.True(star.IsAffordable);
    }

    [Fact]
    public void Compose_ExposesFifaStyleInterestRoleAndAffordability()
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
        var ratings = Enumerable.Range(0, 25).ToDictionary(slot => slot, _ => 70);
        var source = new ScoutCandidateSource(
            2001, 2, "Rakip", "Yıldız Kaleci", MvpSquadPositionGroup.Goalkeeper, "KL",
            82, 24, 86, 78, 70, null, false);

        var candidate = Assert.Single(ScoutTransferDigest.Compose(
            "Kulüp", 100, profiles, ratings, [source], transferBudgetAvailable: 1_000_000).Candidates);

        Assert.Equal("Kilit oyuncu", candidate.RecommendedSquadRole);
        Assert.Contains(candidate.InterestLabel, candidate.ToListLabel(), StringComparison.Ordinal);
        Assert.Contains("BÜTÇE ÜSTÜ", candidate.ToListLabel(), StringComparison.Ordinal);
        Assert.Contains("Açılış teklifi", candidate.ToDetailText(), StringComparison.Ordinal);
        Assert.Contains("Neden önerildi?", candidate.ToDetailText(), StringComparison.Ordinal);
    }
}
