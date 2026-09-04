using FootballCareerSimulator.Application.Interaction.Composition;
using FootballCareerSimulator.Application.ManagerCareer.Composition;
using FootballCareerSimulator.Application.SocialContinuity.Composition;
using FootballCareerSimulator.Application.Transfer.Queries;
using FootballCareerSimulator.Domain.Interaction;
using FootballCareerSimulator.Domain.PlayerCareer;
using FootballCareerSimulator.Domain.Shared;
using FootballCareerSimulator.Domain.SocialContinuity;
using FootballCareerSimulator.Domain.WorldCalendar;

namespace FootballCareerSimulator.Tests.Transfer;

/// <summary>
/// Forma Sözü → Low Trust TransferRequest'in Transfer Masası'nda görünmesi.
/// </summary>
public sealed class TransferDeskPromiseExitPressureTests
{
    private static readonly GameDate Day = GameDate.FromCalendarDate(2026, 8, 1);

    [Fact]
    public void BrokenPromiseEscalation_FeedsTransferDeskPromiseExitPressure()
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
            new PlayerId(88),
            new ClubId(1),
            targetAppearances: 2,
            deadlineOn: Day.AddDays(2),
            createdOn: Day);
        social.StartingOpportunity.EvaluateDeadlines(Day.AddDays(2));
        social.PlayingTime.Create(
            manager.Store.Career.ManagerId,
            new PlayerId(88),
            new ClubId(1),
            targetAppearances: 2,
            deadlineOn: Day.AddDays(8),
            createdOn: Day.AddDays(3));
        social.StartingOpportunity.EvaluateDeadlines(
            Day.AddDays(8),
            promise => interaction.PromiseBroken.TryOpenAfterBroken(promise, Day.AddDays(8)));

        var transfer = Assert.Single(
            interaction.DecisionRequestStore.Requests,
            r => r.IsOpen && r.Kind == DecisionRequestKind.TransferRequest);
        var hint = interaction.Queries.ExplainCausality(transfer.DecisionRequestId);

        var desk = TransferDeskBriefing.Compose(
            windowOpen: true,
            "Açık",
            windowClosesOnDayNumber: 90,
            openNeedCount: 0,
            openExitNeedCount: 0,
            listedTargetCount: 0,
            activeProcessCount: 0,
            pendingOfferCount: 0,
            budgetAvailable: null,
            budgetSpent: null,
            squadFull: false,
            saleCandidatePlayerId: null,
            currentDayNumber: Day.AddDays(8).DayNumber,
            promiseExitPressurePlayerId: transfer.SubjectPlayerId.Value,
            promiseExitPressureHint: hint);

        Assert.True(desk.DemandsAttention);
        Assert.Equal(TransferNextStep.ReasonPromiseExit, desk.NextStep!.ReasonCode);
        Assert.Contains("Söz kırılması", desk.Headline, StringComparison.Ordinal);
        Assert.Contains("kenar oyuncu", desk.NextStep.PulseHeadline, StringComparison.Ordinal);
        Assert.NotNull(hint);
        Assert.Contains("bozuldu", hint, StringComparison.OrdinalIgnoreCase);
    }
}
