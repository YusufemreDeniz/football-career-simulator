using FootballCareerSimulator.Domain.ClubGovernance;
using FootballCareerSimulator.Domain.Competition;

namespace FootballCareerSimulator.Tests.ClubGovernance;

public sealed class MvpLeagueCatalogTests
{
    [Fact]
    public void CreateClubs_ReturnsEighteenRealSuperLigClubsWithUniqueNamesAndCodes()
    {
        var clubs = MvpLeagueCatalog.CreateClubs();

        Assert.Equal(CompetitionMvpConstraints.LeagueTeamCount, clubs.Count);
        Assert.Equal(CompetitionMvpConstraints.LeagueTeamCount, clubs.Select(club => club.DisplayName).Distinct().Count());
        Assert.Equal(CompetitionMvpConstraints.LeagueTeamCount, clubs.Select(club => club.Code.Value).Distinct().Count());
        Assert.Collection(
            clubs,
            club => Assert.Equal("GALATASARAY A.Ş.", club.DisplayName),
            club => Assert.Equal("FENERBAHÇE A.Ş.", club.DisplayName),
            club => Assert.Equal("BEŞİKTAŞ A.Ş.", club.DisplayName),
            club => Assert.Equal("TRABZONSPOR A.Ş.", club.DisplayName),
            club => Assert.Equal("İSTANBUL BAŞAKŞEHİR FK", club.DisplayName),
            club => Assert.Equal("GÖZTEPE A.Ş.", club.DisplayName),
            club => Assert.Equal("SAMSUNSPOR A.Ş.", club.DisplayName),
            club => Assert.Equal("ÇAYKUR RİZESPOR A.Ş.", club.DisplayName),
            club => Assert.Equal("CORENDON ALANYASPOR", club.DisplayName),
            club => Assert.Equal("KONYASPOR", club.DisplayName),
            club => Assert.Equal("KASIMPAŞA A.Ş.", club.DisplayName),
            club => Assert.Equal("GAZİANTEP FUTBOL KULÜBÜ A.Ş.", club.DisplayName),
            club => Assert.Equal("KOCAELİSPOR", club.DisplayName),
            club => Assert.Equal("GENÇLERBİRLİĞİ", club.DisplayName),
            club => Assert.Equal("EYÜPSPOR", club.DisplayName),
            club => Assert.Equal("ERZURUMSPOR FK", club.DisplayName),
            club => Assert.Equal("AMED SPORTİF FAALİYETLER", club.DisplayName),
            club => Assert.Equal("ÇORUM FK", club.DisplayName));
    }
}
