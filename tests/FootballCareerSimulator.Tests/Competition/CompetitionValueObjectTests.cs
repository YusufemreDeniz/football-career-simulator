using FootballCareerSimulator.Domain.Competition;

namespace FootballCareerSimulator.Tests.Competition;

public class CompetitionValueObjectTests
{
    [Fact]
    public void Points_RejectsNegativeValues()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new Points(-1));
    }

    [Fact]
    public void Points_Add_IsCumulative()
    {
        var total = new Points(1).Add(new Points(3));

        Assert.Equal(4, total.Value);
    }

    [Fact]
    public void GoalDifference_AllowsNegativeValues()
    {
        var difference = new GoalDifference(-5).Add(new GoalDifference(7));

        Assert.Equal(2, difference.Value);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(34)]
    public void FixtureRound_AcceptsMvpLeagueRange(int round)
    {
        var fixtureRound = new FixtureRound(round);

        Assert.Equal(round, fixtureRound.Value);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(35)]
    public void FixtureRound_RejectsOutOfRangeValues(int round)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new FixtureRound(round));
    }

    [Theory]
    [InlineData(1)]
    [InlineData(18)]
    public void CompetitionPosition_AcceptsMvpLeagueRange(int position)
    {
        var competitionPosition = new CompetitionPosition(position);

        Assert.Equal(position, competitionPosition.Value);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(19)]
    public void CompetitionPosition_RejectsOutOfRangeValues(int position)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new CompetitionPosition(position));
    }

    [Fact]
    public void MvpConstraints_MatchLeagueSpecification()
    {
        Assert.Equal(18, CompetitionMvpConstraints.LeagueTeamCount);
        Assert.Equal(34, CompetitionMvpConstraints.LeagueMatchesPerTeam);
    }
}
