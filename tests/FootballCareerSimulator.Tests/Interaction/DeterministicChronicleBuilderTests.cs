using FootballCareerSimulator.Application.Interaction.Services;
using FootballCareerSimulator.Domain.Interaction;
using FootballCareerSimulator.Domain.WorldCalendar;

namespace FootballCareerSimulator.Tests.Interaction;

public sealed class DeterministicChronicleBuilderTests
{
    private static readonly GameDate DerbyDay = GameDate.FromCalendarDate(2026, 11, 20);
    private static readonly GameDate FinalDay = GameDate.FromCalendarDate(2027, 5, 15);

    [Fact]
    public void CompileSeasonChronicle_ChampionArc_IncludesTitleAndTopMilestones()
    {
        var compiler = new CareerChronicleCompiler();

        compiler.RecordFact(2026, new NarrativeFactRecord(
            factId: "DerbyWin_1",
            milestoneType: "DerbyWin",
            importanceScore: 85,
            involvedActorNames: new[] { "Galatasaray", "Fenerbahçe" },
            occurredOn: DerbyDay,
            narrativeSnippet: "Tarihi derbi zaferiyle şampiyonluk meşalesi yakıldı."));

        compiler.RecordFact(2026, new NarrativeFactRecord(
            factId: "TitleWin_1",
            milestoneType: "TitleWon",
            importanceScore: 100,
            involvedActorNames: new[] { "Galatasaray" },
            occurredOn: FinalDay,
            narrativeSnippet: "Son haftada gelen şampiyonlukla stadyum karnaval yerine döndü."));

        var chronicle = compiler.CompileSeasonChronicle(
            seasonYear: 2026,
            clubName: "Galatasaray",
            finalLeaguePosition: 1,
            wonTitle: true);

        Assert.Equal(2026, chronicle.SeasonYear);
        Assert.True(chronicle.WonTitle);
        Assert.Equal(1, chronicle.FinalLeaguePosition);
        Assert.Contains("Şampiyonluk Masalı", chronicle.SeasonTitle);
        Assert.Contains("Tarihi derbi zaferiyle", chronicle.DramaticArcNarrative);
        Assert.Contains("şampiyonlukla stadyum karnaval yerine döndü", chronicle.DramaticArcNarrative);
        Assert.Equal(2, chronicle.KeyMilestones.Count);
        // Importance 100 comes before 85
        Assert.Equal("TitleWin_1", chronicle.KeyMilestones[0].FactId);
    }

    [Fact]
    public void CompileSeasonChronicle_MidtableArc_ProducesProperVerdict()
    {
        var compiler = new CareerChronicleCompiler();

        var chronicle = compiler.CompileSeasonChronicle(
            seasonYear: 2026,
            clubName: "Kasımpaşa",
            finalLeaguePosition: 8,
            wonTitle: false);

        Assert.False(chronicle.WonTitle);
        Assert.Equal(8, chronicle.FinalLeaguePosition);
        Assert.Contains("Mücadele Sezonu", chronicle.SeasonTitle);
        Assert.Contains("Zorluklarla dolu", chronicle.SeasonVerdict);
    }

    [Fact]
    public void CompileSeasonChronicle_Determinism_ProducesIdenticalOutputs()
    {
        var compiler1 = new CareerChronicleCompiler();
        var compiler2 = new CareerChronicleCompiler();

        var fact = new NarrativeFactRecord(
            "Fact_1",
            "YouthBreakthrough",
            75,
            new[] { "Genç Forvet" },
            DerbyDay,
            "Altyapıdan çıkan genç oyuncu yıldızlaştı.");

        compiler1.RecordFact(2026, fact);
        compiler2.RecordFact(2026, fact);

        var chronicle1 = compiler1.CompileSeasonChronicle(2026, "Beşiktaş", 3, false);
        var chronicle2 = compiler2.CompileSeasonChronicle(2026, "Beşiktaş", 3, false);

        Assert.Equal(chronicle1.SeasonTitle, chronicle2.SeasonTitle);
        Assert.Equal(chronicle1.DramaticArcNarrative, chronicle2.DramaticArcNarrative);
        Assert.Equal(chronicle1.SeasonVerdict, chronicle2.SeasonVerdict);
    }

    [Fact]
    public void NarrativeFactRecord_Validation_ThrowsOnEmptyFields()
    {
        Assert.Throws<InteractionInvariantViolationException>(() =>
            new NarrativeFactRecord(
                factId: "",
                milestoneType: "Milestone",
                importanceScore: 50,
                involvedActorNames: Array.Empty<string>(),
                occurredOn: DerbyDay,
                narrativeSnippet: "Snippet"));
    }
}
