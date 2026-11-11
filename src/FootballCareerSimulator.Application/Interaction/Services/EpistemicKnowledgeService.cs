using FootballCareerSimulator.Application.Interaction.Queries;
using FootballCareerSimulator.Domain.Interaction;
using FootballCareerSimulator.Domain.Shared;
using FootballCareerSimulator.Domain.SocialContinuity;
using FootballCareerSimulator.Domain.WorldCalendar;

namespace FootballCareerSimulator.Application.Interaction.Services;

/// <summary>
/// Nesnel gerçeklik (Truth) ile iddia (Claim), inanç (Belief) ve dedikoduları (Rumor) yöneten servis (Yenilik 3: Epistemik Bilgi).
/// </summary>
public sealed class EpistemicKnowledgeService
{
    private readonly Dictionary<long, ClaimRecord> _claims = new();
    private readonly Dictionary<(ActorKind Kind, long Id, string SubjectKey), ActorBeliefRecord> _beliefs = new();
    private readonly List<RumorRecord> _rumors = new();

    public void PublishClaim(ClaimRecord claim)
    {
        ArgumentNullException.ThrowIfNull(claim);
        _claims[claim.ClaimId] = claim;
    }

    public void FormBelief(ActorRef observer, string subjectKey, long claimId, GameDate day)
    {
        if (!_claims.TryGetValue(claimId, out var claim))
        {
            return;
        }

        // İnanç güven seviyesi iddianın inandırıcılığına bağlıdır
        var confidence = claim.Credibility;
        var belief = new ActorBeliefRecord(observer, subjectKey, claimId, confidence, day);
        _beliefs[(observer.Kind, observer.Id, subjectKey)] = belief;
    }

    public ActorBeliefRecord? GetBelief(ActorRef observer, string subjectKey)
    {
        _beliefs.TryGetValue((observer.Kind, observer.Id, subjectKey), out var belief);
        return belief;
    }

    public void SpreadRumor(RumorRecord rumor)
    {
        ArgumentNullException.ThrowIfNull(rumor);
        if (_rumors.All(r => r.RumorId != rumor.RumorId))
        {
            _rumors.Add(rumor);
        }
    }

    public void PruneExpiredRumors(GameDate currentDay)
    {
        _rumors.RemoveAll(r => !r.IsActive(currentDay));
    }

    public IReadOnlyList<RumorRecord> GetActiveRumors(GameDate currentDay) =>
        _rumors.Where(r => r.IsActive(currentDay)).ToList();

    public MediaStoryDigest BuildMediaStoryDigest()
    {
        var items = new List<MediaStoryItem>();

        foreach (var claim in _claims.Values.Where(c => c.IsPublic))
        {
            var tag = claim.IsVerifiedTruth
                ? "Doğrulandı"
                : (claim.Credibility >= 75
                    ? "Kulüp Kaynakları"
                    : (claim.Credibility >= 40 ? "İddia" : "Şüpheli"));

            items.Add(new MediaStoryItem(
                claim.ClaimId,
                claim.ContentCode,
                tag,
                claim.Credibility,
                claim.IsVerifiedTruth));
        }

        return new MediaStoryDigest(items);
    }

    public LockerRoomRumorDigest BuildLockerRoomRumorDigest(GameDate currentDay)
    {
        var active = GetActiveRumors(currentDay);
        var items = active.Select(r => new LockerRoomRumorItem(
            r.RumorId,
            r.TopicCode,
            r.NarrativeText,
            DaysRemaining: Math.Max(0, (r.CreatedOn.DayNumber + r.TtlDays) - currentDay.DayNumber)
        )).ToList();

        return new LockerRoomRumorDigest(items, items.Count);
    }
}
