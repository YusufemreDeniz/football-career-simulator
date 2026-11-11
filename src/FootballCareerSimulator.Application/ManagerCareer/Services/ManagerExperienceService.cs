using FootballCareerSimulator.Application.ManagerCareer.Ports;
using FootballCareerSimulator.Domain.ManagerCareer;

namespace FootballCareerSimulator.Application.ManagerCareer.Services;

public sealed record ManagerExperienceAward(
    int ExperienceEarned,
    int PreviousLevel,
    int Level,
    int TotalExperiencePoints)
{
    public bool LeveledUp => Level > PreviousLevel;
}

/// <summary>
/// Lig dışı (kıtasal) maçların menajer XP ödülü. Lig maçlarının XP'si yönetim değerlendirmesinde
/// (<see cref="ManagerCareer.ApplyMatchBoardAssessment"/>) tek kez verilir; burada tekrar verilmez.
/// </summary>
public sealed class ManagerExperienceService
{
    public const double ContinentalMultiplier = 1.5;

    private readonly IManagerCareerStore _store;

    public ManagerExperienceService(IManagerCareerStore store)
    {
        _store = store ?? throw new ArgumentNullException(nameof(store));
    }

    public static int ContinentalMatchExperience(int managedGoals, int opponentGoals) =>
        managedGoals > opponentGoals
            ? (int)(ManagerProgression.WinXp * ContinentalMultiplier)
            : managedGoals == opponentGoals
                ? (int)(ManagerProgression.DrawXp * ContinentalMultiplier)
                : ManagerProgression.LossXp;

    public ManagerExperienceAward AwardContinentalMatch(int managedGoals, int opponentGoals)
    {
        var xp = ContinentalMatchExperience(managedGoals, opponentGoals);
        var career = _store.Career;
        var previousLevel = career.Progression.Level;
        var updated = career.AddExperience(xp);
        _store.Replace(updated);
        return new ManagerExperienceAward(
            xp,
            previousLevel,
            updated.Progression.Level,
            updated.Progression.ExperiencePoints);
    }
}
