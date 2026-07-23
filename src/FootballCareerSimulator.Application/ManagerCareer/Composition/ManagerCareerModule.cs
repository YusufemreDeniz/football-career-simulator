namespace FootballCareerSimulator.Application.ManagerCareer.Composition;

using FootballCareerSimulator.Application.ClubGovernance.Ports;
using FootballCareerSimulator.Application.ManagerCareer.Infrastructure;
using FootballCareerSimulator.Application.ManagerCareer.Ports;
using FootballCareerSimulator.Application.ManagerCareer.Services;
using FootballCareerSimulator.Application.WorldCalendar.Ports;
using FootballCareerSimulator.Domain.ManagerCareer;
using FootballCareerSimulator.Domain.Shared;
using FootballCareerSimulator.Domain.WorldCalendar;

public sealed class ManagerCareerModule
{
    public ManagerCareerModule(
        IManagerCareerStore store,
        ManagerCareerQueryService queries,
        GenerateUnemployedJobOfferHandler? generateJobOffer = null,
        AcceptPendingJobOfferHandler? acceptJobOffer = null)
    {
        Store = store;
        Queries = queries;
        GenerateJobOffer = generateJobOffer;
        AcceptJobOffer = acceptJobOffer;
    }

    public IManagerCareerStore Store { get; }

    public ManagerCareerQueryService Queries { get; }

    public GenerateUnemployedJobOfferHandler? GenerateJobOffer { get; }

    public AcceptPendingJobOfferHandler? AcceptJobOffer { get; }

    public IReadOnlyList<ICommandIdempotencyReset> IdempotencyResets
    {
        get
        {
            var list = new List<ICommandIdempotencyReset>();
            if (GenerateJobOffer is not null)
            {
                list.Add(GenerateJobOffer);
            }

            if (AcceptJobOffer is not null)
            {
                list.Add(AcceptJobOffer);
            }

            return list;
        }
    }

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

    public static ManagerCareerModule CreateForCareer(
        GameDate startDate,
        IClubRegistryStore clubRegistryStore,
        IWorldTimelineStore timelineStore,
        long managerId = 1,
        string displayName = "Teknik Direktör",
        long startingClubId = 1,
        int clubSportiveStrength = 50)
    {
        var module = CreateNewCareer(
            startDate,
            managerId,
            displayName,
            startingClubId,
            clubSportiveStrength);

        var generate = new GenerateUnemployedJobOfferHandler(
            module.Store,
            clubRegistryStore,
            timelineStore);
        var accept = new AcceptPendingJobOfferHandler(
            module.Store,
            clubRegistryStore,
            timelineStore);

        return new ManagerCareerModule(module.Store, module.Queries, generate, accept);
    }
}
