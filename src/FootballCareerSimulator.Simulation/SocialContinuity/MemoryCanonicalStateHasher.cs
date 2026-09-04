using System.Text;
using FootballCareerSimulator.Domain.SocialContinuity;

namespace FootballCareerSimulator.Simulation.SocialContinuity;

public static class MemoryCanonicalStateHasher
{
    public static string BuildCanonicalText(IReadOnlyList<MemoryRecord> memories)
    {
        ArgumentNullException.ThrowIfNull(memories);

        var builder = new StringBuilder("Memories=");
        foreach (var memory in memories.OrderBy(m => m.MemoryId.Value))
        {
            builder.Append("Id=").Append(memory.MemoryId.Value)
                .Append(";RAK=").Append((int)memory.RememberingActor.Kind)
                .Append(";RAI=").Append(memory.RememberingActor.Id)
                .Append(";SK=").Append((int)memory.SubjectKind)
                .Append(";SI=").Append(memory.SubjectId)
                .Append(";Src=").Append(memory.SourceEventKey)
                .Append(";Cat=").Append((int)memory.Category)
                .Append(";Cr=").Append(memory.CreatedOn.DayNumber)
                .Append(";Lr=").Append(memory.LastReinforcedOn.DayNumber)
                .Append(";Bi=").Append(memory.BaseImportance)
                .Append(";Ci=").Append(memory.CurrentInfluence)
                .Append(";V=").Append((int)memory.Valence)
                .Append(";Vis=").Append((int)memory.Visibility)
                .Append(";S=").Append((int)memory.Status)
                .Append(";Rc=").Append(memory.ReinforcementCount)
                .Append(";RP=").Append(memory.RelatedPromiseId?.Value.ToString() ?? "-")
                .Append(";R=").Append(memory.RuleId)
                .Append(";Rv=").Append(memory.RuleVersion)
                .Append('|');
        }

        return builder.ToString();
    }
}
