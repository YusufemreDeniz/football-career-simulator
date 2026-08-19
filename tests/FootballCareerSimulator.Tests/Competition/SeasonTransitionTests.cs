using FootballCareerSimulator.Application.ClubGovernance.Composition;
using FootballCareerSimulator.Application.Competition.Commands;
using FootballCareerSimulator.Application.Competition.Composition;
using FootballCareerSimulator.Application.Competition.Infrastructure;
using FootballCareerSimulator.Application.ContractRegistration.Composition;
using FootballCareerSimulator.Application.ManagerCareer.Composition;
using FootballCareerSimulator.Application.PlayerCareer.Composition;
using FootballCareerSimulator.Application.PlayerCareer.Infrastructure;
using FootballCareerSimulator.Application.PlayerCareer.Services;
using FootballCareerSimulator.Application.TeamPreparation.Commands;
using FootballCareerSimulator.Application.TeamPreparation.Composition;
using FootballCareerSimulator.Application.TeamPreparation.Infrastructure;
using FootballCareerSimulator.Application.TrainingPhysicalState.Composition;
using FootballCareerSimulator.Application.TrainingPhysicalState.Infrastructure;
using FootballCareerSimulator.Application.WorldCalendar.Composition;
using FootballCareerSimulator.Domain.Competition;
using FootballCareerSimulator.Domain.Match;
using FootballCareerSimulator.Domain.TeamPreparation;
using FootballCareerSimulator.Domain.WorldCalendar;

namespace FootballCareerSimulator.Tests.Competition;

public sealed class SeasonTransitionTests
{
    private static readonly GameDate PreseasonStart = GameDate.FromCalendarDate(2026, 7, 1);
    private static readonly GameDate FirstMatchday = GameDate.FromCalendarDate(2026, 8, 1);

    private static CompetitionModule CreateCareerModule()
    {
        var world = WorldCalendarModule.Create(PreseasonStart, rootSeed: 5);
        var clubs = ClubGovernanceModule.CreateMvpLeague();
        return CompetitionModule.CreateForCareer(world.TimelineStore, clubs.Store);
    }

    private static void SetupActiveSeason(CompetitionModule module, long seasonId)
    {
        module.CreateSeason.Handle(
            new CreateSeasonCommand(Guid.NewGuid(), seasonId, PreseasonStart.DayNumber));
        for (var club = 1L; club <= CompetitionMvpConstraints.LeagueTeamCount; club++)
        {
            module.RegisterSeasonParticipant.Handle(
                new RegisterSeasonParticipantCommand(Guid.NewGuid(), seasonId, club));
        }

        module.StartSeason.Handle(
            new StartSeasonCommand(Guid.NewGuid(), seasonId, PreseasonStart.DayNumber));
        module.PlanLeagueFixtures.Handle(
            new PlanLeagueFixturesCommand(
                Guid.NewGuid(),
                seasonId,
                FirstMatchday.DayNumber,
                StartingFixtureId: 1));
    }

    private static void AcceptAllFixtures(CompetitionModule module, long seasonId)
    {
        foreach (var fixture in module.Queries.GetSeasonFixtures(seasonId))
        {
            module.Store.League.AcceptFixtureResult(
                new SeasonId(seasonId),
                new FixtureId(fixture.FixtureId),
                new MatchScore(1, 0),
                GameDate.FromDayNumber(fixture.ScheduledDayNumber));
        }
    }

    [Fact]
    public void GetSeasonProgress_CanComplete_OnlyWhenAllFixturesAccepted()
    {
        var module = CreateCareerModule();
        SetupActiveSeason(module, seasonId: 1);

        var early = module.Queries.GetSeasonProgress(1)!;
        Assert.False(early.CanComplete);
        Assert.False(early.CanArchive);

        AcceptAllFixtures(module, seasonId: 1);

        var ready = module.Queries.GetSeasonProgress(1)!;
        Assert.True(ready.CanComplete);
        Assert.False(ready.CanArchive);
        Assert.Equal(ready.TotalFixtureCount, ready.AcceptedFixtureCount);
    }

    [Fact]
    public void CompleteSeason_FailsWhileFixturesPending()
    {
        var module = CreateCareerModule();
        SetupActiveSeason(module, seasonId: 1);

        Assert.ThrowsAny<Exception>(() =>
            module.CompleteSeason.Handle(
                new CompleteSeasonCommand(Guid.NewGuid(), 1, FirstMatchday.DayNumber)));
    }

    [Fact]
    public void CompleteArchiveStartNext_CreatesActiveSeasonTwo()
    {
        var module = CreateCareerModule();
        SetupActiveSeason(module, seasonId: 1);
        AcceptAllFixtures(module, seasonId: 1);

        module.CompleteSeason.Handle(
            new CompleteSeasonCommand(Guid.NewGuid(), 1, FirstMatchday.AddDays(200).DayNumber));
        Assert.True(module.Queries.GetSeasonProgress(1)!.CanArchive);

        module.ArchiveSeason.Handle(
            new ArchiveSeasonCommand(Guid.NewGuid(), 1, FirstMatchday.AddDays(210).DayNumber));
        Assert.Null(module.Queries.GetCurrentSeason());

        const long nextSeasonId = 2;
        var startDay = FirstMatchday.AddDays(220).DayNumber;
        module.CreateSeason.Handle(new CreateSeasonCommand(Guid.NewGuid(), nextSeasonId, startDay));
        for (var club = 1L; club <= CompetitionMvpConstraints.LeagueTeamCount; club++)
        {
            module.RegisterSeasonParticipant.Handle(
                new RegisterSeasonParticipantCommand(Guid.NewGuid(), nextSeasonId, club));
        }

        module.StartSeason.Handle(new StartSeasonCommand(Guid.NewGuid(), nextSeasonId, startDay));
        module.PlanLeagueFixtures.Handle(
            new PlanLeagueFixturesCommand(
                Guid.NewGuid(),
                nextSeasonId,
                startDay,
                StartingFixtureId: 1));

        var current = module.Queries.GetCurrentSeason();
        Assert.NotNull(current);
        Assert.Equal(2, current.SeasonId);
        Assert.Equal(nameof(SeasonStatus.Active), current.Status);
        Assert.True(current.FixtureCount > 0);
        Assert.Equal(SeasonStatus.Archived, module.Store.League.Seasons.Single(s => s.SeasonId.Value == 1).Status);
    }

    [Fact]
    public void CompleteSeason_ExecutesBoundPlayerLifecycleExactlyOnce()
    {
        var world = WorldCalendarModule.Create(PreseasonStart, rootSeed: 23);
        var clubs = ClubGovernanceModule.CreateMvpLeague();
        var manager = ManagerCareerModule.CreateNewCareer(PreseasonStart, startingClubId: 1);
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
        var competitionStore = new InMemoryLeagueCompetitionStore(
            new LeagueCompetition(new CompetitionId(1)));
        var team = TeamPreparationModule.Create(
            competitionStore,
            manager.Store,
            trainingStore: trainingStore,
            timelineStore: world.TimelineStore,
            contractStore: contracts.Store,
            playerCareerStore: playerStore);
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
            playerLifecycle: lifecycle);

        var clubId = new Domain.Shared.ClubId(1);
        playerStore.Upsert(Domain.PlayerCareer.PlayerCareer.CreateForSlot(
            clubId,
            slotIndex: 0,
            currentAbility: 60,
            potentialAbility: 65,
            birthYear: 1992));
        contracts.Registration.EnsureClubContracts(clubId, PreseasonStart);

        SetupActiveSeason(competition, seasonId: 1);
        AcceptAllFixtures(competition, seasonId: 1);
        var command = new CompleteSeasonCommand(
            Guid.NewGuid(),
            1,
            FirstMatchday.AddDays(200).DayNumber);

        var result = competition.CompleteSeason.Handle(command);
        var repeated = competition.CompleteSeason.Handle(command);

        Assert.Equal(1, result.RetiredPlayerCount);
        Assert.Equal(1, result.GeneratedPlayerCount);
        Assert.Equal(result, repeated);
        Assert.Equal(2, playerStore.Careers.Count);
    }

    [Fact]
    public void NewSeason_PurgesStaleMatchSelections_SoFixtureIdsAreNotGhostApproved()
    {
        var world = WorldCalendarModule.Create(FirstMatchday, rootSeed: 9);
        var clubs = ClubGovernanceModule.CreateMvpLeague();
        var manager = ManagerCareerModule.CreateNewCareer(FirstMatchday, startingClubId: 1);
        var selectionStore = new InMemoryMatchSelectionStore();
        var training = TrainingPhysicalStateModule.Create(
            manager.Store,
            world.TimelineStore,
            matchSelectionStore: selectionStore);
        var competition = CompetitionModule.CreateForCareer(
            world.TimelineStore,
            clubs.Store,
            manager.Store,
            selectionStore,
            training.Store);
        var teamPrep = TeamPreparationModule.Create(
            competition.Store,
            manager.Store,
            selectionStore,
            training.Store,
            world.TimelineStore);

        SetupActiveSeason(competition, seasonId: 1);

        var managedFixtureId = competition.Queries.GetSeasonFixtures(1)
            .First(fixture => fixture.HomeClubId == 1 || fixture.AwayClubId == 1)
            .FixtureId;

        teamPrep.ApproveDefaultSelection.Handle(
            new ApproveDefaultMatchSelectionCommand(Guid.NewGuid(), managedFixtureId, ClubId: 1));
        Assert.True(teamPrep.SelectionQueries.IsApproved(managedFixtureId, 1));

        AcceptAllFixtures(competition, seasonId: 1);
        // AcceptFixtureResult bypasses RemoveForFixture — stale approval remains (ghost risk).
        Assert.True(teamPrep.SelectionQueries.IsApproved(managedFixtureId, 1));

        competition.CompleteSeason.Handle(
            new CompleteSeasonCommand(Guid.NewGuid(), 1, FirstMatchday.AddDays(200).DayNumber));
        competition.ArchiveSeason.Handle(
            new ArchiveSeasonCommand(Guid.NewGuid(), 1, FirstMatchday.AddDays(210).DayNumber));

        // Same purge StartNewSeason / TransitionToNextSeason performs.
        selectionStore.ReplaceAll(Array.Empty<MatchSelection>());

        var startDay = FirstMatchday.AddDays(220).DayNumber;
        competition.CreateSeason.Handle(new CreateSeasonCommand(Guid.NewGuid(), 2, startDay));
        for (var club = 1L; club <= CompetitionMvpConstraints.LeagueTeamCount; club++)
        {
            competition.RegisterSeasonParticipant.Handle(
                new RegisterSeasonParticipantCommand(Guid.NewGuid(), 2, club));
        }

        competition.StartSeason.Handle(new StartSeasonCommand(Guid.NewGuid(), 2, startDay));
        competition.PlanLeagueFixtures.Handle(
            new PlanLeagueFixturesCommand(Guid.NewGuid(), 2, startDay, StartingFixtureId: 1));

        Assert.Empty(selectionStore.Selections);

        var nextDue = teamPrep.SelectionQueries.GetNextDueManagedFixture(startDay);
        Assert.NotNull(nextDue);
        Assert.False(nextDue.IsApproved);
        Assert.False(teamPrep.SelectionQueries.IsApproved(nextDue.FixtureId, 1));
    }
}
