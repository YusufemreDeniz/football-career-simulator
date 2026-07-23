namespace FootballCareerSimulator.Application.TeamPreparation.Services;

using FootballCareerSimulator.Application.TeamPreparation.Queries;
using FootballCareerSimulator.Domain.Shared;
using FootballCareerSimulator.Simulation.TeamPreparation;

public sealed class SquadQueryService
{
    public IReadOnlyList<SquadPlayerReadModel> GetClubSquad(long clubId, int rootSeed)
    {
        var id = new ClubId(clubId);
        var names = MvpSquadRosterGenerator.GeneratePlayerNames(id, rootSeed);
        return names
            .Select((name, index) => new SquadPlayerReadModel(
                SquadNumber: index + 1,
                DisplayName: name,
                SlotIndex: index,
                Rating: MvpSquadStrengthCalculator.GetPlayerRating(id, rootSeed, index)))
            .ToArray();
    }
}
