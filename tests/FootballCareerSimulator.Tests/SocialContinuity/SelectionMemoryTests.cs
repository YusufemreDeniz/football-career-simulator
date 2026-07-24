using FootballCareerSimulator.Application.ClubGovernance.Composition;
using FootballCareerSimulator.Application.Competition.Commands;
using FootballCareerSimulator.Application.Competition.Composition;
using FootballCareerSimulator.Application.ManagerCareer.Composition;
using FootballCareerSimulator.Application.SocialContinuity.Composition;
using FootballCareerSimulator.Application.TeamPreparation.Commands;
using FootballCareerSimulator.Application.TeamPreparation.Composition;
using FootballCareerSimulator.Application.TeamPreparation.Infrastructure;
using FootballCareerSimulator.Application.WorldCalendar.Composition;
using FootballCareerSimulator.Domain.Competition;
using FootballCareerSimulator.Domain.PlayerCareer;
using FootballCareerSimulator.Domain.SocialContinuity;
using FootballCareerSimulator.Domain.TeamPreparation;
using FootballCareerSimulator.Domain.WorldCalendar;
using FootballCareerSimulator.Infrastructure.Career;
using Microsoft.Data.Sqlite;
using PlayerCareerAggregate = FootballCareerSimulator.Domain.PlayerCareer.PlayerCareer;

namespace FootballCareerSimulator.Tests.SocialContinuity;

public sealed class SelectionMemoryTests : IDisposable
{
    private static readonly GameDate Day = GameDate.FromCalendarDate(2026, 8, 1);

    private readonly string _tempDirectory = Path.Combine(
        Path.GetTempPath(),
        "fcs-selection-memory",
        Guid.NewGuid().ToString("N"));

    public SelectionMemoryTests() => Directory.CreateDirectory(_tempDirectory);

    public void Dispose()
    {
        SqliteConnection.ClearAllPools();
        if (Directory.Exists(_tempDirectory))
        {
            Directory.Delete(_tempDirectory, recursive: true);
        }
    }

    [Fact]
    public void RecordStarts_CreatesSelectionMemoryPerPlayer_Idempotently()
    {
        var social = SocialContinuityModule.Create();
        var fixtureId = new FixtureId(9);
        var players = new[] { new PlayerId(1001), new PlayerId(1002) };

        Assert.Equal(2, social.SelectionMemory.RecordStarts(fixtureId, players, Day));
        Assert.Equal(0, social.SelectionMemory.RecordStarts(fixtureId, players, Day));

        Assert.Equal(2, social.MemoryStore.Memories.Count);
        Assert.All(social.MemoryStore.Memories, m =>
        {
            Assert.Equal(MemoryCategory.Selection, m.Category);
            Assert.Equal(MemorySubjectKind.Fixture, m.SubjectKind);
            Assert.Equal(9, m.SubjectId);
            Assert.Equal(MemoryValence.Positive, m.Valence);
            Assert.Equal(MemoryRecord.SelectionStartedRuleId, m.RuleId);
        });
    }

    [Fact]
    public void RepeatedOmission_ReinforcesExistingMemory_Idempotently()
    {
        var social = SocialContinuityModule.Create();
        var player = new PlayerId(30);

        var first = social.SelectionMemory.RecordMatchday(
            new FixtureId(1),
            startingPlayerIds: [new PlayerId(1)],
            benchedPlayerIds: [],
            squadMembers: [new PlayerId(1), player],
            Day);
        Assert.Equal(2, first.Created);
        Assert.Equal(0, first.Reinforced);

        // Fixture 2: SelectionStarted + omitted pekiştirme
        var second = social.SelectionMemory.RecordMatchday(
            new FixtureId(2),
            startingPlayerIds: [new PlayerId(1)],
            benchedPlayerIds: [],
            squadMembers: [new PlayerId(1), player],
            Day.AddDays(1));
        Assert.Equal(0, second.Created);
        Assert.Equal(2, second.Reinforced);

        var omitted = Assert.Single(
            social.MemoryStore.Memories,
            m => m.RuleId == MemoryRecord.SelectionOmittedRuleId);
        Assert.Equal(1, omitted.ReinforcementCount);
        Assert.Equal(55, omitted.CurrentInfluence); // 45 + 10
        Assert.Contains(
            MemoryRecord.BuildSelectionOmittedSourceKey(new FixtureId(2), player.Value),
            omitted.ProcessedReinforcementKeys);
        Assert.Single(
            social.MemoryStore.Memories,
            m => m.RuleId == MemoryRecord.SelectionStartedRuleId);

        var replay = social.SelectionMemory.RecordMatchday(
            new FixtureId(2),
            startingPlayerIds: [new PlayerId(1)],
            benchedPlayerIds: [],
            squadMembers: [new PlayerId(1), player],
            Day.AddDays(1));
        Assert.Equal(0, replay.Applied);
        Assert.Equal(2, replay.Rejected);
        Assert.Equal(
            1,
            social.MemoryStore.Memories.Single(m => m.RuleId == MemoryRecord.SelectionOmittedRuleId)
                .ReinforcementCount);
    }

    [Fact]
    public void SelectionStarted_RejectsWhenReinforcementCapReached()
    {
        var social = SocialContinuityModule.Create();
        var player = new PlayerId(40);

        Assert.Equal(1, social.SelectionMemory.RecordStarts(new FixtureId(1), [player], Day));
        for (var i = 0; i < MemoryRecord.MaxReinforcementsPerMemory; i++)
        {
            var stats = social.SelectionMemory.RecordMatchday(
                new FixtureId(10 + i),
                [player],
                [],
                null,
                Day.AddDays(i + 1));
            Assert.Equal(1, stats.Reinforced);
            Assert.Equal(0, stats.Rejected);
        }

        var capped = social.SelectionMemory.RecordMatchday(
            new FixtureId(99),
            [player],
            [],
            null,
            Day.AddDays(20));
        Assert.Equal(0, capped.Applied);
        Assert.Equal(1, capped.Rejected);
        var memory = Assert.Single(social.MemoryStore.Memories);
        Assert.Equal(MemoryRecord.MaxReinforcementsPerMemory, memory.ReinforcementCount);
        Assert.Equal(35 + (MemoryRecord.MaxReinforcementsPerMemory * 10), memory.CurrentInfluence);
    }

    [Fact]
    public void RecordMatchday_WritesBenchAndOmittedMemories()
    {
        var social = SocialContinuityModule.Create();
        var fixtureId = new FixtureId(12);
        var started = new PlayerId(1);
        var benched = new PlayerId(2);
        var omitted = new PlayerId(3);

        var created = social.SelectionMemory.RecordMatchday(
            fixtureId,
            [started],
            [benched],
            [started, benched, omitted],
            Day);

        Assert.Equal(3, created.Applied);
        Assert.Equal(3, created.Created);
        var replay = social.SelectionMemory.RecordMatchday(
            fixtureId,
            [started],
            [benched],
            [started, benched, omitted],
            Day);
        Assert.Equal(0, replay.Applied);
        Assert.Equal(3, replay.Rejected);

        Assert.Contains(
            social.MemoryStore.Memories,
            m => m.RuleId == MemoryRecord.SelectionStartedRuleId && m.Valence == MemoryValence.Positive);
        Assert.Contains(
            social.MemoryStore.Memories,
            m => m.RuleId == MemoryRecord.SelectionBenchedRuleId && m.Valence == MemoryValence.Neutral);
        Assert.Contains(
            social.MemoryStore.Memories,
            m => m.RuleId == MemoryRecord.SelectionOmittedRuleId
                && m.Valence == MemoryValence.Negative
                && m.RememberingActor.Id == omitted.Value);
    }

    [Fact]
    public void PlayFixtureMatch_WritesSelectionMemoriesForStartingXiAndBench()
    {
        var world = WorldCalendarModule.Create(Day, rootSeed: 21);
        var clubs = ClubGovernanceModule.CreateMvpLeague();
        var manager = ManagerCareerModule.CreateNewCareer(Day, startingClubId: 1);
        var social = SocialContinuityModule.Create();
        var selectionStore = new InMemoryMatchSelectionStore();
        var competition = CompetitionModule.CreateForCareer(
            world.TimelineStore,
            clubs.Store,
            manager.Store,
            selectionStore,
            selectionMemory: social.SelectionMemory);
        var teamPrep = TeamPreparationModule.Create(competition.Store, manager.Store, selectionStore);

        const long seasonId = 1;
        competition.CreateSeason.Handle(
            new CreateSeasonCommand(Guid.NewGuid(), seasonId, Day.DayNumber));
        for (var club = 1L; club <= CompetitionMvpConstraints.LeagueTeamCount; club++)
        {
            competition.RegisterSeasonParticipant.Handle(
                new RegisterSeasonParticipantCommand(Guid.NewGuid(), seasonId, club));
        }

        competition.StartSeason.Handle(new StartSeasonCommand(Guid.NewGuid(), seasonId, Day.DayNumber));
        competition.PlanLeagueFixtures.Handle(
            new PlanLeagueFixturesCommand(
                Guid.NewGuid(),
                seasonId,
                Day.DayNumber,
                StartingFixtureId: 1));

        var fixture = competition.Queries.GetSeasonFixtures(seasonId)
            .First(f => f.HomeClubId == 1 || f.AwayClubId == 1);

        teamPrep.ApproveDefaultSelection.Handle(
            new ApproveDefaultMatchSelectionCommand(Guid.NewGuid(), fixture.FixtureId, ClubId: 1));

        competition.PlayFixtureMatch!.Handle(
            new PlayFixtureMatchCommand(
                Guid.NewGuid(),
                seasonId,
                fixture.FixtureId,
                Day.DayNumber));

        var selectionMemories = social.MemoryStore.Memories
            .Where(m => m.Category == MemoryCategory.Selection)
            .ToArray();
        var expectedPerClub = MatchSelection.StartingXiSize + MatchSelection.MaxBenchSize;
        Assert.Equal(expectedPerClub * 2, selectionMemories.Length);
        Assert.Equal(
            MatchSelection.StartingXiSize * 2,
            selectionMemories.Count(m => m.RuleId == MemoryRecord.SelectionStartedRuleId));
        Assert.Equal(
            MatchSelection.MaxBenchSize * 2,
            selectionMemories.Count(m => m.RuleId == MemoryRecord.SelectionBenchedRuleId));
        Assert.Contains(
            selectionMemories,
            m => m.RememberingActor.Id == PlayerId.FromClubSlot(1, 0).Value);
        Assert.Contains(
            selectionMemories,
            m => m.RuleId == MemoryRecord.SelectionBenchedRuleId
                && m.RememberingActor.Id == PlayerId.FromClubSlot(1, MatchSelection.StartingXiSize).Value);
    }

    [Fact]
    public void SaveLoad_PreservesSelectionMemories()
    {
        var social = SocialContinuityModule.Create();
        social.SelectionMemory.RecordMatchday(
            new FixtureId(4),
            [new PlayerId(1001)],
            [new PlayerId(1002)],
            [new PlayerId(1001), new PlayerId(1002), new PlayerId(1003)],
            Day);

        var world = WorldCalendarModule.Create(Day, rootSeed: 4);
        var path = Path.Combine(_tempDirectory, "selection.db");
        new CareerSqlitePersistence().Save(
            path,
            world.TimelineStore.Timeline,
            new LeagueCompetition(new CompetitionId(1)),
            ClubGovernanceModule.CreateMvpLeague().Store.Registry,
            ManagerCareerModule.CreateNewCareer(Day, startingClubId: 1).Store.Career,
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
            Array.Empty<Promise>(),
            social.MemoryStore.Memories);

        var loaded = new CareerSqlitePersistence().Load(path);
        Assert.Equal(34, loaded.SchemaVersion);
        Assert.Equal(3, loaded.Memories.Count);
        Assert.Contains(loaded.Memories, m => m.RuleId == MemoryRecord.SelectionStartedRuleId);
        Assert.Contains(loaded.Memories, m => m.RuleId == MemoryRecord.SelectionBenchedRuleId);
        Assert.Contains(loaded.Memories, m => m.RuleId == MemoryRecord.SelectionOmittedRuleId);
    }
}
