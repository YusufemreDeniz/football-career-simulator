using FootballCareerSimulator.Domain.ClubGovernance;
using FootballCareerSimulator.Domain.Competition;
using FootballCareerSimulator.Domain.Shared;

namespace FootballCareerSimulator.Tests.ClubGovernance;

public sealed class ClubRegistryTests
{
    [Fact]
    public void CreateMvpLeague_ContainsTwentyClubsWithUniqueIds()
    {
        var registry = LeagueClubRegistry.CreateMvpLeague();

        Assert.Equal(CompetitionMvpConstraints.LeagueTeamCount, registry.Clubs.Count);
        Assert.Equal(
            CompetitionMvpConstraints.LeagueTeamCount,
            registry.Clubs.Select(club => club.Id.Value).Distinct().Count());
    }

    [Fact]
    public void GetClubOrThrow_ReturnsClubForKnownId()
    {
        var registry = LeagueClubRegistry.CreateMvpLeague();
        var club = registry.GetClubOrThrow(new ClubId(5));

        Assert.Equal(5, club.Id.Value);
        Assert.Equal("K05", club.Code.Value);
        Assert.Contains("05", club.DisplayName);
    }

    [Fact]
    public void ClubCode_RejectsInvalidLength()
    {
        Assert.Throws<ClubGovernanceInvariantViolationException>(() => new ClubCode("A"));
    }
}
