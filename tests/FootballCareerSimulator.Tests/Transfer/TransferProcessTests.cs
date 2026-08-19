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

public sealed class TransferProcessTests : IDisposable
{
    private static readonly GameDate Day = GameDate.FromCalendarDate(2026, 7, 1);

    private readonly string _tempDirectory = Path.Combine(
        Path.GetTempPath(),
        "fcs-transfer-process",
        Guid.NewGuid().ToString("N"));

    public TransferProcessTests() => Directory.CreateDirectory(_tempDirectory);

    public void Dispose()
    {
        SqliteConnection.ClearAllPools();
        if (Directory.Exists(_tempDirectory))
        {
            Directory.Delete(_tempDirectory, recursive: true);
        }
    }

    [Fact]
    public void OpenFromListedTarget_StartsUnderEvaluation()
    {
        var modules = CreateModulesWithListedTarget();
        var process = modules.Transfer.Processes.OpenOldestListedTargetForClub(new ClubId(1), Day);

        Assert.True(process.IsActive);
        Assert.Equal(TransferProcessStatus.UnderEvaluation, process.Status);
        Assert.False(process.IsFreeAgent);
        Assert.Equal(2, process.SellingClubId!.Value.Value);
        Assert.Equal(1, modules.Transfer.Queries.GetManagedClubProcesses().ActiveCount);
    }

    [Fact]
    public void Open_IsIdempotentForSameTarget()
    {
        var modules = CreateModulesWithListedTarget();
        var first = modules.Transfer.Processes.OpenOldestListedTargetForClub(new ClubId(1), Day);
        var second = modules.Transfer.Processes.OpenOldestListedTargetForClub(new ClubId(1), Day.AddDays(1));

        Assert.Equal(first.ProcessId, second.ProcessId);
        Assert.Single(modules.Transfer.ProcessStore.Processes);
    }

    [Fact]
    public void Withdraw_ThenArchive_IsTerminal()
    {
        var modules = CreateModulesWithListedTarget();
        var process = modules.Transfer.Processes.OpenOldestListedTargetForClub(new ClubId(1), Day);
        var withdrawn = modules.Transfer.Processes.Withdraw(process.ProcessId, Day.AddDays(1));
        var archived = modules.Transfer.Processes.Archive(withdrawn.ProcessId, Day.AddDays(2));

        Assert.Equal(TransferProcessStatus.Withdrawn, withdrawn.Status);
        Assert.Equal(TransferProcessStatus.Archived, archived.Status);
        Assert.Equal(0, modules.Transfer.Queries.GetManagedClubProcesses().ActiveCount);
    }

    [Fact]
    public void Fail_RequiresReasonAndBlocksSilentReopen()
    {
        var modules = CreateModulesWithListedTarget();
        var process = modules.Transfer.Processes.OpenOldestListedTargetForClub(new ClubId(1), Day);
        modules.Transfer.Processes.Fail(process.ProcessId, "EvaluationStopped", Day.AddDays(1));

        Assert.Throws<TransferInvariantViolationException>(() =>
            modules.Transfer.Processes.Withdraw(process.ProcessId, Day.AddDays(2)));
    }

    [Fact]
    public void SaveLoad_PreservesProcessAtSchemaV21()
    {
        var modules = CreateModulesWithListedTarget();
        modules.Transfer.Processes.OpenOldestListedTargetForClub(new ClubId(1), Day);

        var persistence = new CareerSqlitePersistence();
        var path = Path.Combine(_tempDirectory, "process.db");
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
            modules.Transfer.OfferStore.Offers,
            modules.Transfer.ProposalStore.Proposals,
            Array.Empty<Domain.SocialContinuity.Promise>(),
            Array.Empty<Domain.SocialContinuity.MemoryRecord>());

        var loaded = persistence.Load(path);
        Assert.Equal(45, loaded.SchemaVersion);
        Assert.Single(loaded.TransferProcesses);
        Assert.Equal(TransferProcessStatus.UnderEvaluation, loaded.TransferProcesses[0].Status);
        Assert.Equal(2, loaded.TransferProcesses[0].SellingClubId!.Value.Value);
    }

    private static (
        WorldCalendarModule World,
        ClubGovernanceModule Clubs,
        ManagerCareerModule Manager,
        TransferModule Transfer) CreateModulesWithListedTarget()
    {
        var world = WorldCalendarModule.Create(Day, rootSeed: 31);
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
        var transfer = TransferModule.Create(contracts.Store, teamPrep.SquadStore, manager.Store, contracts.Registration, teamPrep.ClubSquad!, transferBudget: clubs.TransferBudget);
        transfer.Needs.Declare(
            new ClubId(1),
            TransferNeedKind.PositionGap,
            priority: 4,
            "ManualPositionGap",
            Day);
        transfer.ShortlistTargets.SuggestAndListTargetForOldestOpenNeed(new ClubId(1), Day);
        return (world, clubs, manager, transfer);
    }
}
