using FootballCareerSimulator.Domain.Competition;
using FootballCareerSimulator.Domain.Shared;

namespace FootballCareerSimulator.Tests.Competition;

public class CompetitionIdentityTests
{
    [Theory]
    [InlineData(1)]
    [InlineData(20)]
    public void ClubId_AcceptsPositiveValues(long value)
    {
        var id = new ClubId(value);
        Assert.Equal(value, id.Value);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void ClubId_RejectsNonPositiveValues(long value)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new ClubId(value));
    }

    [Fact]
    public void CompetitionId_IsComparable()
    {
        Assert.True(new CompetitionId(1).CompareTo(new CompetitionId(2)) < 0);
    }

    [Fact]
    public void SeasonId_IsComparable()
    {
        Assert.True(new SeasonId(2026).CompareTo(new SeasonId(2027)) < 0);
    }

    [Fact]
    public void FixtureId_RejectsNonPositiveValues()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new FixtureId(0));
    }
}
