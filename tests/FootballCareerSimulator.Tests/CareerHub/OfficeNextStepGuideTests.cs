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
}
