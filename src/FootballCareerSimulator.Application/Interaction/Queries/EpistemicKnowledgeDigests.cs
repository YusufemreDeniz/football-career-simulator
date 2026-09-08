using FootballCareerSimulator.Domain.Shared;

namespace FootballCareerSimulator.Application.Interaction.Queries;

/// <summary>
/// Medyada veya kulüp gündeminde yer alan haberin güvenilirlik rozeti ve doğruluk durumunu içeren okuma modeli (Yenilik 3).
/// </summary>
public sealed record MediaStoryItem(
    long ClaimId,
    string Headline,
    string ReliabilityTag, // "Doğrulandı", "İddia", "Kulüp Kaynakları", "Şüpheli"
    int CredibilityScore,
    bool IsVerifiedTruth);

public sealed record MediaStoryDigest(
    IReadOnlyList<MediaStoryItem> Stories);

/// <summary>
/// Soyunma odasında aktif olan dedikoduların listesini sunan okuma modeli (Yenilik 3).
/// </summary>
public sealed record LockerRoomRumorItem(
    string RumorId,
    string TopicCode,
    string NarrativeText,
    int DaysRemaining);

public sealed record LockerRoomRumorDigest(
    IReadOnlyList<LockerRoomRumorItem> ActiveRumors,
    int TotalActiveCount);
