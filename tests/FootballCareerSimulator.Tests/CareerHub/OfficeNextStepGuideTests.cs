using FootballCareerSimulator.Application.CareerHub.Queries;
using FootballCareerSimulator.Application.Competition.Queries;
using FootballCareerSimulator.Application.TeamPreparation.Queries;
using FootballCareerSimulator.Application.Transfer.Queries;
using FootballCareerSimulator.Application.WorldCalendar.Queries;

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
    public void MatchPulse_UnapprovedWithInjury_LabelsSakatsizApprove()
    {
        var step = OfficeNextStepGuide.ResolveFromPulse(
            TodayPulseDigest.FocusMatch,
            hasDueUnapprovedMatch: true,
            hasDuePlayableMatch: false,
            canAdvanceDay: true,
            hasInjuryPressure: true);

        Assert.Equal(OfficeNextStepGuide.ActionApproveSelection, step!.ActionCode);
        Assert.Equal("Sakatsız Kadro Onayla", step.ButtonLabel);
    }

    [Fact]
    public void MatchPulse_Playable_OpensMatchDay()
    {
        var step = OfficeNextStepGuide.ResolveFromPulse(
            TodayPulseDigest.FocusMatch,
            hasDueUnapprovedMatch: false,
            hasDuePlayableMatch: true,
            canAdvanceDay: false);

        Assert.Equal(OfficeNextStepGuide.ActionOpenMatchDay, step!.ActionCode);
        Assert.Contains("Maç Gününe", step.ButtonLabel, StringComparison.Ordinal);
    }

    [Fact]
    public void MatchPulse_PlayableWithInjury_LabelsXiCheck()
    {
        var step = OfficeNextStepGuide.ResolveFromPulse(
            TodayPulseDigest.FocusMatch,
            hasDueUnapprovedMatch: false,
            hasDuePlayableMatch: true,
            canAdvanceDay: true,
            hasInjuryPressure: true);

        Assert.Equal(OfficeNextStepGuide.ActionOpenMatchDay, step!.ActionCode);
        Assert.Equal("Maç Günü — XI Kontrol", step.ButtonLabel);
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

    [Fact]
    public void BlockedByFixtures_ForcesMatchDayOrApprove()
    {
        var play = OfficeNextStepGuide.ResolveFromPulse(
            TodayPulseDigest.FocusCalm,
            hasDueUnapprovedMatch: false,
            hasDuePlayableMatch: true,
            canAdvanceDay: false,
            primaryBlockerCode: TimeAdvanceBlockerDigest.CodeUnplayedFixtures);

        Assert.Equal(OfficeNextStepGuide.ActionOpenMatchDay, play!.ActionCode);
        Assert.Contains("engel", play.ButtonLabel, StringComparison.Ordinal);

        var approve = OfficeNextStepGuide.ResolveFromPulse(
            TodayPulseDigest.FocusTransfer,
            hasDueUnapprovedMatch: true,
            hasDuePlayableMatch: false,
            canAdvanceDay: false,
            primaryBlockerCode: TimeAdvanceBlockerDigest.CodeUnplayedFixtures);

        Assert.Equal(OfficeNextStepGuide.ActionApproveSelection, approve!.ActionCode);
    }

    [Fact]
    public void BlockedByDecision_ForcesDeskAnswer()
    {
        var step = OfficeNextStepGuide.ResolveFromPulse(
            TodayPulseDigest.FocusCalm,
            hasDueUnapprovedMatch: false,
            hasDuePlayableMatch: false,
            canAdvanceDay: false,
            primaryBlockerCode: TimeAdvanceBlockerDigest.CodePendingDecision);

        Assert.Equal(TodayPulseDigest.FocusDesk, step!.FocusCode);
        Assert.Contains("Zorunlu", step.ButtonLabel, StringComparison.Ordinal);
        Assert.Equal(OfficeNextStepGuide.ActionNavigate, step.ActionCode);
    }

    [Fact]
    public void SeasonReady_BecomesPrimaryTransitionCta()
    {
        var step = OfficeNextStepGuide.ResolveFromPulse(
            TodayPulseDigest.FocusCalm,
            hasDueUnapprovedMatch: false,
            hasDuePlayableMatch: false,
            canAdvanceDay: true,
            seasonTransitionReady: true,
            seasonArchivePhase: false);

        Assert.Equal(OfficeNextStepGuide.ActionTransitionSeason, step!.ActionCode);
        Assert.Contains("Sezonu Bitir", step.ButtonLabel, StringComparison.Ordinal);

        var archive = OfficeNextStepGuide.ResolveFromPulse(
            TodayPulseDigest.FocusSeason,
            hasDueUnapprovedMatch: false,
            hasDuePlayableMatch: false,
            canAdvanceDay: true,
            seasonTransitionReady: true,
            seasonArchivePhase: true);

        Assert.Equal("Yeni Sezona Geç", archive!.ButtonLabel);
    }

    [Fact]
    public void PrepSuggestion_BecomesPrimaryApplyCta()
    {
        var step = OfficeNextStepGuide.ResolveFromPulse(
            TodayPulseDigest.FocusPrep,
            hasDueUnapprovedMatch: false,
            hasDuePlayableMatch: false,
            canAdvanceDay: true,
            prepSuggestion: PrepPlanSuggestion.RecoveryPlan());

        Assert.Equal(OfficeNextStepGuide.ActionApplyPrepSuggestion, step!.ActionCode);
        Assert.Equal("Toparlanma Uygula", step.ButtonLabel);
        Assert.Equal(OfficeNextStepGuide.TargetPrep, step.TargetPageCode);
    }

    [Fact]
    public void RecoveryPathStepOne_OverridesMatchApproveCta()
    {
        var path = InjuryRecoveryPathDigest.Compose(
            hasInjuryPressure: true,
            injuredPlayerNames: ["Tolga Kurt"],
            isOnRecoveryPlan: false,
            hasDueMatch: true,
            isMatchApproved: false);

        var step = OfficeNextStepGuide.ResolveFromPulse(
            TodayPulseDigest.FocusMatch,
            hasDueUnapprovedMatch: true,
            hasDuePlayableMatch: false,
            canAdvanceDay: false,
            primaryBlockerCode: TimeAdvanceBlockerDigest.CodeUnplayedFixtures,
            hasInjuryPressure: true,
            recoveryPath: path);

        Assert.Equal(OfficeNextStepGuide.ActionApplyPrepSuggestion, step!.ActionCode);
        Assert.Equal("Toparlanma Uygula", step.ButtonLabel);
    }

    [Fact]
    public void RecoveryPathStepTwo_ApprovesInjuryAwareXi()
    {
        var path = InjuryRecoveryPathDigest.Compose(
            hasInjuryPressure: true,
            injuredPlayerNames: ["Tolga Kurt"],
            isOnRecoveryPlan: true,
            hasDueMatch: true,
            isMatchApproved: false);

        var step = OfficeNextStepGuide.ResolveFromPulse(
            TodayPulseDigest.FocusCalm,
            hasDueUnapprovedMatch: true,
            hasDuePlayableMatch: false,
            canAdvanceDay: true,
            recoveryPath: path);

        Assert.Equal(OfficeNextStepGuide.ActionApproveSelection, step!.ActionCode);
        Assert.Equal("Sakatsız Kadro Onayla", step.ButtonLabel);
    }

    [Fact]
    public void RecoveryPathStepThree_OpensMatchDay()
    {
        var path = InjuryRecoveryPathDigest.Compose(
            hasInjuryPressure: true,
            injuredPlayerNames: ["Tolga Kurt"],
            isOnRecoveryPlan: true,
            hasDueMatch: true,
            isMatchApproved: true);

        var step = OfficeNextStepGuide.ResolveFromPulse(
            TodayPulseDigest.FocusLeague,
            hasDueUnapprovedMatch: false,
            hasDuePlayableMatch: true,
            canAdvanceDay: true,
            recoveryPath: path);

        Assert.Equal(OfficeNextStepGuide.ActionOpenMatchDay, step!.ActionCode);
        Assert.Equal("Maç Gününe Git", step.ButtonLabel);
    }

    [Fact]
    public void WeekStoryCleanXi_OpensTemizXiMatchDay()
    {
        var match = PreMatchBriefing.Compose(
            new ManagedFixtureSelectionStatusReadModel(
                1, 1, 1, 2, true, 10, "2026-08-15", IsApproved: true),
            "Rival",
            10,
            cleanReturnNames: ["Tolga Kurt"]);
        var story = WeekStoryDigest.Compose(InjuryRecoveryPathDigest.Clear(), match);

        var step = OfficeNextStepGuide.ResolveFromPulse(
            TodayPulseDigest.FocusCalm,
            hasDueUnapprovedMatch: false,
            hasDuePlayableMatch: true,
            canAdvanceDay: true,
            weekStory: story);

        Assert.Equal(OfficeNextStepGuide.ActionOpenMatchDay, step!.ActionCode);
        Assert.Contains("Temiz XI", step.ButtonLabel, StringComparison.Ordinal);
    }

    [Fact]
    public void WeekStoryVerdict_AdvancesDayToCloseArc()
    {
        var story = WeekStoryDigest.Compose(
            InjuryRecoveryPathDigest.Clear(),
            PreMatchBriefing.Clear(),
            closedArcVerdictBeat: "Dönenler işe yaradı — Kurt");

        var step = OfficeNextStepGuide.ResolveWeekStoryStep(
            story,
            hasDueUnapprovedMatch: false,
            hasDuePlayableMatch: false,
            canAdvanceDay: true);

        Assert.Equal(OfficeNextStepGuide.ActionAdvanceDay, step!.ActionCode);
        Assert.Contains("Hikâyeyi kapat", step.ButtonLabel, StringComparison.Ordinal);
    }

    [Fact]
    public void LeagueSurvival_RoutesPrimaryCtaToToday()
    {
        var step = OfficeNextStepGuide.ResolveFromPulse(
            TodayPulseDigest.FocusLeague,
            hasDueUnapprovedMatch: false,
            hasDuePlayableMatch: false,
            canAdvanceDay: true,
            leagueNextStep: LeagueNextStep.ChaseSurvival());

        Assert.Equal(OfficeNextStepGuide.ActionNavigate, step!.ActionCode);
        Assert.Equal(OfficeNextStepGuide.TargetToday, step.TargetPageCode);
        Assert.Contains("Puan Avı", step.ButtonLabel, StringComparison.Ordinal);
    }

    [Fact]
    public void LeagueKickstart_AdvancesDay()
    {
        var step = OfficeNextStepGuide.ResolveFromPulse(
            TodayPulseDigest.FocusLeague,
            hasDueUnapprovedMatch: false,
            hasDuePlayableMatch: false,
            canAdvanceDay: true,
            leagueNextStep: LeagueNextStep.KickstartCalendar());

        Assert.Equal(OfficeNextStepGuide.ActionAdvanceDay, step!.ActionCode);
        Assert.Contains("İlerlet", step.ButtonLabel, StringComparison.Ordinal);
    }

    [Fact]
    public void TransferSellSuggestion_BecomesPrimarySellCta()
    {
        var step = OfficeNextStepGuide.ResolveFromPulse(
            TodayPulseDigest.FocusTransfer,
            hasDueUnapprovedMatch: false,
            hasDuePlayableMatch: false,
            canAdvanceDay: true,
            transferNextStep: TransferNextStep.SellFringe(1025, closingPressure: true));

        Assert.Equal(OfficeNextStepGuide.ActionSellFringe, step!.ActionCode);
        Assert.Contains("Satışa Çıkar", step.ButtonLabel, StringComparison.Ordinal);
        Assert.Equal(OfficeNextStepGuide.TargetTransfer, step.TargetPageCode);
    }

    [Fact]
    public void TransferClosedWindow_OpensWindow()
    {
        var step = OfficeNextStepGuide.ResolveFromPulse(
            TodayPulseDigest.FocusTransfer,
            hasDueUnapprovedMatch: false,
            hasDuePlayableMatch: false,
            canAdvanceDay: true,
            transferNextStep: TransferNextStep.OpenWindow());

        Assert.Equal(OfficeNextStepGuide.ActionOpenTransferWindow, step!.ActionCode);
        Assert.Equal("Pencere Aç", step.ButtonLabel);
    }

    [Fact]
    public void TransferScanNeeds_MapsToScanNeedsAction()
    {
        var step = OfficeNextStepGuide.ResolveFromPulse(
            TodayPulseDigest.FocusTransfer,
            hasDueUnapprovedMatch: false,
            hasDuePlayableMatch: false,
            canAdvanceDay: true,
            transferNextStep: TransferNextStep.ScanNeeds());

        Assert.Equal(OfficeNextStepGuide.ActionScanNeeds, step!.ActionCode);
        Assert.Equal("İhtiyaç Tara", step.ButtonLabel);
    }

    [Fact]
    public void TransferAdvanceProcess_MapsToAdvanceAction()
    {
        var step = OfficeNextStepGuide.ResolveFromPulse(
            TodayPulseDigest.FocusTransfer,
            hasDueUnapprovedMatch: false,
            hasDuePlayableMatch: false,
            canAdvanceDay: true,
            transferNextStep: TransferNextStep.AdvanceProcess(closingPressure: false));

        Assert.Equal(OfficeNextStepGuide.ActionAdvanceProcess, step!.ActionCode);
        Assert.Equal("Süreci İlerlet", step.ButtonLabel);
    }
}
