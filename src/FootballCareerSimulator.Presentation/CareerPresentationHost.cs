using FootballCareerSimulator.Application.Career.Services;
using FootballCareerSimulator.Application.ClubGovernance.Composition;
using FootballCareerSimulator.Application.Competition.Composition;
using FootballCareerSimulator.Application.Competition.Infrastructure;
using FootballCareerSimulator.Application.Competition.Services;
using FootballCareerSimulator.Application.ContractRegistration.Composition;
using FootballCareerSimulator.Application.Interaction.Composition;
using FootballCareerSimulator.Application.Interaction.Infrastructure;
using FootballCareerSimulator.Application.Interaction.Services;
using FootballCareerSimulator.Application.ManagerCareer.Composition;
using FootballCareerSimulator.Application.PlayerCareer.Composition;
using FootballCareerSimulator.Application.PlayerCareer.Infrastructure;
using FootballCareerSimulator.Application.SocialContinuity.Composition;
using FootballCareerSimulator.Application.TeamPreparation.Composition;
using FootballCareerSimulator.Application.TrainingPhysicalState.Composition;
using FootballCareerSimulator.Application.TrainingPhysicalState.Infrastructure;
using FootballCareerSimulator.Application.ContractRegistration.Services;
using FootballCareerSimulator.Application.SocialContinuity.Services;
using FootballCareerSimulator.Application.Transfer.Composition;
using FootballCareerSimulator.Application.Transfer.Services;
using FootballCareerSimulator.Application.WorldCalendar.Composition;
using FootballCareerSimulator.Application.WorldCalendar.Infrastructure;
using FootballCareerSimulator.Application.WorldCalendar.Ports;
using FootballCareerSimulator.Domain.ClubGovernance;
using FootballCareerSimulator.Domain.Competition;
using FootballCareerSimulator.Domain.WorldCalendar;
using FootballCareerSimulator.Infrastructure.Career;
using FootballCareerSimulator.Simulation;
using Godot;

namespace FootballCareerSimulator.Presentation;

public sealed class CareerPresentationHost
{
    public CareerPresentationHost(
        WorldCalendarModule worldModule,
        CompetitionModule competitionModule,
        ClubGovernanceModule clubModule,
        ManagerCareerModule managerModule,
        TeamPreparationModule teamPreparationModule,
        TrainingPhysicalStateModule trainingModule,
        PlayerCareerModule playerCareerModule,
        ContractRegistrationModule contractModule,
        TransferModule transferModule,
        SocialContinuityModule socialContinuityModule,
        InteractionModule interactionModule,
        CareerGameSessionService gameSession,
        string defaultSavePath)
    {
        WorldModule = worldModule ?? throw new ArgumentNullException(nameof(worldModule));
        CompetitionModule = competitionModule ?? throw new ArgumentNullException(nameof(competitionModule));
        ClubModule = clubModule ?? throw new ArgumentNullException(nameof(clubModule));
        ManagerModule = managerModule ?? throw new ArgumentNullException(nameof(managerModule));
        TeamPreparationModule = teamPreparationModule
            ?? throw new ArgumentNullException(nameof(teamPreparationModule));
        TrainingModule = trainingModule ?? throw new ArgumentNullException(nameof(trainingModule));
        PlayerCareerModule = playerCareerModule
            ?? throw new ArgumentNullException(nameof(playerCareerModule));
        ContractModule = contractModule ?? throw new ArgumentNullException(nameof(contractModule));
        TransferModule = transferModule ?? throw new ArgumentNullException(nameof(transferModule));
        SocialContinuityModule = socialContinuityModule
            ?? throw new ArgumentNullException(nameof(socialContinuityModule));
        InteractionModule = interactionModule ?? throw new ArgumentNullException(nameof(interactionModule));
        GameSession = gameSession ?? throw new ArgumentNullException(nameof(gameSession));
        DefaultSavePath = defaultSavePath ?? throw new ArgumentNullException(nameof(defaultSavePath));
    }

    public WorldCalendarModule WorldModule { get; }
    public CompetitionModule CompetitionModule { get; }
    public ClubGovernanceModule ClubModule { get; }
    public ManagerCareerModule ManagerModule { get; }
    public TeamPreparationModule TeamPreparationModule { get; }
    public TrainingPhysicalStateModule TrainingModule { get; }
    public PlayerCareerModule PlayerCareerModule { get; }
    public ContractRegistrationModule ContractModule { get; }
    public TransferModule TransferModule { get; }
    public SocialContinuityModule SocialContinuityModule { get; }
    public InteractionModule InteractionModule { get; }
    public CareerGameSessionService GameSession { get; }
    public string DefaultSavePath { get; }

    public static CareerPresentationHost CreateDefault(string? defaultSavePath = null)
    {
        var startDate = GameDate.FromCalendarDate(2026, 7, 1);
        var timelineStore = new InMemoryWorldTimelineStore(
            WorldTimeline.Create(startDate, rootSeed: 42, SimulationRandomContext.Version));
        var competitionStore = new InMemoryLeagueCompetitionStore(
            new LeagueCompetition(new CompetitionId(MvpLeagueIdentity.DefaultCompetitionId)));
        var clubModule = ClubGovernanceModule.CreateMvpLeague();
        const long startingClubId = 1;
        var startingStrength = clubModule.Queries.GetClub(startingClubId)?.SportiveStrength ?? 50;
        var decisionStore = new InMemoryDecisionRequestStore();
        var dialogueSessionStore = new InMemoryDialogueSessionStore();
        var worldModule = WorldCalendarModule.Create(
            startDate,
            rootSeed: 42,
            blockerSources:
            [
                new UnplayedFixturesTimeAdvanceBlockerSource(competitionStore, timelineStore),
                new DecisionRequestTimeAdvanceBlockerSource(decisionStore),
            ],
            timelineStore: timelineStore);

        var managerModule = ManagerCareerModule.CreateForCareer(
            startDate,
            clubModule.Store,
            worldModule.TimelineStore,
            startingClubId: startingClubId,
            clubSportiveStrength: startingStrength);

        var trainingStore = new InMemoryTrainingPhysicalStateStore();
        var playerStore = new InMemoryPlayerCareerStore();
        var contractModule = ContractRegistrationModule.Create(
            playerStore,
            managerModule.Store,
            worldModule.TimelineStore);
        var playerCareer = PlayerCareerModule.Create(
            managerModule.Store,
            worldModule.TimelineStore,
            trainingStore,
            playerStore,
            contractModule.Registration);
        var teamPreparation = TeamPreparationModule.Create(
            competitionStore,
            managerModule.Store,
            trainingStore: trainingStore,
            timelineStore: worldModule.TimelineStore,
            contractStore: contractModule.Store,
            playerCareerStore: playerStore);
        var training = TrainingPhysicalStateModule.Create(
            managerModule.Store,
            worldModule.TimelineStore,
            trainingStore,
            playerCareer.Development,
            teamPreparation.ClubSquad,
            teamPreparation.SelectionStore);
        var socialContinuity = SocialContinuityModule.Create();
        clubModule.BindWageBudget(contractModule.Store);
        contractModule.Registration.BindPromiseInvalidation(socialContinuity.Invalidation);
        contractModule.Registration.BindRelationships(socialContinuity.RelationshipEvaluation);
        managerModule.AcceptJobOffer?.BindCareerMemory(socialContinuity.CareerMemory);
        managerModule.AcceptJobOffer?.BindClubHistoryMemory(socialContinuity.ClubHistoryMemory);
        managerModule.AcceptJobOffer?.BindRelationships(socialContinuity.RelationshipEvaluation);
        var transferModule = TransferModule.Create(
            contractModule.Store,
            teamPreparation.SquadStore,
            managerModule.Store,
            contractModule.Registration,
            teamPreparation.ClubSquad
                ?? throw new InvalidOperationException("ClubSquad service is required for transfers."),
            transferWindow: worldModule.TransferWindowQuery,
            transferBudget: clubModule.TransferBudget,
            wageBudget: clubModule.WageBudget,
            clubRegistry: clubModule.Store,
            freeAgentStore: contractModule.FreeAgentStore,
            promiseInvalidation: socialContinuity.Invalidation,
            transferMemory: socialContinuity.TransferMemory,
            clubHistoryMemory: socialContinuity.ClubHistoryMemory,
            relationships: socialContinuity.RelationshipEvaluation);
        var eventRuleForBind = worldModule.EventRuleEvaluation
            ?? throw new InvalidOperationException("Event & Rule Evaluation iskeleti bağlı değil.");
        worldModule.CloseTransferWindow.BindWindowClosedConsequences(
            new TransferWindowClosedConsequenceApplier(
                transferModule.WindowClose,
                eventRuleForBind.Gate));
        worldModule.AdvanceSimulationTime.BindContractExpiryConsequences(
            new ContractExpiryDayBoundaryApplier(
                contractModule.Registration,
                eventRuleForBind.Gate));
        var interactionModule = InteractionModule.Create(
            managerModule.Store,
            socialContinuity.PlayingTime,
            decisionStore,
            socialContinuity.RelationshipEvaluation,
            socialContinuity.DecisionMemory,
            socialContinuity.PromiseStore,
            dialogueSessionStore,
            socialContinuity.StartingOpportunity,
            transferModule.Needs);
        worldModule.AdvanceSimulationTime.BindPromiseDeadlineConsequences(
            new PromiseDeadlineDayBoundaryApplier(
                socialContinuity.StartingOpportunity,
                eventRuleForBind.Gate,
                interactionModule.PromiseBroken));

        var competitionModule = CompetitionModule.CreateForCareerFromStore(
            competitionStore,
            worldModule.TimelineStore,
            clubModule.Store,
            managerModule.Store,
            teamPreparation.SelectionStore,
            training.Store,
            playerCareer.Store,
            playerCareer.Development,
            teamPreparation.TacticPlanStore,
            teamPreparation.SquadStore,
            socialContinuity.StartingOpportunity,
            socialContinuity.SelectionMemory,
            socialContinuity.PlayingTime,
            socialContinuity.Invalidation,
            socialContinuity.CareerMemory,
            socialContinuity.ClubHistoryMemory,
            socialContinuity.MatchPerformanceMemory,
            socialContinuity.RelationshipEvaluation,
            interactionModule.PostMatchPress);
        var persistence = new CareerSqlitePersistence();

        var eventRule = worldModule.EventRuleEvaluation
            ?? throw new InvalidOperationException("Event & Rule Evaluation iskeleti bağlı değil.");

        ICommandIdempotencyReset[] idempotencyResets =
        [
            worldModule.AdvanceSimulationTime,
            worldModule.OpenPlanningPeriod,
            worldModule.CompletePlanningPeriod,
            worldModule.OpenTransferWindow,
            worldModule.CloseTransferWindow,
            eventRule,
            .. competitionModule.IdempotencyResets,
            .. teamPreparation.IdempotencyResets,
            training.IdempotencyReset,
            .. managerModule.IdempotencyResets,
        ];

        var gameSession = new CareerGameSessionService(
            worldModule.TimelineStore,
            competitionModule.Store,
            clubModule.Store,
            managerModule.Store,
            teamPreparation.SelectionStore,
            teamPreparation.SquadStore,
            teamPreparation.TacticPlanStore,
            transferModule.NeedStore,
            transferModule.ShortlistStore,
            transferModule.TargetStore,
            transferModule.ProcessStore,
            transferModule.OfferStore,
            transferModule.ProposalStore,
            socialContinuity.PromiseStore,
            socialContinuity.MemoryStore,
            socialContinuity.RelationshipStore,
            interactionModule.DecisionRequestStore,
            interactionModule.DialogueSessionStore,
            interactionModule.DisciplinaryActionStore,
            training.Store,
            playerCareer.Store,
            contractModule.Store,
            contractModule.FreeAgentStore,
            persistence,
            idempotencyResets,
            eventRule.Registry,
            eventRule.ScheduledEvaluationStore);

        var savePath = defaultSavePath ?? Path.Combine(OS.GetUserDataDir(), "career_save.db");
        return new CareerPresentationHost(
            worldModule,
            competitionModule,
            clubModule,
            managerModule,
            teamPreparation,
            training,
            playerCareer,
            contractModule,
            transferModule,
            socialContinuity,
            interactionModule,
            gameSession,
            savePath);
    }
}
