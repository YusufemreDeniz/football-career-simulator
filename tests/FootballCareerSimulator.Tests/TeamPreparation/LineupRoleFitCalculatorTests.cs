using FootballCareerSimulator.Domain.TeamPreparation;
using FootballCareerSimulator.Simulation.TeamPreparation;

namespace FootballCareerSimulator.Tests.TeamPreparation;

public sealed class LineupRoleFitCalculatorTests
{
    [Fact]
    public void Evaluate_NaturalFourFourTwo_ProducesPositiveMatchModifier()
    {
        var fit = MvpLineupRoleFitCalculator.Evaluate(
            Formation.F442,
            NaturalFourFourTwo());

        Assert.Equal(100, fit.Score);
        Assert.Equal(11, fit.NaturalFitCount);
        Assert.Equal(2, fit.MatchStrengthModifier);
        Assert.All(fit.PlayerNaturalFits, Assert.True);
    }

    [Fact]
    public void Evaluate_OutOfPositionChoice_ReducesMatchModifier()
    {
        var lineup = NaturalFourFourTwo().ToArray();
        lineup[1] = new MvpSquadPlayerProfile("Yedek Kaleci", MvpSquadPositionRole.Goalkeeper);

        var fit = MvpLineupRoleFitCalculator.Evaluate(Formation.F442, lineup);

        Assert.Equal(91, fit.Score);
        Assert.Equal(10, fit.NaturalFitCount);
        Assert.Equal(1, fit.MatchStrengthModifier);
        Assert.False(fit.PlayerNaturalFits[1]);
    }

    private static IReadOnlyList<MvpSquadPlayerProfile> NaturalFourFourTwo() =>
    [
        Player("Kaleci", MvpSquadPositionRole.Goalkeeper),
        Player("Sağ Bek", MvpSquadPositionRole.RightBack),
        Player("Stoper 1", MvpSquadPositionRole.CentreBack),
        Player("Stoper 2", MvpSquadPositionRole.CentreBack),
        Player("Sol Bek", MvpSquadPositionRole.LeftBack),
        Player("Sağ Orta", MvpSquadPositionRole.RightMidfielder),
        Player("Merkez 1", MvpSquadPositionRole.CentralMidfielder),
        Player("Merkez 2", MvpSquadPositionRole.DefensiveMidfielder),
        Player("Sol Orta", MvpSquadPositionRole.LeftMidfielder),
        Player("Santrfor 1", MvpSquadPositionRole.Striker),
        Player("Santrfor 2", MvpSquadPositionRole.Striker),
    ];

    private static MvpSquadPlayerProfile Player(string name, MvpSquadPositionRole role) =>
        new(name, role);
}
