namespace FootballCareerSimulator.Domain.ClubGovernance;

using FootballCareerSimulator.Domain.Shared;

/// <summary>
/// MVP 20 kulüplük sabit katalog (fictional isimler).
/// </summary>
public static class MvpLeagueCatalog
{
    private static readonly (string Name, string Code, int Strength)[] Clubs =
    [
        ("Boğaziçi Spor", "BOS", 78),
        ("Anadolu Yıldızı", "AYI", 74),
        ("Ege Kartalı", "EGE", 71),
        ("Karadeniz FK", "KAR", 69),
        ("Akdeniz United", "AKD", 67),
        ("Trakya Spor", "TRA", 65),
        ("Kapadokya SK", "KAP", 63),
        ("Marmara 1907", "MAR", 72),
        ("Toros Spor", "TOR", 61),
        ("Çukurova FK", "CUK", 59),
        ("Kuzey Yıldızı", "KUZ", 57),
        ("Güney Kartalı", "GUN", 55),
        ("İpekyolu SK", "IPE", 53),
        ("Yıldırım Spor", "YIL", 68),
        ("Sahil United", "SAH", 51),
        ("Dağlıca FK", "DAG", 49),
        ("Ova Spor", "OVA", 47),
        ("Vadi SK", "VAD", 45),
        ("Kıyı Yıldızı", "KIY", 52),
        ("Merkez FK", "MER", 50),
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
