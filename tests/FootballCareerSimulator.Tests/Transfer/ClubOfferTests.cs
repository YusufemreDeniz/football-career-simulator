using FootballCareerSimulator.Application.ClubGovernance.Composition;
using FootballCareerSimulator.Application.Competition.Composition;
using FootballCareerSimulator.Application.ContractRegistration.Composition;
using FootballCareerSimulator.Application.ManagerCareer.Composition;
using FootballCareerSimulator.Application.PlayerCareer.Composition;
using FootballCareerSimulator.Application.PlayerCareer.Infrastructure;
using FootballCareerSimulator.Application.TeamPreparation.Composition;
using FootballCareerSimulator.Application.TrainingPhysicalState.Infrastructure;
using FootballCareerSimulator.Application.Transfer.Composition;
using FootballCareerSimulator.Application.WorldCalendar.Composition;
using FootballCareerSimulator.Domain.Competition;
using FootballCareerSimulator.Domain.Shared;
using FootballCareerSimulator.Domain.TeamPreparation;
using FootballCareerSimulator.Domain.TrainingPhysicalState;
using FootballCareerSimulator.Domain.Transfer;
using FootballCareerSimulator.Domain.WorldCalendar;
using FootballCareerSimulator.Infrastructure.Career;
using Microsoft.Data.Sqlite;

namespace FootballCareerSimulator.Tests.Transfer;

public sealed class ClubOfferTests : IDisposable
{
    private static readonly GameDate Day = GameDate.FromCalendarDate(2026, 7, 1);

    private readonly string _tempDirectory = Path.Combine(
        Path.GetTempPath(),
        "fcs-club-offer",
        Guid.NewGuid().ToString("N"));

    public ClubOfferTests() => Directory.CreateDirectory(_tempDirectory);

    public void Dispose()
    {
        SqliteConnection.ClearAllPools();
        if (Directory.Exists(_tempDirectory))
        {
            Directory.Delete(_tempDirectory, recursive: true);
        }
    }

    [Fact]
    public void Submit_AfterSportingApproval_EntersNegotiation()
    {
        var modules = CreateModulesWithSportingApproval();
        var process = modules.Transfer.ProcessStore.Processes.Single();

        var offer = modules.Transfer.ClubOffers.SubmitClubOffer(process.ProcessId, 4_000_000, Day);

        Assert.True(offer.IsPending);
        Assert.Equal(1, offer.Round);
        Assert.Equal(
            TransferProcessStatus.ClubNegotiation,
            modules.Transfer.ProcessStore.Get(process.ProcessId)!.Status);
    }

    [Fact]
    public void CounterThenAccept_ReachesClubAgreement()
    {
        var modules = CreateModulesWithSportingApproval();
        var processId = modules.Transfer.ProcessStore.Processes.Single().ProcessId;
        modules.Transfer.ClubOffers.SubmitClubOffer(processId, 3_000_000, Day);
        var counter = modules.Transfer.ClubOffers.CounterPendingOffer(processId, 4_500_000, Day.AddDays(1));
        Assert.Equal(2, counter.Round);

        var accepted = modules.Transfer.ClubOffers.AcceptPendingOffer(processId);
        Assert.Equal(ClubOfferStatus.Accepted, accepted.Status);
        Assert.Equal(
            TransferProcessStatus.ClubAgreementReached,
            modules.Transfer.ProcessStore.Get(processId)!.Status);
    }

    [Fact]
    public void Reject_KeepsNegotiationOpenForNewOffer()
    {
        var modules = CreateModulesWithSportingApproval();
        var processId = modules.Transfer.ProcessStore.Processes.Single().ProcessId;
        modules.Transfer.ClubOffers.SubmitClubOffer(processId, 2_000_000, Day);
        modules.Transfer.ClubOffers.RejectPendingOffer(processId);

        var next = modules.Transfer.ClubOffers.SubmitClubOffer(processId, 2_500_000, Day.AddDays(1));
        Assert.Equal(2, next.Round);
        Assert.True(next.IsPending);
    }

    [Fact]
    public void SaveLoad_PreservesOffersAtSchemaV23()
    {
        var modules = CreateModulesWithSportingApproval();
        var processId = modules.Transfer.ProcessStore.Processes.Single().ProcessId;
        modules.Transfer.ClubOffers.SubmitClubOffer(processId, 6_000_000, Day);

        var persistence = new CareerSqlitePersistence();
        var path = Path.Combine(_tempDirectory, "offers.db");
        persistence.Save(
            path,
            modules.World.TimelineStore.Timeline,
            new LeagueCompetition(new CompetitionId(1)),
            modules.Clubs.Store.Registry,
            modules.Manager.Store.Career,
            Array.Empty<MatchSelection>(),
            Array.Empty<WeeklyTrainingPlan>(),
            Array.Empty<PlayerPhysicalState>(),
            Array.Empty<Domain.PlayerCareer.PlayerCareer>(),
            Array.Empty<Domain.ContractRegistration.PlayerContract>(),
            Array.Empty<ClubSquad>(),
            Array.Empty<Domain.ContractRegistration.PlayerFreeAgency>(),
            Array.Empty<TacticPlan>(),
            modules.Transfer.NeedStore.Needs,
            modules.Transfer.ShortlistStore.Entries,
            modules.Transfer.TargetStore.Targets,
            modules.Transfer.ProcessStore.Processes,
            modules.Transfer.OfferStore.Offers);

        var loaded = persistence.Load(path);
        Assert.Equal(23, loaded.SchemaVersion);
        Assert.Single(loaded.ClubOffers);
        Assert.Equal(6_000_000, loaded.ClubOffers[0].OfferedFee);
        Assert.Equal(ClubOfferStatus.Pending, loaded.ClubOffers[0].Status);
        Assert.Equal(TransferProcessStatus.ClubNegotiation, loaded.TransferProcesses[0].Status);
    }

    private static (
        WorldCalendarModule World,
        ClubGovernanceModule Clubs,
        ManagerCareerModule Manager,
        TransferModule Transfer) CreateModulesWithSportingApproval()
    {
        var world = WorldCalendarModule.Create(Day, rootSeed: 51);
        var clubs = ClubGovernanceModule.CreateMvpLeague();
        var manager = ManagerCareerModule.CreateNewCareer(Day, startingClubId: 1);
        var competition = CompetitionModule.CreateForCareer(world.TimelineStore, clubs.Store);
        var trainingStore = new InMemoryTrainingPhysicalStateStore();
        var playerStore = new InMemoryPlayerCareerStore();
        var contracts = ContractRegistrationModule.Create(
            playerStore,
            manager.Store,
            world.TimelineStore);
        _ = PlayerCareerModule.Create(
            manager.Store,
            world.TimelineStore,
            trainingStore,
            playerStore,
            contracts.Registration);
        var teamPrep = TeamPreparationModule.Create(
            competition.Store,
            manager.Store,
            trainingStore: trainingStore,
            timelineStore: world.TimelineStore,
            contractStore: contracts.Store,
            playerCareerStore: playerStore);
        var transfer = TransferModule.Create(contracts.Store, teamPrep.SquadStore, manager.Store);
        transfer.Needs.Declare(
            new ClubId(1),
            TransferNeedKind.PositionGap,
            priority: 4,
            "ManualPositionGap",
            Day);
        transfer.ShortlistTargets.SuggestAndListTargetForOldestOpenNeed(new ClubId(1), Day);
        var process = transfer.Processes.OpenOldestListedTargetForClub(new ClubId(1), Day);
        transfer.Processes.RequestSportingApproval(process.ProcessId);
        transfer.Processes.GrantSportingApproval(process.ProcessId);
        return (world, clubs, manager, transfer);
    }
}
