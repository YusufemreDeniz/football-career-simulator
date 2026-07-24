using FootballCareerSimulator.Domain.ManagerCareer;
using FootballCareerSimulator.Domain.PlayerCareer;
using FootballCareerSimulator.Domain.WorldCalendar;

namespace FootballCareerSimulator.Domain.SocialContinuity;

/// <summary>
/// Yönlü ilişki: MVP iskelet Futbolcu → Teknik Direktör (Trust / Respect / Compatibility).
/// </summary>
public sealed class RelationshipRecord
{
    public const int MinDimension = 0;
    public const int MaxDimension = 100;
    public const int NeutralStart = 50;

    private readonly HashSet<string> _processedEffectKeys;

    private RelationshipRecord(
        RelationshipId relationshipId,
        ActorRef observer,
        ActorRef subject,
        int trust,
        int respect,
        int professionalCompatibility,
        RelationshipStatus status,
        GameDate createdOn,
        GameDate lastChangedOn,
        string? lastChangeReasonCode,
        IEnumerable<string> processedEffectKeys)
    {
        RelationshipId = relationshipId;
        Observer = observer;
        Subject = subject;
        Trust = trust;
        Respect = respect;
        ProfessionalCompatibility = professionalCompatibility;
        Status = status;
        CreatedOn = createdOn;
        LastChangedOn = lastChangedOn;
        LastChangeReasonCode = lastChangeReasonCode;
        _processedEffectKeys = processedEffectKeys.ToHashSet(StringComparer.Ordinal);
    }

    public RelationshipId RelationshipId { get; }

    public ActorRef Observer { get; }

    public ActorRef Subject { get; }

    public int Trust { get; }

    public int Respect { get; }

    public int ProfessionalCompatibility { get; }

    public RelationshipStatus Status { get; }

    public GameDate CreatedOn { get; }

    public GameDate LastChangedOn { get; }

    public string? LastChangeReasonCode { get; }

    public IReadOnlyCollection<string> ProcessedEffectKeys => _processedEffectKeys;

    public static RelationshipRecord CreatePlayerToManager(
        RelationshipId relationshipId,
        PlayerId playerId,
        ManagerId managerId,
        GameDate day) =>
        new(
            relationshipId,
            new ActorRef(ActorKind.Player, playerId.Value),
            new ActorRef(ActorKind.Manager, managerId.Value),
            NeutralStart,
            NeutralStart,
            NeutralStart,
            RelationshipStatus.Active,
            day,
            day,
            lastChangeReasonCode: "CreatedNeutral",
            Array.Empty<string>());

    public RelationshipRecord ApplyDimensionDeltas(
        string effectKey,
        int trustDelta,
        int respectDelta,
        int compatibilityDelta,
        string reasonCode,
        GameDate day)
    {
        if (string.IsNullOrWhiteSpace(effectKey))
        {
            throw new SocialContinuityInvariantViolationException("Effect key is required.");
        }

        if (string.IsNullOrWhiteSpace(reasonCode))
        {
            throw new SocialContinuityInvariantViolationException("Reason code is required.");
        }

        if (_processedEffectKeys.Contains(effectKey))
        {
            return this;
        }

        if (Status != RelationshipStatus.Active)
        {
            throw new SocialContinuityInvariantViolationException(
                "Only active relationships can apply dimension deltas in this vertical.");
        }

        var nextKeys = new HashSet<string>(_processedEffectKeys, StringComparer.Ordinal) { effectKey };
        return new RelationshipRecord(
            RelationshipId,
            Observer,
            Subject,
            Clamp(Trust + trustDelta),
            Clamp(Respect + respectDelta),
            Clamp(ProfessionalCompatibility + compatibilityDelta),
            Status,
            CreatedOn,
            day,
            reasonCode.Trim(),
            nextKeys);
    }

    public static RelationshipRecord Rehydrate(
        RelationshipId relationshipId,
        ActorRef observer,
        ActorRef subject,
        int trust,
        int respect,
        int professionalCompatibility,
        RelationshipStatus status,
        GameDate createdOn,
        GameDate lastChangedOn,
        string? lastChangeReasonCode,
        IEnumerable<string> processedEffectKeys)
    {
        ArgumentNullException.ThrowIfNull(processedEffectKeys);
        if (observer.Kind != ActorKind.Player || subject.Kind != ActorKind.Manager)
        {
            throw new SocialContinuityInvariantViolationException(
                "MVP relationship direction must be Player → Manager.");
        }

        ValidateDimension(trust);
        ValidateDimension(respect);
        ValidateDimension(professionalCompatibility);

        return new RelationshipRecord(
            relationshipId,
            observer,
            subject,
            trust,
            respect,
            professionalCompatibility,
            status,
            createdOn,
            lastChangedOn,
            lastChangeReasonCode,
            processedEffectKeys);
    }

    private static int Clamp(int value) =>
        Math.Clamp(value, MinDimension, MaxDimension);

    private static void ValidateDimension(int value)
    {
        if (value is < MinDimension or > MaxDimension)
        {
            throw new SocialContinuityInvariantViolationException(
                $"Relationship dimension must be between {MinDimension} and {MaxDimension}.");
        }
    }
}
