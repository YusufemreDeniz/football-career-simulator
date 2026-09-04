using FootballCareerSimulator.Application.CareerHub.Queries;
using FootballCareerSimulator.Application.ClubGovernance.Composition;
using FootballCareerSimulator.Application.Competition.Commands;
using FootballCareerSimulator.Application.Competition.Composition;
using FootballCareerSimulator.Application.Interaction.Composition;
using FootballCareerSimulator.Application.ManagerCareer.Composition;
using FootballCareerSimulator.Application.SocialContinuity.Composition;
using FootballCareerSimulator.Application.SocialContinuity.Services;
using FootballCareerSimulator.Application.TeamPreparation.Commands;
using FootballCareerSimulator.Application.TeamPreparation.Composition;
using FootballCareerSimulator.Application.TeamPreparation.Infrastructure;
using FootballCareerSimulator.Application.TeamPreparation.Services;
using FootballCareerSimulator.Application.WorldCalendar.Commands;
using FootballCareerSimulator.Application.WorldCalendar.Composition;
using FootballCareerSimulator.Domain.Competition;
using FootballCareerSimulator.Domain.Interaction;
using FootballCareerSimulator.Domain.ManagerCareer;
using FootballCareerSimulator.Domain.PlayerCareer;
using FootballCareerSimulator.Domain.Shared;
using FootballCareerSimulator.Domain.SocialContinuity;
using FootballCareerSimulator.Domain.TeamPreparation;
using FootballCareerSimulator.Domain.WorldCalendar;
using FootballCareerSimulator.Infrastructure.Career;
using Microsoft.Data.Sqlite;
using PlayerCareerAggregate = FootballCareerSimulator.Domain.PlayerCareer.PlayerCareer;

namespace FootballCareerSimulator.Tests.SocialContinuity;

/// <summary>
/// D-313 / docs/14 §25.1 Forma Sözü Zinciri çapraz senaryo kanıtı.
/// </summary>
public sealed class FormaSozuZinciriTests : IDisposable
{
    private static readonly GameDate Day = GameDate.FromCalendarDate(2026, 8, 1);

    private readonly string _tempDirectory = Path.Combine(
        Path.GetTempPath(),
        "fcs-forma-sozu",
        Guid.NewGuid().ToString("N"));

    public FormaSozuZinciriTests() => Directory.CreateDirectory(_tempDirectory);

    public void Dispose()
    {
        SqliteConnection.ClearAllPools();
        if (Directory.Exists(_tempDirectory))
        {
            Directory.Delete(_tempDirectory, recursive: true);
        }
    }

    [Fact]
    public void FormaSozuZinciri_RequestGrantBreak_MemoryRelationshipDecision_SaveLoadPreserves()
    {
        var modules = CreateBound();
        var playerId = new PlayerId(501);
        var managerId = modules.Manager.Store.Career.ManagerId;
        var clubId = modules.Manager.Store.Career.ActiveEmployment!.ClubId;

        var request = modules.Interaction.Decisions.OpenPlayingTimeRequest(playerId, Day);
        modules.Interaction.Decisions.Answer(
            request.DecisionRequestId,
            DecisionRequest.OptionGrantPlayingTimePromise,
            Day,
            playingTimeTargetAppearances: 2);

        var active = Assert.Single(
            modules.Social.PromiseStore.Promises,
            p => p.Kind == PromiseKind.PlayingTime && p.IsActive);
        Assert.Equal(2, active.TargetStarts);
        Assert.Equal(56, modules.Social.RelationshipStore.FindPlayerToManager(501, 1)!.Trust);

        var midPath = Path.Combine(_tempDirectory, "mid-chain.db");
        SaveSocial(modules, midPath);
        ReloadSocial(modules, midPath);

        active = Assert.Single(
            modules.Social.PromiseStore.Promises,
            p => p.Kind == PromiseKind.PlayingTime && p.IsActive);
        Assert.Equal(PromiseStatus.Active, active.Status);
        Assert.Equal(0, active.StartsGiven);

        var advance = modules.World.AdvanceSimulationTime.Handle(
            new AdvanceSimulationTimeCommand(Guid.NewGuid(), active.DeadlineOn.DayNumber));

        Assert.True(advance.Succeeded);
        Assert.Equal(1, advance.PromiseDeadlineResolvedCount);
        Assert.Equal(1, advance.PromiseBrokenCrisisOpenedCount);

        var broken = Assert.Single(modules.Social.PromiseStore.Promises, p => p.Kind == PromiseKind.PlayingTime);
        Assert.Equal(PromiseStatus.Broken, broken.Status);
        Assert.Equal(2, modules.Social.MemoryStore.Memories.Count(m => m.Category == MemoryCategory.Promise));
        Assert.Equal(1, modules.Social.MemoryStore.Memories.Count(m => m.Category == MemoryCategory.Trust));

        var relationship = modules.Social.RelationshipStore.FindPlayerToManager(501, 1)!;
        Assert.Equal(44, relationship.Trust);
        Assert.Equal("PromiseBrokenTrust", relationship.LastChangeReasonCode);

        Assert.Contains(
            modules.Interaction.DecisionRequestStore.Requests,
            r => r.IsOpen
                 && r.Kind == DecisionRequestKind.PlayingTimeRequest
                 && r.SubjectPlayerId.Value == 501);

        var causality = PlayerManagementDigest.Compose(
            clubId,
            managerId.Value,
            modules.World.TimelineStore.Timeline.CurrentDate,
            [new Simulation.TeamPreparation.MvpSquadPlayerProfile("Test", Simulation.TeamPreparation.MvpSquadPositionGroup.Midfielder)],
            [new Application.TeamPreparation.Queries.SquadPlayerReadModel(1, "Test", 0, 70)],
            ClubSquad.Empty(clubId).EnsureMember(playerId, 0, Day),
            Array.Empty<PlayerCareerAggregate>(),
            new Dictionary<(long ClubId, int SlotIndex), Domain.TrainingPhysicalState.PlayerPhysicalState>(),
            Array.Empty<Domain.ContractRegistration.PlayerContract>(),
            modules.Social.RelationshipStore.Relationships,
            modules.Social.PromiseStore.Promises,
            modules.Social.MemoryStore.Memories).Players.Single();

        Assert.Contains("bozuldu", causality.CausalitySummary, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("güveni düşürdü", causality.CausalitySummary, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("hafıza", causality.CausalitySummary, StringComparison.OrdinalIgnoreCase);

        var finalPath = Path.Combine(_tempDirectory, "after-break.db");
        SaveSocial(modules, finalPath);
        var loaded = new CareerSqlitePersistence().Load(finalPath);
        Assert.Equal(PromiseStatus.Broken, Assert.Single(loaded.Promises).Status);
        Assert.Contains(loaded.Memories, m => m.Category == MemoryCategory.Promise);
        Assert.Contains(loaded.Memories, m => m.Category == MemoryCategory.Trust);
        Assert.Equal(44, Assert.Single(loaded.Relationships).Trust);
        Assert.Equal("PromiseBrokenTrust", Assert.Single(loaded.Relationships).LastChangeReasonCode);

        modules.Social.StartingOpportunity.EvaluateDeadlines(
            modules.World.TimelineStore.Timeline.CurrentDate,
            _ => Assert.Fail("Broken promise must not resolve twice."));
        Assert.Equal(44, modules.Social.RelationshipStore.FindPlayerToManager(501, 1)!.Trust);
        Assert.Equal(
            1,
            modules.Interaction.DecisionRequestStore.Requests.Count(r =>
                r.IsOpen && r.Kind == DecisionRequestKind.PlayingTimeRequest && r.SubjectPlayerId.Value == 501));
    }

    [Fact]
    public void FormaSozuZinciri_MatchAppearance_Fulfills_AndRaisesTrust()
    {
        var world = WorldCalendarModule.Create(Day, rootSeed: 77);
        var clubs = ClubGovernanceModule.CreateMvpLeague();
        var manager = ManagerCareerModule.CreateNewCareer(Day, startingClubId: 1);
        var social = SocialContinuityModule.Create();
        var selectionStore = new InMemoryMatchSelectionStore();
        var competition = CompetitionModule.CreateForCareer(
            world.TimelineStore,
            clubs.Store,
            manager.Store,
            selectionStore,
            selectionMemory: social.SelectionMemory,
            playingTimePromises: social.PlayingTime,
            relationships: social.RelationshipEvaluation);
        var teamPrep = TeamPreparationModule.Create(competition.Store, manager.Store, selectionStore);
        var interaction = InteractionModule.Create(
            manager.Store,
            social.PlayingTime,
            relationships: social.RelationshipEvaluation,
            decisionMemory: social.DecisionMemory,
            promiseStore: social.PromiseStore,
            relationshipStore: social.RelationshipStore);

        var benchPlayerId = PlayerId.FromClubSlot(1, MatchSelection.StartingXiSize);
        var request = interaction.Decisions.OpenPlayingTimeRequest(benchPlayerId, Day);
        interaction.Decisions.Answer(
            request.DecisionRequestId,
            DecisionRequest.OptionGrantPlayingTimePromise,
            Day,
            playingTimeTargetAppearances: 1);

        competition.CreateSeason.Handle(new CreateSeasonCommand(Guid.NewGuid(), 1, Day.DayNumber));
        for (var club = 1L; club <= CompetitionMvpConstraints.LeagueTeamCount; club++)
        {
            competition.RegisterSeasonParticipant.Handle(
                new RegisterSeasonParticipantCommand(Guid.NewGuid(), 1, club));
        }

        competition.StartSeason.Handle(new StartSeasonCommand(Guid.NewGuid(), 1, Day.DayNumber));
        competition.PlanLeagueFixtures.Handle(
            new PlanLeagueFixturesCommand(Guid.NewGuid(), 1, Day.DayNumber, StartingFixtureId: 1));

        var fixture = competition.Queries.GetSeasonFixtures(1)
            .First(f => f.HomeClubId == 1 || f.AwayClubId == 1);
        teamPrep.ApproveDefaultSelection.Handle(
            new ApproveDefaultMatchSelectionCommand(Guid.NewGuid(), fixture.FixtureId, ClubId: 1));
        competition.PlayFixtureMatch!.Handle(
            new PlayFixtureMatchCommand(Guid.NewGuid(), 1, fixture.FixtureId, Day.DayNumber));

        var promise = Assert.Single(social.PromiseStore.Promises, p => p.Kind == PromiseKind.PlayingTime);
        Assert.Equal(PromiseStatus.Fulfilled, promise.Status);
        Assert.Equal(1, promise.StartsGiven);
        Assert.Contains(social.MemoryStore.Memories, m => m.Category == MemoryCategory.Promise);
        Assert.Contains(social.MemoryStore.Memories, m => m.Category == MemoryCategory.Trust);

        var relationship = social.RelationshipStore.FindPlayerToManager(benchPlayerId.Value, 1)!;
        Assert.Equal(64, relationship.Trust);
        Assert.Equal("PromiseFulfilledTrust", relationship.LastChangeReasonCode);
        Assert.DoesNotContain(
            interaction.DecisionRequestStore.Requests,
            r => r.IsOpen && r.SubjectPlayerId == benchPlayerId);
    }

    [Fact]
    public void FormaSozuZinciri_SecondBreak_EscalatesToTransfer_WhenTrustLow()
    {
        var modules = CreateBound();
        var playerId = new PlayerId(502);

        modules.Social.PlayingTime.Create(
            modules.Manager.Store.Career.ManagerId,
            playerId,
            new ClubId(1),
            targetAppearances: 2,
            deadlineOn: Day.AddDays(2),
            createdOn: Day);
        modules.Social.StartingOpportunity.EvaluateDeadlines(
            Day.AddDays(2),
            promise => modules.Interaction.PromiseBroken.TryOpenAfterBroken(promise, Day.AddDays(2)));

        Assert.Contains(
            modules.Interaction.DecisionRequestStore.Requests,
            r => r.Kind == DecisionRequestKind.PlayingTimeRequest && r.SubjectPlayerId.Value == 502);

        modules.Social.PlayingTime.Create(
            modules.Manager.Store.Career.ManagerId,
            playerId,
            new ClubId(1),
            targetAppearances: 2,
            deadlineOn: Day.AddDays(9),
            createdOn: Day.AddDays(3));
        modules.Social.StartingOpportunity.EvaluateDeadlines(
            Day.AddDays(9),
            promise => modules.Interaction.PromiseBroken.TryOpenAfterBroken(promise, Day.AddDays(9)));

        Assert.Equal(
            RelationshipDimensionBand.Low,
            RelationshipDimensionBands.FromValue(
                modules.Social.RelationshipStore.FindPlayerToManager(502, 1)!.Trust));
        Assert.Contains(
            modules.Interaction.DecisionRequestStore.Requests,
            r => r.IsOpen
                 && r.Kind == DecisionRequestKind.TransferRequest
                 && r.SubjectPlayerId.Value == 502);

        var grant = modules.Interaction.DialogueOptions
            .GetForDecision(
                modules.Interaction.DecisionRequestStore.Requests
                    .First(r => r.IsOpen && r.Kind == DecisionRequestKind.PlayingTimeRequest)
                    .DecisionRequestId)
            .Options
            .First(o => o.OptionCode == DecisionRequest.OptionGrantPlayingTimePromise);
        Assert.False(grant.IsEligible);
        Assert.Contains("Güven düşük", grant.IneligibilityReason!, StringComparison.Ordinal);
    }

    [Fact]
    public void PlayingTimePromise_PlayerMissingFromSquad_IsAtRiskInPreMatchTension()
    {
        var modules = CreateMatchTensionBound();
        modules.Social.PlayingTime.Create(
            modules.Manager.Store.Career.ManagerId,
            new PlayerId(9999),
            new ClubId(1),
            targetAppearances: 2,
            deadlineOn: Day.AddDays(20),
            createdOn: Day);

        var fixtures = modules.Competition.Queries.GetSeasonFixtures(1);
        var managed = fixtures.First(f => f.HomeClubId == 1 || f.AwayClubId == 1);
        var current = modules.World.Queries.GetCurrentGameDate().DayNumber;
        if (managed.ScheduledDayNumber > current)
        {
            modules.World.AdvanceSimulationTime.Handle(
                new AdvanceSimulationTimeCommand(Guid.NewGuid(), managed.ScheduledDayNumber));
        }

        var tension = modules.TeamPrep.PromiseTension.GetForNextDueMatch(
            modules.World.Queries.GetCurrentGameDate().DayNumber);
        Assert.NotNull(tension);
        Assert.True(tension!.HasTension);
        Assert.Equal(PreMatchPromiseTensionQueryService.ToneAtRisk, tension.ToneCode);
        var line = Assert.Single(tension.Lines, l => l.KindName == "Oyun süresi");
        Assert.Equal(PreMatchPromiseTensionQueryService.PlacementOut, line.PlacementCode);
    }

    private static (
        WorldCalendarModule World,
        ManagerCareerModule Manager,
        SocialContinuityModule Social,
        InteractionModule Interaction) CreateBound()
    {
        var world = WorldCalendarModule.Create(Day, rootSeed: 313);
        var manager = ManagerCareerModule.CreateNewCareer(Day, startingClubId: 1);
        var social = SocialContinuityModule.Create();
        var interaction = InteractionModule.Create(
            manager.Store,
            social.PlayingTime,
            relationships: social.RelationshipEvaluation,
            decisionMemory: social.DecisionMemory,
            promiseStore: social.PromiseStore,
            startingOpportunity: social.StartingOpportunity,
            relationshipStore: social.RelationshipStore);

        world.AdvanceSimulationTime.BindPromiseDeadlineConsequences(
            new PromiseDeadlineDayBoundaryApplier(
                social.StartingOpportunity,
                world.EventRuleEvaluation!.Gate,
                interaction.PromiseBroken));

        return (world, manager, social, interaction);
    }

    private static (
        WorldCalendarModule World,
        CompetitionModule Competition,
        ManagerCareerModule Manager,
        TeamPreparationModule TeamPrep,
        SocialContinuityModule Social) CreateMatchTensionBound()
    {
        var world = WorldCalendarModule.Create(Day, rootSeed: 314);
        var clubs = ClubGovernanceModule.CreateMvpLeague();
        var manager = ManagerCareerModule.CreateNewCareer(Day, startingClubId: 1);
        var selectionStore = new InMemoryMatchSelectionStore();
        var competition = CompetitionModule.CreateForCareer(
            world.TimelineStore,
            clubs.Store,
            manager.Store,
            selectionStore);
        var social = SocialContinuityModule.Create();
        var teamPrep = TeamPreparationModule.Create(
            competition.Store,
            manager.Store,
            selectionStore,
            promiseStore: social.PromiseStore);

        competition.CreateSeason.Handle(new CreateSeasonCommand(Guid.NewGuid(), 1, Day.DayNumber));
        for (var club = 1L; club <= CompetitionMvpConstraints.LeagueTeamCount; club++)
        {
            competition.RegisterSeasonParticipant.Handle(
                new RegisterSeasonParticipantCommand(Guid.NewGuid(), 1, club));
        }

        competition.StartSeason.Handle(new StartSeasonCommand(Guid.NewGuid(), 1, Day.DayNumber));
        competition.PlanLeagueFixtures.Handle(
            new PlanLeagueFixturesCommand(Guid.NewGuid(), 1, Day.DayNumber, StartingFixtureId: 1));

        var squad = ClubSquad.Empty(new ClubId(1));
        for (var slot = 0; slot < MatchSelection.StartingXiSize + MatchSelection.MaxBenchSize; slot++)
        {
            squad = squad.EnsureMember(PlayerId.FromClubSlot(1, slot), slot, Day);
        }

        teamPrep.SquadStore.Upsert(squad);

        return (world, competition, manager, teamPrep, social);
    }

    private static void SaveSocial(
        (
            WorldCalendarModule World,
            ManagerCareerModule Manager,
            SocialContinuityModule Social,
            InteractionModule Interaction) modules,
        string path) =>
        new CareerSqlitePersistence().Save(
            path,
            modules.World.TimelineStore.Timeline,
            new LeagueCompetition(new CompetitionId(1)),
            ClubGovernanceModule.CreateMvpLeague().Store.Registry,
            modules.Manager.Store.Career,
            Array.Empty<MatchSelection>(),
            Array.Empty<Domain.TrainingPhysicalState.WeeklyTrainingPlan>(),
            Array.Empty<Domain.TrainingPhysicalState.PlayerPhysicalState>(),
            Array.Empty<PlayerCareerAggregate>(),
            Array.Empty<Domain.ContractRegistration.PlayerContract>(),
            Array.Empty<ClubSquad>(),
            Array.Empty<Domain.ContractRegistration.PlayerFreeAgency>(),
            Array.Empty<TacticPlan>(),
            Array.Empty<Domain.Transfer.TransferNeed>(),
            Array.Empty<Domain.Transfer.ShortlistEntry>(),
            Array.Empty<Domain.Transfer.TransferTarget>(),
            Array.Empty<Domain.Transfer.TransferProcess>(),
            Array.Empty<Domain.Transfer.ClubOffer>(),
            Array.Empty<Domain.Transfer.PlayerContractProposal>(),
            modules.Social.PromiseStore.Promises,
            modules.Social.MemoryStore.Memories,
            modules.Social.RelationshipStore.Relationships,
            modules.Interaction.DecisionRequestStore.Requests);

    private static void ReloadSocial(
        (
            WorldCalendarModule World,
            ManagerCareerModule Manager,
            SocialContinuityModule Social,
            InteractionModule Interaction) modules,
        string path)
    {
        var loaded = new CareerSqlitePersistence().Load(path);
        modules.Social.PromiseStore.ReplaceAll(loaded.Promises);
        modules.Social.MemoryStore.ReplaceAll(loaded.Memories);
        modules.Social.RelationshipStore.ReplaceAll(loaded.Relationships);
        modules.Interaction.DecisionRequestStore.ReplaceAll(loaded.DecisionRequests);
    }
}
