using FootballCareerSimulator.Domain.WorldCalendar;

namespace FootballCareerSimulator.Domain.SocialContinuity;

/// <summary>
/// Soyunma odasındaki seyrek (sparse) nüfuz bağı (Yenilik 6: Social Contagion).
/// $O(N^2)$ yoğun matris yerine her oyuncu için en fazla 4 anlamlı bağ saklanır.
/// </summary>
public sealed class SparseInfluenceLink
{
    public const int MaxTiesPerPlayer = 4;
    public const int DefaultCooldownDays = 14;

    public SparseInfluenceLink(
        long sourcePlayerId,
        long targetPlayerId,
        string tieType,
        int strength = 70,
        GameDate? lastContagionDay = null)
    {
        if (sourcePlayerId <= 0 || targetPlayerId <= 0)
        {
            throw new SocialContinuityInvariantViolationException("Player IDs must be positive.");
        }

        if (sourcePlayerId == targetPlayerId)
        {
            throw new SocialContinuityInvariantViolationException("Player cannot have an influence tie with themselves.");
        }

        if (string.IsNullOrWhiteSpace(tieType))
        {
            throw new SocialContinuityInvariantViolationException("Tie type is required.");
        }

        SourcePlayerId = sourcePlayerId;
        TargetPlayerId = targetPlayerId;
        TieType = tieType;
        Strength = Math.Clamp(strength, 0, 100);
        LastContagionDay = lastContagionDay;
    }

    public long SourcePlayerId { get; }

    public long TargetPlayerId { get; }

    /// <summary>
    /// Nüfuz bağının türü ("Captain", "ViceCaptain", "Mentor", "AgeCohort").
    /// </summary>
    public string TieType { get; }

    /// <summary>
    /// Bağın gücü (0–100).
    /// </summary>
    public int Strength { get; }

    public GameDate? LastContagionDay { get; private set; }

    public bool CanPropagate(GameDate day, int cooldownDays = DefaultCooldownDays)
    {
        if (LastContagionDay is null)
        {
            return true;
        }

        return day.DayNumber >= LastContagionDay.Value.DayNumber + cooldownDays;
    }

    public void MarkPropagated(GameDate day)
    {
        LastContagionDay = day;
    }
}
