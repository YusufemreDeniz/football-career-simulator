using FootballCareerSimulator.Application.PlayerCareer.Ports;
using FootballCareerSimulator.Application.TeamPreparation.Ports;
using FootballCareerSimulator.Application.TeamPreparation.Queries;
using FootballCareerSimulator.Domain.Shared;
using FootballCareerSimulator.Simulation.TeamPreparation;

namespace FootballCareerSimulator.Application.TeamPreparation.Services;

public sealed class SquadQueryService
{
    private readonly IClubSquadStore? _squadStore;
    private readonly IPlayerCareerStore? _playerCareerStore;

    public SquadQueryService(
        IClubSquadStore? squadStore = null,
        IPlayerCareerStore? playerCareerStore = null)
    {
        _squadStore = squadStore;
        _playerCareerStore = playerCareerStore;
    }

    public IReadOnlyList<SquadPlayerReadModel> GetClubSquad(long clubId, int rootSeed)
    {
        var id = new ClubId(clubId);
        var names = MvpSquadRosterGenerator.GeneratePlayerNames(id, rootSeed);
        var squad = _squadStore?.Get(id);

        if (squad is not null && squad.Members.Count > 0)
        {
            return squad.Members
                .OrderBy(m => m.SlotIndex)
                .Select(m =>
                {
                    var career = _playerCareerStore?.Get(id, m.SlotIndex);
                    var rating = career?.CurrentAbility
                        ?? MvpSquadStrengthCalculator.GetPlayerRating(id, rootSeed, m.SlotIndex);
                    var name = m.SlotIndex < names.Count
                        ? names[m.SlotIndex]
                        : $"Oyuncu {m.SlotIndex + 1}";
                    return new SquadPlayerReadModel(
                        SquadNumber: m.SlotIndex + 1,
                        DisplayName: name,
                        SlotIndex: m.SlotIndex,
                        Rating: rating);
                })
                .ToArray();
        }

        return names
            .Select((name, index) => new SquadPlayerReadModel(
                SquadNumber: index + 1,
                DisplayName: name,
                SlotIndex: index,
                Rating: MvpSquadStrengthCalculator.GetPlayerRating(id, rootSeed, index)))
            .ToArray();
    }
}
