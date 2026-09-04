using System.Text;
using FootballCareerSimulator.Domain.Transfer;

namespace FootballCareerSimulator.Simulation.Transfer;

public static class ShortlistTargetCanonicalStateHasher
{
    public static string BuildCanonicalText(
        IReadOnlyList<ShortlistEntry> shortlist,
        IReadOnlyList<TransferTarget> targets)
    {
        ArgumentNullException.ThrowIfNull(shortlist);
        ArgumentNullException.ThrowIfNull(targets);

        var builder = new StringBuilder("Shortlist=");
        foreach (var entry in shortlist.OrderBy(e => e.EntryId.Value))
        {
            builder.Append("E=").Append(entry.EntryId.Value)
                .Append(";C=").Append(entry.ClubId.Value)
                .Append(";P=").Append(entry.PlayerId.Value)
                .Append(";N=").Append(entry.NeedId?.Value.ToString() ?? "-")
                .Append(";R=").Append(entry.Priority)
                .Append(";S=").Append((int)entry.Status)
                .Append(";A=").Append(entry.AddedOn.DayNumber)
                .Append(";X=").Append(entry.ArchivedOn?.DayNumber.ToString() ?? "-")
                .Append('|');
        }

        builder.Append("Targets=");
        foreach (var target in targets.OrderBy(t => t.TargetId.Value))
        {
            builder.Append("T=").Append(target.TargetId.Value)
                .Append(";N=").Append(target.NeedId.Value)
                .Append(";C=").Append(target.ClubId.Value)
                .Append(";P=").Append(target.PlayerId.Value)
                .Append(";L=").Append(target.ShortlistEntryId?.Value.ToString() ?? "-")
                .Append(";S=").Append((int)target.Status)
                .Append(";D=").Append(target.ListedOn.DayNumber)
                .Append(";X=").Append(target.DroppedOn?.DayNumber.ToString() ?? "-")
                .Append('|');
        }

        return builder.ToString();
    }
}
