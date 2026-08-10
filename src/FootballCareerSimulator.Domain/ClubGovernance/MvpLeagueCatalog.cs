namespace FootballCareerSimulator.Domain.ClubGovernance;

using FootballCareerSimulator.Domain.Shared;

/// <summary>
/// 2026-2027 Türkiye Süper Ligi'nin TFF'de kayıtlı 18 kulübü.
/// </summary>
public static class MvpLeagueCatalog
{
    private static readonly (string Name, string Code, int Strength)[] Clubs =
    [
        ("GALATASARAY A.Ş.", "GAL", 95),
        ("FENERBAHÇE A.Ş.", "FEN", 93),
        ("BEŞİKTAŞ A.Ş.", "BJK", 87),
        ("TRABZONSPOR A.Ş.", "TRA", 85),
        ("İSTANBUL BAŞAKŞEHİR FK", "IBF", 79),
        ("GÖZTEPE A.Ş.", "GOZ", 78),
        ("SAMSUNSPOR A.Ş.", "SAM", 77),
        ("ÇAYKUR RİZESPOR A.Ş.", "RIZ", 73),
        ("CORENDON ALANYASPOR", "ALA", 72),
        ("KONYASPOR", "KON", 71),
        ("KASIMPAŞA A.Ş.", "KAS", 69),
        ("GAZİANTEP FUTBOL KULÜBÜ A.Ş.", "GFK", 70),
        ("KOCAELİSPOR", "KOC", 68),
        ("GENÇLERBİRLİĞİ", "GEN", 66),
        ("EYÜPSPOR", "EYP", 67),
        ("ERZURUMSPOR FK", "ERZ", 62),
        ("AMED SPORTİF FAALİYETLER", "AME", 64),
        ("ÇORUM FK", "COR", 61),
    ];

    public static IReadOnlyList<Club> CreateClubs()
    {
        var clubs = new List<Club>(Clubs.Length);
        for (var index = 0; index < Clubs.Length; index++)
        {
            var entry = Clubs[index];
            clubs.Add(Club.Create(
                new ClubId(index + 1),
                entry.Name,
                new ClubCode(entry.Code),
                entry.Strength));
        }

        return clubs;
    }
}
