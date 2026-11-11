using FootballCareerSimulator.Simulation.TeamPreparation;

namespace FootballCareerSimulator.Tests.TeamPreparation;

public sealed class OpponentMatchPlanResolverTests
{
    [Fact]
    public void CongestedMidTableMatch_RotatesThreePlayersDeterministically()
    {
        var input = Input(daysSincePreviousMatch: 3, position: 9, round: 10);

        var first = OpponentMatchPlanResolver.Resolve(input);
        var second = OpponentMatchPlanResolver.Resolve(input);

        Assert.Equal(OpponentMatchPriority.Rotation, first.Priority);
        Assert.Equal(3, first.RotationCount);
        Assert.Equal(11, first.StartingSlots.Count);
        Assert.Equal(3, first.StartingSlots.Count(slot => slot >= 11));
        Assert.Equal(first.Priority, second.Priority);
        Assert.Equal(first.Intent, second.Intent);
        Assert.Equal(first.StartingSlots, second.StartingSlots);
        Assert.Equal(first.MatchStrengthModifier, second.MatchStrengthModifier);
    }

    [Fact]
    public void LateTitleRaceMatch_UsesMustWinPlanWithoutRotation()
    {
        var plan = OpponentMatchPlanResolver.Resolve(
            Input(daysSincePreviousMatch: 3, position: 2, round: 28, clubStrength: 78, opponentStrength: 65));

        Assert.Equal(OpponentMatchPriority.MustWin, plan.Priority);
        Assert.Equal(OpponentTacticalIntent.AggressivePress, plan.Intent);
        Assert.Equal(0, plan.RotationCount);
        Assert.Equal(2, plan.MatchStrengthModifier);
    }

    [Fact]
    public void Underdog_ChoosesCompactCounter()
    {
        var plan = OpponentMatchPlanResolver.Resolve(
            Input(daysSincePreviousMatch: 7, position: 8, round: 8, clubStrength: 55, opponentStrength: 75));

        Assert.Equal(OpponentTacticalIntent.CompactCounter, plan.Intent);
        Assert.Contains("kompakt", plan.Headline, StringComparison.Ordinal);
    }

    private static OpponentMatchPlanInput Input(
        int daysSincePreviousMatch,
        int position,
        int round,
        int clubStrength = 65,
        int opponentStrength = 65) =>
        new(
            ClubId: 2,
            FixtureId: 91,
            Round: round,
            LeagueSize: 18,
            LeaguePosition: position,
            ClubStrength: clubStrength,
            OpponentStrength: opponentStrength,
            DaysSincePreviousMatch: daysSincePreviousMatch,
            RootSeed: 913,
            AvailableSlots: Enumerable.Range(0, 25).ToArray());
}
