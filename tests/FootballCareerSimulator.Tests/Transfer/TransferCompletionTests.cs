using FootballCareerSimulator.Application.ClubGovernance.Composition;
using FootballCareerSimulator.Application.Competition.Composition;
using FootballCareerSimulator.Application.ContractRegistration.Composition;
using FootballCareerSimulator.Application.ManagerCareer.Composition;
using FootballCareerSimulator.Application.PlayerCareer.Composition;
using FootballCareerSimulator.Application.PlayerCareer.Infrastructure;
using FootballCareerSimulator.Application.TeamPreparation.Composition;
using FootballCareerSimulator.Application.TeamPreparation.Services;
using FootballCareerSimulator.Application.TrainingPhysicalState.Infrastructure;
using FootballCareerSimulator.Application.Transfer.Composition;
using FootballCareerSimulator.Application.WorldCalendar.Composition;
using FootballCareerSimulator.Domain.Competition;
using FootballCareerSimulator.Domain.PlayerCareer;
using FootballCareerSimulator.Domain.Shared;
using FootballCareerSimulator.Domain.TeamPreparation;
using FootballCareerSimulator.Domain.TrainingPhysicalState;
using FootballCareerSimulator.Domain.Transfer;
using FootballCareerSimulator.Domain.WorldCalendar;
using FootballCareerSimulator.Infrastructure.Career;
using Microsoft.Data.Sqlite;

namespace FootballCareerSimulator.Tests.Transfer;

public sealed class TransferCompletionTests : IDisposable
{
    private static readonly GameDate Day = GameDate.FromCalendarDate(2026, 7, 1);

    private readonly string _tempDirectory = Path.Combine(
        Path.GetTempPath(),
        "fcs-transfer-completion",
        Guid.NewGuid().ToString("N"));

    public TransferCompletionTests() => Directory.CreateDirectory(_tempDirectory);

    public void Dispose()
    {
        SqliteConnection.ClearAllPools();
        if (Directory.Exists(_tempDirectory))
        {
            Directory.Delete(_tempDirectory, recursive: true);
        }
    }

    [Fact]
    public void Complete_ClubToClub_MovesContractAndSquad()
    {
        var modules = CreateModulesThroughFinancialApproval(freeAgent: false);
        var process = modules.Transfer.ProcessStore.Processes.Single();
        var playerId = process.PlayerId;
        var buying = process.BuyingClubId;
        var selling = process.SellingClubId!.Value;

        Assert.True(modules.Squad.Get(selling)!.ContainsPlayer(playerId));
        Assert.False(modules.Squad.Get(buying)?.ContainsPlayer(playerId) ?? true);

        var completed = modules.Transfer.Completion.Complete(process.ProcessId, Day);

        Assert.Equal(TransferProcessStatus.Archived, completed.Status);
        Assert.True(completed.IsCompleted);
        Assert.Equal(buying, modules.Contracts.Registration.GetActiveClub(playerId, Day));
        Assert.Equal(25_000, modules.Contracts.Store.GetByPlayer(playerId)!.WeeklyWage);
        Assert.True(modules.Squad.Get(buying)!.ContainsPlayer(playerId));
        Assert.False(modules.Squad.Get(selling)!.ContainsPlayer(playerId));
    }

    [Fact]
    public void Complete_FreeAgent_ActivatesWithoutLastClubRestriction()
    {
        var modules = CreateModulesThroughFinancialApproval(freeAgent: true);
        var process = modules.Transfer.ProcessStore.Processes.Single();
        Assert.True(process.IsFreeAgent);

        var completed = modules.Transfer.Completion.Complete(process.ProcessId, Day);

        Assert.Equal(TransferProcessStatus.Archived, completed.Status);
        Assert.Equal(process.BuyingClubId, modules.Contracts.Registration.GetActiveClub(process.PlayerId, Day));
        Assert.False(modules.Contracts.Registration.IsFreeAgent(process.PlayerId));
        Assert.True(modules.Squad.Get(process.BuyingClubId)!.ContainsPlayer(process.PlayerId));
    }

    [Fact]
    public void Complete_IsIdempotentWhenArchived()
    {
        var modules = CreateModulesThroughFinancialApproval(freeAgent: false);
        var processId = modules.Transfer.ProcessStore.Processes.Single().ProcessId;
        var first = modules.Transfer.Completion.Complete(processId, Day);
        var second = modules.Transfer.Completion.Complete(processId, Day.AddDays(1));

        Assert.Equal(first.Status, second.Status);
        Assert.Equal(TransferProcessStatus.Archived, second.Status);
    }

    [Fact]
    public void Complete_WithoutFinancialApproval_Throws()
    {
        var modules = CreateModulesThroughFinancialApproval(freeAgent: false);
        var process = modules.Transfer.ProcessStore.Processes.Single();
        // rewind to player agreement by rehydrating store entry
        var pending = TransferProcess.Rehydrate(
            process.ProcessId,
            process.NeedId,
            process.TargetId,
            process.BuyingClubId,
            process.PlayerId,
            process.SellingClubId,
            process.IsFreeAgent,
            TransferProcessStatus.PlayerAgreementReached,
            failureReasonCode: null,
            process.OpenedOn,
            terminalOn: null);
        modules.Transfer.ProcessStore.Upsert(pending);

        Assert.Throws<TransferInvariantViolationException>(() =>
            modules.Transfer.Completion.Complete(process.ProcessId, Day));
    }

    [Fact]
    public void SaveLoad_PreservesArchivedCompletionAtSchemaV26()
    {
        var modules = CreateModulesThroughFinancialApproval(freeAgent: false);
        var processId = modules.Transfer.ProcessStore.Processes.Single().ProcessId;
        modules.Transfer.Completion.Complete(processId, Day);

        var persistence = new CareerSqlitePersistence();
        var path = Path.Combine(_tempDirectory, "completion.db");
        persistence.Save(
            path,
            modules.World.TimelineStore.Timeline,
            new LeagueCompetition(new CompetitionId(1)),
            modules.Clubs.Store.Registry,
            modules.Manager.Store.Career,
            Array.Empty<MatchSelection>(),
            Array.Empty<WeeklyTrainingPlan>(),
            Array.Empty<PlayerPhysicalState>(),
            modules.Players.Careers,
            modules.Contracts.Store.Contracts,
            modules.Squad.Squads,
            modules.Contracts.FreeAgentStore.FreeAgents,
            Array.Empty<TacticPlan>(),
            modules.Transfer.NeedStore.Needs,
            modules.Transfer.ShortlistStore.Entries,
            modules.Transfer.TargetStore.Targets,
            modules.Transfer.ProcessStore.Processes,
            modules.Transfer.OfferStore.Offers,
            modules.Transfer.ProposalStore.Proposals,
            Array.Empty<Domain.SocialContinuity.Promise>(),
            Array.Empty<Domain.SocialContinuity.MemoryRecord>());

        var loaded = persistence.Load(path);
        Assert.Equal(40, loaded.SchemaVersion);
        Assert.Single(loaded.TransferProcesses);
        Assert.Equal(TransferProcessStatus.Archived, loaded.TransferProcesses[0].Status);
        Assert.Equal(
            modules.Transfer.ProcessStore.Processes.Single().BuyingClubId,
            loaded.Contracts.Single(c => c.PlayerId == loaded.TransferProcesses[0].PlayerId).ClubId);
    }

    private static (
        WorldCalendarModule World,
        ClubGovernanceModule Clubs,
        ManagerCareerModule Manager,
        TransferModule Transfer,
        ContractRegistrationModule Contracts,
        Application.TeamPreparation.Ports.IClubSquadStore Squad,
        Application.PlayerCareer.Ports.IPlayerCareerStore Players) CreateModulesThroughFinancialApproval(
        bool freeAgent)
    {
        var world = WorldCalendarModule.Create(Day, rootSeed: 81);
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

        players.Development.EnsureClub(new ClubId(1), world.TimelineStore.Timeline.RootSeed, Day);
        players.Development.EnsureClub(new ClubId(2), world.TimelineStore.Timeline.RootSeed, Day);
        teamPrep.ClubSquad!.SyncFromActiveContracts(new ClubId(1), Day);
        teamPrep.ClubSquad.SyncFromActiveContracts(new ClubId(2), Day);

        var transfer = TransferModule.Create(
            contracts.Store,
            teamPrep.SquadStore,
            manager.Store,
            contracts.Registration,
            teamPrep.ClubSquad,
            transferBudget: clubs.TransferBudget);

        transfer.Needs.Declare(
            new ClubId(1),
            TransferNeedKind.PositionGap,
            priority: 4,
            freeAgent ? "FreeAgentGap" : "ManualPositionGap",
            Day);

        TransferProcess process;
        if (freeAgent)
        {
            var need = transfer.NeedStore.Needs.Single();
            var faPlayer = new PlayerId(42);
            playerStore.Upsert(Domain.PlayerCareer.PlayerCareer.Rehydrate(
                faPlayer,
                new ClubId(2),
                slotIndex: 0,
                currentAbility: 60,
                potentialAbility: 70,
                developmentPoints: 0,
                lastDevelopedOn: null,
                birthYear: 2000,
                lastAgedCalendarYear: null));
            contracts.FreeAgentStore.Upsert(
                Domain.ContractRegistration.PlayerFreeAgency.Release(faPlayer, new ClubId(2), Day));

            var target = TransferTarget.List(
                new TransferTargetId(1),
                need.NeedId,
                new ClubId(1),
                faPlayer,
                shortlistEntryId: null,
                Day);
            transfer.TargetStore.Upsert(target);
            process = transfer.Processes.OpenOldestListedTargetForClub(new ClubId(1), Day);
            Assert.True(process.IsFreeAgent);
            transfer.Processes.RequestSportingApproval(process.ProcessId);
            transfer.Processes.GrantSportingApproval(process.ProcessId);
            transfer.ContractProposals.SubmitContractProposal(process.ProcessId, 25_000, 3, Day);
            transfer.ContractProposals.AcceptPendingProposal(process.ProcessId);
        }
        else
        {
            transfer.ShortlistTargets.SuggestAndListTargetForOldestOpenNeed(new ClubId(1), Day);
            process = transfer.Processes.OpenOldestListedTargetForClub(new ClubId(1), Day);
            transfer.Processes.RequestSportingApproval(process.ProcessId);
            transfer.Processes.GrantSportingApproval(process.ProcessId);
            transfer.ClubOffers.SubmitClubOffer(process.ProcessId, 5_000_000, Day);
            transfer.ClubOffers.AcceptPendingOffer(process.ProcessId);
            transfer.ContractProposals.SubmitContractProposal(process.ProcessId, 25_000, 3, Day);
            transfer.ContractProposals.AcceptPendingProposal(process.ProcessId);
        }

        transfer.Processes.RequestFinancialApproval(process.ProcessId);
        transfer.Processes.GrantFinancialApproval(process.ProcessId);

        // Buying club is full (25); free one slot so completion can add the incoming player.
        FreeOneBuyingClubSlot(contracts, teamPrep.ClubSquad, playerStore, Day);

        return (world, clubs, manager, transfer, contracts, teamPrep.SquadStore, playerStore);
    }

    private static void FreeOneBuyingClubSlot(
        ContractRegistrationModule contracts,
        ClubSquadService clubSquad,
        Application.PlayerCareer.Ports.IPlayerCareerStore playerStore,
        GameDate day)
    {
        var outgoing = playerStore.Careers
            .Where(c => c.OriginClubId.Value == 1)
            .OrderByDescending(c => c.SlotIndex)
            .Select(c => c.Id)
            .First(id => contracts.Store.GetByPlayer(id)?.IsActiveOn(day) == true);
        var existing = contracts.Store.GetByPlayer(outgoing)!;
        contracts.Store.Upsert(Domain.ContractRegistration.PlayerContract.Rehydrate(
            existing.Id,
            existing.PlayerId,
            existing.ClubId,
            existing.StartDate,
            existing.StartDate,
            existing.WeeklyWage,
            Domain.ContractRegistration.ContractStatus.Expired));
        clubSquad.SyncFromActiveContracts(new ClubId(1), day);
    }
}
