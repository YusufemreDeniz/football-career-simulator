using FootballCareerSimulator.Application.ClubGovernance.Composition;
using FootballCareerSimulator.Application.ClubGovernance.Infrastructure;
using FootballCareerSimulator.Application.Competition.Infrastructure;
using FootballCareerSimulator.Application.Interaction.Infrastructure;
using FootballCareerSimulator.Application.ManagerCareer.Composition;
using FootballCareerSimulator.Application.ManagerCareer.Infrastructure;
using FootballCareerSimulator.Application.PlayerCareer.Queries;
using FootballCareerSimulator.Application.PlayerCareer.Services;
using FootballCareerSimulator.Application.WorldCalendar.Composition;
using FootballCareerSimulator.Application.WorldCalendar.Infrastructure;
using FootballCareerSimulator.Domain.Competition;
using FootballCareerSimulator.Domain.Interaction;
using FootballCareerSimulator.Domain.Shared;
using FootballCareerSimulator.Domain.WorldCalendar;
using FootballCareerSimulator.Infrastructure.Career;
using FootballCareerSimulator.Simulation.PlayerCareer;
using Microsoft.Data.Sqlite;

namespace FootballCareerSimulator.Tests.PlayerCareer;

public sealed class YouthAcademyIntakeTests : IDisposable
{
    private static readonly GameDate Day = GameDate.FromCalendarDate(2026, 7, 1);

    private readonly string _tempDirectory = Path.Combine(
        Path.GetTempPath(),
        "fcs-youth-academy",
        Guid.NewGuid().ToString("N"));

    public YouthAcademyIntakeTests() => Directory.CreateDirectory(_tempDirectory);

    public void Dispose()
    {
        SqliteConnection.ClearAllPools();
        if (Directory.Exists(_tempDirectory))
        {
            Directory.Delete(_tempDirectory, recursive: true);
        }
    }

    [Fact]
    public void Generate_SameWorldInputsProduceStableUniqueSeasonalCandidates()
    {
        var first = MvpYouthAcademyIntakeGenerator.Generate(
            new ClubId(1),
            new SeasonId(3),
            rootSeed: 912,
            sportiveStrength: 95,
            rngVersion: MvpYouthAcademyIntakeGenerator.GeneratorVersion1);
        var repeated = MvpYouthAcademyIntakeGenerator.Generate(
            new ClubId(1),
            new SeasonId(3),
            rootSeed: 912,
            sportiveStrength: 95,
            rngVersion: MvpYouthAcademyIntakeGenerator.GeneratorVersion1);
        var nextSeason = MvpYouthAcademyIntakeGenerator.Generate(
            new ClubId(1),
            new SeasonId(4),
            rootSeed: 912,
            sportiveStrength: 95,
            rngVersion: MvpYouthAcademyIntakeGenerator.GeneratorVersion1);

        Assert.Equal(first, repeated);
        Assert.InRange(
            first.Count,
            MvpYouthAcademyIntakeGenerator.MinCandidateCount,
            MvpYouthAcademyIntakeGenerator.MaxCandidateCount);
        Assert.Equal(first.Count, first.Select(candidate => candidate.PlayerId).Distinct().Count());
        Assert.Equal(first.Count, first.Select(candidate => candidate.DisplayName).Distinct().Count());
        Assert.All(first, candidate =>
        {
            Assert.InRange(
                candidate.Age,
                MvpYouthAcademyIntakeGenerator.MinCandidateAge,
                MvpYouthAcademyIntakeGenerator.MaxCandidateAge);
            Assert.True(candidate.PotentialAbility > candidate.CurrentAbility);
        });
        Assert.Empty(
            first.Select(candidate => candidate.PlayerId)
                .Intersect(nextSeason.Select(candidate => candidate.PlayerId)));
        Assert.Collection(
            first,
            candidate => Assert.Equal(
                (6_000_000_001_000_000_031L, "Yağız Arslan", "CentreBack", 16, 48, 78),
                (candidate.PlayerId.Value, candidate.DisplayName, candidate.PositionRole.ToString(),
                    candidate.Age, candidate.CurrentAbility, candidate.PotentialAbility)),
            candidate => Assert.Equal(
                (6_000_000_001_000_000_032L, "Cem Demir", "Striker", 16, 49, 90),
                (candidate.PlayerId.Value, candidate.DisplayName, candidate.PositionRole.ToString(),
                    candidate.Age, candidate.CurrentAbility, candidate.PotentialAbility)),
            candidate => Assert.Equal(
                (6_000_000_001_000_000_033L, "Cem Taş", "RightWinger", 17, 44, 85),
                (candidate.PlayerId.Value, candidate.DisplayName, candidate.PositionRole.ToString(),
                    candidate.Age, candidate.CurrentAbility, candidate.PotentialAbility)));
        Assert.Throws<NotSupportedException>(() =>
            MvpYouthAcademyIntakeGenerator.Generate(
                new ClubId(1),
                new SeasonId(3),
                rootSeed: 912,
                sportiveStrength: 95,
                rngVersion: "999"));
    }

    [Fact]
    public void AcceptReject_AreStableIdempotentDecisionsForCurrentIntake()
    {
        var context = CreateContext();
        var initial = context.Service.GetManagedClubIntake()!;
        var acceptedId = initial.Candidates[0].PlayerId;
        var rejectedId = initial.Candidates[1].PlayerId;

        var accepted = context.Service.AcceptManagedCandidate(acceptedId);
        var acceptedAgain = context.Service.AcceptManagedCandidate(acceptedId);
        var rejected = context.Service.RejectManagedCandidate(rejectedId);

        Assert.Equal(YouthAcademyCandidateDecisionStatus.Accepted, accepted.DecisionStatus);
        Assert.Equal(accepted.DecisionRequestId, acceptedAgain.DecisionRequestId);
        Assert.Equal(YouthAcademyCandidateDecisionStatus.Rejected, rejected.DecisionStatus);
        Assert.Equal(2, context.Decisions.Requests.Count);
        Assert.All(context.Decisions.Requests, request =>
        {
            Assert.Equal(DecisionRequestKind.YouthAcademyCandidate, request.Kind);
            Assert.False(request.IsHardBlocker);
            Assert.Equal(DecisionRequestStatus.Answered, request.Status);
        });
        Assert.Throws<YouthAcademyIntakeException>(() =>
            context.Service.RejectManagedCandidate(acceptedId));

        var recreated = new YouthAcademyIntakeService(
            context.Clubs.Store,
            context.Competition,
            context.Manager.Store,
            context.World.TimelineStore,
            context.Decisions);
        var restored = recreated.GetManagedClubIntake()!;
        Assert.Equal(
            YouthAcademyCandidateDecisionStatus.Accepted,
            restored.Candidates.Single(candidate => candidate.PlayerId == acceptedId).DecisionStatus);
        Assert.Equal(
            YouthAcademyCandidateDecisionStatus.Rejected,
            restored.Candidates.Single(candidate => candidate.PlayerId == rejectedId).DecisionStatus);
    }

    [Fact]
    public void Intake_RemainsHiddenUntilSeasonRevealDay()
    {
        var context = CreateContext(futureReveal: true);

        var hidden = context.Service.GetManagedClubIntake()!;

        Assert.False(hidden.IsRevealed);
        Assert.False(hidden.IsComplete);
        Assert.Empty(hidden.Candidates);

        context.World.TimelineStore.Timeline.AdvanceTo(Day.AddDays(7));
        var revealed = context.Service.GetManagedClubIntake()!;

        Assert.True(revealed.IsRevealed);
        Assert.InRange(
            revealed.Candidates.Count,
            MvpYouthAcademyIntakeGenerator.MinCandidateCount,
            MvpYouthAcademyIntakeGenerator.MaxCandidateCount);
    }

    [Fact]
    public void DecisionState_RoundTripsThroughCurrentCareerSchemaWithoutMigration()
    {
        var context = CreateContext();
        var acceptedId = context.Service.GetManagedClubIntake()!.Candidates[0].PlayerId;
        context.Service.AcceptManagedCandidate(acceptedId);
        var path = Path.Combine(_tempDirectory, "academy.db");
        var persistence = new CareerSqlitePersistence();

        persistence.Save(
            path,
            context.World.TimelineStore.Timeline,
            context.Competition.League,
            context.Clubs.Store.Registry,
            context.Manager.Store.Career,
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
            Array.Empty<Domain.SocialContinuity.Promise>(),
            Array.Empty<Domain.SocialContinuity.MemoryRecord>(),
            decisionRequests: context.Decisions.Requests);

        var loaded = persistence.Load(path);

        Assert.Equal(48, loaded.SchemaVersion);
        var request = Assert.Single(loaded.DecisionRequests);
        Assert.Equal(DecisionRequestKind.YouthAcademyCandidate, request.Kind);
        Assert.Equal(acceptedId, request.SubjectPlayerId.Value);
        Assert.Equal(DecisionRequest.OptionAcceptYouthAcademyCandidate, request.SelectedOptionCode);

        var restoredDecisions = new InMemoryDecisionRequestStore();
        restoredDecisions.ReplaceAll(loaded.DecisionRequests);
        var restoredService = new YouthAcademyIntakeService(
            new InMemoryClubRegistryStore(loaded.ClubRegistry),
            new InMemoryLeagueCompetitionStore(loaded.League),
            new InMemoryManagerCareerStore(loaded.ManagerCareer),
            new InMemoryWorldTimelineStore(loaded.Timeline),
            restoredDecisions);
        var restoredCandidate = restoredService.GetManagedClubIntake()!.Candidates
            .Single(candidate => candidate.PlayerId == acceptedId);
        Assert.Equal(YouthAcademyCandidateDecisionStatus.Accepted, restoredCandidate.DecisionStatus);
    }

    [Fact]
    public void Load_V45SaveWithAcademyDecision_MigratesToCurrentContract()
    {
        var context = CreateContext();
        var acceptedId = context.Service.GetManagedClubIntake()!.Candidates[0].PlayerId;
        context.Service.AcceptManagedCandidate(acceptedId);
        var path = Path.Combine(_tempDirectory, "academy-v45.db");
        var persistence = new CareerSqlitePersistence();

        persistence.Save(
            path,
            context.World.TimelineStore.Timeline,
            context.Competition.League,
            context.Clubs.Store.Registry,
            context.Manager.Store.Career,
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
            Array.Empty<Domain.SocialContinuity.Promise>(),
            Array.Empty<Domain.SocialContinuity.MemoryRecord>(),
            decisionRequests: context.Decisions.Requests);

        using (var connection = new SqliteConnection($"Data Source={path}"))
        {
            connection.Open();
            using var command = connection.CreateCommand();
            command.CommandText = "UPDATE ProductionSaveManifest SET SchemaVersion = 45;";
            command.ExecuteNonQuery();
        }

        var loaded = persistence.Load(path);

        Assert.True(loaded.WasMigrated);
        Assert.Equal(48, loaded.SchemaVersion);
        var request = Assert.Single(loaded.DecisionRequests);
        Assert.Equal(DecisionRequestKind.YouthAcademyCandidate, request.Kind);
        Assert.Equal(acceptedId, request.SubjectPlayerId.Value);
        Assert.Equal(DecisionRequest.OptionAcceptYouthAcademyCandidate, request.SelectedOptionCode);
    }

    private static AcademyContext CreateContext(bool futureReveal = false)
    {
        var world = WorldCalendarModule.Create(Day, rootSeed: 912);
        var clubs = ClubGovernanceModule.CreateMvpLeague();
        var managedClub = clubs.Store.Registry.GetClubOrThrow(new ClubId(1));
        var manager = ManagerCareerModule.CreateNewCareer(
            Day,
            startingClubId: managedClub.Id.Value,
            clubSportiveStrength: managedClub.SportiveStrength);
        var league = new LeagueCompetition(new CompetitionId(1));
        var revealDay = futureReveal ? Day.AddDays(7) : Day;
        var season = league.CreateSeason(new SeasonId(3), revealDay);
        foreach (var club in clubs.Store.Registry.Clubs)
        {
            season.RegisterParticipant(club.Id);
        }

        if (!futureReveal)
        {
            league.StartSeason(season.SeasonId, Day);
        }
        var competition = new InMemoryLeagueCompetitionStore(league);
        var decisions = new InMemoryDecisionRequestStore();
        var service = new YouthAcademyIntakeService(
            clubs.Store,
            competition,
            manager.Store,
            world.TimelineStore,
            decisions);
        return new AcademyContext(world, clubs, manager, competition, decisions, service);
    }

    private sealed record AcademyContext(
        WorldCalendarModule World,
        ClubGovernanceModule Clubs,
        ManagerCareerModule Manager,
        InMemoryLeagueCompetitionStore Competition,
        InMemoryDecisionRequestStore Decisions,
        YouthAcademyIntakeService Service);
}
