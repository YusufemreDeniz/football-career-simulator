using FootballCareerSimulator.Application.CareerHub.Queries;

namespace FootballCareerSimulator.Tests.CareerHub;

public sealed class OfficeNextStepGuideTests
{
    [Fact]
    public void TransferFocus_RoutesToTransferPage()
    {
        var step = OfficeNextStepGuide.Resolve(TodayPulseDigest.FocusTransfer);
        Assert.NotNull(step);
        Assert.Equal("Transfer Masası", step!.ButtonLabel);
        Assert.Equal(OfficeNextStepGuide.TargetTransfer, step.TargetPageCode);
        Assert.Equal(OfficeNextStepGuide.ActionNavigate, step.ActionCode);
    }

    [Fact]
    public void DeskFocus_StaysOnTodayForMasada()
    {
        var step = OfficeNextStepGuide.Resolve(TodayPulseDigest.FocusDesk);
        Assert.NotNull(step);
        Assert.Equal(OfficeNextStepGuide.TargetToday, step!.TargetPageCode);
        Assert.Contains("Masada", step.ButtonLabel, StringComparison.Ordinal);
    }

    [Fact]
    public void CalmOrUnknown_HasNoShortcut()
    {
        Assert.Null(OfficeNextStepGuide.Resolve(TodayPulseDigest.FocusCalm));
        Assert.Null(OfficeNextStepGuide.Resolve(null));
        Assert.Null(OfficeNextStepGuide.Resolve(" "));
        Assert.Null(OfficeNextStepGuide.Resolve("Unknown"));
    }

    [Fact]
    public void SquadAndPrep_RouteToClubAndPrep()
    {
        Assert.Equal(
            OfficeNextStepGuide.TargetClub,
            OfficeNextStepGuide.Resolve(TodayPulseDigest.FocusSquad)!.TargetPageCode);
        Assert.Equal(
            OfficeNextStepGuide.TargetPrep,
            OfficeNextStepGuide.Resolve(TodayPulseDigest.FocusPrep)!.TargetPageCode);
        Assert.Equal(
            OfficeNextStepGuide.TargetWorld,
            OfficeNextStepGuide.Resolve(TodayPulseDigest.FocusLeague)!.TargetPageCode);
    }

    [Fact]
    public void MatchPulse_Unapproved_ApprovesSelection()
    {
        var step = OfficeNextStepGuide.ResolveFromPulse(
            TodayPulseDigest.FocusMatch,
            hasDueUnapprovedMatch: true,
            hasDuePlayableMatch: false,
            canAdvanceDay: true);

        Assert.Equal(OfficeNextStepGuide.ActionApproveSelection, step!.ActionCode);
        Assert.Contains("Kadro", step.ButtonLabel, StringComparison.Ordinal);
    }

    [Fact]
    public void MatchPulse_Playable_PlaysMatches()
    {
        var step = OfficeNextStepGuide.ResolveFromPulse(
            TodayPulseDigest.FocusMatch,
            hasDueUnapprovedMatch: false,
            hasDuePlayableMatch: true,
            canAdvanceDay: false);

        Assert.Equal(OfficeNextStepGuide.ActionPlayMatches, step!.ActionCode);
        Assert.Contains("Oyna", step.ButtonLabel, StringComparison.Ordinal);
    }

    [Fact]
    public void CalmPulse_CanAdvance_AdvancesDay()
    {
        var step = OfficeNextStepGuide.ResolveFromPulse(
            TodayPulseDigest.FocusCalm,
            hasDueUnapprovedMatch: false,
            hasDuePlayableMatch: false,
            canAdvanceDay: true);

        Assert.Equal(OfficeNextStepGuide.ActionAdvanceDay, step!.ActionCode);
        Assert.Contains("İlerlet", step.ButtonLabel, StringComparison.Ordinal);
    }

    [Fact]
    public void CalmPulse_BlockedAdvance_NoShortcut()
    {
        Assert.Null(OfficeNextStepGuide.ResolveFromPulse(
            TodayPulseDigest.FocusCalm,
            hasDueUnapprovedMatch: false,
            hasDuePlayableMatch: false,
            canAdvanceDay: false));
    }
}
