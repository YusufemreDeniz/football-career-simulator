using FootballCareerSimulator.Domain.Shared;

namespace FootballCareerSimulator.Application.SocialContinuity.Queries;

/// <summary>
/// Soyunma odasındaki genel duygu iklimini ve nüfuz liderlerini gösteren okuma modeli (Yenilik 6).
/// </summary>
public sealed record DressingRoomClimateDigest(
    ClubId ClubId,
    string ClimateStateTurkish, // "Kenetlenmiş", "Sakin", "Bölünmüş", "İsyan Eşiğinde"
    int HarmonyScore,
    int FrictionScore,
    IReadOnlyList<string> KeyObservations);
