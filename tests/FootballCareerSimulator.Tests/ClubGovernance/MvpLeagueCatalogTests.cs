using FootballCareerSimulator.Domain.ClubGovernance;
using FootballCareerSimulator.Domain.Competition;

namespace FootballCareerSimulator.Tests.ClubGovernance;

public sealed class MvpLeagueCatalogTests
{
    [Fact]
    public void CreateClubs_ReturnsTwentyUniqueNamesAndCodes()
    {
        var clubs = MvpLeagueCatalog.CreateClubs();

        Assert.Equal(CompetitionMvpConstraints.LeagueTeamCount, clubs.Count);
        Assert.Equal(CompetitionMvpConstraints.LeagueTeamCount, clubs.Select(club => club.DisplayName).Distinct().Count());
        Assert.Equal(CompetitionMvpConstraints.LeagueTeamCount, clubs.Select(club => club.Code.Value).Distinct().Count());
        Assert.DoesNotContain(clubs, club => club.DisplayName.StartsWith("Kulüp ", StringComparison.Ordinal));
    }
}
