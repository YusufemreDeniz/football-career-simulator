using System.Text;
using FootballCareerSimulator.Domain.SocialContinuity;

namespace FootballCareerSimulator.Simulation.SocialContinuity;

public static class RelationshipCanonicalStateHasher
{
    public static string BuildCanonicalText(IReadOnlyList<RelationshipRecord> relationships)
    {
        ArgumentNullException.ThrowIfNull(relationships);

        var builder = new StringBuilder("Relationships=");
        foreach (var relationship in relationships.OrderBy(r => r.RelationshipId.Value))
        {
            builder.Append("Id=").Append(relationship.RelationshipId.Value)
                .Append(";OK=").Append((int)relationship.Observer.Kind)
                .Append(";OI=").Append(relationship.Observer.Id)
                .Append(";SK=").Append((int)relationship.Subject.Kind)
                .Append(";SI=").Append(relationship.Subject.Id)
                .Append(";T=").Append(relationship.Trust)
                .Append(";R=").Append(relationship.Respect)
                .Append(";C=").Append(relationship.ProfessionalCompatibility)
                .Append(";S=").Append((int)relationship.Status)
                .Append(";Cr=").Append(relationship.CreatedOn.DayNumber)
                .Append(";Lc=").Append(relationship.LastChangedOn.DayNumber)
                .Append(";Reason=").Append(relationship.LastChangeReasonCode ?? "-")
                .Append(";Fx=")
                .Append(string.Join(',', relationship.ProcessedEffectKeys.OrderBy(k => k, StringComparer.Ordinal)))
                .Append('|');
        }

        return builder.ToString();
    }
}
