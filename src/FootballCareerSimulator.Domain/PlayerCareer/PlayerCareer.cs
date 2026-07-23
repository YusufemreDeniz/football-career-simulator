using FootballCareerSimulator.Domain.Shared;
using FootballCareerSimulator.Domain.TeamPreparation;
using FootballCareerSimulator.Domain.WorldCalendar;

namespace FootballCareerSimulator.Domain.PlayerCareer;

/// <summary>
/// Slot bağlı kalıcı sportif kariyer (MVP: CA/PA + gelişim + yaşlanma/düşüş).
/// Squad/physical/contract sahibi değildir.
/// </summary>
public sealed class PlayerCareer
{
    public const int MinAbility = 40;
    public const int MaxAbility = 99;
    public const int PeakStartAge = 24;
    public const int DeclineStartAge = 30;

    private PlayerCareer(
        PlayerId id,
        ClubId originClubId,
        int slotIndex,
        int currentAbility,
        int potentialAbility,
        int developmentPoints,
        GameDate? lastDevelopedOn,
        int birthYear,
        int? lastAgedCalendarYear)
    {
        Id = id;
        OriginClubId = originClubId;
        SlotIndex = slotIndex;
        CurrentAbility = currentAbility;
        PotentialAbility = potentialAbility;
        DevelopmentPoints = developmentPoints;
        LastDevelopedOn = lastDevelopedOn;
        BirthYear = birthYear;
        LastAgedCalendarYear = lastAgedCalendarYear;
    }

    public PlayerId Id { get; }

    public ClubId OriginClubId { get; }

    public int SlotIndex { get; }

    public int CurrentAbility { get; }

    public int PotentialAbility { get; }

    public int DevelopmentPoints { get; }

    public GameDate? LastDevelopedOn { get; }

    public int BirthYear { get; }

    public int? LastAgedCalendarYear { get; }

    public static PlayerCareer CreateForSlot(
        ClubId clubId,
        int slotIndex,
        int currentAbility,
        int potentialAbility,
        int birthYear)
    {
        EnsureSlot(slotIndex);
        EnsureAbility(currentAbility, nameof(currentAbility));
        EnsureAbility(potentialAbility, nameof(potentialAbility));
        EnsureBirthYear(birthYear);
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
            lastDevelopedOn: null,
            birthYear,
            lastAgedCalendarYear: null);
    }

    public static PlayerCareer Rehydrate(
        PlayerId id,
        ClubId originClubId,
        int slotIndex,
        int currentAbility,
        int potentialAbility,
        int developmentPoints,
        GameDate? lastDevelopedOn,
        int birthYear,
        int? lastAgedCalendarYear)
    {
        EnsureSlot(slotIndex);
        EnsureAbility(currentAbility, nameof(currentAbility));
        EnsureAbility(potentialAbility, nameof(potentialAbility));
        EnsureBirthYear(birthYear);
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
            lastDevelopedOn,
            birthYear,
            lastAgedCalendarYear);
    }

    public int AgeYears(GameDate day) => Math.Max(15, day.Year - BirthYear);

    public CareerPhase GetPhase(GameDate day)
    {
        var age = AgeYears(day);
        if (age < PeakStartAge)
        {
            return CareerPhase.Developing;
        }

        if (age < DeclineStartAge)
        {
            return CareerPhase.Peak;
        }

        return CareerPhase.Declining;
    }

    public PlayerCareer ApplyDevelopmentGain(int points, GameDate day)
    {
        if (points <= 0)
        {
            return this;
        }

        var phase = GetPhase(day);
        var effective = phase switch
        {
            CareerPhase.Developing => points + 1,
            CareerPhase.Declining => Math.Max(1, points / 2),
            _ => points,
        };

        var total = DevelopmentPoints + effective;
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
            day,
            BirthYear,
            LastAgedCalendarYear);
    }

    public PlayerCareer ApplyAnnualAging(GameDate day)
    {
        if (LastAgedCalendarYear is int agedYear && agedYear >= day.Year)
        {
            return this;
        }

        var age = AgeYears(day);
        var ability = CurrentAbility;
        var potential = PotentialAbility;

        if (age >= DeclineStartAge)
        {
            var drop = age >= 33 ? 2 : 1;
            ability = Math.Max(MinAbility, ability - drop);
            potential = Math.Max(ability, potential - (age >= 34 ? 1 : 0));
        }

        return new PlayerCareer(
            Id,
            OriginClubId,
            SlotIndex,
            ability,
            potential,
            DevelopmentPoints,
            LastDevelopedOn,
            BirthYear,
            day.Year);
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

    private static void EnsureBirthYear(int birthYear)
    {
        if (birthYear is < 1960 or > 2100)
        {
            throw new PlayerCareerInvariantViolationException(
                $"Birth year {birthYear} is out of supported range.");
        }
    }
}
