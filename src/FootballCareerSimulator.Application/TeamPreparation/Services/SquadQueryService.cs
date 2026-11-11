using FootballCareerSimulator.Application.PlayerCareer.Ports;
using FootballCareerSimulator.Application.TeamPreparation.Ports;
using FootballCareerSimulator.Application.TeamPreparation.Queries;
using FootballCareerSimulator.Domain.Shared;
using FootballCareerSimulator.Simulation.DataPacks;
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
                    var career = _playerCareerStore?.Careers.FirstOrDefault(c => c.Id == m.PlayerId)
                        ?? _playerCareerStore?.Get(id, m.SlotIndex);
                    var rating = career?.CurrentAbility
                        ?? MvpSquadStrengthCalculator.GetPlayerRating(id, rootSeed, m.SlotIndex);

                    string name;
                    if (EuropeanElite202627DataPack.TryResolvePlayer(m.PlayerId.Value, out var euroName, out _, out _, out var euroProf))
                    {
                        name = euroName;
                        if (career is null && euroProf?.CurrentAbility is int ca)
                        {
                            rating = ca;
                        }
                    }
                    else if (m.PlayerId.Value >= 1001 && m.PlayerId.Value <= 99999)
                    {
                        var srcClub = (m.PlayerId.Value - 1) / 1000L;
                        var srcSlot = (int)((m.PlayerId.Value - 1) % 1000L);
                        if (srcClub != clubId)
                        {
                            var srcNames = MvpSquadRosterGenerator.GeneratePlayerNames(new ClubId(srcClub), rootSeed);
                            name = srcSlot < srcNames.Count ? srcNames[srcSlot] : $"Oyuncu {m.SlotIndex + 1}";
                            if (career is null)
                            {
                                rating = MvpSquadStrengthCalculator.GetPlayerRating(new ClubId(srcClub), rootSeed, srcSlot);
                            }
                        }
                        else
                        {
                            name = m.SlotIndex < names.Count ? names[m.SlotIndex] : $"Oyuncu {m.SlotIndex + 1}";
                        }
                    }
                    else
                    {
                        name = m.SlotIndex < names.Count
                            ? names[m.SlotIndex]
                            : $"Oyuncu {m.SlotIndex + 1}";
                    }

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
