using FootballCareerSimulator.Application.TeamPreparation.Queries;
using FootballCareerSimulator.Simulation.TrainingPhysicalState;

namespace FootballCareerSimulator.Tests.TeamPreparation;

public sealed class SelectionAutoSwapWarningTests
{
    [Fact]
    public void FormatBeatLines_NamesInjuredAndReplacement()
    {
        var lines = SelectionAutoSwapWarning.FormatBeatLines(
            [new MvpAvailabilityAwareSelection.AvailabilityAutoSwap(0, 11)],
            ["Ali Yılmaz", "B", "C", "D", "E", "F", "G", "H", "I", "J", "K", "Can Demir"]);

        Assert.Single(lines);
        Assert.Equal("Sakat XI'de: Ali Yılmaz — yerine Can Demir.", lines[0]);
    }

    [Fact]
    public void FormatSubstitution_UsesPlayerNames()
    {
        Assert.Equal(
            "Ali Yılmaz çıktı · Can Demir XI'ye girdi",
            SelectionAutoSwapWarning.FormatSubstitution("Ali Yılmaz", "Can Demir"));

        Assert.Equal(
            "Ali Yılmaz çıktı · Can Demir XI'ye girdi",
            SelectionAutoSwapWarning.FormatSubstitution(
                0,
                11,
                ["Ali Yılmaz", "B", "C", "D", "E", "F", "G", "H", "I", "J", "K", "Can Demir"]));
    }

    [Fact]
    public void FormatHalfTimeBridge_NamesSwapForResultKickoff()
    {
        Assert.Equal(
            "Devre arasında Ali Yılmaz↔Can Demir.",
            SelectionAutoSwapWarning.FormatHalfTimeBridge("Ali Yılmaz", "Can Demir"));
    }

    [Fact]
    public void FormatToastSuffix_CompressesPairs()
    {
        var suffix = SelectionAutoSwapWarning.FormatToastSuffix(
            [
                new MvpAvailabilityAwareSelection.AvailabilityAutoSwap(0, 11),
                new MvpAvailabilityAwareSelection.AvailabilityAutoSwap(1, 12),
            ],
            ["Ali Yılmaz", "Efe Kaya", "C", "D", "E", "F", "G", "H", "I", "J", "K", "Can Demir", "Mert Koç"]);

        Assert.Equal("sakatlar dışarı (Ali Yılmaz→Can Demir, Efe Kaya→Mert Koç)", suffix);
    }

    [Fact]
    public void PreMatchBriefing_SurfacesAutoSwapBeforeApprove()
    {
        var briefing = PreMatchBriefing.Compose(
            new ManagedFixtureSelectionStatusReadModel(
                1, 1, 1, 2, true, 10, "2026-08-15", IsApproved: false),
            "Rival",
            10,
            injuredSlotCount: 1,
            injuredPlayerNames: ["Ali Yılmaz"],
            autoSwapWarningLines: ["Sakat XI'de: Ali Yılmaz — yerine Can Demir."]);

        Assert.Contains("yedekler gelir", briefing.Headline, StringComparison.OrdinalIgnoreCase);
        Assert.Contains(briefing.BeatLines, b => b.StartsWith("Sakat XI'de:", StringComparison.Ordinal));
        Assert.Contains(briefing.BeatLines, b => b.Contains("otomatik dışarı", StringComparison.OrdinalIgnoreCase));

        var bridge = briefing.ToKickoffBridgeLines();
        Assert.Contains(bridge, l => l.StartsWith("Sakat XI'de:", StringComparison.Ordinal));
    }
}
