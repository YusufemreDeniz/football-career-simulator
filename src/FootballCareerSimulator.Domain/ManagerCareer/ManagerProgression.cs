namespace FootballCareerSimulator.Domain.ManagerCareer;

/// <summary>
/// Menajerin kariyer boyu XP kazanımı, seviye atlaması ve beceri gelişimini yöneten model.
/// </summary>
public sealed record ManagerProgression
{
    public const int MinLevel = 1;
    public const int MaxLevel = 50;
    public const int BaseXpPerLevel = 500;

    public const int WinXp = 100;
    public const int DrawXp = 40;
    public const int LossXp = 15;
    public const int SeasonTargetXp = 500;
    public const int TrophyXp = 1000;

    public ManagerProgression(
        int level,
        int experiencePoints,
        ManagerSkillProfile skills)
    {
        Level = Math.Clamp(level, MinLevel, MaxLevel);
        ExperiencePoints = Math.Max(0, experiencePoints);
        Skills = skills ?? ManagerSkillProfile.CreateDefault();
    }

    public int Level { get; init; }

    public int ExperiencePoints { get; init; }

    public ManagerSkillProfile Skills { get; init; }

    /// <summary>
    /// Bir sonraki seviyeye geçmek için gereken toplam kümülatif XP miktarını verir.
    /// </summary>
    public static int CumulativeXpRequiredForLevel(int targetLevel)
    {
        if (targetLevel <= 1)
        {
            return 0;
        }

        // Örn: Lvl 2 = 500, Lvl 3 = 1200, Lvl 4 = 2100...
        var count = targetLevel - 1;
        return (count * BaseXpPerLevel) + (count * (count - 1) * 100);
    }

    public int XpToNextLevel =>
        Level >= MaxLevel ? 0 : Math.Max(0, CumulativeXpRequiredForLevel(Level + 1) - ExperiencePoints);

    public static ManagerProgression CreateInitial(StartingBackground? background = null)
    {
        var skills = background.HasValue
            ? ManagerSkillProfile.ForBackground(background.Value)
            : ManagerSkillProfile.CreateDefault();

        return new ManagerProgression(
            level: 1,
            experiencePoints: 0,
            skills: skills);
    }

    /// <summary>
    /// Menajere XP ekler; gerekirse seviye atlatır ve becerilerini artırır.
    /// </summary>
    public ManagerProgression AddExperience(int xp)
    {
        if (xp <= 0)
        {
            return this;
        }

        var newXp = ExperiencePoints + xp;
        var currentLevel = Level;

        while (currentLevel < MaxLevel && newXp >= CumulativeXpRequiredForLevel(currentLevel + 1))
        {
            currentLevel++;
        }

        var levelsGained = currentLevel - Level;
        var updatedSkills = Skills;

        if (levelsGained > 0)
        {
            // Her seviye atlamada tüm becerilere +1 puan dengeli artış
            updatedSkills = updatedSkills.ApplySkillGain(
                tactical: levelsGained,
                youth: levelsGained,
                man: levelsGained,
                negotiation: levelsGained);
        }

        return new ManagerProgression(currentLevel, newXp, updatedSkills);
    }
}
