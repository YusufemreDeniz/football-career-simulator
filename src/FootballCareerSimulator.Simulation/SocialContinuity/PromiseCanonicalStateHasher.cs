using System.Text;
using FootballCareerSimulator.Domain.SocialContinuity;

namespace FootballCareerSimulator.Simulation.SocialContinuity;

public static class PromiseCanonicalStateHasher
{
    public static string BuildCanonicalText(IReadOnlyList<Promise> promises)
    {
        ArgumentNullException.ThrowIfNull(promises);

        var builder = new StringBuilder("Promises=");
        foreach (var promise in promises.OrderBy(p => p.PromiseId.Value))
        {
            builder.Append("Id=").Append(promise.PromiseId.Value)
                .Append(";K=").Append((int)promise.Kind)
                .Append(";PrK=").Append((int)promise.Promisor.Kind)
                .Append(";PrI=").Append(promise.Promisor.Id)
                .Append(";PeK=").Append((int)promise.Promisee.Kind)
                .Append(";PeI=").Append(promise.Promisee.Id)
                .Append(";C=").Append(promise.ClubId.Value)
                .Append(";T=").Append(promise.TargetStarts)
                .Append(";G=").Append(promise.StartsGiven)
                .Append(";Dl=").Append(promise.DeadlineOn.DayNumber)
                .Append(";Cr=").Append(promise.CreatedOn.DayNumber)
                .Append(";S=").Append((int)promise.Status)
                .Append(";Term=").Append(promise.TerminalOn?.DayNumber.ToString() ?? "-")
                .Append(";Fx=").Append(string.Join(',', promise.CountedFixtureIds.OrderBy(id => id)))
                .Append('|');
        }

        return builder.ToString();
    }
}
