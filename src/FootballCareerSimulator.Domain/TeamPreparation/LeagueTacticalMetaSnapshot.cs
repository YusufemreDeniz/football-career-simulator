namespace FootballCareerSimulator.Domain.TeamPreparation;

/// <summary>
/// Bir ligin veya turnuvanın sezonluk taktiksel meta durumunu ve hakim formasyon dağılımını temsil eder (Yenilik 4: Dinamik Taktik Ekosistemi).
/// </summary>
public sealed class LeagueTacticalMetaSnapshot
{
    public LeagueTacticalMetaSnapshot(
        string competitionCode,
        int seasonYear,
        int formation433Share,
        int formation442Share,
        int formation352Share,
        int highPressPopularity,
        TacticalApproach dominantApproach)
    {
        if (string.IsNullOrWhiteSpace(competitionCode))
        {
            throw new TeamPreparationInvariantViolationException("Competition code is required.");
        }

        CompetitionCode = competitionCode;
        SeasonYear = seasonYear;
        Formation433Share = Math.Clamp(formation433Share, 0, 100);
        Formation442Share = Math.Clamp(formation442Share, 0, 100);
        Formation352Share = Math.Clamp(formation352Share, 0, 100);
        HighPressPopularity = Math.Clamp(highPressPopularity, 0, 100);
        DominantApproach = dominantApproach;
    }

    public string CompetitionCode { get; }

    public int SeasonYear { get; }

    public int Formation433Share { get; }

    public int Formation442Share { get; }

    public int Formation352Share { get; }

    public int HighPressPopularity { get; }

    public TacticalApproach DominantApproach { get; }

    public Formation DominantFormation
    {
        get
        {
            if (Formation433Share >= Formation442Share && Formation433Share >= Formation352Share)
            {
                return Formation.F433;
            }

            if (Formation352Share >= Formation442Share)
            {
                return Formation.F352;
            }

            return Formation.F442;
        }
    }

    public static Formation GetCounterFormation(Formation formation) => formation switch
    {
        Formation.F433 => Formation.F352, // 5'li orta saha ile 3'lü merkeze üstünlük
        Formation.F352 => Formation.F442, // Kanat bindirmeleriyle 3'lü stoperin arkasına sarkma
        Formation.F442 => Formation.F433, // Hatlar arası üçgenlerle 2'li merkeze sayısal üstünlük
        Formation.F4231 => Formation.F433, // Üçlü orta saha ile çift ön liberoyu çevreleme
        Formation.F4312 => Formation.F352, // Geniş kanat beklerle dar elmas ortayı aşma
        Formation.F4222 => Formation.F4231, // Merkezde kompakt 3'lü hatla karşılama
        Formation.F343 => Formation.F4231, // Kanat bek arkalarını işleme
        Formation.F3241 => Formation.F442, // Hızlı kenar kontra atakları
        Formation.F3331 => Formation.F433, // Geniş kanat hücumlarıyla üçgenleri kırma
        Formation.F4141 => Formation.F4231, // Tek ön libero çevresine 10 numara yükleme
        Formation.F532 => Formation.F433, // Kanat forvetlerle 5'li bloğu açma
        Formation.F523 => Formation.F4231, // Orta saha ikilisini sayısal üstünlükle ezme
        Formation.F424 => Formation.F4141, // Kalabalık orta saha ile 2'li merkezi düşürme
        Formation.F451 => Formation.F343, // Agresif hücum hattıyla kilidi açma
        _ => Formation.F442,
    };

    public static TacticalApproach GetCounterApproach(TacticalApproach approach) => approach switch
    {
        TacticalApproach.Attacking => TacticalApproach.Defensive, // Hızlı kontra atak antitezi
        TacticalApproach.Defensive => TacticalApproach.Attacking, // Baskılı ve sabırlı hücum
        TacticalApproach.Balanced => TacticalApproach.Attacking,
        _ => TacticalApproach.Balanced,
    };
}
