using FootballCareerSimulator.Application.SocialContinuity.Ports;
using FootballCareerSimulator.Application.SocialContinuity.Queries;
using FootballCareerSimulator.Domain.SocialContinuity;

namespace FootballCareerSimulator.Application.SocialContinuity.Services;

/// <summary>
/// Relationship salt-okunur sorguları.
/// </summary>
public sealed class RelationshipQueryService
{
    private readonly IRelationshipStore _store;

    public RelationshipQueryService(IRelationshipStore store)
    {
        _store = store ?? throw new ArgumentNullException(nameof(store));
    }

    public ManagerRelationshipsReadModel GetActiveForManager(long managerId, int take = 8)
    {
        if (take < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(take), take, "Take must be positive.");
        }

        var active = _store.Relationships
            .Where(r =>
                r.Status == RelationshipStatus.Active
                && r.Subject.Kind == ActorKind.Manager
                && r.Subject.Id == managerId)
            .OrderByDescending(r => Math.Abs(r.Trust - RelationshipRecord.NeutralStart)
                + Math.Abs(r.Respect - RelationshipRecord.NeutralStart)
                + Math.Abs(r.ProfessionalCompatibility - RelationshipRecord.NeutralStart))
            .ThenByDescending(r => r.LastChangedOn.DayNumber)
            .ThenBy(r => r.RelationshipId.Value)
            .ToArray();

        return new ManagerRelationshipsReadModel(
            managerId,
            active.Length,
            active.Take(take).Select(ToLine).ToArray());
    }

    private static RelationshipLineReadModel ToLine(RelationshipRecord relationship) =>
        new(
            relationship.RelationshipId.Value,
            relationship.Observer.Id,
            relationship.Subject.Id,
            relationship.Trust,
            relationship.Respect,
            relationship.ProfessionalCompatibility,
            DimensionLabel(relationship.Trust),
            DimensionLabel(relationship.Respect),
            DimensionLabel(relationship.ProfessionalCompatibility),
            relationship.Status.ToString(),
            relationship.LastChangeReasonCode,
            relationship.LastChangedOn.DayNumber);

    private static string DimensionLabel(int value) =>
        RelationshipDimensionBands.FromValue(value) switch
        {
            RelationshipDimensionBand.Low => "Düşük",
            RelationshipDimensionBand.High => "Yüksek",
            _ => "Nötr",
        };
}
