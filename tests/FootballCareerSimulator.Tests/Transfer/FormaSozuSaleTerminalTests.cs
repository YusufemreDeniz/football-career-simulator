using FootballCareerSimulator.Application.ClubGovernance.Composition;
using FootballCareerSimulator.Application.Competition.Composition;
using FootballCareerSimulator.Application.ContractRegistration.Composition;
using FootballCareerSimulator.Application.Interaction.Composition;
using FootballCareerSimulator.Application.ManagerCareer.Composition;
using FootballCareerSimulator.Application.PlayerCareer.Composition;
using FootballCareerSimulator.Application.PlayerCareer.Infrastructure;
using FootballCareerSimulator.Application.SocialContinuity.Composition;
using FootballCareerSimulator.Application.TeamPreparation.Composition;
using FootballCareerSimulator.Application.TeamPreparation.Services;
using FootballCareerSimulator.Application.TrainingPhysicalState.Infrastructure;
using FootballCareerSimulator.Application.Transfer.Composition;
using FootballCareerSimulator.Application.Transfer.Queries;
using FootballCareerSimulator.Application.WorldCalendar.Composition;
using FootballCareerSimulator.Application.WorldCalendar.Infrastructure;
using FootballCareerSimulator.Domain.Interaction;
using FootballCareerSimulator.Domain.PlayerCareer;
using FootballCareerSimulator.Domain.Shared;
using FootballCareerSimulator.Domain.SocialContinuity;
using FootballCareerSimulator.Domain.TeamPreparation;
using FootballCareerSimulator.Domain.Transfer;
using FootballCareerSimulator.Domain.WorldCalendar;

namespace FootballCareerSimulator.Tests.Transfer;

/// <summary>
/// D-358: Forma Sözü kabul → yönetilen satış → terminal sosyal state + okunabilir aftermath.
/// </summary>
public sealed class FormaSozuSaleTerminalTests
{
    private static readonly GameDate Day = GameDate.FromCalendarDate(2026, 8, 1);

    [Fact]
    public void ManagedSaleAftermathDigest_ReadsTerminalSocialState()
    {
        var playerId = 501L;
        var managerId = 1L;
        var exitCode = TransferNeed.BuildPlayerExitReasonCode(new PlayerId(playerId));
        var closedNeed = TransferNeed.Rehydrate(
            new TransferNeedId(1),
            new ClubId(1),
            TransferNeedKind.PlayerExitRequest,
            TransferNeedStatus.Closed,
            priority: 5,
            exitCode,
            Day,
            Day.AddDays(1));
        var promise = Promise.Rehydrate(
            new PromiseId(1),
            PromiseKind.PlayingTime,
            new ActorRef(ActorKind.Manager, managerId),
            new ActorRef(ActorKind.Player, playerId),
            new ClubId(1),
            targetStarts: 2,
            startsGiven: 0,
            Day.AddDays(10),
            Day,
            PromiseStatus.Invalidated,
            terminalOn: Day.AddDays(1),
            countedFixtureIds: Array.Empty<long>());
        var relationship = RelationshipRecord.Rehydrate(
            new RelationshipId(1),
            new ActorRef(ActorKind.Player, playerId),
            new ActorRef(ActorKind.Manager, managerId),
            trust: 20,
            respect: 50,
            professionalCompatibility: 50,
            RelationshipStatus.Dormant,
            Day,
            Day.AddDays(1),
            "TransferCompleted",
            Array.Empty<string>());
        var memory = MemoryRecord.CreateTransferCompleted(
            new MemoryId(1),
            new ActorRef(ActorKind.Player, playerId),
            new TransferProcessId(9),
            Day.AddDays(1),
            isFreeAgent: false);

        var digest = ManagedSaleAftermathDigest.Compose(
            playerId,
            managerId,
            "Alıcı FC",
            transferFee: 1_000_000,
            activeContractCount: 24,
            maxSquadMembers: 25,
            [closedNeed],
            [promise],
            [relationship],
            [memory]);

        Assert.True(digest.ExitNeedClosed);
        Assert.True(digest.NoActivePromise);
        Assert.True(digest.PromiseInvalidated);
        Assert.True(digest.RelationshipDormant);
        Assert.True(digest.TransferMemoryRecorded);
        Assert.Contains("forma sözü geçersizleşti", digest.ToStatusMessage(), StringComparison.OrdinalIgnoreCase);
        Assert.Contains("uyku moduna", digest.ToStatusMessage(), StringComparison.OrdinalIgnoreCase);
        Assert.Contains("hafızaya", digest.ToStatusMessage(), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void FormaSozu_AcknowledgeSell_ClosesNeedPromiseRelationshipAndWritesMemory()
    {
        var modules = CreateBound(seed: 358);
        modules.Players.Development.EnsureClub(new ClubId(1), 358, Day);
        modules.TeamPrep.ClubSquad!.SyncFromActiveContracts(new ClubId(1), Day);
        SeedUnmanagedClubSquadsWithSpace(modules);

        var playerId = new PlayerId(
            modules.TeamPrep.ClubSquad.SuggestSaleCandidatePlayerId(new ClubId(1), Day)!.Value);
        var managerId = modules.Manager.Store.Career.ManagerId;

        modules.Social.PlayingTime.Create(
            managerId,
            playerId,
            new ClubId(1),
            targetAppearances: 2,
            deadlineOn: Day.AddDays(2),
            createdOn: Day);
        modules.Social.StartingOpportunity.EvaluateDeadlines(Day.AddDays(2));

        modules.Social.PlayingTime.Create(
            managerId,
            playerId,
            new ClubId(1),
            targetAppearances: 2,
            deadlineOn: Day.AddDays(8),
            createdOn: Day.AddDays(3));
        modules.Social.StartingOpportunity.EvaluateDeadlines(
            Day.AddDays(8),
            promise => modules.Interaction.PromiseBroken.TryOpenAfterBroken(promise, Day.AddDays(8)));

        var transferRequest = Assert.Single(
            modules.Interaction.DecisionRequestStore.Requests,
            r => r.IsOpen && r.Kind == DecisionRequestKind.TransferRequest);
        modules.Interaction.Decisions.Answer(
            transferRequest.DecisionRequestId,
            DecisionRequest.OptionAcknowledgeTransferRequest,
            Day.AddDays(8));

        Assert.Equal(playerId.Value, modules.Transfer.Queries.GetPreferredPlayerExitSaleCandidateId());

        var sale = modules.Transfer.AiSimulation.TrySellManagedClubPlayer(
            new ClubId(1),
            playerId,
            Day.AddDays(8),
            worldSeed: 358);

        Assert.True(sale.Sold, sale.Message);

        var digest = ManagedSaleAftermathDigest.Compose(
            playerId.Value,
            managerId.Value,
            $"Kulüp #{sale.BuyingClubId}",
            sale.TransferFee!.Value,
            modules.TeamPrep.ClubSquad.GetCapacityDigest(new ClubId(1), Day.AddDays(8)).ActiveContractCount,
            ClubSquad.MaxMembers,
            modules.Transfer.NeedStore.Needs,
            modules.Social.PromiseStore.Promises,
            modules.Social.RelationshipStore.Relationships,
            modules.Social.MemoryStore.Memories);

        Assert.True(digest.ExitNeedClosed);
        Assert.True(digest.NoActivePromise);
        Assert.False(digest.PromiseInvalidated); // kırık söz zaten terminal; Invalidate yalnızca Active'e uygulanır
        Assert.True(digest.RelationshipDormant);
        Assert.True(digest.TransferMemoryRecorded);
        Assert.Contains("Bozulmuş forma sözü", digest.ToStatusMessage(), StringComparison.Ordinal);
        Assert.DoesNotContain(
            modules.Social.PromiseStore.Promises,
            p => p.Promisee.Id == playerId.Value && p.IsActive);
        Assert.Contains(
            modules.Social.PromiseStore.Promises,
            p => p.Promisee.Id == playerId.Value && p.Status == PromiseStatus.Broken);
        Assert.Equal(
            RelationshipStatus.Dormant,
            modules.Social.RelationshipStore.FindPlayerToManager(playerId.Value, managerId.Value)!.Status);
    }

    private static (
        WorldCalendarModule World,
        ManagerCareerModule Manager,
        SocialContinuityModule Social,
        InteractionModule Interaction,
        TransferModule Transfer,
        ContractRegistrationModule Contracts,
        PlayerCareerModule Players,
        TeamPreparationModule TeamPrep) CreateBound(int seed)
    {
        var world = WorldCalendarModule.Create(Day, rootSeed: seed);
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
        var social = SocialContinuityModule.Create();
        var transfer = TransferModule.Create(
            contracts.Store,
            teamPrep.SquadStore,
            manager.Store,
            contracts.Registration,
            teamPrep.ClubSquad!,
            transferWindow: new TimelineTransferWindowQuery(world.TimelineStore),
            transferBudget: clubs.TransferBudget,
            clubRegistry: clubs.Store,
            freeAgentStore: contracts.FreeAgentStore,
            promiseInvalidation: social.Invalidation,
            transferMemory: social.TransferMemory,
            clubHistoryMemory: social.ClubHistoryMemory,
            relationships: social.RelationshipEvaluation);
        var interaction = InteractionModule.Create(
            manager.Store,
            social.PlayingTime,
            relationships: social.RelationshipEvaluation,
            decisionMemory: social.DecisionMemory,
            promiseStore: social.PromiseStore,
            startingOpportunity: social.StartingOpportunity,
            transferNeeds: transfer.Needs,
            relationshipStore: social.RelationshipStore,
            memoryStore: social.MemoryStore);

        return (world, manager, social, interaction, transfer, contracts, players, teamPrep);
    }

    private static void SeedUnmanagedClubSquadsWithSpace(
        (
            WorldCalendarModule World,
            ManagerCareerModule Manager,
            SocialContinuityModule Social,
            InteractionModule Interaction,
            TransferModule Transfer,
            ContractRegistrationModule Contracts,
            PlayerCareerModule Players,
            TeamPreparationModule TeamPrep) modules)
    {
        for (var clubId = 2L; clubId <= 6L; clubId++)
        {
            var id = new ClubId(clubId);
            modules.Players.Development.EnsureClub(id, modules.World.TimelineStore.Timeline.RootSeed, Day);
            FreeOneSlot(modules.Contracts, modules.TeamPrep.ClubSquad!, modules.Players.Store, id, Day);
        }
    }

    private static void FreeOneSlot(
        ContractRegistrationModule contracts,
        ClubSquadService clubSquad,
        Application.PlayerCareer.Ports.IPlayerCareerStore playerStore,
        ClubId clubId,
        GameDate day)
    {
        var outgoing = playerStore.Careers
            .Where(c => c.OriginClubId.Value == clubId.Value)
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
        clubSquad.SyncFromActiveContracts(clubId, day);
    }
}
