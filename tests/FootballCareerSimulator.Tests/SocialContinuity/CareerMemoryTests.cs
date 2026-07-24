using FootballCareerSimulator.Application.ClubGovernance.Composition;
using FootballCareerSimulator.Application.ManagerCareer.Commands;
using FootballCareerSimulator.Application.ManagerCareer.Composition;
using FootballCareerSimulator.Application.SocialContinuity.Composition;
using FootballCareerSimulator.Application.WorldCalendar.Composition;
using FootballCareerSimulator.Domain.Competition;
using FootballCareerSimulator.Domain.ManagerCareer;
using FootballCareerSimulator.Domain.Shared;
using FootballCareerSimulator.Domain.SocialContinuity;
using FootballCareerSimulator.Domain.WorldCalendar;
using FootballCareerSimulator.Infrastructure.Career;
using Microsoft.Data.Sqlite;

namespace FootballCareerSimulator.Tests.SocialContinuity;

public sealed class CareerMemoryTests : IDisposable
{
    private static readonly GameDate Day = GameDate.FromCalendarDate(2026, 8, 1);

    private readonly string _tempDirectory = Path.Combine(
        Path.GetTempPath(),
        "fcs-career-memory",
        Guid.NewGuid().ToString("N"));

    public CareerMemoryTests() => Directory.CreateDirectory(_tempDirectory);

    public void Dispose()
    {
        SqliteConnection.ClearAllPools();
        if (Directory.Exists(_tempDirectory))
        {
            Directory.Delete(_tempDirectory, recursive: true);
        }
    }

    [Fact]
    public void RecordDismissal_CreatesNegativeCareerMemory_Idempotently()
    {
        var social = SocialContinuityModule.Create();
        var managerId = new ManagerId(1);
        var clubId = new ClubId(7);
        var fixtureId = new FixtureId(11);

        Assert.Equal(1, social.CareerMemory.RecordDismissal(managerId, clubId, fixtureId, Day));
        Assert.Equal(0, social.CareerMemory.RecordDismissal(managerId, clubId, fixtureId, Day));

        var memory = Assert.Single(social.MemoryStore.Memories);
        Assert.Equal(MemoryCategory.Career, memory.Category);
        Assert.Equal(MemoryValence.Negative, memory.Valence);
        Assert.Equal(MemorySubjectKind.Club, memory.SubjectKind);
        Assert.Equal(7, memory.SubjectId);
        Assert.Equal(ActorKind.Manager, memory.RememberingActor.Kind);
        Assert.Equal(MemoryRecord.ManagerDismissedRuleId, memory.RuleId);
        Assert.Equal(85, memory.BaseImportance);
    }

    [Fact]
    public void AcceptJobOffer_CreatesPositiveCareerMemory()
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
        manager.Store.Replace(career.DismissDueToBoardConfidence(new FixtureId(1), Day).Career);

        manager.GenerateJobOffer!.Handle(new GenerateUnemployedJobOfferCommand(Guid.NewGuid()));
        manager.AcceptJobOffer.Handle(new AcceptPendingJobOfferCommand(Guid.NewGuid()));

        var hired = Assert.Single(
            social.MemoryStore.Memories,
            m => m.RuleId == MemoryRecord.ManagerHiredRuleId);
        Assert.Equal(MemoryCategory.Career, hired.Category);
        Assert.Equal(MemoryValence.Positive, hired.Valence);
        Assert.Equal(MemorySubjectKind.Club, hired.SubjectKind);
        Assert.Equal(70, hired.BaseImportance);
    }

    [Fact]
    public void SaveLoad_PreservesCareerMemoriesAtSchemaV31()
    {
        var social = SocialContinuityModule.Create();
        social.CareerMemory.RecordDismissal(
            new ManagerId(1),
            new ClubId(2),
            new FixtureId(9),
            Day);
        social.CareerMemory.RecordHiring(
            new ManagerId(1),
            new ClubId(3),
            new JobOfferId(4),
            Day.AddDays(10));

        var path = Path.Combine(_tempDirectory, "career-memory.db");
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
            Array.Empty<Domain.Transfer.TransferNeed>(),
            Array.Empty<Domain.Transfer.ShortlistEntry>(),
            Array.Empty<Domain.Transfer.TransferTarget>(),
            Array.Empty<Domain.Transfer.TransferProcess>(),
            Array.Empty<Domain.Transfer.ClubOffer>(),
            Array.Empty<Domain.Transfer.PlayerContractProposal>(),
            social.PromiseStore.Promises,
            social.MemoryStore.Memories);

        var loaded = new CareerSqlitePersistence().Load(path);
        Assert.Equal(36, loaded.SchemaVersion);
        Assert.Equal(2, loaded.Memories.Count(m => m.Category == MemoryCategory.Career));
        Assert.Contains(loaded.Memories, m => m.RuleId == MemoryRecord.ManagerDismissedRuleId);
        Assert.Contains(loaded.Memories, m => m.RuleId == MemoryRecord.ManagerHiredRuleId);
    }
}
