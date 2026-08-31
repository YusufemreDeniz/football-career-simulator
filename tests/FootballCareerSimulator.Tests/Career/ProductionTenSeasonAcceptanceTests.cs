using System.Diagnostics;
using FootballCareerSimulator.Application.Career.Services;
using FootballCareerSimulator.Application.ClubGovernance.Composition;
using FootballCareerSimulator.Application.Competition.Commands;
using FootballCareerSimulator.Application.Competition.Composition;
using FootballCareerSimulator.Application.ContractRegistration.Composition;
using FootballCareerSimulator.Application.Interaction.Composition;
using FootballCareerSimulator.Application.ManagerCareer.Commands;
using FootballCareerSimulator.Application.ManagerCareer.Composition;
using FootballCareerSimulator.Application.PlayerCareer.Composition;
using FootballCareerSimulator.Application.PlayerCareer.Infrastructure;
using FootballCareerSimulator.Application.PlayerCareer.Services;
using FootballCareerSimulator.Application.SocialContinuity.Composition;
using FootballCareerSimulator.Application.TeamPreparation.Commands;
using FootballCareerSimulator.Application.TeamPreparation.Composition;
using FootballCareerSimulator.Application.TeamPreparation.Infrastructure;
using FootballCareerSimulator.Application.TrainingPhysicalState.Commands;
using FootballCareerSimulator.Application.TrainingPhysicalState.Composition;
using FootballCareerSimulator.Application.TrainingPhysicalState.Infrastructure;
using FootballCareerSimulator.Application.Transfer.Composition;
using FootballCareerSimulator.Application.WorldCalendar.Commands;
using FootballCareerSimulator.Application.WorldCalendar.Composition;
using FootballCareerSimulator.Domain.Competition;
using FootballCareerSimulator.Domain.ContractRegistration;
using FootballCareerSimulator.Domain.ManagerCareer;
using FootballCareerSimulator.Domain.Shared;
using FootballCareerSimulator.Domain.TeamPreparation;
using FootballCareerSimulator.Domain.TrainingPhysicalState;
using FootballCareerSimulator.Domain.WorldCalendar;
using FootballCareerSimulator.Infrastructure.Career;
using FootballCareerSimulator.Simulation.TrainingPhysicalState;
using Microsoft.Data.Sqlite;
using Xunit.Abstractions;

namespace FootballCareerSimulator.Tests.Career;

public sealed class ProductionTenSeasonAcceptanceTests : IDisposable
{
    private static readonly GameDate StartDay = GameDate.FromCalendarDate(2026, 7, 1);

    private readonly string _tempDirectory = Path.Combine(
        Path.GetTempPath(),
        "fcs-production-ten-season",
        Guid.NewGuid().ToString("N"));
    private readonly ITestOutputHelper _output;

    public ProductionTenSeasonAcceptanceTests(ITestOutputHelper output)
    {
        _output = output;
        Directory.CreateDirectory(_tempDirectory);
    }

    public void Dispose()
    {
        SqliteConnection.ClearAllPools();
        if (Directory.Exists(_tempDirectory))
        {
            Directory.Delete(_tempDirectory, recursive: true);
        }
    }

    [Fact]
    [Trait("Category", "LongRunning")]
    public void ProductionCareer_TenSeasons_AllContextsRemainLoadableAndReferentiallyValid()
    {
        var host = CreateHost();
        var stopwatch = Stopwatch.StartNew();
        var transferCompletions = 0;
        var dismissalCount = 0;
        var midSaveBytes = 0L;

        for (var clubId = 1L; clubId <= CompetitionMvpConstraints.LeagueTeamCount; clubId++)
        {
            var id = new ClubId(clubId);
            host.Players.Development.EnsureClub(id, host.World.TimelineStore.Timeline.RootSeed, StartDay);
            host.Team.ClubSquad!.SyncFromActiveContracts(id, StartDay);
        }

        for (var seasonNumber = 1; seasonNumber <= 10; seasonNumber++)
        {
            var preseason = seasonNumber == 1
                ? StartDay
                : host.World.TimelineStore.Timeline.CurrentDate.AddDays(60);
            AdvanceTo(host.World, preseason);

            host.Competition.CreateSeason.Handle(
                new CreateSeasonCommand(Guid.NewGuid(), seasonNumber, preseason.DayNumber));
            for (var clubId = 1L; clubId <= CompetitionMvpConstraints.LeagueTeamCount; clubId++)
            {
                host.Competition.RegisterSeasonParticipant.Handle(
                    new RegisterSeasonParticipantCommand(Guid.NewGuid(), seasonNumber, clubId));
            }

            host.Competition.StartSeason.Handle(
                new StartSeasonCommand(Guid.NewGuid(), seasonNumber, preseason.DayNumber));
            var firstMatchday = preseason.AddDays(31);
            host.Competition.PlanLeagueFixtures.Handle(
                new PlanLeagueFixturesCommand(
                    Guid.NewGuid(),
                    seasonNumber,
                    firstMatchday.DayNumber,
                    StartingFixtureId: ((seasonNumber - 1L) * CompetitionMvpConstraints.TotalLeagueFixtures) + 1));

            host.World.OpenTransferWindow.Handle(
                new OpenTransferWindowCommand(Guid.NewGuid(), preseason.AddDays(20).DayNumber));
            for (var tick = 0; tick < 3; tick++)
            {
                transferCompletions += host.Transfer.AiSimulation
                    .RunWindowTick(preseason, host.World.TimelineStore.Timeline.RootSeed + seasonNumber + tick)
                    .CompletedCount;
            }

            host.World.CloseTransferWindow.Handle(new CloseTransferWindowCommand(Guid.NewGuid()));
            ApplyManagedTraining(host);

            foreach (var fixture in host.Competition.Queries.GetSeasonFixtures(seasonNumber)
                         .OrderBy(item => item.ScheduledDayNumber)
                         .ThenBy(item => item.FixtureId))
            {
                var fixtureDay = GameDate.FromDayNumber(fixture.ScheduledDayNumber);
                AdvanceTo(host.World, fixtureDay);
                var employment = host.Manager.Store.Career.ActiveEmployment;
                if (employment is { } active
                    && (fixture.HomeClubId == active.ClubId.Value || fixture.AwayClubId == active.ClubId.Value))
                {
                    ApplyManagedTraining(host);
                    var squad = host.Team.ClubSquad!.SyncFromActiveContracts(active.ClubId, fixtureDay);
                    host.Team.SelectionStore.Upsert(MvpAvailabilityAwareSelection.ApproveDefaultPreferringAvailable(
                        new FixtureId(fixture.FixtureId),
                        active.ClubId,
                        fixtureDay,
                        host.Training.Store.PhysicalBySlot,
                        squad));
                }

                var played = host.Competition.PlayFixtureMatch!.Handle(
                    new PlayFixtureMatchCommand(
                        Guid.NewGuid(),
                        seasonNumber,
                        fixture.FixtureId,
                        fixture.ScheduledDayNumber));
                if (played.Consequences?.ManagerDismissed == true)
                {
                    dismissalCount++;
                }

                if (!host.Manager.Store.Career.IsEmployed)
                {
                    host.Manager.GenerateJobOffer!.Handle(
                        new GenerateUnemployedJobOfferCommand(Guid.NewGuid()));
                    host.Manager.AcceptJobOffer!.Handle(
                        new AcceptPendingJobOfferCommand(Guid.NewGuid()));
                    ApplyManagedTraining(host);
                }
            }

            var closeDay = host.World.TimelineStore.Timeline.CurrentDate;
            var completed = host.Competition.CompleteSeason.Handle(
                new CompleteSeasonCommand(Guid.NewGuid(), seasonNumber, closeDay.DayNumber));
            Assert.Equal(completed.RetiredPlayerCount, completed.GeneratedPlayerCount);
            host.Competition.ArchiveSeason.Handle(
                new ArchiveSeasonCommand(Guid.NewGuid(), seasonNumber, closeDay.DayNumber));

            AssertPopulationIntegrity(host, closeDay);

            if (seasonNumber == 5)
            {
                var midPath = Path.Combine(_tempDirectory, "season-5.db");
                host.Session.Save(midPath);
                midSaveBytes = new FileInfo(midPath).Length;
                var loaded = host.Session.Load(midPath);
                Assert.True(loaded.Succeeded);
                Assert.True(loaded.WasMigrated is false);
                Assert.Equal(5 * CompetitionMvpConstraints.TotalLeagueFixtures, loaded.LoadedFixtureCount);
            }
        }

        stopwatch.Stop();
        var finalPath = Path.Combine(_tempDirectory, "season-10.db");
        var saved = host.Session.Save(finalPath);
        var finalBytes = new FileInfo(finalPath).Length;
        var final = new CareerSqlitePersistence().Load(finalPath);

        _output.WriteLine(
            "10-season acceptance: {0} fixtures, {1} active/{2} retired players, "
            + "{3} transfers, {4} dismissals, save {5:N0}->{6:N0} bytes, elapsed {7}.",
            final.League.Seasons.Sum(season => season.Fixtures.Count),
            final.PlayerCareers.Count(player => !player.IsRetired),
            final.PlayerCareers.Count(player => player.IsRetired),
            transferCompletions,
            dismissalCount,
            midSaveBytes,
            finalBytes,
            stopwatch.Elapsed);

        Assert.True(saved.Succeeded);
        Assert.Equal(48, final.SchemaVersion);
        Assert.Equal(10, final.League.Seasons.Count);
        Assert.All(final.League.Seasons, season => Assert.Equal(SeasonStatus.Archived, season.Status));
        Assert.Equal(
            10 * CompetitionMvpConstraints.TotalLeagueFixtures,
            final.League.Seasons.Sum(season => season.Fixtures.Count));
        Assert.All(
            final.League.Seasons.SelectMany(season => season.Fixtures),
            fixture => Assert.Equal(FixtureStatus.ResultAccepted, fixture.Status));
        Assert.Equal(450, final.PlayerCareers.Count(player => !player.IsRetired));
        Assert.Contains(final.PlayerCareers, player => player.IsRetired);
        Assert.Equal(
            final.PlayerCareers.Count,
            final.PlayerCareers.Select(player => player.Id).Distinct().Count());
        Assert.True(dismissalCount > 0);
        Assert.True(final.ManagerCareer.EmploymentHistory.Count > 0);
        Assert.True(transferCompletions > 0);
        Assert.True(final.Memories.Count > 0);
        Assert.True(final.Relationships.Count > 0);
        Assert.InRange(final.FreeAgents.Count, 1, 60);
        Assert.True(finalBytes < 20 * 1024 * 1024, $"Final save too large: {finalBytes} bytes.");
        Assert.True(finalBytes < Math.Max(midSaveBytes * 3, 1),
            $"Save growth is uncontrolled: mid={midSaveBytes}, final={finalBytes}.");
        Assert.True(stopwatch.Elapsed < TimeSpan.FromMinutes(12),
            $"Ten-season production acceptance took {stopwatch.Elapsed}.");
    }

    private static AcceptanceHost CreateHost()
    {
        var world = WorldCalendarModule.Create(StartDay, rootSeed: 42);
        var clubs = ClubGovernanceModule.CreateMvpLeague();
        var manager = ManagerCareerModule.CreateForCareer(
            StartDay,
            clubs.Store,
            world.TimelineStore,
            startingClubId: 1);
        manager.Store.Replace(ManagerCareer.StartNewCareer(
            new ManagerId(1),
            "10 Sezon Menajeri",
            new ClubId(1),
            StartDay,
            SeasonExpectationTier.MidTable,
            initialBoardConfidence: 20));

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
        var competitionStore = new FootballCareerSimulator.Application.Competition.Infrastructure.InMemoryLeagueCompetitionStore(
            new LeagueCompetition(new CompetitionId(1)));
        var selectionStore = new InMemoryMatchSelectionStore();
        var team = TeamPreparationModule.Create(
            competitionStore,
            manager.Store,
            selectionStore,
            trainingStore,
            world.TimelineStore,
            contracts.Store,
            playerStore);
        var training = TrainingPhysicalStateModule.Create(
            manager.Store,
            world.TimelineStore,
            trainingStore,
            players.Development,
            team.ClubSquad,
            selectionStore);
        var social = SocialContinuityModule.Create();
        team.BindPromiseStore(social.PromiseStore);
        contracts.Registration.BindPromiseInvalidation(social.Invalidation);
        contracts.Registration.BindRelationships(social.RelationshipEvaluation);
        manager.AcceptJobOffer!.BindCareerMemory(social.CareerMemory);
        manager.AcceptJobOffer.BindClubHistoryMemory(social.ClubHistoryMemory);
        clubs.BindWageBudget(contracts.Store);

        var transfer = TransferModule.Create(
            contracts.Store,
            team.SquadStore,
            manager.Store,
            contracts.Registration,
            team.ClubSquad!,
            transferWindow: world.TransferWindowQuery,
            transferBudget: clubs.TransferBudget,
            wageBudget: clubs.WageBudget,
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
        var lifecycle = new SeasonPlayerLifecycleService(
            playerStore,
            players.Development,
            contracts.Registration,
            team.ClubSquad!,
            trainingStore,
            world.TimelineStore);
        var competition = CompetitionModule.CreateForCareerFromStore(
            competitionStore,
            world.TimelineStore,
            clubs.Store,
            manager.Store,
            selectionStore,
            trainingStore,
            playerStore,
            players.Development,
            team.TacticPlanStore,
            team.SquadStore,
            social.StartingOpportunity,
            social.SelectionMemory,
            social.PlayingTime,
            social.Invalidation,
            social.CareerMemory,
            social.ClubHistoryMemory,
            social.MatchPerformanceMemory,
            social.RelationshipEvaluation,
            interaction.PostMatchPress,
            interaction.PostMatchPlayingTimeDemand,
            interaction.PostMatchBoardDemand,
            interaction.PostMatchDiscipline,
            lifecycle);

        var eventRule = world.EventRuleEvaluation!;
        var resets = new List<FootballCareerSimulator.Application.WorldCalendar.Ports.ICommandIdempotencyReset>
        {
            world.AdvanceSimulationTime,
            world.OpenPlanningPeriod,
            world.CompletePlanningPeriod,
            world.OpenTransferWindow,
            world.CloseTransferWindow,
            eventRule,
            training.IdempotencyReset,
        };
        resets.AddRange(competition.IdempotencyResets);
        resets.AddRange(team.IdempotencyResets);
        resets.AddRange(manager.IdempotencyResets);

        var session = new CareerGameSessionService(
            world.TimelineStore,
            competition.Store,
            clubs.Store,
            manager.Store,
            selectionStore,
            team.SquadStore,
            team.TacticPlanStore,
            transfer.NeedStore,
            transfer.ShortlistStore,
            transfer.TargetStore,
            transfer.ProcessStore,
            transfer.OfferStore,
            transfer.ProposalStore,
            social.PromiseStore,
            social.MemoryStore,
            social.RelationshipStore,
            interaction.DecisionRequestStore,
            interaction.DialogueSessionStore,
            interaction.DisciplinaryActionStore,
            trainingStore,
            playerStore,
            contracts.Store,
            contracts.FreeAgentStore,
            new CareerSqlitePersistence(),
            resets,
            eventRule.Registry,
            eventRule.ScheduledEvaluationStore);

        return new AcceptanceHost(
            world,
            competition,
            clubs,
            manager,
            team,
            training,
            players,
            contracts,
            transfer,
            social,
            interaction,
            session);
    }

    private static void ApplyManagedTraining(AcceptanceHost host)
    {
        if (!host.Manager.Store.Career.IsEmployed)
        {
            return;
        }

        host.Training.SetWeeklyPlan.Handle(new SetWeeklyTrainingPlanCommand(
            Guid.NewGuid(),
            (int)TrainingFocus.Recovery,
            (int)TrainingIntensity.Low,
            (int)RestApproach.Heavy));
    }

    private static void AdvanceTo(WorldCalendarModule world, GameDate day)
    {
        if (world.TimelineStore.Timeline.CurrentDate.DayNumber >= day.DayNumber)
        {
            return;
        }

        world.AdvanceSimulationTime.Handle(
            new AdvanceSimulationTimeCommand(Guid.NewGuid(), day.DayNumber));
    }

    private static void AssertPopulationIntegrity(AcceptanceHost host, GameDate day)
    {
        var activePlayers = host.Players.Store.Careers.Where(player => !player.IsRetired).ToArray();
        Assert.Equal(450, activePlayers.Length);
        Assert.Equal(450, activePlayers.Select(player => player.Id).Distinct().Count());
        var activeContractPlayerIds = host.Contracts.Store.Contracts
            .Where(contract => contract.IsActiveOn(day))
            .Select(contract => contract.PlayerId)
            .ToHashSet();
        Assert.DoesNotContain(
            host.Players.Store.Careers.Where(player => player.IsRetired),
            retired => activeContractPlayerIds.Contains(retired.Id));
        Assert.All(
            host.Team.SquadStore.Squads,
            squad => Assert.DoesNotContain(
                squad.Members,
                member => host.Players.Store.Careers.Any(
                    player => player.Id == member.PlayerId && player.IsRetired)));
    }

    private sealed record AcceptanceHost(
        WorldCalendarModule World,
        CompetitionModule Competition,
        ClubGovernanceModule Clubs,
        ManagerCareerModule Manager,
        TeamPreparationModule Team,
        TrainingPhysicalStateModule Training,
        PlayerCareerModule Players,
        ContractRegistrationModule Contracts,
        TransferModule Transfer,
        SocialContinuityModule Social,
        InteractionModule Interaction,
        CareerGameSessionService Session);
}
