using FootballCareerSimulator.Application.Interaction.Composition;
using FootballCareerSimulator.Application.Interaction.Services;
using FootballCareerSimulator.Application.ManagerCareer.Composition;
using FootballCareerSimulator.Application.SocialContinuity.Composition;
using FootballCareerSimulator.Application.WorldCalendar.Composition;
using FootballCareerSimulator.Domain.Interaction;
using FootballCareerSimulator.Domain.PlayerCareer;
using FootballCareerSimulator.Domain.SocialContinuity;
using FootballCareerSimulator.Domain.WorldCalendar;
using FootballCareerSimulator.Infrastructure.Career;
using Microsoft.Data.Sqlite;

namespace FootballCareerSimulator.Tests.Interaction;

public sealed class DecisionRequestTests : IDisposable
{
    private static readonly GameDate Day = GameDate.FromCalendarDate(2026, 8, 1);

    private readonly string _tempDirectory = Path.Combine(
        Path.GetTempPath(),
        "fcs-decision",
        Guid.NewGuid().ToString("N"));

    public DecisionRequestTests() => Directory.CreateDirectory(_tempDirectory);

    public void Dispose()
    {
        SqliteConnection.ClearAllPools();
        if (Directory.Exists(_tempDirectory))
        {
            Directory.Delete(_tempDirectory, recursive: true);
        }
    }

    [Fact]
    public void GrantAnswer_CreatesPlayingTimePromise_AndClearsHardBlocker()
    {
        var manager = ManagerCareerModule.CreateNewCareer(Day, startingClubId: 1);
        var social = SocialContinuityModule.Create();
        var interaction = InteractionModule.Create(manager.Store, social.PlayingTime);

        var request = interaction.Decisions.OpenPlayingTimeRequest(new PlayerId(10), Day);
        Assert.True(request.IsHardBlocker);
        Assert.Single(interaction.TimeAdvanceBlocker.GetActiveBlockers());

        var answered = interaction.Decisions.Answer(
            request.DecisionRequestId,
            DecisionRequest.OptionGrantPlayingTimePromise,
            Day);
        Assert.Equal(DecisionRequestStatus.Answered, answered.Status);
        Assert.Empty(interaction.TimeAdvanceBlocker.GetActiveBlockers());

        var promise = Assert.Single(social.PromiseStore.Promises);
        Assert.Equal(PromiseKind.PlayingTime, promise.Kind);
        Assert.Equal(10, promise.Promisee.Id);
    }

    [Fact]
    public void RefuseAnswer_DoesNotCreatePromise()
    {
        var manager = ManagerCareerModule.CreateNewCareer(Day, startingClubId: 1);
        var social = SocialContinuityModule.Create();
        var interaction = InteractionModule.Create(manager.Store, social.PlayingTime);

        var request = interaction.Decisions.OpenPlayingTimeRequest(new PlayerId(11), Day);
        interaction.Decisions.Answer(
            request.DecisionRequestId,
            DecisionRequest.OptionRefuse,
            Day);

        Assert.Empty(social.PromiseStore.Promises);
        Assert.Equal(0, interaction.Queries.GetPending().OpenCount);
    }

    [Fact]
    public void SoftDecision_ExpiresWhenDue()
    {
        var manager = ManagerCareerModule.CreateNewCareer(Day, startingClubId: 1);
        var interaction = InteractionModule.Create(manager.Store);

        interaction.Decisions.OpenPlayingTimeRequest(
            new PlayerId(12),
            Day,
            deadlineDays: 3,
            isHardBlocker: false);

        Assert.Equal(0, interaction.Decisions.ExpireDue(Day.AddDays(2)));
        Assert.Equal(1, interaction.Decisions.ExpireDue(Day.AddDays(3)));
        Assert.Equal(
            DecisionRequestStatus.Expired,
            interaction.DecisionRequestStore.Requests.Single().Status);
    }

    [Fact]
    public void SaveLoad_PreservesDecisionRequestAtSchemaV34()
    {
        var manager = ManagerCareerModule.CreateNewCareer(Day, startingClubId: 1);
        var social = SocialContinuityModule.Create();
        var interaction = InteractionModule.Create(manager.Store, social.PlayingTime);
        interaction.Decisions.OpenPlayingTimeRequest(new PlayerId(13), Day);

        var path = Path.Combine(_tempDirectory, "decision.db");
        new CareerSqlitePersistence().Save(
            path,
            WorldCalendarModule.Create(Day, rootSeed: 2).TimelineStore.Timeline,
            new Domain.Competition.LeagueCompetition(new Domain.Competition.CompetitionId(1)),
            Application.ClubGovernance.Composition.ClubGovernanceModule.CreateMvpLeague().Store.Registry,
            manager.Store.Career,
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
            social.MemoryStore.Memories,
            social.RelationshipStore.Relationships,
            interaction.DecisionRequestStore.Requests);

        var loaded = new CareerSqlitePersistence().Load(path);
        Assert.Equal(34, loaded.SchemaVersion);
        var request = Assert.Single(loaded.DecisionRequests);
        Assert.Equal(DecisionRequestKind.PlayingTimeRequest, request.Kind);
        Assert.Equal(13, request.SubjectPlayerId.Value);
        Assert.True(request.IsHardBlocker);
    }
}
