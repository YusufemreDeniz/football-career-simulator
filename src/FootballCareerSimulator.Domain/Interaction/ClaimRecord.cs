using FootballCareerSimulator.Domain.Shared;
using FootballCareerSimulator.Domain.SocialContinuity;
using FootballCareerSimulator.Domain.WorldCalendar;

namespace FootballCareerSimulator.Domain.Interaction;

/// <summary>
/// Bir aktörün veya medyanın ortaya attığı iddia kaydı (Yenilik 3: Epistemik Bilgi Ayrımı).
/// Nesnel gerçekten (Truth) farklı olarak iddia subjektif ve yanıltıcı olabilir.
/// </summary>
public sealed class ClaimRecord
{
    public const int MinCredibility = 0;
    public const int MaxCredibility = 100;

    public ClaimRecord(
        long claimId,
        string subjectKey,
        ActorRef claimantActor,
        string contentCode,
        int credibility,
        bool isPublic,
        GameDate publishedOn,
        bool isVerifiedTruth = false)
    {
        if (claimId <= 0)
        {
            throw new InteractionInvariantViolationException("Claim ID must be positive.");
        }

        if (string.IsNullOrWhiteSpace(subjectKey))
        {
            throw new InteractionInvariantViolationException("Subject key is required.");
        }

        if (string.IsNullOrWhiteSpace(contentCode))
        {
            throw new InteractionInvariantViolationException("Content code is required.");
        }

        ClaimId = claimId;
        SubjectKey = subjectKey;
        ClaimantActor = claimantActor;
        ContentCode = contentCode;
        Credibility = Math.Clamp(credibility, MinCredibility, MaxCredibility);
        IsPublic = isPublic;
        PublishedOn = publishedOn;
        IsVerifiedTruth = isVerifiedTruth;
    }

    public long ClaimId { get; }

    public string SubjectKey { get; }

    public ActorRef ClaimantActor { get; }

    public string ContentCode { get; }

    public int Credibility { get; }

    public bool IsPublic { get; }

    public GameDate PublishedOn { get; }

    public bool IsVerifiedTruth { get; }
}
