using FootballCareerSimulator.Application.CareerHub.Queries;
using FootballCareerSimulator.Application.Competition.Queries;
using FootballCareerSimulator.Application.Interaction.Queries;
using FootballCareerSimulator.Application.TeamPreparation.Queries;
using FootballCareerSimulator.Application.TrainingPhysicalState.Queries;
using FootballCareerSimulator.Application.Transfer.Queries;
using FootballCareerSimulator.Domain.TeamPreparation;

namespace FootballCareerSimulator.Tests.Competition;

public sealed class PostMatchOfficeDigestTests
{
    [Fact]
    public void Quiet_WhenNoManagedNarrative()
    {
        var digest = PostMatchOfficeDigest.Compose(
            narrative: null,
            DecisionDeskDigest.Clear(),
            hasManagedMatch: false);

        Assert.Equal(PostMatchOfficeDigest.Brand, digest.BrandTitle);
        Assert.Contains("Ofis sakin", digest.Headline, StringComparison.Ordinal);
        Assert.Contains("Öneri:", digest.ToDisplayText(), StringComparison.Ordinal);
    }

    [Fact]
    public void FromTodayPulse_MirrorsPrepFocus()
    {
        var prep = PreparationBriefing.Compose(
            new ClubTrainingSummaryReadModel(
                1, null, null, null, null, null, null, null, null, null,
                HasPlan: false, 0, 0),
            new TacticPlanReadModel(1, "4-4-2", "Dengeli", 1),
            "±0",
            daysUntilNextMatch: 4);
        var pulse = TodayPulseDigest.Compose(
            DecisionDeskDigest.Clear(),
            PreMatchBriefing.Clear(),
            prep,
            LeagueOk());

        var digest = PostMatchOfficeDigest.FromTodayPulse(pulse);

        Assert.Equal(TodayPulseDigest.FocusPrep, digest.NextFocusCode);
        Assert.Contains("Nabız konuşuyor", digest.Headline, StringComparison.Ordinal);
        Assert.Contains(digest.BeatLines, b => b.StartsWith("Sıradaki:", StringComparison.Ordinal));
        Assert.Contains("birincil düğme", digest.AdviceLine, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void PressAndHardDesk_LeadsCrisisHeadline()
    {
        var narrative = MatchNightNarrative.Compose(
            "A 0-2 B",
            0,
            2,
            managedIsHome: true,
            hasManagedMatch: true,
            tacticNote: null,
            dayNumber: 20,
            beatLines: Array.Empty<string>(),
            afterWhistleLines: ["Yönetim güveni -5 → 40 (İncelemede)", "Basın sorusu açıldı."],
            otherScorelines: Array.Empty<string>(),
            kickoffLines: ["Ev vs B · bugün", "Maça söz riskiyle girdin."],
            enteredWithPromiseRisk: true);

        var desk = DecisionDeskDigest.Compose(
            new PendingDecisionsReadModel(
                1,
                [
                    new DecisionRequestLineReadModel(
                        7,
                        "Kritik basın sorusu",
                        42,
                        1,
                        "Open",
                        IsHardBlocker: true,
                        20,
                        22,
                        null),
                ]),
            currentDayNumber: 20);

        var digest = PostMatchOfficeDigest.Compose(narrative, desk, hasManagedMatch: true);

        Assert.Equal("Ofiste kriz — cevap vermeden ilerleyemezsin.", digest.Headline);
        Assert.Contains(digest.BeatLines, b => b.Contains("Basın sorusu", StringComparison.Ordinal));
        Assert.Contains(digest.BeatLines, b => b.Contains("Masada zorunlu", StringComparison.Ordinal));
        Assert.Contains(digest.BeatLines, b => b.Contains("söz gerilimi", StringComparison.OrdinalIgnoreCase));

        var text = digest.ToStatusMessage();
        Assert.Contains("Ofiste", text, StringComparison.Ordinal);
        Assert.Contains("· ", text, StringComparison.Ordinal);
        Assert.Contains("Öneri:", text, StringComparison.Ordinal);
    }

    [Fact]
    public void ComfortableWin_RelaxesOffice()
    {
        var narrative = MatchNightNarrative.Compose(
            "A 3-0 B",
            3,
            0,
            managedIsHome: true,
            hasManagedMatch: true,
            tacticNote: "taktik +1",
            dayNumber: 5,
            beatLines: Array.Empty<string>(),
            afterWhistleLines: ["Yönetim güveni +3 → 70 (Güvenli)"],
            otherScorelines: Array.Empty<string>());

        var digest = PostMatchOfficeDigest.Compose(
            narrative,
            DecisionDeskDigest.Clear(),
            hasManagedMatch: true);

        Assert.Equal("Ofis rahatladı — gece senindi.", digest.Headline);
        Assert.Contains(digest.BeatLines, b => b.Contains("Masada yeni zorunlu dosya yok", StringComparison.Ordinal));
    }

    [Fact]
    public void WinWithTransferPulse_PointsNextStep()
    {
        var narrative = MatchNightNarrative.Compose(
            "A 2-0 B",
            2,
            0,
            managedIsHome: true,
            hasManagedMatch: true,
            tacticNote: null,
            dayNumber: 8,
            beatLines: Array.Empty<string>(),
            afterWhistleLines: ["Yönetim güveni +2 → 65 (Güvenli)"],
            otherScorelines: Array.Empty<string>());

        var squad = SquadCapacityDigest.Compose(
            ClubSquad.MaxMembers,
            ClubSquad.MaxMembers,
            ClubSquad.MaxMembers,
            Array.Empty<long>());
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
        var pulse = TodayPulseDigest.Compose(
            DecisionDeskDigest.Clear(),
            PreMatchBriefing.Clear(),
            PrepOk(),
            LeagueOk(),
            squad,
            transfer);

        var digest = PostMatchOfficeDigest.Compose(
            narrative,
            DecisionDeskDigest.Clear(),
            hasManagedMatch: true,
            pulse);

        Assert.Equal(TodayPulseDigest.FocusTransfer, digest.NextFocusCode);
        Assert.Contains("Transfer Masası", digest.Headline, StringComparison.Ordinal);
        Assert.Contains(digest.BeatLines, b => b.StartsWith("Sıradaki:", StringComparison.Ordinal));
        Assert.Contains("Transfer Masası", digest.AdviceLine, StringComparison.Ordinal);
        Assert.Contains("Öneri:", digest.ToDisplayText(), StringComparison.Ordinal);
    }

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
