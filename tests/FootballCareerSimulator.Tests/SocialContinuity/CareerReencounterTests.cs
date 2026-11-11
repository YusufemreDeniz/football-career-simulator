using FootballCareerSimulator.Application.ClubGovernance.Composition;
using FootballCareerSimulator.Application.Competition.Commands;
using FootballCareerSimulator.Application.Competition.Composition;
using FootballCareerSimulator.Application.ManagerCareer.Composition;
using FootballCareerSimulator.Application.SocialContinuity.Composition;
using FootballCareerSimulator.Application.TeamPreparation.Infrastructure;
using FootballCareerSimulator.Application.WorldCalendar.Composition;
using FootballCareerSimulator.Domain.Competition;
using FootballCareerSimulator.Domain.ManagerCareer;
using FootballCareerSimulator.Domain.PlayerCareer;
using FootballCareerSimulator.Domain.Shared;
using FootballCareerSimulator.Domain.SocialContinuity;
using FootballCareerSimulator.Domain.TeamPreparation;
using FootballCareerSimulator.Domain.WorldCalendar;

namespace FootballCareerSimulator.Tests.SocialContinuity;

public sealed class CareerReencounterTests
{
    private static readonly GameDate Day = GameDate.FromCalendarDate(2026, 8, 1);

    [Fact]
    public void PlayingFormerClub_ReactivatesFormerPlayerAndRecordsBothMemoriesOnce()
    {
        var world = WorldCalendarModule.Create(Day, rootSeed: 29);
        var clubs = ClubGovernanceModule.CreateMvpLeague();
        var manager = ManagerCareerModule.CreateNewCareer(Day, startingClubId: 1);
        var career = ManagerCareer.StartNewCareerForClubStrength(
            new ManagerId(1),
            "TD",
            new ClubId(1),
            GameDate.FromCalendarDate(2026, 4, 1),
            clubSportiveStrength: 50,
            initialBoardConfidence: 32);
        career = career.ApplyMatchBoardAssessment(
            new FixtureId(900),
            MatchOutcomeForManagedClub.Loss,
            leaguePosition: 18,
            leagueSize: 18).Career;
        career = career.DismissDueToBoardConfidence(
            new FixtureId(900),
            GameDate.FromCalendarDate(2026, 7, 1)).Career;
        career = career.ReceiveJobOffer(
            JobOffer.CreateOffered(new JobOfferId(50), new ClubId(2), Day)).Career;
        career = career.AcceptPendingJobOffer(Day, SeasonExpectationTier.MidTable).Career;
        manager.Store.Replace(career);

        var social = SocialContinuityModule.Create();
        var formerPlayer = new PlayerId(1010);
        social.RelationshipEvaluation.ApplySelectionStarted(
            new FixtureId(800),
            formerPlayer,
            career.ManagerId,
            GameDate.FromCalendarDate(2026, 4, 10));
        social.RelationshipEvaluation.MarkDormantForPlayerLeaving(
            formerPlayer,
            GameDate.FromCalendarDate(2026, 7, 10));

        var squadStore = new InMemoryClubSquadStore();
        var selectionStore = new InMemoryMatchSelectionStore();
        squadStore.Upsert(ClubSquad.Rehydrate(
            new ClubId(1),
            [SquadMember.Create(
                formerPlayer,
                slotIndex: 0,
                joinedOn: GameDate.FromCalendarDate(2026, 3, 1))]));
        var competition = CompetitionModule.CreateForCareer(
            world.TimelineStore,
            clubs.Store,
            manager.Store,
            matchSelectionStore: selectionStore,
            clubSquadStore: squadStore,
            clubHistoryMemory: social.ClubHistoryMemory,
            relationships: social.RelationshipEvaluation);
        competition.CreateSeason.Handle(new CreateSeasonCommand(Guid.NewGuid(), 1, Day.DayNumber));
        for (var clubId = 1L; clubId <= CompetitionMvpConstraints.LeagueTeamCount; clubId++)
        {
            competition.RegisterSeasonParticipant.Handle(
                new RegisterSeasonParticipantCommand(Guid.NewGuid(), 1, clubId));
        }

        competition.StartSeason.Handle(new StartSeasonCommand(Guid.NewGuid(), 1, Day.DayNumber));
        competition.PlanLeagueFixtures.Handle(
            new PlanLeagueFixturesCommand(Guid.NewGuid(), 1, Day.DayNumber, StartingFixtureId: 1));
        var fixture = competition.Queries.GetSeasonFixtures(1)
            .First(item =>
                item.HomeClubId is 1 or 2
                && item.AwayClubId is 1 or 2);
        var command = new PlayFixtureMatchCommand(
            Guid.NewGuid(),
            1,
            fixture.FixtureId,
            fixture.ScheduledDayNumber);
        selectionStore.Upsert(MatchSelection.ApproveDefault(
            new FixtureId(fixture.FixtureId),
            new ClubId(2)));

        var result = competition.PlayFixtureMatch!.Handle(command);
        var repeated = competition.PlayFixtureMatch.Handle(command);

        Assert.True(result.Consequences!.FormerClubEncounter);
        Assert.Equal(1, result.Consequences.FormerPlayerEncounterCount);
        Assert.Equal(result, repeated);
        Assert.Equal(
            RelationshipStatus.Active,
            social.RelationshipStore.FindPlayerToManager(formerPlayer.Value, career.ManagerId.Value)!.Status);
        Assert.Single(
            social.MemoryStore.Memories,
            memory => memory.RuleId == MemoryRecord.FormerClubEncounterRuleId);
        Assert.Single(
            social.MemoryStore.Memories,
            memory => memory.RuleId == MemoryRecord.FormerPlayerEncounterRuleId);
    }
}
