using FootballCareerSimulator.Domain.SocialContinuity;
using FootballCareerSimulator.Domain.Transfer;

namespace FootballCareerSimulator.Application.Transfer.Queries;

/// <summary>
/// Yönetilen satış sonrası salt-okunur sonuç — Promise / Relationship / Memory / Need
/// gerçek state'ten; UI iş kuralı üretmez.
/// </summary>
public sealed record ManagedSaleAftermathDigest(
    long PlayerId,
    string BuyerDisplayName,
    int TransferFee,
    int ActiveContractCount,
    int MaxSquadMembers,
    bool ExitNeedClosed,
    bool NoActivePromise,
    bool PromiseInvalidated,
    bool RelationshipDormant,
    bool TransferMemoryRecorded,
    string Headline,
    IReadOnlyList<string> BeatLines)
{
    public string ToStatusMessage()
    {
        var beats = BeatLines.Count == 0
            ? string.Empty
            : "\n" + string.Join("\n", BeatLines.Select(b => "· " + b));
        return $"{Headline}{beats}";
    }

    public static ManagedSaleAftermathDigest Compose(
        long playerId,
        long managerId,
        string buyerDisplayName,
        int transferFee,
        int activeContractCount,
        int maxSquadMembers,
        IReadOnlyList<TransferNeed> needs,
        IReadOnlyList<Promise> promises,
        IReadOnlyList<RelationshipRecord> relationships,
        IReadOnlyList<MemoryRecord> memories)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(buyerDisplayName);
        ArgumentNullException.ThrowIfNull(needs);
        ArgumentNullException.ThrowIfNull(promises);
        ArgumentNullException.ThrowIfNull(relationships);
        ArgumentNullException.ThrowIfNull(memories);

        var exitReason = TransferNeed.BuildPlayerExitReasonCode(
            new Domain.PlayerCareer.PlayerId(playerId));
        var exitNeedClosed = !needs.Any(n =>
            n.IsOpen
            && n.Kind == TransferNeedKind.PlayerExitRequest
            && string.Equals(n.ReasonCode, exitReason, StringComparison.Ordinal));

        var playerPromises = promises
            .Where(p =>
                p.Promisee.Kind == ActorKind.Player
                && p.Promisee.Id == playerId)
            .ToArray();
        var promiseInvalidated = playerPromises.Any(p => p.Status == PromiseStatus.Invalidated);
        var noActivePromise = playerPromises.Length == 0
            || playerPromises.All(p => !p.IsActive);
        var promiseAlreadyBroken = playerPromises.Any(p => p.Status == PromiseStatus.Broken)
            && !promiseInvalidated;

        var relationship = relationships.FirstOrDefault(r =>
            r.Observer.Kind == ActorKind.Player
            && r.Observer.Id == playerId
            && r.Subject.Kind == ActorKind.Manager
            && r.Subject.Id == managerId);
        var relationshipDormant = relationship?.Status == RelationshipStatus.Dormant;

        var transferMemoryRecorded = memories.Any(m =>
            m.Category == MemoryCategory.Transfer
            && m.Status == MemoryStatus.Active
            && m.RuleId == MemoryRecord.TransferCompletedRuleId
            && ((m.RememberingActor.Kind == ActorKind.Player && m.RememberingActor.Id == playerId)
                || (m.RememberingActor.Kind == ActorKind.Manager && m.RememberingActor.Id == managerId)));

        var beats = new List<string>
        {
            $"Bedel {transferFee:N0} · sözleşme {activeContractCount}/{maxSquadMembers}",
        };

        if (exitNeedClosed)
        {
            beats.Add("Ayrılma ihtiyacı kapandı.");
        }

        if (promiseInvalidated)
        {
            beats.Add("Aktif forma sözü geçersizleşti.");
        }
        else if (promiseAlreadyBroken)
        {
            beats.Add("Bozulmuş forma sözü zaten kapalıydı — satış zinciri kapattı.");
        }
        else if (noActivePromise && playerPromises.Length > 0)
        {
            beats.Add("Aktif forma sözü kalmadı.");
        }

        if (relationshipDormant)
        {
            beats.Add("Oyuncu–menajer ilişkisi uyku moduna geçti.");
        }

        if (transferMemoryRecorded)
        {
            beats.Add("Transfer hafızaya işlendi.");
        }

        beats.Add("Öneri: Günün Nabzı ve bütçeye bak — slot açıldı.");

        return new ManagedSaleAftermathDigest(
            playerId,
            buyerDisplayName,
            transferFee,
            activeContractCount,
            maxSquadMembers,
            exitNeedClosed,
            noActivePromise,
            promiseInvalidated,
            relationshipDormant,
            transferMemoryRecorded,
            $"Satış Tamam\n#{playerId} → {buyerDisplayName}.",
            beats);
    }
}
