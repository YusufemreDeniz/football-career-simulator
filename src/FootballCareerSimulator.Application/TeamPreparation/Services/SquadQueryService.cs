namespace FootballCareerSimulator.Application.TeamPreparation.Services;

using FootballCareerSimulator.Application.TeamPreparation.Queries;
using FootballCareerSimulator.Simulation.TeamPreparation;

public sealed class SquadQueryService
{
    public IReadOnlyList<SquadPlayerReadModel> GetClubSquad(long clubId, int rootSeed)
    {
        var names = MvpSquadRosterGenerator.GeneratePlayerNames(new Domain.Shared.ClubId(clubId), rootSeed);
        return names
            .Select((name, index) => new SquadPlayerReadModel(index + 1, name))
            .ToArray();
    }
}
