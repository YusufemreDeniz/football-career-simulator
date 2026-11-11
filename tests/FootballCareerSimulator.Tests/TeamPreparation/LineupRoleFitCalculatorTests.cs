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

    [Fact]
    public void ComputeOptimalPitchSlots_ThreeFiveTwo_PutsPlayersInAppropriateLines()
    {
        // 1 GK, 3 CB, 5 MID (including wingers/CM), 2 ST
        var squad = new List<MvpSquadPlayerProfile>
        {
            Player("GK", MvpSquadPositionRole.Goalkeeper),
            Player("CB 1", MvpSquadPositionRole.CentreBack),
            Player("CB 2", MvpSquadPositionRole.CentreBack),
            Player("CB 3", MvpSquadPositionRole.CentreBack),
            Player("LWB", MvpSquadPositionRole.LeftMidfielder),
            Player("RWB", MvpSquadPositionRole.RightMidfielder),
            Player("CM 1", MvpSquadPositionRole.CentralMidfielder),
            Player("CM 2", MvpSquadPositionRole.DefensiveMidfielder),
            Player("CAM", MvpSquadPositionRole.AttackingMidfielder),
            Player("ST 1", MvpSquadPositionRole.Striker),
            Player("ST 2", MvpSquadPositionRole.Striker),
        };

        // Shuffle deliberately
        var shuffled = squad.OrderBy(_ => Guid.NewGuid()).ToList();
        var slotMap = MvpLineupRoleFitCalculator.ComputeOptimalPitchSlots(Formation.F352, shuffled);

        Assert.Equal(11, slotMap.Length);
        var aligned = slotMap.Select(idx => shuffled[idx]).ToList();

        // GK must be slot 0
        Assert.Equal(MvpSquadPositionRole.Goalkeeper, aligned[0].PositionRole);
        // Slots 1..3 must be defenders (CB)
        for (var i = 1; i <= 3; i++)
        {
            Assert.Equal(MvpSquadPositionGroup.Defender, aligned[i].PositionGroup);
        }
        // Slots 4..8 must be midfielders
        for (var i = 4; i <= 8; i++)
        {
            Assert.Equal(MvpSquadPositionGroup.Midfielder, aligned[i].PositionGroup);
        }
        // Slots 9..10 must be forwards
        for (var i = 9; i <= 10; i++)
        {
            Assert.Equal(MvpSquadPositionGroup.Forward, aligned[i].PositionGroup);
        }
    }

    [Fact]
    public void ComputeOptimalPitchSlots_FourThreeThree_PutsAttackersAtFrontAndKeepersInGoal()
    {
        var squad = new List<MvpSquadPlayerProfile>
        {
            Player("GK", MvpSquadPositionRole.Goalkeeper),
            Player("LB", MvpSquadPositionRole.LeftBack),
            Player("CB 1", MvpSquadPositionRole.CentreBack),
            Player("CB 2", MvpSquadPositionRole.CentreBack),
            Player("RB", MvpSquadPositionRole.RightBack),
            Player("DM", MvpSquadPositionRole.DefensiveMidfielder),
            Player("CM 1", MvpSquadPositionRole.CentralMidfielder),
            Player("CM 2", MvpSquadPositionRole.AttackingMidfielder),
            Player("LW", MvpSquadPositionRole.LeftWinger),
            Player("RW", MvpSquadPositionRole.RightWinger),
            Player("ST", MvpSquadPositionRole.Striker),
        };

        var reversed = squad.AsEnumerable().Reverse().ToList();
        var slotMap = MvpLineupRoleFitCalculator.ComputeOptimalPitchSlots(Formation.F433, reversed);

        Assert.Equal(11, slotMap.Length);
        var aligned = slotMap.Select(idx => reversed[idx]).ToList();

        Assert.Equal(MvpSquadPositionRole.Goalkeeper, aligned[0].PositionRole);
        for (var i = 1; i <= 4; i++)
        {
            Assert.Equal(MvpSquadPositionGroup.Defender, aligned[i].PositionGroup);
        }
        for (var i = 5; i <= 7; i++)
        {
            Assert.Equal(MvpSquadPositionGroup.Midfielder, aligned[i].PositionGroup);
        }
        for (var i = 8; i <= 10; i++)
        {
            Assert.Equal(MvpSquadPositionGroup.Forward, aligned[i].PositionGroup);
        }
    }

    [Fact]
    public void EvaluatePitchRoleFit_ScoresReflectPositionalFidelity()
    {
        var gk = Player("GK", MvpSquadPositionRole.Goalkeeper);
        var st = Player("ST", MvpSquadPositionRole.Striker);
        var cb = Player("CB", MvpSquadPositionRole.CentreBack);

        var gkInGk = MvpLineupRoleFitCalculator.EvaluatePitchRoleFit(gk, MvpSquadPositionRole.Goalkeeper);
        var gkInSt = MvpLineupRoleFitCalculator.EvaluatePitchRoleFit(gk, MvpSquadPositionRole.Striker);
        var cbInCb = MvpLineupRoleFitCalculator.EvaluatePitchRoleFit(cb, MvpSquadPositionRole.CentreBack);
        var cbInLb = MvpLineupRoleFitCalculator.EvaluatePitchRoleFit(cb, MvpSquadPositionRole.LeftBack);

        Assert.True(gkInGk >= 100);
        Assert.True(gkInSt <= 5);
        Assert.True(cbInCb >= 100);
        Assert.True(cbInLb > gkInSt);
    }

    [Fact]
    public void AllFourteenFormations_HaveValidElevenRolesAndPitchCoordinates()
    {
        var allFormations = Enum.GetValues<Formation>();
        Assert.Equal(14, allFormations.Length);

        foreach (var formation in allFormations)
        {
            var required = MvpLineupRoleFitCalculator.RequiredRolesFor(formation);
            Assert.Equal(11, required.Count);
            Assert.Equal(MvpSquadPositionRole.Goalkeeper, required[0]);

            var pitchRoles = MvpLineupRoleFitCalculator.PitchRolesFor(formation);
            Assert.Equal(11, pitchRoles.Count);
            Assert.Equal(MvpSquadPositionRole.Goalkeeper, pitchRoles[0]);
        }
    }

    [Fact]
    public void ComputeOptimalPitchSlots_FourTwoThreeOne_PutsPlayersInAccurateLines()
    {
        var squad = new List<MvpSquadPlayerProfile>
        {
            Player("GK", MvpSquadPositionRole.Goalkeeper),
            Player("LB", MvpSquadPositionRole.LeftBack),
            Player("CB 1", MvpSquadPositionRole.CentreBack),
            Player("CB 2", MvpSquadPositionRole.CentreBack),
            Player("RB", MvpSquadPositionRole.RightBack),
            Player("DM 1", MvpSquadPositionRole.DefensiveMidfielder),
            Player("DM 2", MvpSquadPositionRole.DefensiveMidfielder),
            Player("LM", MvpSquadPositionRole.LeftWinger),
            Player("CAM", MvpSquadPositionRole.AttackingMidfielder),
            Player("RM", MvpSquadPositionRole.RightWinger),
            Player("ST", MvpSquadPositionRole.Striker),
        };

        var shuffled = squad.OrderBy(_ => Guid.NewGuid()).ToList();
        var slotMap = MvpLineupRoleFitCalculator.ComputeOptimalPitchSlots(Formation.F4231, shuffled);

        Assert.Equal(11, slotMap.Length);
        var aligned = slotMap.Select(idx => shuffled[idx]).ToList();

        Assert.Equal(MvpSquadPositionRole.Goalkeeper, aligned[0].PositionRole);
        for (var i = 1; i <= 4; i++)
        {
            Assert.Equal(MvpSquadPositionGroup.Defender, aligned[i].PositionGroup);
        }
        Assert.Equal(MvpSquadPositionGroup.Midfielder, aligned[5].PositionGroup);
        Assert.Equal(MvpSquadPositionGroup.Midfielder, aligned[6].PositionGroup);
        Assert.Equal(MvpSquadPositionGroup.Forward, aligned[7].PositionGroup);
        Assert.Equal(MvpSquadPositionGroup.Midfielder, aligned[8].PositionGroup);
        Assert.Equal(MvpSquadPositionGroup.Forward, aligned[9].PositionGroup);
        Assert.Equal(MvpSquadPositionGroup.Forward, aligned[10].PositionGroup);
    }

    [Fact]
    public void ComputeOptimalPitchSlots_FiveThreeTwo_PutsFiveDefenders()
    {
        var squad = new List<MvpSquadPlayerProfile>
        {
            Player("GK", MvpSquadPositionRole.Goalkeeper),
            Player("LWB", MvpSquadPositionRole.LeftBack),
            Player("CB 1", MvpSquadPositionRole.CentreBack),
            Player("CB 2", MvpSquadPositionRole.CentreBack),
            Player("CB 3", MvpSquadPositionRole.CentreBack),
            Player("RWB", MvpSquadPositionRole.RightBack),
            Player("CM 1", MvpSquadPositionRole.CentralMidfielder),
            Player("CM 2", MvpSquadPositionRole.DefensiveMidfielder),
            Player("CAM", MvpSquadPositionRole.AttackingMidfielder),
            Player("ST 1", MvpSquadPositionRole.Striker),
            Player("ST 2", MvpSquadPositionRole.Striker),
        };

        var reversed = squad.AsEnumerable().Reverse().ToList();
        var slotMap = MvpLineupRoleFitCalculator.ComputeOptimalPitchSlots(Formation.F532, reversed);

        Assert.Equal(11, slotMap.Length);
        var aligned = slotMap.Select(idx => reversed[idx]).ToList();

        Assert.Equal(MvpSquadPositionRole.Goalkeeper, aligned[0].PositionRole);
        for (var i = 1; i <= 5; i++)
        {
            Assert.Equal(MvpSquadPositionGroup.Defender, aligned[i].PositionGroup);
        }
        for (var i = 6; i <= 8; i++)
        {
            Assert.Equal(MvpSquadPositionGroup.Midfielder, aligned[i].PositionGroup);
        }
        for (var i = 9; i <= 10; i++)
        {
            Assert.Equal(MvpSquadPositionGroup.Forward, aligned[i].PositionGroup);
        }
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
