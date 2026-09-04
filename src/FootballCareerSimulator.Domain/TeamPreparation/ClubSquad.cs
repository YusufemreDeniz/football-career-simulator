using FootballCareerSimulator.Domain.PlayerCareer;
using FootballCareerSimulator.Domain.Shared;
using FootballCareerSimulator.Domain.WorldCalendar;

namespace FootballCareerSimulator.Domain.TeamPreparation;

/// <summary>
/// Kulübün kalıcı A takım kadrosu (membership). MatchSelection tek maçlıktır.
/// </summary>
public sealed class ClubSquad
{
    public const int MaxMembers = MatchSelection.MaxSquadSlot - MatchSelection.MinSquadSlot + 1;

    /// <summary>
    /// Oynanabilir taban: XI + tam yedek. AI satış ve nüfus sürekliliği bu eşiği korur.
    /// </summary>
    public const int MinimumPlayableContracts =
        MatchSelection.StartingXiSize + MatchSelection.MaxBenchSize;

    private ClubSquad(ClubId clubId, IReadOnlyList<SquadMember> members)
    {
        ClubId = clubId;
        Members = members;
    }

    public ClubId ClubId { get; }

    public IReadOnlyList<SquadMember> Members { get; }

    public bool ContainsPlayer(PlayerId playerId) =>
        Members.Any(m => m.PlayerId == playerId);

    public bool ContainsSlot(int slotIndex) =>
        Members.Any(m => m.SlotIndex == slotIndex);

    public static ClubSquad Empty(ClubId clubId) => new(clubId, Array.Empty<SquadMember>());

    public static ClubSquad Rehydrate(ClubId clubId, IReadOnlyList<SquadMember> members)
    {
        ArgumentNullException.ThrowIfNull(members);
        return FromMembers(clubId, members);
    }

    public ClubSquad EnsureMember(PlayerId playerId, int slotIndex, GameDate joinedOn)
    {
        if (ContainsPlayer(playerId))
        {
            var existing = Members.Single(m => m.PlayerId == playerId);
            if (existing.SlotIndex == slotIndex)
            {
                return this;
            }

            throw new TeamPreparationInvariantViolationException(
                $"Player {playerId.Value} is already in club {ClubId.Value} squad at slot {existing.SlotIndex}.");
        }

        if (ContainsSlot(slotIndex))
        {
            throw new TeamPreparationInvariantViolationException(
                $"Slot {slotIndex} is already occupied in club {ClubId.Value} squad.");
        }

        var next = Members.Append(SquadMember.Create(playerId, slotIndex, joinedOn)).ToArray();
        return FromMembers(ClubId, next);
    }

    public ClubSquad WithoutPlayer(PlayerId playerId)
    {
        if (!ContainsPlayer(playerId))
        {
            return this;
        }

        return FromMembers(ClubId, Members.Where(m => m.PlayerId != playerId).ToArray());
    }

    public ClubSquad ReplaceMembers(IReadOnlyList<SquadMember> members) =>
        FromMembers(ClubId, members);

    private static ClubSquad FromMembers(ClubId clubId, IReadOnlyList<SquadMember> members)
    {
        if (members.Count > MaxMembers)
        {
            throw new TeamPreparationInvariantViolationException(
                $"Club squad cannot exceed {MaxMembers} members.");
        }

        if (members.Select(m => m.PlayerId.Value).Distinct().Count() != members.Count)
        {
            throw new TeamPreparationInvariantViolationException(
                "Club squad cannot contain duplicate players.");
        }

        if (members.Select(m => m.SlotIndex).Distinct().Count() != members.Count)
        {
            throw new TeamPreparationInvariantViolationException(
                "Club squad cannot contain duplicate slots.");
        }

        var ordered = members.OrderBy(m => m.SlotIndex).ThenBy(m => m.PlayerId.Value).ToArray();
        return new ClubSquad(clubId, ordered);
    }
}
