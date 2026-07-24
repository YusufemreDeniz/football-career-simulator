using FootballCareerSimulator.Application.Interaction.Composition;
using FootballCareerSimulator.Application.ManagerCareer.Composition;
using FootballCareerSimulator.Application.SocialContinuity.Composition;
using FootballCareerSimulator.Domain.Interaction;
using FootballCareerSimulator.Domain.PlayerCareer;
using FootballCareerSimulator.Domain.WorldCalendar;

namespace FootballCareerSimulator.Tests.Interaction;

public sealed class DialogueOptionGenerationTests
{
    private static readonly GameDate Day = GameDate.FromCalendarDate(2026, 8, 1);

    [Fact]
    public void PlayingTimeRequest_GeneratesGrantAndRefuse_Deterministically()
    {
        var manager = ManagerCareerModule.CreateNewCareer(Day, startingClubId: 1);
        var social = SocialContinuityModule.Create();
        var interaction = InteractionModule.Create(
            manager.Store,
            social.PlayingTime,
            promiseStore: social.PromiseStore);

        var request = interaction.Decisions.OpenPlayingTimeRequest(new PlayerId(20), Day);
        var first = interaction.DialogueOptions.GetForDecision(request.DecisionRequestId);
        var second = interaction.DialogueOptions.GetForDecision(request.DecisionRequestId);

        Assert.True(first.DecisionIsOpen);
        Assert.Equal("PlayingTimeRequest", first.DialogueTypeName);
        Assert.Equal(2, first.Options.Count);
        Assert.Equal(DecisionRequest.OptionGrantPlayingTimePromise, first.Options[0].OptionCode);
        Assert.Equal(DecisionRequest.OptionRefuse, first.Options[1].OptionCode);
        Assert.All(first.Options, o => Assert.True(o.IsEligible));
        Assert.Equal(first.Options.Select(o => o.OptionCode), second.Options.Select(o => o.OptionCode));
        Assert.Equal(first.Options.Select(o => o.DisplayText), second.Options.Select(o => o.DisplayText));
    }

    [Fact]
    public void GrantOption_BecomesIneligible_WhenActivePlayingTimePromiseExists()
    {
        var manager = ManagerCareerModule.CreateNewCareer(Day, startingClubId: 1);
        var social = SocialContinuityModule.Create();
        var interaction = InteractionModule.Create(
            manager.Store,
            social.PlayingTime,
            promiseStore: social.PromiseStore);

        social.PlayingTime.Create(
            manager.Store.Career.ManagerId,
            new PlayerId(21),
            manager.Store.Career.ActiveEmployment!.ClubId,
            targetAppearances: 3,
            deadlineOn: Day.AddDays(30),
            createdOn: Day);

        var request = interaction.Decisions.OpenPlayingTimeRequest(new PlayerId(21), Day);
        var options = interaction.DialogueOptions.GetForDecision(request.DecisionRequestId);
        var grant = Assert.Single(
            options.Options,
            o => o.OptionCode == DecisionRequest.OptionGrantPlayingTimePromise);
        Assert.False(grant.IsEligible);
        Assert.Contains("aktif forma süresi", grant.IneligibilityReason!, StringComparison.OrdinalIgnoreCase);
        Assert.True(Assert.Single(
            options.Options,
            o => o.OptionCode == DecisionRequest.OptionRefuse).IsEligible);
    }

    [Fact]
    public void Answer_RejectsIneligibleGrant_AtSelectionTime()
    {
        var manager = ManagerCareerModule.CreateNewCareer(Day, startingClubId: 1);
        var social = SocialContinuityModule.Create();
        var interaction = InteractionModule.Create(
            manager.Store,
            social.PlayingTime,
            relationships: social.RelationshipEvaluation,
            decisionMemory: social.DecisionMemory,
            promiseStore: social.PromiseStore);

        social.PlayingTime.Create(
            manager.Store.Career.ManagerId,
            new PlayerId(22),
            manager.Store.Career.ActiveEmployment!.ClubId,
            targetAppearances: 2,
            deadlineOn: Day.AddDays(20),
            createdOn: Day);

        var request = interaction.Decisions.OpenPlayingTimeRequest(new PlayerId(22), Day);
        var ex = Assert.Throws<InteractionInvariantViolationException>(() =>
            interaction.Decisions.Answer(
                request.DecisionRequestId,
                DecisionRequest.OptionGrantPlayingTimePromise,
                Day));
        Assert.Contains("aktif forma süresi", ex.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(DecisionRequestStatus.Open, interaction.DecisionRequestStore.Get(request.DecisionRequestId)!.Status);
    }

    [Fact]
    public void ClosedDecision_YieldsNoOptions()
    {
        var manager = ManagerCareerModule.CreateNewCareer(Day, startingClubId: 1);
        var social = SocialContinuityModule.Create();
        var interaction = InteractionModule.Create(
            manager.Store,
            social.PlayingTime,
            promiseStore: social.PromiseStore);

        var request = interaction.Decisions.OpenPlayingTimeRequest(new PlayerId(23), Day);
        interaction.Decisions.Answer(
            request.DecisionRequestId,
            DecisionRequest.OptionRefuse,
            Day);

        var options = interaction.DialogueOptions.GetForDecision(request.DecisionRequestId);
        Assert.False(options.DecisionIsOpen);
        Assert.Empty(options.Options);
    }
}
