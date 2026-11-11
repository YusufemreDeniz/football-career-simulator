using FootballCareerSimulator.Domain.Shared;

namespace FootballCareerSimulator.Domain.SocialContinuity;

/// <summary>
/// Bir kulübün A takım soyunma odası atmosferi ve iklimi (Yenilik 6: Social Contagion).
/// </summary>
public sealed class DressingRoomClimate
{
    public const int MinScore = 0;
    public const int MaxScore = 100;

    public DressingRoomClimate(
        ClubId clubId,
        int harmony = 70,
        int friction = 20)
    {
        ClubId = clubId;
        Harmony = Math.Clamp(harmony, MinScore, MaxScore);
        Friction = Math.Clamp(friction, MinScore, MaxScore);
    }

    public ClubId ClubId { get; }

    /// <summary>
    /// Takım içi genel uyum ve birliktelik puanı (0–100).
    /// </summary>
    public int Harmony { get; private set; }

    /// <summary>
    /// Takımdaki gerilim ve sürtüşme seviyesi (0–100).
    /// </summary>
    public int Friction { get; private set; }

    public string State => Friction switch
    {
        >= 70 => "Rebellious", // İsyan Eşiğinde
        >= 45 => "Split",      // Bölünmüş
        >= 25 => "Calm",       // Sakin
        _ => "United",         // Kenetlenmiş
    };

    public void ApplyCrisis(int severity)
    {
        var bounded = Math.Max(1, severity);
        Friction = Math.Clamp(Friction + bounded, MinScore, MaxScore);
        Harmony = Math.Clamp(Harmony - (bounded / 2), MinScore, MaxScore);
    }

    public void ApplyHarmonyBoost(int boost)
    {
        var bounded = Math.Max(1, boost);
        Harmony = Math.Clamp(Harmony + bounded, MinScore, MaxScore);
        Friction = Math.Clamp(Friction - (bounded / 2), MinScore, MaxScore);
    }
}
