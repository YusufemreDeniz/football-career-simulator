using FootballCareerSimulator.Application.CareerHub.Queries;
using FootballCareerSimulator.Application.Competition.Queries;
using FootballCareerSimulator.Application.Interaction.Queries;
using FootballCareerSimulator.Application.TeamPreparation.Queries;
using FootballCareerSimulator.Application.TrainingPhysicalState.Queries;
using FootballCareerSimulator.Application.Transfer.Queries;

namespace FootballCareerSimulator.Tests.CareerHub;

public sealed class TodayPulseDigestTests
{
    [Fact]
    public void HardDesk_LeadsPulse()
    {
        var pulse = TodayPulseDigest.Compose(
            Desk(hard: true, open: true, "Basın kapıda."),
            MatchReady(),
            PrepOk(),
            LeagueOk());

        Assert.Equal(TodayPulseDigest.FocusDesk, pulse.PrimaryFocusCode);
        Assert.Contains("Masada", pulse.Headline, StringComparison.Ordinal);
        Assert.Contains(pulse.PulseLines, l => l.StartsWith("Masada:", StringComparison.Ordinal));
        Assert.Contains("Günün Nabzı", pulse.ToDisplayText(), StringComparison.Ordinal);
    }

    [Fact]
    public void UnapprovedMatch_BeatsCalmPrep()
    {
        var pulse = TodayPulseDigest.Compose(
            DecisionDeskDigest.Clear(),
            PreMatchBriefing.Compose(
                new ManagedFixtureSelectionStatusReadModel(
                    1, 1, 1, 2, true, 10, "2026-08-15", IsApproved: false),
                "Rival",
                10),
            PrepOk(),
            LeagueOk());

        Assert.Equal(TodayPulseDigest.FocusMatch, pulse.PrimaryFocusCode);
        Assert.Contains("kadroyu", pulse.Headline, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void NamedInjury_ShowsPulseLineAndMatchFocus()
    {
        var prep = PreparationBriefing.Compose(
            new ClubTrainingSummaryReadModel(
                1,
                (int)Domain.TrainingPhysicalState.TrainingFocus.General,
                (int)Domain.TrainingPhysicalState.TrainingIntensity.Medium,
                (int)Domain.TrainingPhysicalState.RestApproach.Normal,
                null, null, null, 1, 30, 70, true, 1, 1,
                InjuredPlayerNames: ["Ali Yılmaz"]),
            new TacticPlanReadModel(1, "4-4-2", "Dengeli", 1),
            "±0",
            daysUntilNextMatch: 4);

        var pulse = TodayPulseDigest.Compose(
            DecisionDeskDigest.Clear(),
            PreMatchBriefing.Compose(
                new ManagedFixtureSelectionStatusReadModel(
                    1, 1, 1, 2, true, 10, "2026-08-15", IsApproved: false),
                "Rival",
                10,
                injuredSlotCount: 1,
                injuredPlayerNames: ["Ali Yılmaz"]),
            prep,
            LeagueOk());

        Assert.Equal(TodayPulseDigest.FocusMatch, pulse.PrimaryFocusCode);
        Assert.Contains("sakatsız", pulse.Headline, StringComparison.OrdinalIgnoreCase);
        Assert.Contains(pulse.PulseLines, l => l.StartsWith("Sakat:", StringComparison.Ordinal)
            && l.Contains("Ali Yılmaz", StringComparison.Ordinal));
    }

    [Fact]
    public void NamedInjuryWithoutDueMatch_PrepRecoveryFocus()
    {
        var prep = PreparationBriefing.Compose(
            new ClubTrainingSummaryReadModel(
                1,
                (int)Domain.TrainingPhysicalState.TrainingFocus.General,
                (int)Domain.TrainingPhysicalState.TrainingIntensity.Medium,
                (int)Domain.TrainingPhysicalState.RestApproach.Normal,
                null, null, null, 1, 30, 70, true, 1, 1,
                InjuredPlayerNames: ["Can Demir"]),
            new TacticPlanReadModel(1, "4-4-2", "Dengeli", 1),
            "±0",
            daysUntilNextMatch: 5);

        var pulse = TodayPulseDigest.Compose(
            DecisionDeskDigest.Clear(),
            PreMatchBriefing.Clear(),
            prep,
            LeagueOk());

        Assert.Equal(TodayPulseDigest.FocusPrep, pulse.PrimaryFocusCode);
        Assert.Contains("Can Demir", pulse.Headline, StringComparison.Ordinal);
        Assert.Contains("Toparlanma", pulse.Headline, StringComparison.Ordinal);
    }

    [Fact]
    public void FatiguedPrep_SurfacesWhenElseQuiet()
    {
        var prep = PreparationBriefing.Compose(
            new ClubTrainingSummaryReadModel(
                1,
                (int)Domain.TrainingPhysicalState.TrainingFocus.Fitness,
                (int)Domain.TrainingPhysicalState.TrainingIntensity.High,
                (int)Domain.TrainingPhysicalState.RestApproach.Light,
                null, null, null, 1, 70, 55, true, 0, 0),
            new TacticPlanReadModel(1, "4-3-3", "Dengeli", 1),
            "+1",
            daysUntilNextMatch: 1);

        var pulse = TodayPulseDigest.Compose(
            DecisionDeskDigest.Clear(),
            PreMatchBriefing.Clear(),
            prep,
            LeagueOk());

        Assert.Equal(TodayPulseDigest.FocusPrep, pulse.PrimaryFocusCode);
        Assert.Contains("Toparlanma", pulse.Headline, StringComparison.Ordinal);
        Assert.True(prep.DemandsAttention);
        Assert.Equal(PrepPlanSuggestion.ApplyRecovery, prep.Suggestion!.ActionCode);
    }

    [Fact]
    public void LowFitnessNearMatch_SurfacesPrepSuggestion()
    {
        var prep = PreparationBriefing.Compose(
            new ClubTrainingSummaryReadModel(
                1,
                (int)Domain.TrainingPhysicalState.TrainingFocus.General,
                (int)Domain.TrainingPhysicalState.TrainingIntensity.Medium,
                (int)Domain.TrainingPhysicalState.RestApproach.Normal,
                null, null, null, 1, 30, 40, true, 0, 0),
            new TacticPlanReadModel(1, "4-3-3", "Dengeli", 1),
            "+1",
            daysUntilNextMatch: 2);

        var pulse = TodayPulseDigest.Compose(
            DecisionDeskDigest.Clear(),
            PreMatchBriefing.Clear(),
            prep,
            LeagueOk());

        Assert.Equal(TodayPulseDigest.FocusPrep, pulse.PrimaryFocusCode);
        Assert.Equal(PrepPlanSuggestion.ApplyFitness, prep.Suggestion!.ActionCode);
        Assert.Contains("Kondisyon", pulse.Headline, StringComparison.Ordinal);
    }

    [Fact]
    public void AlreadyRecovering_DoesNotDemandPrepAttention()
    {
        var prep = PreparationBriefing.Compose(
            new ClubTrainingSummaryReadModel(
                1,
                (int)Domain.TrainingPhysicalState.TrainingFocus.Recovery,
                (int)Domain.TrainingPhysicalState.TrainingIntensity.Low,
                (int)Domain.TrainingPhysicalState.RestApproach.Heavy,
                null, null, null, 1, 70, 55, true, 0, 0),
            new TacticPlanReadModel(1, "4-3-3", "Dengeli", 1),
            "+1",
            daysUntilNextMatch: 1);

        var pulse = TodayPulseDigest.Compose(
            DecisionDeskDigest.Clear(),
            PreMatchBriefing.Clear(),
            prep,
            LeagueOk());

        Assert.False(prep.DemandsAttention);
        Assert.Equal(TodayPulseDigest.FocusCalm, pulse.PrimaryFocusCode);
    }

    [Fact]
    public void BottomTableLeague_SurfacesSurvivalFocus()
    {
        var league = LeagueWorldBriefing.Compose(
            "Active",
            12,
            30,
            clubCount: 10,
            managedRank: 10,
            managedPoints: 5,
            managedPlayed: 12,
            managedGoalDifference: -14,
            managedClubName: "Struggle",
            leaderClubName: "Giants",
            leaderPoints: 28,
            nextMatchLine: null);

        var pulse = TodayPulseDigest.Compose(
            DecisionDeskDigest.Clear(),
            PreMatchBriefing.Clear(),
            PrepOk(),
            league);

        Assert.Equal(TodayPulseDigest.FocusLeague, pulse.PrimaryFocusCode);
        Assert.Contains("Küme", pulse.Headline, StringComparison.Ordinal);
    }

    [Fact]
    public void SummitLeague_SurfacesProtectPrepFocus()
    {
        var league = LeagueWorldBriefing.Compose(
            "Active",
            10,
            30,
            clubCount: 8,
            managedRank: 1,
            managedPoints: 22,
            managedPlayed: 10,
            managedGoalDifference: 8,
            managedClubName: "Home",
            leaderClubName: "Home",
            leaderPoints: 22,
            nextMatchLine: null);

        var pulse = TodayPulseDigest.Compose(
            DecisionDeskDigest.Clear(),
            PreMatchBriefing.Clear(),
            PrepOk(),
            league);

        Assert.Equal(TodayPulseDigest.FocusLeague, pulse.PrimaryFocusCode);
        Assert.Contains("Hazırlık", pulse.Headline, StringComparison.Ordinal);
    }

    [Fact]
    public void CalmDay_WhenEverythingQuiet()
    {
        var pulse = TodayPulseDigest.Compose(
            DecisionDeskDigest.Clear(),
            PreMatchBriefing.Clear(),
            PrepOk(),
            LeagueOk());

        Assert.Equal(TodayPulseDigest.FocusCalm, pulse.PrimaryFocusCode);
        Assert.Contains("Sakin", pulse.Headline, StringComparison.Ordinal);
    }

    [Fact]
    public void SquadOverflow_BeatsCalmMatch()
    {
        var squad = SquadCapacityDigest.Compose(
            activeContractCount: 26,
            squadMemberCount: 25,
            maxMembers: 25,
            overflowPlayerIds: [2001]);

        var pulse = TodayPulseDigest.Compose(
            DecisionDeskDigest.Clear(),
            MatchReady(),
            PrepOk(),
            LeagueOk(),
            squad);

        Assert.Equal(TodayPulseDigest.FocusSquad, pulse.PrimaryFocusCode);
        Assert.Contains("Kadro taştı", pulse.Headline, StringComparison.Ordinal);
        Assert.Contains("Yer Aç", pulse.Headline, StringComparison.Ordinal);
        Assert.Contains(pulse.PulseLines, l => l.StartsWith("Kadro:", StringComparison.Ordinal));
    }

    [Fact]
    public void FullSquad_OnCalmDay_HintsYerAc_WithoutBeatingMatch()
    {
        var full = SquadCapacityDigest.Compose(
            activeContractCount: 25,
            squadMemberCount: 25,
            maxMembers: 25,
            overflowPlayerIds: Array.Empty<long>());

        var calm = TodayPulseDigest.Compose(
            DecisionDeskDigest.Clear(),
            PreMatchBriefing.Clear(),
            PrepOk(),
            LeagueOk(),
            full);

        Assert.Equal(TodayPulseDigest.FocusSquad, calm.PrimaryFocusCode);
        Assert.Contains("Yer Aç", calm.Headline, StringComparison.Ordinal);
        Assert.Contains(calm.PulseLines, l => l.StartsWith("Kadro:", StringComparison.Ordinal));

        var withMatch = TodayPulseDigest.Compose(
            DecisionDeskDigest.Clear(),
            MatchReady(),
            PrepOk(),
            LeagueOk(),
            full);

        Assert.Equal(TodayPulseDigest.FocusMatch, withMatch.PrimaryFocusCode);
        Assert.Contains(withMatch.PulseLines, l => l.StartsWith("Kadro:", StringComparison.Ordinal));
    }

    [Fact]
    public void TransferAttention_OnCalmDay_BeatsSquadFull_ButNotMatchReady()
    {
        var full = SquadCapacityDigest.Compose(25, 25, 25, Array.Empty<long>());
        var transfer = TransferDeskBriefing.Compose(
            windowOpen: true,
            "Açık",
            40,
            openNeedCount: 0,
            openExitNeedCount: 0,
            listedTargetCount: 0,
            activeProcessCount: 0,
            pendingOfferCount: 0,
            budgetAvailable: 1_000_000,
            budgetSpent: 0,
            squadFull: true,
            saleCandidatePlayerId: 2001);

        Assert.True(transfer.DemandsAttention);

        var calm = TodayPulseDigest.Compose(
            DecisionDeskDigest.Clear(),
            PreMatchBriefing.Clear(),
            PrepOk(),
            LeagueOk(),
            full,
            transfer);

        Assert.Equal(TodayPulseDigest.FocusTransfer, calm.PrimaryFocusCode);
        Assert.Contains("Satışa Çıkar", calm.Headline, StringComparison.Ordinal);
        Assert.Contains(calm.PulseLines, l => l.StartsWith("Transfer:", StringComparison.Ordinal));

        var withMatch = TodayPulseDigest.Compose(
            DecisionDeskDigest.Clear(),
            MatchReady(),
            PrepOk(),
            LeagueOk(),
            full,
            transfer);

        Assert.Equal(TodayPulseDigest.FocusMatch, withMatch.PrimaryFocusCode);
        Assert.Contains(withMatch.PulseLines, l => l.StartsWith("Transfer:", StringComparison.Ordinal));
    }

    [Fact]
    public void SeasonReady_BecomesPrimaryFocus_OnCalmDay_ButNotOverLockedMatch()
    {
        var seasonReady = TodayPulseDigest.Compose(
            DecisionDeskDigest.Clear(),
            PreMatchBriefing.Clear(),
            PrepOk(),
            LeagueOk(),
            seasonTransitionReady: true,
            seasonArchivePhase: false);

        Assert.Equal(TodayPulseDigest.FocusSeason, seasonReady.PrimaryFocusCode);
        Assert.Contains("Sezon bitti", seasonReady.Headline, StringComparison.Ordinal);
        Assert.Contains(seasonReady.PulseLines, l => l.StartsWith("Sezon:", StringComparison.Ordinal));

        var archive = TodayPulseDigest.Compose(
            DecisionDeskDigest.Clear(),
            PreMatchBriefing.Clear(),
            PrepOk(),
            LeagueOk(),
            seasonTransitionReady: true,
            seasonArchivePhase: true);

        Assert.Equal(TodayPulseDigest.FocusSeason, archive.PrimaryFocusCode);
        Assert.Contains("yeni sezona", archive.Headline, StringComparison.OrdinalIgnoreCase);

        var lockedMatch = TodayPulseDigest.Compose(
            DecisionDeskDigest.Clear(),
            PreMatchBriefing.Compose(
                new ManagedFixtureSelectionStatusReadModel(
                    1, 1, 1, 2, false, 10, "2026-08-15", IsApproved: false),
                "Rival",
                10),
            PrepOk(),
            LeagueOk(),
            seasonTransitionReady: true);

        Assert.Equal(TodayPulseDigest.FocusMatch, lockedMatch.PrimaryFocusCode);
    }

    [Fact]
    public void OpenWindowWithoutAttention_ShowsWindowRhythmLine()
    {
        var transfer = TransferDeskBriefing.Compose(
            windowOpen: true,
            windowStatusName: "Açık",
            windowClosesOnDayNumber: 40,
            openNeedCount: 0,
            openExitNeedCount: 0,
            listedTargetCount: 0,
            activeProcessCount: 0,
            pendingOfferCount: 0,
            budgetAvailable: null,
            budgetSpent: null,
            squadFull: false,
            saleCandidatePlayerId: null,
            currentDayNumber: 10);

        var pulse = TodayPulseDigest.Compose(
            DecisionDeskDigest.Clear(),
            PreMatchBriefing.Clear(),
            PrepOk(),
            LeagueOk(),
            transfer: transfer);

        Assert.False(transfer.DemandsAttention);
        Assert.Contains(
            pulse.PulseLines,
            l => l == "Transfer: Pencere açık — transfer masası çalışıyor.");
    }

    [Fact]
    public void ClosedWindowRecently_ShowsClosingRhythmLine()
    {
        var transfer = TransferDeskBriefing.Compose(
            windowOpen: false,
            windowStatusName: "Kapalı",
            windowClosesOnDayNumber: 38,
            openNeedCount: 0,
            openExitNeedCount: 0,
            listedTargetCount: 0,
            activeProcessCount: 0,
            pendingOfferCount: 0,
            budgetAvailable: null,
            budgetSpent: null,
            squadFull: false,
            saleCandidatePlayerId: null,
            currentDayNumber: 40);

        var pulse = TodayPulseDigest.Compose(
            DecisionDeskDigest.Clear(),
            PreMatchBriefing.Clear(),
            PrepOk(),
            LeagueOk(),
            transfer: transfer);

        Assert.Equal(
            "Transfer: Pencere kapandı — kadro bu haliyle ilerliyor.",
            pulse.PulseLines.FirstOrDefault(l => l.StartsWith("Transfer:", StringComparison.Ordinal)));
    }

    [Fact]
    public void PlayerExitSellFringe_BeatsCalmPrepDemand()
    {
        var prep = PreparationBriefing.Compose(
            new ClubTrainingSummaryReadModel(
                1,
                (int)Domain.TrainingPhysicalState.TrainingFocus.General,
                (int)Domain.TrainingPhysicalState.TrainingIntensity.Medium,
                (int)Domain.TrainingPhysicalState.RestApproach.Normal,
                null, null, null, 1, 30, 70, true, 1, 1,
                InjuredPlayerNames: ["Yorgun"]),
            new TacticPlanReadModel(1, "4-4-2", "Dengeli", 1),
            "±0",
            daysUntilNextMatch: 5);

        var transfer = TransferDeskBriefing.Compose(
            windowOpen: true,
            "Açık",
            windowClosesOnDayNumber: 90,
            openNeedCount: 1,
            openExitNeedCount: 1,
            listedTargetCount: 0,
            activeProcessCount: 0,
            pendingOfferCount: 0,
            budgetAvailable: null,
            budgetSpent: null,
            squadFull: false,
            saleCandidatePlayerId: 501,
            currentDayNumber: 40);

        Assert.True(prep.DemandsAttention);
        Assert.Equal(TransferNextStep.ReasonSellFringe, transfer.NextStep!.ReasonCode);

        var pulse = TodayPulseDigest.Compose(
            DecisionDeskDigest.Clear(),
            PreMatchBriefing.Clear(),
            prep,
            LeagueOk(),
            transfer: transfer);

        Assert.Equal(TodayPulseDigest.FocusTransfer, pulse.PrimaryFocusCode);
        Assert.Contains("#501", pulse.Headline, StringComparison.Ordinal);
        Assert.Contains("Satışa Çıkar", pulse.Headline, StringComparison.Ordinal);
    }

    [Fact]
    public void SittingOutDesk_SurfacesCausalityOnPulseLine()
    {
        var desk = new DecisionDeskDigest(
            true,
            true,
            "Masada (zorunlu)",
            "Yedek kaldı — forma süresi istiyor.",
            "destek",
            1,
            "Forma süresi talebi",
            1,
            "Son 3 maçta yedek/kadro dışı — forma istiyor");

        var pulse = TodayPulseDigest.Compose(
            desk,
            MatchReady(),
            PrepOk(),
            LeagueOk());

        Assert.Equal(TodayPulseDigest.FocusDesk, pulse.PrimaryFocusCode);
        Assert.Contains(
            pulse.PulseLines,
            l => l.Contains("yedek/kadro dışı", StringComparison.OrdinalIgnoreCase));
    }

    private static DecisionDeskDigest Desk(bool hard, bool open, string headline) =>
        open
            ? new DecisionDeskDigest(
                true,
                hard,
                hard ? "Masada (zorunlu)" : "Masada",
                headline,
                "destek",
                1,
                "Kritik basın sorusu",
                1)
            : DecisionDeskDigest.Clear();

    private static PreMatchBriefing MatchReady() =>
        PreMatchBriefing.Compose(
            new ManagedFixtureSelectionStatusReadModel(
                1, 1, 1, 2, true, 10, "2026-08-15", IsApproved: true),
            "Rival",
            10);

    private static PreparationBriefing PrepOk() =>
        PreparationBriefing.Compose(
            new ClubTrainingSummaryReadModel(
                1,
                (int)Domain.TrainingPhysicalState.TrainingFocus.General,
                (int)Domain.TrainingPhysicalState.TrainingIntensity.Medium,
                (int)Domain.TrainingPhysicalState.RestApproach.Normal,
                null, null, null, 1, 30, 70, true, 0, 0),
            new TacticPlanReadModel(1, "4-4-2", "Dengeli", 1),
            "±0",
            daysUntilNextMatch: 4);

    private static LeagueWorldBriefing LeagueOk() =>
        LeagueWorldBriefing.Compose(
            "Active",
            8,
            30,
            8,
            managedRank: 4,
            managedPoints: 12,
            managedPlayed: 8,
            managedGoalDifference: 1,
            managedClubName: "Home",
            leaderClubName: "Leaders",
            leaderPoints: 18,
            nextMatchLine: null);
}
