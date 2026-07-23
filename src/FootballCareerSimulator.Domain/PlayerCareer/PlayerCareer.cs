using FootballCareerSimulator.Domain.Shared;
using FootballCareerSimulator.Domain.TeamPreparation;
using FootballCareerSimulator.Domain.WorldCalendar;

namespace FootballCareerSimulator.Domain.PlayerCareer;

/// <summary>
/// Slot bağlı kalıcı sportif kariyer (MVP: CA/PA + birikimli gelişim).
/// Squad/physical/contract sahibi değildir.
/// </summary>
public sealed class PlayerCareer
{
    public const int MinAbility = 40;
    public const int MaxAbility = 99;

    private PlayerCareer(
        PlayerId id,
        ClubId originClubId,
        int slotIndex,
        int currentAbility,
        int potentialAbility,
        int developmentPoints,
        GameDate? lastDevelopedOn)
    {
        Id = id;
        OriginClubId = originClubId;
        SlotIndex = slotIndex;
        CurrentAbility = currentAbility;
        PotentialAbility = potentialAbility;
        DevelopmentPoints = developmentPoints;
        LastDevelopedOn = lastDevelopedOn;
    }

    public PlayerId Id { get; }

    /// <summary>MVP roster bağlantısı; transfer sonrası kimlik değişmez, yalnızca bu alan güncellenir.</summary>
    public ClubId OriginClubId { get; }

    public int SlotIndex { get; }

    public int CurrentAbility { get; }

    public int PotentialAbility { get; }

    public int DevelopmentPoints { get; }

    public GameDate? LastDevelopedOn { get; }

    public static PlayerCareer CreateForSlot(
        ClubId clubId,
        int slotIndex,
        int currentAbility,
        int potentialAbility)
    {
        EnsureSlot(slotIndex);
        EnsureAbility(currentAbility, nameof(currentAbility));
        EnsureAbility(potentialAbility, nameof(potentialAbility));
        if (potentialAbility < currentAbility)
        {
            throw new PlayerCareerInvariantViolationException(
                "Potential ability cannot be below current ability.");
        }

        return new PlayerCareer(
            PlayerId.FromClubSlot(clubId.Value, slotIndex),
            clubId,
            slotIndex,
            currentAbility,
            potentialAbility,
            developmentPoints: 0,
            lastDevelopedOn: null);
    }

    public static PlayerCareer Rehydrate(
        PlayerId id,
        ClubId originClubId,
        int slotIndex,
        int currentAbility,
        int potentialAbility,
        int developmentPoints,
        GameDate? lastDevelopedOn)
    {
        EnsureSlot(slotIndex);
        EnsureAbility(currentAbility, nameof(currentAbility));
        EnsureAbility(potentialAbility, nameof(potentialAbility));
        if (potentialAbility < currentAbility)
        {
            throw new PlayerCareerInvariantViolationException(
                "Potential ability cannot be below current ability.");
        }

        if (developmentPoints < 0)
        {
            throw new PlayerCareerInvariantViolationException(
                "Development points cannot be negative.");
        }

        return new PlayerCareer(
            id,
            originClubId,
            slotIndex,
            currentAbility,
            potentialAbility,
            developmentPoints,
            lastDevelopedOn);
    }

    public PlayerCareer ApplyDevelopmentGain(int points, GameDate day)
    {
        if (points <= 0)
        {
            return this;
        }

        var total = DevelopmentPoints + points;
        var ability = CurrentAbility;
        while (total >= 10 && ability < PotentialAbility)
        {
            total -= 10;
            ability++;
        }

        if (ability >= PotentialAbility)
        {
            total = Math.Min(total, 9);
        }

        return new PlayerCareer(
            Id,
            OriginClubId,
            SlotIndex,
            ability,
            PotentialAbility,
            total,
            day);
    }

    private static void EnsureSlot(int slotIndex)
    {
        if (slotIndex is < MatchSelection.MinSquadSlot or > MatchSelection.MaxSquadSlot)
        {
            throw new PlayerCareerInvariantViolationException(
                $"Squad slot {slotIndex} is out of range ({MatchSelection.MinSquadSlot}-{MatchSelection.MaxSquadSlot}).");
        }
    }

    private static void EnsureAbility(int value, string name)
    {
        if (value is < MinAbility or > MaxAbility)
        {
            throw new PlayerCareerInvariantViolationException(
                $"{name} must be between {MinAbility} and {MaxAbility}.");
        }
    }
}
