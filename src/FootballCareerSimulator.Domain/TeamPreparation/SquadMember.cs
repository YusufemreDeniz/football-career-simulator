using FootballCareerSimulator.Domain.PlayerCareer;
using FootballCareerSimulator.Domain.WorldCalendar;

namespace FootballCareerSimulator.Domain.TeamPreparation;

/// <summary>
/// A takım kadro üyeliği (MVP: oyuncu + slot; rol/pozisyon sonra).
/// </summary>
public sealed class SquadMember
{
    private SquadMember(PlayerId playerId, int slotIndex, GameDate joinedOn)
    {
        PlayerId = playerId;
        SlotIndex = slotIndex;
        JoinedOn = joinedOn;
    }

    public PlayerId PlayerId { get; }

    public int SlotIndex { get; }

    public GameDate JoinedOn { get; }

    public static SquadMember Create(PlayerId playerId, int slotIndex, GameDate joinedOn)
    {
        EnsureSlot(slotIndex);
        return new SquadMember(playerId, slotIndex, joinedOn);
    }

    public static SquadMember Rehydrate(PlayerId playerId, int slotIndex, GameDate joinedOn) =>
        Create(playerId, slotIndex, joinedOn);

    private static void EnsureSlot(int slotIndex)
    {
        if (slotIndex is < MatchSelection.MinSquadSlot or > MatchSelection.MaxSquadSlot)
        {
            throw new TeamPreparationInvariantViolationException(
                $"Squad slot {slotIndex} is out of range ({MatchSelection.MinSquadSlot}-{MatchSelection.MaxSquadSlot}).");
        }
    }
}
