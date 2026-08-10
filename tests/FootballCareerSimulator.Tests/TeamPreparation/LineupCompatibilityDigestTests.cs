using FootballCareerSimulator.Application.TeamPreparation.Queries;
using FootballCareerSimulator.Domain.TeamPreparation;
using FootballCareerSimulator.Simulation.TeamPreparation;

namespace FootballCareerSimulator.Tests.TeamPreparation;

public sealed class LineupCompatibilityDigestTests
{
    [Fact]
    public void Compose_NaturalFourFourTwo_ReturnsFullCompatibility()
    {
        var digest = LineupCompatibilityDigest.Compose(
            Formation.F442,
            Enumerable.Range(0, 11).ToArray(),
            BalancedFourFourTwo());

        Assert.True(digest.HasLineup);
        Assert.Equal(100, digest.Score);
        Assert.Equal(11, digest.NaturalFitCount);
        Assert.Equal(LineupCompatibilitySignal.Strong, digest.Signal);
        Assert.Equal("KL 1/1 · DEF 4/4 · ORT 4/4 · HÜC 2/2", digest.BalanceLine);
        Assert.All(digest.Players, player => Assert.True(player.IsNaturalFit));
    }

    [Fact]
    public void Compose_ChangingToFourThreeThree_IdentifiesMissingForward()
    {
        var digest = LineupCompatibilityDigest.Compose(
            Formation.F433,
            Enumerable.Range(0, 11).ToArray(),
            BalancedFourFourTwo());

        Assert.Equal(91, digest.Score);
        Assert.Equal(10, digest.NaturalFitCount);
        Assert.Equal(LineupCompatibilitySignal.Watch, digest.Signal);
        Assert.Contains("Eksik: 1 HÜC", digest.DetailLine);
        Assert.Single(digest.Players, player => !player.IsNaturalFit);
    }

    [Fact]
    public void Compose_UnbalancedSelection_ReturnsRiskSignal()
    {
        var squad = Enumerable.Range(0, 3)
            .Select(index => Profile($"Kaleci {index}", MvpSquadPositionGroup.Goalkeeper))
            .Concat(Enumerable.Range(0, 8)
                .Select(index => Profile($"Savunma {index}", MvpSquadPositionGroup.Defender)))
            .ToArray();

        var digest = LineupCompatibilityDigest.Compose(
            Formation.F442,
            Enumerable.Range(0, 11).ToArray(),
            squad);

        Assert.Equal(45, digest.Score);
        Assert.Equal(LineupCompatibilitySignal.Risk, digest.Signal);
        Assert.Contains("Eksik: 4 ORT, 2 HÜC", digest.DetailLine);
    }

    [Fact]
    public void Compose_EmptySelection_ReturnsClearDigest()
    {
        var digest = LineupCompatibilityDigest.Compose(
            Formation.F442,
            Array.Empty<int>(),
            BalancedFourFourTwo());

        Assert.False(digest.HasLineup);
    }

    private static IReadOnlyList<MvpSquadPlayerProfile> BalancedFourFourTwo() =>
    [
        Profile("Kaleci", MvpSquadPositionGroup.Goalkeeper),
        Profile("Defans 1", MvpSquadPositionGroup.Defender),
        Profile("Defans 2", MvpSquadPositionGroup.Defender),
        Profile("Defans 3", MvpSquadPositionGroup.Defender),
        Profile("Defans 4", MvpSquadPositionGroup.Defender),
        Profile("Orta 1", MvpSquadPositionGroup.Midfielder),
        Profile("Orta 2", MvpSquadPositionGroup.Midfielder),
        Profile("Orta 3", MvpSquadPositionGroup.Midfielder),
        Profile("Orta 4", MvpSquadPositionGroup.Midfielder),
        Profile("Forvet 1", MvpSquadPositionGroup.Forward),
        Profile("Forvet 2", MvpSquadPositionGroup.Forward),
    ];

    private static MvpSquadPlayerProfile Profile(
        string name,
        MvpSquadPositionGroup position) =>
        new(name, position);
}
