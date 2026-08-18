using FootballCareerSimulator.Application.Interaction.Composition;
using FootballCareerSimulator.Application.Interaction.Queries;
using FootballCareerSimulator.Application.ManagerCareer.Composition;
using FootballCareerSimulator.Application.SocialContinuity.Composition;
using FootballCareerSimulator.Domain.Interaction;
using FootballCareerSimulator.Domain.PlayerCareer;
using FootballCareerSimulator.Domain.Shared;
using FootballCareerSimulator.Domain.SocialContinuity;
using FootballCareerSimulator.Domain.WorldCalendar;

namespace FootballCareerSimulator.Tests.Interaction;

public sealed class DecisionDeskPromisePressureTests
{
    private static readonly GameDate Day = GameDate.FromCalendarDate(2026, 8, 1);

    [Fact]
    public void GetPending_PrioritizesLowTrustTransfer_OverLaterPlayingTime()
    {
        var manager = ManagerCareerModule.CreateNewCareer(Day, startingClubId: 1);
        var social = SocialContinuityModule.Create();
        var interaction = InteractionModule.Create(
            manager.Store,
            social.PlayingTime,
            relationships: social.RelationshipEvaluation,
            decisionMemory: social.DecisionMemory,
            promiseStore: social.PromiseStore,
            relationshipStore: social.RelationshipStore,
            memoryStore: social.MemoryStore);

        // Soft playing-time (deadline daha erken) — Low Trust transfer'den sonra gelmeli.
        interaction.Decisions.OpenPlayingTimeRequest(
            new PlayerId(10),
            Day,
            deadlineDays: 3,
            isHardBlocker: false);

        // İki kırık söz → Trust Low → TransferRequest
        social.PlayingTime.Create(
            manager.Store.Career.ManagerId,
            new PlayerId(11),
            new ClubId(1),
            targetAppearances: 2,
            deadlineOn: Day.AddDays(2),
            createdOn: Day);
        social.StartingOpportunity.EvaluateDeadlines(Day.AddDays(2));
        social.PlayingTime.Create(
            manager.Store.Career.ManagerId,
            new PlayerId(11),
            new ClubId(1),
            targetAppearances: 2,
            deadlineOn: Day.AddDays(8),
            createdOn: Day.AddDays(3));
        social.StartingOpportunity.EvaluateDeadlines(
            Day.AddDays(8),
            promise => interaction.PromiseBroken.TryOpenAfterBroken(promise, Day.AddDays(8)));

        Assert.Equal(
            RelationshipDimensionBand.Low,
            RelationshipDimensionBands.FromValue(
                social.RelationshipStore.FindPlayerToManager(11, 1)!.Trust));

        var pending = interaction.Queries.GetPending(take: 5);
        Assert.True(pending.OpenCount >= 2);
        Assert.Equal("Transfer isteği", pending.OpenRequests[0].KindName);
        Assert.Equal(11, pending.OpenRequests[0].SubjectPlayerId);
    }

    [Fact]
    public void ExplainCausality_AndDesk_SurfaceBrokenPromiseForTransfer()
    {
        var manager = ManagerCareerModule.CreateNewCareer(Day, startingClubId: 1);
        var social = SocialContinuityModule.Create();
        var interaction = InteractionModule.Create(
            manager.Store,
            social.PlayingTime,
            relationships: social.RelationshipEvaluation,
            decisionMemory: social.DecisionMemory,
            promiseStore: social.PromiseStore,
            relationshipStore: social.RelationshipStore,
            memoryStore: social.MemoryStore);

        social.PlayingTime.Create(
            manager.Store.Career.ManagerId,
            new PlayerId(12),
            new ClubId(1),
            targetAppearances: 2,
            deadlineOn: Day.AddDays(2),
            createdOn: Day);
        social.StartingOpportunity.EvaluateDeadlines(Day.AddDays(2));
        social.PlayingTime.Create(
            manager.Store.Career.ManagerId,
            new PlayerId(12),
            new ClubId(1),
            targetAppearances: 2,
            deadlineOn: Day.AddDays(8),
            createdOn: Day.AddDays(3));
        social.StartingOpportunity.EvaluateDeadlines(
            Day.AddDays(8),
            promise => interaction.PromiseBroken.TryOpenAfterBroken(promise, Day.AddDays(8)));

        var transfer = Assert.Single(
            interaction.DecisionRequestStore.Requests,
            r => r.Kind == DecisionRequestKind.TransferRequest && r.IsOpen);
        var causality = interaction.Queries.ExplainCausality(transfer.DecisionRequestId);
        Assert.NotNull(causality);
        Assert.Contains("bozuldu", causality, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("güven düşük", causality, StringComparison.OrdinalIgnoreCase);

        var pending = interaction.Queries.GetPending(take: 3);
        var desk = DecisionDeskDigest.Compose(pending, Day.AddDays(8).DayNumber, causality);
        Assert.Equal("Söz kırıldı — oyuncu ayrılmak istiyor.", desk.Headline);
        Assert.Contains("bozuldu", desk.SupportingLine, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(causality, desk.CausalityLine);
    }

    [Fact]
    public void PlayingTimeCrisis_AfterFirstBreak_ExplainsBrokenPromise()
    {
        var manager = ManagerCareerModule.CreateNewCareer(Day, startingClubId: 1);
        var social = SocialContinuityModule.Create();
        var interaction = InteractionModule.Create(
            manager.Store,
            social.PlayingTime,
            relationships: social.RelationshipEvaluation,
            decisionMemory: social.DecisionMemory,
            promiseStore: social.PromiseStore,
            relationshipStore: social.RelationshipStore,
            memoryStore: social.MemoryStore);

        social.PlayingTime.Create(
            manager.Store.Career.ManagerId,
            new PlayerId(13),
            new ClubId(1),
            targetAppearances: 2,
            deadlineOn: Day.AddDays(2),
            createdOn: Day);
        social.StartingOpportunity.EvaluateDeadlines(
            Day.AddDays(2),
            promise => interaction.PromiseBroken.TryOpenAfterBroken(promise, Day.AddDays(2)));

        var request = Assert.Single(
            interaction.DecisionRequestStore.Requests,
            r => r.Kind == DecisionRequestKind.PlayingTimeRequest && r.IsOpen);
        var causality = interaction.Queries.ExplainCausality(request.DecisionRequestId);
        Assert.NotNull(causality);
        Assert.Contains("forma sözü bozuldu", causality, StringComparison.OrdinalIgnoreCase);

        var desk = DecisionDeskDigest.Compose(
            interaction.Queries.GetPending(take: 1),
            Day.AddDays(2).DayNumber,
            causality);
        Assert.Equal("Forma sözü bozuldu — yeni talep masada.", desk.Headline);
    }
}
