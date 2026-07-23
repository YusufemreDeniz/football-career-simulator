using FootballCareerSimulator.Domain.PlayerCareer;
using FootballCareerSimulator.Domain.Shared;
using FootballCareerSimulator.Domain.TeamPreparation;
using FootballCareerSimulator.Domain.TrainingPhysicalState;
using FootballCareerSimulator.Domain.WorldCalendar;
using FootballCareerSimulator.Simulation.TeamPreparation;

namespace FootballCareerSimulator.Simulation.PlayerCareer;

/// <summary>
/// Antrenman/maç tetiklerinden deterministik gelişim puanı uygular.
/// </summary>
public static class MvpPlayerDevelopmentApplier
{
    public static IReadOnlyList<Domain.PlayerCareer.PlayerCareer> EnsureClubSquad(
        ClubId clubId,
        int rootSeed,
        GameDate referenceDay,
        IReadOnlyDictionary<(long ClubId, int SlotIndex), Domain.PlayerCareer.PlayerCareer> existing)
    {
        ArgumentNullException.ThrowIfNull(existing);

        var result = new List<Domain.PlayerCareer.PlayerCareer>(
            MatchSelection.MaxSquadSlot - MatchSelection.MinSquadSlot + 1);

        for (var slot = MatchSelection.MinSquadSlot; slot <= MatchSelection.MaxSquadSlot; slot++)
        {
            if (existing.TryGetValue((clubId.Value, slot), out var career))
            {
                result.Add(career);
                continue;
            }

            var ca = MvpSquadStrengthCalculator.GetPlayerRating(clubId, rootSeed, slot);
            var pa = Math.Min(
                Domain.PlayerCareer.PlayerCareer.MaxAbility,
                ca + 5 + (slot % 10));
            var birthYear = MvpAgingApplier.ResolveBirthYear(referenceDay.Year, slot, rootSeed);
            result.Add(Domain.PlayerCareer.PlayerCareer.CreateForSlot(clubId, slot, ca, pa, birthYear));
        }

        return result;
    }

    public static Domain.PlayerCareer.PlayerCareer ApplyWeeklyTraining(
        Domain.PlayerCareer.PlayerCareer career,
        WeeklyTrainingPlan plan,
        PlayerPhysicalState? physical,
        GameDate day)
    {
        ArgumentNullException.ThrowIfNull(career);
        ArgumentNullException.ThrowIfNull(plan);

        if (physical is not null && !physical.IsAvailableOn(day))
        {
            return career;
        }

        var points = plan.Intensity switch
        {
            TrainingIntensity.Low => 2,
            TrainingIntensity.Medium => 4,
            TrainingIntensity.High => 6,
            _ => 4,
        };

        if (plan.Focus == TrainingFocus.Fitness)
        {
            points += 1;
        }
        else if (plan.Focus == TrainingFocus.Recovery)
        {
            points = Math.Max(1, points - 2);
        }

        return career.ApplyDevelopmentGain(points, day);
    }

    public static Domain.PlayerCareer.PlayerCareer ApplyMatchAppearance(
        Domain.PlayerCareer.PlayerCareer career,
        GameDate day) =>
        career.ApplyDevelopmentGain(5, day);
}
