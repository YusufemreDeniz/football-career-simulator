using FootballCareerSimulator.Application.ClubGovernance.Composition;
using FootballCareerSimulator.Application.Competition.Composition;
using FootballCareerSimulator.Application.ContractRegistration.Composition;
using FootballCareerSimulator.Application.ManagerCareer.Commands;
using FootballCareerSimulator.Application.ManagerCareer.Composition;
using FootballCareerSimulator.Application.PlayerCareer.Composition;
using FootballCareerSimulator.Application.PlayerCareer.Infrastructure;
using FootballCareerSimulator.Application.SocialContinuity.Composition;
using FootballCareerSimulator.Application.TeamPreparation.Composition;
using FootballCareerSimulator.Application.TeamPreparation.Services;
using FootballCareerSimulator.Application.TrainingPhysicalState.Infrastructure;
using FootballCareerSimulator.Application.Transfer.Composition;
using FootballCareerSimulator.Application.WorldCalendar.Composition;
using FootballCareerSimulator.Domain.Competition;
using FootballCareerSimulator.Domain.ManagerCareer;
using FootballCareerSimulator.Domain.PlayerCareer;
using FootballCareerSimulator.Domain.Shared;
using FootballCareerSimulator.Domain.SocialContinuity;
using FootballCareerSimulator.Domain.Transfer;
using FootballCareerSimulator.Domain.WorldCalendar;
using FootballCareerSimulator.Infrastructure.Career;
using Microsoft.Data.Sqlite;

namespace FootballCareerSimulator.Tests.SocialContinuity;

public sealed class ClubHistoryMemoryTests : IDisposable
{
    private static readonly GameDate Day = GameDate.FromCalendarDate(2026, 8, 1);

    private readonly string _tempDirectory = Path.Combine(
        Path.GetTempPath(),
        "fcs-club-history-memory",
        Guid.NewGuid().ToString("N"));

    public ClubHistoryMemoryTests() => Directory.CreateDirectory(_tempDirectory);

    public void Dispose()
    {
        SqliteConnection.ClearAllPools();
        if (Directory.Exists(_tempDirectory))
        {
            Directory.Delete(_tempDirectory, recursive: true);
        }
    }

    [Fact]
    public void RecordManagerLeftDismissed_CreatesNegativeClubHistory_Idempotently()
    {
        var social = SocialContinuityModule.Create();
        var managerId = new ManagerId(1);
        var clubId = new ClubId(2);
        var fixtureId = new FixtureId(5);

        Assert.Equal(1, social.ClubHistoryMemory.RecordManagerLeftDismissed(managerId, clubId, fixtureId, Day));
        Assert.Equal(0, social.ClubHistoryMemory.RecordManagerLeftDismissed(managerId, clubId, fixtureId, Day));

        var memory = Assert.Single(social.MemoryStore.Memories);
        Assert.Equal(MemoryCategory.ClubHistory, memory.Category);
        Assert.Equal(MemoryValence.Negative, memory.Valence);
        Assert.Equal(MemoryRecord.ClubHistoryLeftDismissedRuleId, memory.RuleId);
        Assert.Equal(2, memory.SubjectId);
    }

    [Fact]
    public void AcceptJobOffer_WhenReturningToLastClub_CreatesClubHistoryReturned()
    {
        var world = WorldCalendarModule.Create(Day, rootSeed: 3);
        var clubs = ClubGovernanceModule.CreateMvpLeague();
        var manager = ManagerCareerModule.CreateForCareer(
            Day,
            clubs.Store,
            world.TimelineStore,
            startingClubId: 1);
        var social = SocialContinuityModule.Create();
        manager.AcceptJobOffer!.BindCareerMemory(social.CareerMemory);
        manager.AcceptJobOffer.BindClubHistoryMemory(social.ClubHistoryMemory);

        var career = ManagerCareer.StartNewCareerForClubStrength(
            new ManagerId(1),
            "TD",
            new ClubId(1),
            Day,
            clubSportiveStrength: 50,
            initialBoardConfidence: 32);
        career = career.ApplyMatchBoardAssessment(
            new FixtureId(1),
            MatchOutcomeForManagedClub.Loss,
            leaguePosition: 20,
            leagueSize: 20).Career;
        career = career.DismissDueToBoardConfidence(new FixtureId(1), Day).Career;
        var offer = JobOffer.CreateOffered(new JobOfferId(99), new ClubId(1), Day);
        manager.Store.Replace(career.ReceiveJobOffer(offer).Career);

        manager.AcceptJobOffer.Handle(new AcceptPendingJobOfferCommand(Guid.NewGuid()));

        var returned = Assert.Single(
            social.MemoryStore.Memories,
            m => m.RuleId == MemoryRecord.ClubHistoryReturnedRuleId);
        Assert.Equal(MemoryCategory.ClubHistory, returned.Category);
        Assert.Equal(MemoryValence.Positive, returned.Valence);
        Assert.Equal(1, returned.SubjectId);
    }

    [Fact]
    public void CompleteTransfer_ClubToClub_CreatesLeftAndJoinedClubHistory()
    {
        var (social, transfer) = CreateThroughFinancialApproval(freeAgent: false);
        var process = transfer.ProcessStore.Processes.Single();

        transfer.Completion.Complete(process.ProcessId, Day);

        Assert.Contains(
            social.MemoryStore.Memories,
            m => m.RuleId == MemoryRecord.ClubHistoryLeftTransferRuleId);
        Assert.Contains(
            social.MemoryStore.Memories,
            m => m.RuleId == MemoryRecord.ClubHistoryJoinedTransferRuleId);
        Assert.Equal(
            2,
            social.MemoryStore.Memories.Count(m => m.Category == MemoryCategory.ClubHistory));
    }

    [Fact]
    public void CompleteTransfer_FreeAgent_CreatesOnlyJoinedClubHistory()
    {
        var (social, transfer) = CreateThroughFinancialApproval(freeAgent: true);
        var process = transfer.ProcessStore.Processes.Single();

        transfer.Completion.Complete(process.ProcessId, Day);

        Assert.DoesNotContain(
            social.MemoryStore.Memories,
            m => m.RuleId == MemoryRecord.ClubHistoryLeftTransferRuleId);
        var joined = Assert.Single(
            social.MemoryStore.Memories,
            m => m.RuleId == MemoryRecord.ClubHistoryJoinedTransferRuleId);
        Assert.Equal(MemoryValence.Positive, joined.Valence);
    }

    [Fact]
    public void SaveLoad_PreservesClubHistoryAtSchemaV31()
    {
        var social = SocialContinuityModule.Create();
        social.ClubHistoryMemory.RecordManagerLeftDismissed(
            new ManagerId(1),
            new ClubId(2),
            new FixtureId(3),
            Day);
        social.ClubHistoryMemory.RecordManagerReturned(
            new ManagerId(1),
            new ClubId(2),
            new JobOfferId(8),
            Day.AddDays(5));

        var path = Path.Combine(_tempDirectory, "club-history.db");
        new CareerSqlitePersistence().Save(
            path,
            WorldCalendarModule.Create(Day, rootSeed: 5).TimelineStore.Timeline,
            new LeagueCompetition(new CompetitionId(1)),
            ClubGovernanceModule.CreateMvpLeague().Store.Registry,
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
            social.PromiseStore.Promises,
            social.MemoryStore.Memories);

        var loaded = new CareerSqlitePersistence().Load(path);
        Assert.Equal(46, loaded.SchemaVersion);
        Assert.Equal(2, loaded.Memories.Count(m => m.Category == MemoryCategory.ClubHistory));
    }

    private static (SocialContinuityModule Social, TransferModule Transfer) CreateThroughFinancialApproval(
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

        var social = SocialContinuityModule.Create();
        var transfer = TransferModule.Create(
            contracts.Store,
            teamPrep.SquadStore,
            manager.Store,
            contracts.Registration,
            teamPrep.ClubSquad,
            transferBudget: clubs.TransferBudget,
            promiseInvalidation: social.Invalidation,
            transferMemory: social.TransferMemory,
            clubHistoryMemory: social.ClubHistoryMemory);

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
        FreeOneBuyingClubSlot(contracts, teamPrep.ClubSquad, playerStore, Day);
        return (social, transfer);
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
