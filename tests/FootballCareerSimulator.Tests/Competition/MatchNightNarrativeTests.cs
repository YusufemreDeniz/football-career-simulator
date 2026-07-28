using FootballCareerSimulator.Application.Competition.Queries;

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
    public void Compose_ManagedMatch_UsesMaçGecesiBrand()
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
            otherScorelines: ["C 0-0 D"]);

        Assert.Equal("Maç Gecesi", narrative.BrandTitle);
        Assert.Equal("İnce bir galibiyet.", narrative.OutcomeTone);
        Assert.Equal("Gün 12 · taktik +1", narrative.SupportingLine);
        Assert.Single(narrative.BeatLines);
        Assert.Single(narrative.AfterWhistleLines);
        Assert.Single(narrative.OtherScorelines);
    }
}
