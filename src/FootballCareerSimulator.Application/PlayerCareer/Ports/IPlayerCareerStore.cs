using FootballCareerSimulator.Domain.Shared;
using PlayerCareerAggregate = FootballCareerSimulator.Domain.PlayerCareer.PlayerCareer;

namespace FootballCareerSimulator.Application.PlayerCareer.Ports;

public interface IPlayerCareerStore
{
    IReadOnlyList<PlayerCareerAggregate> Careers { get; }

    IReadOnlyDictionary<(long ClubId, int SlotIndex), PlayerCareerAggregate> ByClubSlot { get; }

    PlayerCareerAggregate? Get(ClubId clubId, int slotIndex);

    void Upsert(PlayerCareerAggregate career);

    void ReplaceAll(IEnumerable<PlayerCareerAggregate> careers);

    void ReplaceClub(ClubId clubId, IEnumerable<PlayerCareerAggregate> careers);
}
