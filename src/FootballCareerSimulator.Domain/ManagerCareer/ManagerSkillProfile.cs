namespace FootballCareerSimulator.Domain.ManagerCareer;

/// <summary>
/// SM25 ve modern futbol menajerlik oyunlarından esinlenen menajer beceri profili.
/// Menajerin taktik, genç yetiştirme, insan yönetimi ve transfer pazarlığı yetkinliklerini temsil eder.
/// </summary>
public sealed record ManagerSkillProfile
{
    public const int MinScore = 0;
    public const int MaxScore = 100;
    public const int DefaultScore = 50;

    public ManagerSkillProfile(
        int tacticalAcumen = DefaultScore,
        int youthDevelopment = DefaultScore,
        int manManagement = DefaultScore,
        int negotiationSkill = DefaultScore)
    {
        TacticalAcumen = Math.Clamp(tacticalAcumen, MinScore, MaxScore);
        YouthDevelopment = Math.Clamp(youthDevelopment, MinScore, MaxScore);
        ManManagement = Math.Clamp(manManagement, MinScore, MaxScore);
        NegotiationSkill = Math.Clamp(negotiationSkill, MinScore, MaxScore);
    }

    /// <summary>
    /// Taktiksel bilgi, maç analizi ve oyun planı uygulama yetkinliği (0–100).
    /// </summary>
    public int TacticalAcumen { get; init; }

    /// <summary>
    /// Genç yetenekleri keşfetme ve altyapı oyuncularını geliştirme kabiliyeti (0–100).
    /// </summary>
    public int YouthDevelopment { get; init; }

    /// <summary>
    /// Soyunma odası liderliği, oyuncu ilişkileri ve moral yönetimi (0–100).
    /// </summary>
    public int ManManagement { get; init; }

    /// <summary>
    /// Transfer ve kontrat pazarlıklarında maliyet düşürme kabiliyeti (0–100).
    /// </summary>
    public int NegotiationSkill { get; init; }

    public static ManagerSkillProfile CreateDefault() =>
        new(DefaultScore, DefaultScore, DefaultScore, DefaultScore);

    public static ManagerSkillProfile ForBackground(StartingBackground background) =>
        background switch
        {
            StartingBackground.AmateurHeadCoach =>
                new(tacticalAcumen: 40, youthDevelopment: 45, manManagement: 40, negotiationSkill: 35),

            StartingBackground.YouthAcademyCoach =>
                new(tacticalAcumen: 45, youthDevelopment: 75, manManagement: 55, negotiationSkill: 35),

            StartingBackground.AssistantCoach =>
                new(tacticalAcumen: 60, youthDevelopment: 50, manManagement: 55, negotiationSkill: 50),

            StartingBackground.LowerLeagueYouthManager =>
                new(tacticalAcumen: 50, youthDevelopment: 65, manManagement: 50, negotiationSkill: 40),

            StartingBackground.RecentlyRetiredPlayer =>
                new(tacticalAcumen: 45, youthDevelopment: 40, manManagement: 75, negotiationSkill: 50),

            StartingBackground.TacticalSpecialist =>
                new(tacticalAcumen: 75, youthDevelopment: 45, manManagement: 40, negotiationSkill: 45),

            _ => CreateDefault(),
        };

    public ManagerSkillProfile ApplySkillGain(int tactical = 0, int youth = 0, int man = 0, int negotiation = 0) =>
        new(
            tacticalAcumen: TacticalAcumen + tactical,
            youthDevelopment: YouthDevelopment + youth,
            manManagement: ManManagement + man,
            negotiationSkill: NegotiationSkill + negotiation);
}
