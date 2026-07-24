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
using FootballCareerSimulator.Application.WorldCalendar.Infrastructure;
using FootballCareerSimulator.Domain.ClubGovernance;
using FootballCareerSimulator.Domain.Shared;
using FootballCareerSimulator.Domain.Transfer;
using FootballCareerSimulator.Domain.WorldCalendar;
using FootballCareerSimulator.Infrastructure.Career;
using Microsoft.Data.Sqlite;

namespace FootballCareerSimulator.Tests.Transfer;

public sealed class WageBudgetGateTests : IDisposable
{
    private static readonly GameDate Day = GameDate.FromCalendarDate(2026, 7, 1);

    private readonly string _tempDirectory = Path.Combine(
        Path.GetTempPath(),
        "fcs-wage-budget",
        Guid.NewGuid().ToString("N"));

    public WageBudgetGateTests() => Directory.CreateDirectory(_tempDirectory);

    public void Dispose()
    {
        SqliteConnection.ClearAllPools();
        if (Directory.Exists(_tempDirectory))
        {
            Directory.Delete(_tempDirectory, recursive: true);
        }
    }

    [Fact]
    public void AcceptProposal_ReservesWage_Complete_Releases()
    {
        var modules = CreateWithSportingApproval();
        var processId = modules.Transfer.ProcessStore.Processes.Single().ProcessId;
        modules.Transfer.ClubOffers.SubmitClubOffer(processId, 1_000_000, Day);
        modules.Transfer.ClubOffers.AcceptPendingOffer(processId);
        modules.Transfer.ContractProposals.SubmitContractProposal(processId, 20_000, 3, Day);

        modules.Transfer.ContractProposals.AcceptPendingProposal(processId);
        var reserved = modules.Clubs.WageBudget!.Get(new ClubId(1), Day);
        Assert.Equal(20_000, reserved.Reserved);

        modules.Transfer.Processes.RequestFinancialApproval(processId);
        modules.Transfer.Processes.GrantFinancialApproval(processId);
        FreeOneBuyingSlot(modules);
        modules.Transfer.Completion.Complete(processId, Day);

        var after = modules.Clubs.WageBudget.Get(new ClubId(1), Day);
        Assert.Equal(0, after.Reserved);
        Assert.True(after.Committed >= 20_000);
    }

    [Fact]
    public void AcceptProposal_OverWageLimit_Fails()
    {
        var modules = CreateWithSportingApproval();
        var processId = modules.Transfer.ProcessStore.Processes.Single().ProcessId;
        modules.Transfer.ClubOffers.SubmitClubOffer(processId, 1_000_000, Day);
        modules.Transfer.ClubOffers.AcceptPendingOffer(processId);

        var available = modules.Clubs.WageBudget!.Get(new ClubId(1), Day).Available;
        modules.Transfer.ContractProposals.SubmitContractProposal(
            processId,
            available + 1,
            3,
            Day);

        Assert.Throws<TransferInvariantViolationException>(() =>
            modules.Transfer.ContractProposals.AcceptPendingProposal(processId));
        Assert.Equal(0, modules.Clubs.WageBudget.Get(new ClubId(1), Day).Reserved);
    }

    [Fact]
    public void FinancialReject_ReleasesWageReservation()
    {
        var modules = CreateWithSportingApproval();
        var processId = modules.Transfer.ProcessStore.Processes.Single().ProcessId;
        modules.Transfer.ClubOffers.SubmitClubOffer(processId, 1_000_000, Day);
        modules.Transfer.ClubOffers.AcceptPendingOffer(processId);
        modules.Transfer.ContractProposals.SubmitContractProposal(processId, 18_000, 2, Day);
        modules.Transfer.ContractProposals.AcceptPendingProposal(processId);
        modules.Transfer.Processes.RequestFinancialApproval(processId);

        modules.Transfer.Processes.RejectFinancialApproval(processId, "BoardRejected", Day);
        Assert.Equal(0, modules.Clubs.WageBudget!.Get(new ClubId(1), Day).Reserved);
    }

    [Fact]
    public void SaveLoad_PreservesWageBudgetAtSchemaV29()
    {
        var clubs = ClubGovernanceModule.CreateMvpLeague();
        clubs.Store.Replace(
            clubs.Store.Registry.WithClub(
                clubs.Store.Registry.GetClubOrThrow(new ClubId(1))
                    .ReserveWeeklyWage(12_000, committedWeeklyWage: 0)));

        var world = WorldCalendarModule.Create(Day, rootSeed: 3);
        var path = Path.Combine(_tempDirectory, "wage.db");
        new CareerSqlitePersistence().Save(
            path,
            world.TimelineStore.Timeline,
            new Domain.Competition.LeagueCompetition(new Domain.Competition.CompetitionId(1)),
            clubs.Store.Registry,
            ManagerCareerModule.CreateNewCareer(Day, startingClubId: 1).Store.Career,
            Array.Empty<Domain.TeamPreparation.MatchSelection>(),
            Array.Empty<Domain.TrainingPhysicalState.WeeklyTrainingPlan>(),
            Array.Empty<Domain.TrainingPhysicalState.PlayerPhysicalState>(),
            Array.Empty<Domain.PlayerCareer.PlayerCareer>(),
            Array.Empty<Domain.ContractRegistration.PlayerContract>(),
            Array.Empty<Domain.TeamPreparation.ClubSquad>(),
            Array.Empty<Domain.ContractRegistration.PlayerFreeAgency>(),
            Array.Empty<Domain.TeamPreparation.TacticPlan>(),
            Array.Empty<TransferNeed>(),
            Array.Empty<ShortlistEntry>(),
            Array.Empty<TransferTarget>(),
            Array.Empty<TransferProcess>(),
            Array.Empty<ClubOffer>(),
            Array.Empty<PlayerContractProposal>());

        var loaded = new CareerSqlitePersistence().Load(path);
        Assert.Equal(29, loaded.SchemaVersion);
        var club = loaded.ClubRegistry.GetClubOrThrow(new ClubId(1));
        Assert.Equal(12_000, club.ReservedWeeklyWage);
        Assert.Equal(Club.DefaultWageBudgetLimit(club.SportiveStrength), club.WageBudgetLimit);
    }

    private static void FreeOneBuyingSlot(
        (
            WorldCalendarModule World,
            ClubGovernanceModule Clubs,
            TransferModule Transfer,
            ContractRegistrationModule Contracts,
            PlayerCareerModule Players,
            TeamPreparationModule TeamPrep) modules)
    {
        var outgoing = modules.Players.Store.Careers
            .Where(c => c.OriginClubId.Value == 1)
            .OrderByDescending(c => c.SlotIndex)
            .Select(c => c.Id)
            .First(id => modules.Contracts.Store.GetByPlayer(id)?.IsActiveOn(Day) == true);
        var existing = modules.Contracts.Store.GetByPlayer(outgoing)!;
        modules.Contracts.Store.Upsert(Domain.ContractRegistration.PlayerContract.Rehydrate(
            existing.Id,
            existing.PlayerId,
            existing.ClubId,
            existing.StartDate,
            existing.StartDate,
            existing.WeeklyWage,
            Domain.ContractRegistration.ContractStatus.Expired));
        modules.TeamPrep.ClubSquad!.SyncFromActiveContracts(new ClubId(1), Day);
    }

    private static (
        WorldCalendarModule World,
        ClubGovernanceModule Clubs,
        TransferModule Transfer,
        ContractRegistrationModule Contracts,
        PlayerCareerModule Players,
        TeamPreparationModule TeamPrep) CreateWithSportingApproval()
    {
        var modules = CreateBase();
        modules.Players.Development.EnsureClub(new ClubId(1), 91, Day);
        modules.Players.Development.EnsureClub(new ClubId(2), 91, Day);
        modules.TeamPrep.ClubSquad!.SyncFromActiveContracts(new ClubId(1), Day);
        modules.TeamPrep.ClubSquad.SyncFromActiveContracts(new ClubId(2), Day);

        modules.Transfer.Needs.Declare(
            new ClubId(1),
            TransferNeedKind.PositionGap,
            priority: 4,
            "ManualPositionGap",
            Day);
        modules.Transfer.ShortlistTargets.SuggestAndListTargetForOldestOpenNeed(new ClubId(1), Day);
        var process = modules.Transfer.Processes.OpenOldestListedTargetForClub(new ClubId(1), Day);
        modules.Transfer.Processes.RequestSportingApproval(process.ProcessId);
        modules.Transfer.Processes.GrantSportingApproval(process.ProcessId);
        return modules;
    }

    private static (
        WorldCalendarModule World,
        ClubGovernanceModule Clubs,
        TransferModule Transfer,
        ContractRegistrationModule Contracts,
        PlayerCareerModule Players,
        TeamPreparationModule TeamPrep) CreateBase()
    {
        var world = WorldCalendarModule.Create(Day, rootSeed: 91);
        var clubs = ClubGovernanceModule.CreateMvpLeague();
        var manager = ManagerCareerModule.CreateNewCareer(Day, startingClubId: 1);
        var competition = CompetitionModule.CreateForCareer(world.TimelineStore, clubs.Store);
        var trainingStore = new InMemoryTrainingPhysicalStateStore();
        var playerStore = new InMemoryPlayerCareerStore();
        var contracts = ContractRegistrationModule.Create(
            playerStore,
            manager.Store,
            world.TimelineStore);
        clubs.BindWageBudget(contracts.Store);
        var players = PlayerCareerModule.Create(
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
        var transfer = TransferModule.Create(
            contracts.Store,
            teamPrep.SquadStore,
            manager.Store,
            contracts.Registration,
            teamPrep.ClubSquad!,
            transferWindow: new TimelineTransferWindowQuery(world.TimelineStore),
            transferBudget: clubs.TransferBudget,
            wageBudget: clubs.WageBudget,
            clubRegistry: clubs.Store,
            freeAgentStore: contracts.FreeAgentStore);
        return (world, clubs, transfer, contracts, players, teamPrep);
    }
}
