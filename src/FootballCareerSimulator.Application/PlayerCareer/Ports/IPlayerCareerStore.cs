using FootballCareerSimulator.Domain.PlayerCareer;
using FootballCareerSimulator.Domain.Shared;
using PlayerCareerAggregate = FootballCareerSimulator.Domain.PlayerCareer.PlayerCareer;

namespace FootballCareerSimulator.Application.PlayerCareer.Ports;

public interface IPlayerCareerStore
{
    IReadOnlyList<PlayerCareerAggregate> Careers { get; }

    IReadOnlyDictionary<(long ClubId, int SlotIndex), PlayerCareerAggregate> ByClubSlot { get; }

    PlayerCareerAggregate? GetById(PlayerId playerId) =>
        Careers.FirstOrDefault(c => c.Id == playerId);

    PlayerCareerAggregate? Get(ClubId clubId, int slotIndex);

    IReadOnlyList<PlayerCareerAggregate> GetActiveByOriginClub(ClubId clubId) =>
        Careers
            .Where(c => c.OriginClubId == clubId && !c.IsRetired)
            .OrderBy(c => c.SlotIndex)
            .ThenBy(c => c.Id.Value)
            .ToArray();

    void Upsert(PlayerCareerAggregate career);

    void ReplaceAll(IEnumerable<PlayerCareerAggregate> careers);

    void ReplaceClub(ClubId clubId, IEnumerable<PlayerCareerAggregate> careers);
}
