using System.Text;
using FootballCareerSimulator.Domain.TrainingPhysicalState;

namespace FootballCareerSimulator.Simulation.TrainingPhysicalState;

public static class TrainingPhysicalStateCanonicalStateHasher
{
    public static string BuildCanonicalText(
        IReadOnlyList<WeeklyTrainingPlan> plans,
        IReadOnlyList<PlayerPhysicalState> physicalStates)
    {
        ArgumentNullException.ThrowIfNull(plans);
        ArgumentNullException.ThrowIfNull(physicalStates);

        var builder = new StringBuilder("Training=");
        foreach (var plan in plans.OrderBy(p => p.ClubId.Value))
        {
            builder.Append("C=").Append(plan.ClubId.Value)
                .Append(";F=").Append((int)plan.Focus)
                .Append(";I=").Append((int)plan.Intensity)
                .Append(";R=").Append((int)plan.RestApproach)
                .Append(";D=").Append(plan.SetAt.DayNumber)
                .Append('|');
        }

        builder.Append("Physical=");
        foreach (var state in physicalStates.OrderBy(s => s.PlayerId.Value))
        {
            builder.Append("P=").Append(state.PlayerId.Value)
                .Append(";C=").Append(state.ClubId?.Value.ToString() ?? "-")
                .Append(";S=").Append(state.SlotIndex?.ToString() ?? "-")
                .Append(";Fat=").Append(state.Fatigue)
                .Append(";Fit=").Append(state.Fitness)
                .Append(";Inj=").Append((int)state.InjurySeverity)
                .Append(";Until=").Append(state.InjuredUntilDayNumber?.ToString() ?? "-")
                .Append(";M7=").Append(state.MatchMinutesLast7Days)
                .Append(";M14=").Append(state.MatchMinutesLast14Days)
                .Append('|');
        }

        return builder.ToString();
    }
}
