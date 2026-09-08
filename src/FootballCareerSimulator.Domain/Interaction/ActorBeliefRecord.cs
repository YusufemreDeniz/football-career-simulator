using FootballCareerSimulator.Domain.Shared;
using FootballCareerSimulator.Domain.SocialContinuity;
using FootballCareerSimulator.Domain.WorldCalendar;

namespace FootballCareerSimulator.Domain.Interaction;

/// <summary>
/// Bir aktörün bir konu veya diğer aktör hakkında edindiği inanç kaydı (Yenilik 3: Epistemik Bilgi Ayrımı).
/// </summary>
public sealed class ActorBeliefRecord
{
    public const int MinConfidence = 0;
    public const int MaxConfidence = 100;

    public ActorBeliefRecord(
        ActorRef observerActor,
        string subjectKey,
        long believedClaimId,
        int confidenceLevel,
        GameDate formedOn)
    {
        if (string.IsNullOrWhiteSpace(subjectKey))
        {
            throw new InteractionInvariantViolationException("Subject key is required.");
        }

        if (believedClaimId <= 0)
        {
            throw new InteractionInvariantViolationException("Believed claim ID must be positive.");
        }

        ObserverActor = observerActor;
        SubjectKey = subjectKey;
        BelievedClaimId = believedClaimId;
        ConfidenceLevel = Math.Clamp(confidenceLevel, MinConfidence, MaxConfidence);
        FormedOn = formedOn;
    }

    public ActorRef ObserverActor { get; }

    public string SubjectKey { get; }

    public long BelievedClaimId { get; }

    public int ConfidenceLevel { get; }

    public GameDate FormedOn { get; }
}
