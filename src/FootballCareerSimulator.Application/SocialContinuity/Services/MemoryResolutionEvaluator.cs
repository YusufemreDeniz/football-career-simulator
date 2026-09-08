using FootballCareerSimulator.Application.SocialContinuity.Queries;
using FootballCareerSimulator.Domain.SocialContinuity;
using FootballCareerSimulator.Domain.WorldCalendar;

namespace FootballCareerSimulator.Application.SocialContinuity.Services;

/// <summary>
/// Hafıza kayıtlarının telafi ve yeniden anlamlandırma mantığını işleten kural değerlendiricisi (Yenilik 7: Redemption Memory).
/// </summary>
public sealed class MemoryResolutionEvaluator
{
    public MemoryRecord EvaluateSelectionRedemption(
        MemoryRecord memory,
        bool isStartingAppearance,
        bool isCrucialMatch,
        GameDate day)
    {
        ArgumentNullException.ThrowIfNull(memory);

        if (memory.Valence != MemoryValence.Negative || memory.ResolutionStatus == MemoryResolutionStatus.Resolved)
        {
            return memory;
        }

        if (memory.Category is not (MemoryCategory.Selection or MemoryCategory.Promise or MemoryCategory.Relationship))
        {
            return memory;
        }

        if (isStartingAppearance && isCrucialMatch)
        {
            return memory.MarkResolution(
                MemoryResolutionStatus.Resolved,
                day,
                "Kritik karşılaşmada ilk 11'de görev verilerek geçmiş güvensizlik telafi edildi.");
        }

        if (isStartingAppearance)
        {
            return memory.MarkResolution(
                MemoryResolutionStatus.PartiallyResolved,
                day,
                "İlk 11'e geri dönülmesi geçmiş olumsuzluğun etkisini azalttı.");
        }

        return memory;
    }

    public MemoryRecord EvaluatePromiseFulfillmentRedemption(
        MemoryRecord brokenPromiseMemory,
        GameDate day)
    {
        ArgumentNullException.ThrowIfNull(brokenPromiseMemory);

        if (brokenPromiseMemory.Category != MemoryCategory.Promise
            || brokenPromiseMemory.Valence != MemoryValence.Negative)
        {
            return brokenPromiseMemory;
        }

        return brokenPromiseMemory.MarkResolution(
            MemoryResolutionStatus.Resolved,
            day,
            "Sonraki vaadin eksiksiz yerine getirilmesiyle geçmiş söz kırılması telafi edildi.");
    }

    public MemoryRecord EvaluatePublicDefenseRedemption(
        MemoryRecord memory,
        GameDate day)
    {
        ArgumentNullException.ThrowIfNull(memory);

        if (memory.Valence != MemoryValence.Negative || memory.ResolutionStatus == MemoryResolutionStatus.Resolved)
        {
            return memory;
        }

        return memory.MarkResolution(
            MemoryResolutionStatus.Contradicted,
            day,
            "Basın önünde açık destek verilerek geçmiş sürtüşme sorgulandı.");
    }

    public MemoryNarrativeDigest BuildDigest(long actorId, IEnumerable<MemoryRecord> memories)
    {
        ArgumentNullException.ThrowIfNull(memories);

        var list = new List<MemoryNarrativeItem>();
        var unresolvedCount = 0;
        var resolvedCount = 0;

        foreach (var m in memories)
        {
            var isRedeemed = m.ResolutionStatus == MemoryResolutionStatus.Resolved;
            if (isRedeemed)
            {
                resolvedCount++;
            }
            else if (m.ResolutionStatus == MemoryResolutionStatus.Unresolved && m.Valence == MemoryValence.Negative)
            {
                unresolvedCount++;
            }

            list.Add(new MemoryNarrativeItem(
                m.MemoryId.Value,
                m.Category.ToString(),
                m.SourceEventKey,
                m.CurrentInfluence,
                m.Valence,
                m.ResolutionStatus,
                m.ResolutionReasonCode,
                isRedeemed));
        }

        return new MemoryNarrativeDigest(
            actorId,
            list,
            unresolvedCount,
            resolvedCount);
    }
}
