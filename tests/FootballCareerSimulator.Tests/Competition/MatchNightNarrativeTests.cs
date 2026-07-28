using FootballCareerSimulator.Application.Competition.Queries;
using FootballCareerSimulator.Application.TeamPreparation.Queries;
using FootballCareerSimulator.Application.TeamPreparation.Services;

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
            dayNumber: 12,
            beatLines: ["12' Ev gol · X"],
            afterWhistleLines: ["Yönetim güveni +2 → 60 (Stabil)"],
            otherScorelines: ["C 0-0 D"],
            kickoffLines: ["Ev vs B · bugün", "Maça söz riskiyle girdin."],
            enteredWithPromiseRisk: true);

        Assert.Equal("Maç Gecesi", narrative.BrandTitle);
        Assert.Equal("Söz gerilimine rağmen kazandın.", narrative.OutcomeTone);
        Assert.Equal("Gün 12 · taktik +1", narrative.SupportingLine);
        Assert.Single(narrative.BeatLines);
        Assert.Single(narrative.AfterWhistleLines);
        Assert.Single(narrative.OtherScorelines);
        Assert.Equal(2, narrative.KickoffLines.Count);
        Assert.Contains("söz riski", narrative.KickoffLines[1], StringComparison.OrdinalIgnoreCase);
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
}
