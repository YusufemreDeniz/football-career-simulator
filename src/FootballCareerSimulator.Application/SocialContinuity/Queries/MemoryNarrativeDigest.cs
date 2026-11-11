using FootballCareerSimulator.Domain.SocialContinuity;

namespace FootballCareerSimulator.Application.SocialContinuity.Queries;

/// <summary>
/// Oyuncunun veya menajerin hafızasındaki olayların telafi ve anlamlandırma durumunu açıklayan anlatı projeksiyonu.
/// </summary>
public sealed record MemoryNarrativeItem(
    long MemoryId,
    string Category,
    string Summary,
    int Influence,
    MemoryValence Valence,
    MemoryResolutionStatus ResolutionStatus,
    string? ResolutionReason,
    bool IsRedeemed);

public sealed record MemoryNarrativeDigest(
    long ActorId,
    IReadOnlyList<MemoryNarrativeItem> Memories,
    int ActiveUnresolvedCount,
    int ResolvedCount);
