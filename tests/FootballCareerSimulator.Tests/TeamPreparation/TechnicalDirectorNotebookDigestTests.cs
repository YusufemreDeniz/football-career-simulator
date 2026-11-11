using FootballCareerSimulator.Application.CareerHub.Queries;
using FootballCareerSimulator.Application.Competition.Queries;
using FootballCareerSimulator.Application.TeamPreparation.Queries;

namespace FootballCareerSimulator.Tests.TeamPreparation;

public sealed class TechnicalDirectorNotebookDigestTests
{
    [Fact]
    public void EmptyHistory_DoesNotInventALesson()
    {
        var digest = TechnicalDirectorNotebookDigest.Compose([]);

        Assert.Equal(TechnicalDirectorNotebookDigest.Brand, digest.BrandTitle);
        Assert.False(digest.HasHistory);
        Assert.Empty(digest.BeatLines);
    }

    [Fact]
    public void Compose_KeepsLatestThreeAndShowsTacticThreatAndOutcome()
    {
        var digest = TechnicalDirectorNotebookDigest.Compose(
        [
            Entry(10, "A", OpponentThreatKind.Neutral, MatchupPlanSignal.Balance, MatchupPlanOutcomeSignal.Neutral),
            Entry(11, "B", OpponentThreatKind.WinningStreak, MatchupPlanSignal.Risk, MatchupPlanOutcomeSignal.Warning),
            Entry(12, "C", OpponentThreatKind.ProductiveAttack, MatchupPlanSignal.Risk, MatchupPlanOutcomeSignal.Positive),
            Entry(13, "D", OpponentThreatKind.DefensiveResistance, MatchupPlanSignal.Opportunity, MatchupPlanOutcomeSignal.Positive),
        ]);

        Assert.True(digest.HasHistory);
        Assert.Equal("Son 3 maçtan dersler", digest.Headline);
        Assert.Equal(3, digest.BeatLines.Count);
        Assert.Contains("Gün 13, D", digest.BeatLines[0], StringComparison.Ordinal);
        Assert.Contains("savunma direnci", digest.BeatLines[0], StringComparison.Ordinal);
        Assert.Contains("Fırsat→Olumlu", digest.BeatLines[0], StringComparison.Ordinal);
        Assert.Contains("Gün 11, B", digest.BeatLines[2], StringComparison.Ordinal);
        Assert.Contains("Risk→Uyarı", digest.BeatLines[2], StringComparison.Ordinal);
        Assert.DoesNotContain(digest.BeatLines, line => line.Contains("Gün 10", StringComparison.Ordinal));
    }

    [Fact]
    public void HubState_NormalizesDuplicateHistoryAndKeepsLatestThree()
    {
        var duplicate = Entry(13, "D", OpponentThreatKind.Neutral, MatchupPlanSignal.Balance, MatchupPlanOutcomeSignal.Neutral);
        var state = HubNarrativeUiState.Compose(
            null,
            false,
            null,
            null,
            [
                Entry(10, "A", OpponentThreatKind.Neutral, MatchupPlanSignal.Balance, MatchupPlanOutcomeSignal.Neutral),
                Entry(11, "B", OpponentThreatKind.Neutral, MatchupPlanSignal.Balance, MatchupPlanOutcomeSignal.Neutral),
                Entry(12, "C", OpponentThreatKind.Neutral, MatchupPlanSignal.Balance, MatchupPlanOutcomeSignal.Neutral),
                duplicate,
                duplicate,
            ]);

        Assert.Equal(3, state.MatchupPlanHistory.Count);
        Assert.Equal([11, 12, 13], state.MatchupPlanHistory.Select(entry => entry.DayNumber));
        Assert.False(state.IsEmpty);
    }

    [Theory]
    [InlineData(0, "Rakip", "Seçim", "Karar", "dayNumber")]
    [InlineData(1, " ", "Seçim", "Karar", "opponentName")]
    [InlineData(1, "Rakip", " ", "Karar", "selectionLine")]
    [InlineData(1, "Rakip", "Seçim", " ", "verdictLine")]
    public void Entry_RejectsInvalidIdentityFields(
        int dayNumber,
        string opponent,
        string selection,
        string verdict,
        string parameterName)
    {
        var exception = Assert.ThrowsAny<ArgumentException>(() =>
            MatchupPlanNotebookEntry.Compose(
                dayNumber,
                opponent,
                selection,
                OpponentThreatKind.Neutral,
                MatchupPlanSignal.Balance,
                MatchupPlanOutcomeSignal.Neutral,
                verdict));

        Assert.Equal(parameterName, exception.ParamName);
    }

    private static MatchupPlanNotebookEntry Entry(
        int day,
        string opponent,
        OpponentThreatKind threat,
        MatchupPlanSignal plan,
        MatchupPlanOutcomeSignal outcome) =>
        MatchupPlanNotebookEntry.Compose(
            day,
            opponent,
            "Seçim: 4-3-3 · Hücum",
            threat,
            plan,
            outcome,
            "Maç sonu dersi.");
}
