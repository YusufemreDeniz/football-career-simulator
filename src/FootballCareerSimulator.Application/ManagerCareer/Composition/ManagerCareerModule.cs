namespace FootballCareerSimulator.Application.ManagerCareer.Composition;

using FootballCareerSimulator.Application.ManagerCareer.Infrastructure;
using FootballCareerSimulator.Application.ManagerCareer.Ports;
using FootballCareerSimulator.Application.ManagerCareer.Services;
using FootballCareerSimulator.Domain.ManagerCareer;
using FootballCareerSimulator.Domain.Shared;
using FootballCareerSimulator.Domain.WorldCalendar;

public sealed class ManagerCareerModule
{
    public ManagerCareerModule(IManagerCareerStore store, ManagerCareerQueryService queries)
    {
        Store = store;
        Queries = queries;
    }

    public IManagerCareerStore Store { get; }

    public ManagerCareerQueryService Queries { get; }

    public static ManagerCareerModule CreateNewCareer(
        GameDate startDate,
        long managerId = 1,
        string displayName = "Teknik Direktör",
        long startingClubId = 1,
        int clubSportiveStrength = 50)
    {
        var career = ManagerCareer.StartNewCareerForClubStrength(
            new ManagerId(managerId),
            displayName,
            new ClubId(startingClubId),
            startDate,
            clubSportiveStrength);
        var store = new InMemoryManagerCareerStore(career);
        return new ManagerCareerModule(store, new ManagerCareerQueryService(store));
    }
}
