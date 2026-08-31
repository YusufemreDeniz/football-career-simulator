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
using FootballCareerSimulator.Domain.ClubGovernance;
using FootballCareerSimulator.Domain.Shared;
using FootballCareerSimulator.Domain.Transfer;
using FootballCareerSimulator.Domain.WorldCalendar;
using FootballCareerSimulator.Infrastructure.Career;
using Microsoft.Data.Sqlite;

namespace FootballCareerSimulator.Tests.Transfer;

public sealed class TransferBudgetReservationTests : IDisposable
{
    private static readonly GameDate Day = GameDate.FromCalendarDate(2026, 7, 1);

    private readonly string _tempDirectory = Path.Combine(
        Path.GetTempPath(),
        "fcs-transfer-budget",
        Guid.NewGuid().ToString("N"));

    public TransferBudgetReservationTests() => Directory.CreateDirectory(_tempDirectory);

    public void Dispose()
    {
        SqliteConnection.ClearAllPools();
        if (Directory.Exists(_tempDirectory))
        {
            Directory.Delete(_tempDirectory, recursive: true);
        }
    }

    [Fact]
    public void SubmitOffer_ReservesFee_Reject_Releases()
    {
        var modules = CreateWithSportingApproval();
        var before = modules.Clubs.TransferBudget.Get(new ClubId(1));

        modules.Transfer.ClubOffers.SubmitClubOffer(
            modules.Transfer.ProcessStore.Processes.Single().ProcessId,
            1_000_000,
            Day);

        var reserved = modules.Clubs.TransferBudget.Get(new ClubId(1));
        Assert.Equal(before.Reserved + 1_000_000, reserved.Reserved);
        Assert.Equal(before.Available - 1_000_000, reserved.Available);

        modules.Transfer.ClubOffers.RejectPendingOffer(
            modules.Transfer.ProcessStore.Processes.Single().ProcessId);

        var after = modules.Clubs.TransferBudget.Get(new ClubId(1));
        Assert.Equal(before.Reserved, after.Reserved);
        Assert.Equal(before.Available, after.Available);
    }

    [Fact]
    public void OversizedOffer_IsRejectedWithoutReservation()
    {
        var modules = CreateWithSportingApproval();
        var processId = modules.Transfer.ProcessStore.Processes.Single().ProcessId;
        var before = modules.Clubs.TransferBudget.Get(new ClubId(1));

        Assert.Throws<TransferInvariantViolationException>(() =>
            modules.Transfer.ClubOffers.SubmitClubOffer(processId, before.Available + 1, Day));

        Assert.Equal(before.Reserved, modules.Clubs.TransferBudget.Get(new ClubId(1)).Reserved);
    }

    [Fact]
    public void Complete_AppliesReservedSpend()
    {
        var modules = CreateWithFinancialApproval();
        var processId = modules.Transfer.ProcessStore.Processes.Single().ProcessId;
        var fee = 5_000_000;
        var before = modules.Clubs.TransferBudget.Get(new ClubId(1));
        Assert.Equal(fee, before.Reserved);

        modules.Transfer.Completion.Complete(processId, Day);

        var after = modules.Clubs.TransferBudget.Get(new ClubId(1));
        Assert.Equal(0, after.Reserved);
        Assert.Equal(before.Spent + fee, after.Spent);
    }

    [Fact]
    public void SaveLoad_PreservesBudgetAtSchemaV28()
    {
        var clubs = ClubGovernanceModule.CreateMvpLeague();
        clubs.TransferBudget.Reserve(new ClubId(1), 250_000);

        var world = WorldCalendarModule.Create(Day, rootSeed: 11);
        var persistence = new CareerSqlitePersistence();
        var path = Path.Combine(_tempDirectory, "budget.db");
        persistence.Save(
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
            Array.Empty<PlayerContractProposal>(),
            Array.Empty<Domain.SocialContinuity.Promise>(),
            Array.Empty<Domain.SocialContinuity.MemoryRecord>());

        var loaded = persistence.Load(path);
        Assert.Equal(47, loaded.SchemaVersion);
        var club = loaded.ClubRegistry.GetClubOrThrow(new ClubId(1));
        Assert.Equal(250_000, club.ReservedTransferFunds);
        Assert.Equal(Club.DefaultTransferBudgetLimit(club.SportiveStrength), club.TransferBudgetLimit);
    }

    private static (
        WorldCalendarModule World,
        ClubGovernanceModule Clubs,
        TransferModule Transfer) CreateWithSportingApproval()
    {
        var modules = CreateBase();
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
        return (modules.World, modules.Clubs, modules.Transfer);
    }

    private static (
        WorldCalendarModule World,
        ClubGovernanceModule Clubs,
        TransferModule Transfer) CreateWithFinancialApproval()
    {
        var modules = CreateBase();
        modules.Players.Development.EnsureClub(new ClubId(1), modules.World.TimelineStore.Timeline.RootSeed, Day);
        modules.Players.Development.EnsureClub(new ClubId(2), modules.World.TimelineStore.Timeline.RootSeed, Day);
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
        modules.Transfer.ClubOffers.SubmitClubOffer(process.ProcessId, 5_000_000, Day);
        modules.Transfer.ClubOffers.AcceptPendingOffer(process.ProcessId);
        modules.Transfer.ContractProposals.SubmitContractProposal(process.ProcessId, 25_000, 3, Day);
        modules.Transfer.ContractProposals.AcceptPendingProposal(process.ProcessId);
        modules.Transfer.Processes.RequestFinancialApproval(process.ProcessId);
        modules.Transfer.Processes.GrantFinancialApproval(process.ProcessId);

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
        modules.TeamPrep.ClubSquad.SyncFromActiveContracts(new ClubId(1), Day);

        return (modules.World, modules.Clubs, modules.Transfer);
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
            transferBudget: clubs.TransferBudget);
        return (world, clubs, transfer, contracts, players, teamPrep);
    }
}
