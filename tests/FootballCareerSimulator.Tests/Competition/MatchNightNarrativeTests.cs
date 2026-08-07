using FootballCareerSimulator.Application.Competition.Queries;
using FootballCareerSimulator.Application.TeamPreparation.Queries;
using FootballCareerSimulator.Application.TeamPreparation.Services;
using FootballCareerSimulator.Domain.WorldCalendar;
using FootballCareerSimulator.Simulation.TrainingPhysicalState;

namespace FootballCareerSimulator.Tests.Competition;

public sealed class MatchNightNarrativeTests
{
    [Fact]
    public void Tone_NarrowWin_IsThinVictory()
    {
        Assert.Equal(
            "İnce bir galibiyet.",
            MatchNightNarrative.ToneForManaged(2, 1, managedIsHome: true, Array.Empty<string>()));
    }

    [Fact]
    public void Tone_BlowoutLossWithPress_MentionsPress()
    {
        Assert.Equal(
            "Ağır yenilgi — basın kapıda.",
            MatchNightNarrative.ToneForManaged(
                0,
                4,
                managedIsHome: true,
                ["Basın sorusu açıldı."]));
    }

    [Fact]
    public void Tone_PromiseRiskLoss_CarriesKickoffTension()
    {
        Assert.Equal(
            "Söz gerilimiyle girdin; gece ağır bitti.",
            MatchNightNarrative.ToneForManaged(
                0,
                1,
                managedIsHome: true,
                Array.Empty<string>(),
                enteredWithPromiseRisk: true));
    }

    [Fact]
    public void Tone_PromiseRiskWin_CelebratesDespiteTension()
    {
        Assert.Equal(
            "Söz gerilimine rağmen kazandın.",
            MatchNightNarrative.ToneForManaged(
                2,
                0,
                managedIsHome: true,
                Array.Empty<string>(),
                enteredWithPromiseRisk: true));
    }

    [Fact]
    public void Compose_ManagedMatch_UsesMaçGecesiBrand_AndKickoffBridge()
    {
        var narrative = MatchNightNarrative.Compose(
            "A 1-0 B",
            1,
            0,
            managedIsHome: true,
            hasManagedMatch: true,
            tacticNote: "taktik +1",
            dayNumber: GameDate.FromCalendarDate(2026, 8, 15).DayNumber,
            beatLines: ["12' Ev gol · X"],
            afterWhistleLines: ["Yönetim güveni +2 → 60 (Stabil)"],
            otherScorelines: ["C 0-0 D"],
            kickoffLines: ["Ev vs B · bugün", "Maça söz riskiyle girdin."],
            enteredWithPromiseRisk: true);

        Assert.Equal("Maç Gecesi", narrative.BrandTitle);
        Assert.Equal("Söz gerilimine rağmen kazandın.", narrative.OutcomeTone);
        Assert.Equal("Tarih 2026-08-15 · taktik +1", narrative.SupportingLine);
        Assert.Single(narrative.BeatLines);
        Assert.Single(narrative.AfterWhistleLines);
        Assert.Single(narrative.OtherScorelines);
        Assert.Equal(2, narrative.KickoffLines.Count);
        Assert.Contains("söz riski", narrative.KickoffLines[1], StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void PreferKickoffBridgeLines_KeepsHalfTimeDecisionAndSub()
    {
        var preferred = MatchNightNarrative.PreferKickoffBridgeLines(
            [
                "Ev vs B · bugün",
                "Maça söz riskiyle girdin.",
                "Taktik: 4-3-3",
                "XI yorgunluk 40 · fitness 70",
                "Sakat: Ali",
                "Devre arası: A 0-1 B",
                "Devre arasında hücuma geçtin.",
                "Devre arasında Ali Yılmaz↔Can Demir.",
            ],
            maxLines: 6);

        Assert.Equal(6, preferred.Count);
        Assert.Equal("Ev vs B · bugün", preferred[0]);
        Assert.Contains(preferred, l => l.Contains("hücuma", StringComparison.Ordinal));
        Assert.Contains(preferred, l => l.Contains('↔'));
        Assert.Contains(preferred, l => l.StartsWith("Devre arası:", StringComparison.Ordinal));
    }

    [Fact]
    public void Compose_CarriesLineupBridgeForResultScreen()
    {
        var names = Enumerable.Range(0, 25).Select(i => $"P{i} N{i}").ToArray();
        var strip = MatchDayLineupStrip.Compose(
            true,
            true,
            Enumerable.Range(2, 11).ToArray(),
            [new MvpAvailabilityAwareSelection.AvailabilityAutoSwap(0, 11)],
            names);

        var narrative = MatchNightNarrative.Compose(
            "A 2-1 B",
            2,
            1,
            managedIsHome: true,
            hasManagedMatch: true,
            tacticNote: null,
            dayNumber: 10,
            beatLines: [],
            afterWhistleLines: [],
            otherScorelines: [],
            kickoffLines: ["Ev vs B · bugün"],
            lineupBridge: strip);

        Assert.NotNull(narrative.LineupBridge);
        Assert.Equal(11, narrative.LineupBridge!.StartingXi.Count);
        Assert.Contains("Sahaya bu XI ile çıktın", narrative.LineupBridge.ResultBridgeCaption, StringComparison.Ordinal);
        Assert.Equal(1, narrative.ManagedGoalMargin);
    }

    [Fact]
    public void Compose_ManagedMatch_CarriesStadiumAtmosphere()
    {
        var atmosphere = StadiumAtmosphereDigest.Compose(
            isHome: true,
            managedRank: 1,
            clubCount: 10);

        var narrative = MatchNightNarrative.Compose(
            "A 2-1 B",
            2,
            1,
            managedIsHome: true,
            hasManagedMatch: true,
            tacticNote: null,
            dayNumber: 10,
            beatLines: [],
            afterWhistleLines: [],
            otherScorelines: [],
            atmosphere: atmosphere);

        Assert.Same(atmosphere, narrative.Atmosphere);
    }

    [Fact]
    public void Compose_UnmanagedMatch_DropsStadiumAtmosphere()
    {
        var atmosphere = StadiumAtmosphereDigest.Compose(
            isHome: true,
            managedRank: 1,
            clubCount: 10);

        var narrative = MatchNightNarrative.Compose(
            "A 2-1 B",
            2,
            1,
            managedIsHome: true,
            hasManagedMatch: false,
            tacticNote: null,
            dayNumber: 10,
            beatLines: [],
            afterWhistleLines: [],
            otherScorelines: [],
            atmosphere: atmosphere);

        Assert.Null(narrative.Atmosphere);
    }

    [Fact]
    public void Briefing_ToKickoffBridgeLines_KeepsFixtureAndRisk()
    {
        var tension = new PreMatchPromiseTensionReadModel(
            1,
            1,
            true,
            true,
            PreMatchPromiseTensionQueryService.ToneAtRisk,
            "risk",
            [
                new PreMatchPromiseTensionLine(
                    1,
                    5,
                    12,
                    "İlk 11",
                    PreMatchPromiseTensionQueryService.PlacementBench,
                    "Oyuncu#5 YEDEKTE — söz risk altında."),
            ]);

        var briefing = PreMatchBriefing.Compose(
            new ManagedFixtureSelectionStatusReadModel(
                10,
                1,
                1,
                2,
                IsHome: true,
                ScheduledDayNumber: 10,
                ScheduledIsoDate: "2026-08-15",
                IsApproved: true),
            "Rival",
            currentDayNumber: 10,
            formationName: "4-3-3",
            approachName: "Dengeli",
            averageFatigue: 35,
            averageFitness: 70,
            tension: tension);

        var bridge = briefing.ToKickoffBridgeLines();
        Assert.Contains(bridge, l => l.Contains("Ev vs Rival", StringComparison.Ordinal));
        Assert.Contains(bridge, l => l.Contains("söz riskiyle", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(bridge, l => l.StartsWith("Taktik:", StringComparison.Ordinal));
        Assert.True(bridge.Count <= 4);
    }

    [Fact]
    public void Compose_WithManyAfterWhistleLines_KeepsCriticalOnes()
    {
        var narrative = MatchNightNarrative.Compose(
            "A 0-3 B",
            0,
            3,
            managedIsHome: true,
            hasManagedMatch: true,
            tacticNote: null,
            dayNumber: 20,
            beatLines: ["12' Ev gol · Kaya"],
            afterWhistleLines:
            [
                "Devre arasında hücuma geçtin.",
                "Devre arasında Ali Yılmaz↔Can Demir.",
                "Yönetim güveni -5 → 40 (İncelemede)",
                "Sakatlık: Tolga Kurt",
                "Basın sorusu açıldı.",
            ],
            otherScorelines: [],
            kickoffLines: ["Ev vs B · bugün"]);

        Assert.Equal(3, narrative.AfterWhistleLines.Count);
        Assert.Contains(narrative.AfterWhistleLines, l => l.Contains("Yönetim güveni", StringComparison.Ordinal));
        Assert.Contains(narrative.AfterWhistleLines, l => l.Contains("basın", StringComparison.OrdinalIgnoreCase));
        Assert.Equal(
            "Ağır yenilgi — basın kapıda.",
            narrative.OutcomeTone);
    }

    [Fact]
    public void Compose_WithCrowdedAfterWhistle_KeepsMatchupPlanOutcome()
    {
        var planOutcome = $"{MatchupPlanOutcomeDigest.Brand} · Seçim: 4-3-3 · Hücum";
        var narrative = MatchNightNarrative.Compose(
            "A 2-1 B",
            2,
            1,
            managedIsHome: true,
            hasManagedMatch: true,
            tacticNote: null,
            dayNumber: 20,
            beatLines: [],
            afterWhistleLines:
            [
                "Devre arasında hücuma geçtin.",
                "Sakatlık: Tolga Kurt",
                "Yönetim güveni +2 → 60 (Stabil)",
                planOutcome,
            ],
            otherScorelines: []);

        Assert.Contains(planOutcome, narrative.AfterWhistleLines);
        Assert.Equal(3, narrative.AfterWhistleLines.Count);
    }

    [Fact]
    public void ComposeHalfSegmentedBeats_BothHalvesWithExtras_PlacesExtrasAtSecondHalfStart()
    {
        var beats = MatchNightNarrative.ComposeHalfSegmentedBeats(
            ["12' Ev gol · Kaya"],
            ["58' Dep gol · Ali", "71' Ev kırmızı · Can"],
            ["46' Karar · Hücuma geçtin", "46' Değişiklik · Ali↔Can"]);

        Assert.Equal(
            [
                "1. Yarı",
                "12' Ev gol · Kaya",
                "2. Yarı",
                "46' Karar · Hücuma geçtin",
                "46' Değişiklik · Ali↔Can",
                "58' Dep gol · Ali",
                "71' Ev kırmızı · Can",
            ],
            beats);
    }

    [Fact]
    public void ComposeHalfSegmentedBeats_FirstHalfOnly_OmitsSecondHalfHeader()
    {
        var beats = MatchNightNarrative.ComposeHalfSegmentedBeats(
            ["12' Ev gol · Kaya"],
            []);

        Assert.Equal(["1. Yarı", "12' Ev gol · Kaya"], beats);
    }

    [Fact]
    public void ComposeHalfSegmentedBeats_SecondHalfOnly_OmitsFirstHalfHeader()
    {
        var beats = MatchNightNarrative.ComposeHalfSegmentedBeats(
            [],
            ["58' Dep gol · Ali"]);

        Assert.Equal(["2. Yarı", "58' Dep gol · Ali"], beats);
    }

    [Fact]
    public void ComposeHalfSegmentedBeats_ExtrasOnly_ShowsSecondHalfHeader()
    {
        var beats = MatchNightNarrative.ComposeHalfSegmentedBeats(
            [],
            [],
            ["46' Karar · Hücuma geçtin"]);

        Assert.Equal(["2. Yarı", "46' Karar · Hücuma geçtin"], beats);
    }

    [Fact]
    public void ComposeHalfSegmentedBeats_EmptyEverything_ReturnsEmpty()
    {
        var beats = MatchNightNarrative.ComposeHalfSegmentedBeats([], []);

        Assert.Empty(beats);
    }
}
