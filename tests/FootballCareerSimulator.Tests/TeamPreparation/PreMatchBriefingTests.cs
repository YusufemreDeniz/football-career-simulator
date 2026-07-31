using FootballCareerSimulator.Application.TeamPreparation.Queries;
using FootballCareerSimulator.Application.TeamPreparation.Services;

namespace FootballCareerSimulator.Tests.TeamPreparation;

public sealed class PreMatchBriefingTests
{
    [Fact]
    public void Clear_WhenNoPendingFixture()
    {
        var briefing = PreMatchBriefing.Compose(
            pending: null,
            opponentName: "—",
            currentDayNumber: 10);

        Assert.False(briefing.HasMatch);
        Assert.Equal(PreMatchBriefing.Brand, briefing.BrandTitle);
        Assert.Contains("vadesi gelmiş maç yok", briefing.Headline, StringComparison.Ordinal);
        Assert.Contains("Sıradaki Maç", briefing.ToDisplayText(), StringComparison.Ordinal);
    }

    [Fact]
    public void Unapproved_AsksForSelectionFirst()
    {
        var pending = Fixture(approved: false, scheduledDay: 12);
        var briefing = PreMatchBriefing.Compose(
            pending,
            opponentName: "Rival FC",
            currentDayNumber: 10,
            formationName: "4-3-3",
            approachName: "Dengeli");

        Assert.True(briefing.HasMatch);
        Assert.False(briefing.IsReadyToKickOff);
        Assert.Equal("Henüz hazır değilsin — önce kadroyu onayla.", briefing.Headline);
        Assert.Contains("Ev vs Rival FC", briefing.FixtureLine, StringComparison.Ordinal);
        Assert.Contains("2 gün sonra", briefing.FixtureLine, StringComparison.Ordinal);
        Assert.Contains(briefing.BeatLines, b => b.Contains("Kadro onayı bekliyor", StringComparison.Ordinal));
        Assert.Contains(briefing.BeatLines, b => b.Contains("4-3-3", StringComparison.Ordinal));
    }

    [Fact]
    public void ApprovedWithPromiseRisk_WarnsBeforeKickOff()
    {
        var tension = new PreMatchPromiseTensionReadModel(
            FixtureId: 1,
            ClubId: 1,
            SelectionApproved: true,
            HasTension: true,
            PreMatchPromiseTensionQueryService.ToneAtRisk,
            "YEDEKTE",
            [
                new PreMatchPromiseTensionLine(
                    9,
                    5,
                    12,
                    "İlk 11",
                    PreMatchPromiseTensionQueryService.PlacementBench,
                    "Oyuncu#5 YEDEKTE — söz risk altında."),
            ]);

        var briefing = PreMatchBriefing.Compose(
            Fixture(approved: true, scheduledDay: 10),
            "Rival FC",
            currentDayNumber: 10,
            averageFatigue: 40,
            averageFitness: 72,
            injuredSlotCount: 0,
            tension: tension);

        Assert.True(briefing.IsReadyToKickOff);
        Assert.True(briefing.HasPromiseRisk);
        Assert.Contains("söz riski", briefing.Headline, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("bugün", briefing.FixtureLine, StringComparison.Ordinal);
        Assert.Contains(briefing.BeatLines, b => b.Contains("yorgunluk 40", StringComparison.Ordinal));
        Assert.Contains(briefing.BeatLines, b => b.StartsWith("Söz riski:", StringComparison.Ordinal));

        var text = briefing.ToDisplayText();
        Assert.Contains("· ", text, StringComparison.Ordinal);
    }

    [Fact]
    public void UnapprovedWithInjury_NamesPlayersAndForcesXi()
    {
        var briefing = PreMatchBriefing.Compose(
            Fixture(approved: false, scheduledDay: 10),
            "Rival FC",
            currentDayNumber: 10,
            injuredSlotCount: 2,
            injuredPlayerNames: ["Ali Yılmaz", "Can Demir"]);

        Assert.True(briefing.HasInjuryPressure);
        Assert.False(briefing.IsReadyToKickOff);
        Assert.Contains("sakatsız XI", briefing.Headline, StringComparison.OrdinalIgnoreCase);
        Assert.Contains(briefing.BeatLines, b => b.Contains("Ali Yılmaz", StringComparison.Ordinal));
        Assert.Contains(briefing.BeatLines, b => b.Contains("sakatsız XI onayla", StringComparison.OrdinalIgnoreCase));

        var bridge = briefing.ToKickoffBridgeLines();
        Assert.Contains(bridge, l => l.Contains("sakatlık", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(bridge, l => l.StartsWith("Sakat:", StringComparison.Ordinal));
    }

    [Fact]
    public void ApprovedOnTrack_ReadyHeadline()
    {
        var tension = new PreMatchPromiseTensionReadModel(
            1,
            1,
            true,
            true,
            PreMatchPromiseTensionQueryService.ToneOnTrack,
            "XI'da",
            [
                new PreMatchPromiseTensionLine(
                    1,
                    2,
                    0,
                    "İlk 11",
                    PreMatchPromiseTensionQueryService.PlacementStarting,
                    "Oyuncu#2 XI'da — söz yolunda."),
            ]);

        var briefing = PreMatchBriefing.Compose(
            Fixture(approved: true, scheduledDay: 11),
            "Away United",
            currentDayNumber: 10,
            tension: tension);

        Assert.Equal("Hazırsın — sözler yolunda, düdük için basabilirsin.", briefing.Headline);
        Assert.Contains("yarın", briefing.FixtureLine, StringComparison.Ordinal);
        Assert.False(briefing.HasPromiseRisk);
        Assert.Contains(briefing.BeatLines, b => b.StartsWith("Söz:", StringComparison.Ordinal));
    }

    [Fact]
    public void CleanReturn_SurfacesTemizXiOnBriefingAndKickoff()
    {
        var briefing = PreMatchBriefing.Compose(
            Fixture(approved: true, scheduledDay: 10),
            "Rival FC",
            currentDayNumber: 10,
            cleanReturnNames: ["Tolga Kurt"]);

        Assert.True(briefing.HasCleanReturn);
        Assert.Contains("Temiz XI", briefing.Headline, StringComparison.Ordinal);
        Assert.Contains(briefing.BeatLines, b => b.StartsWith("Temiz XI", StringComparison.Ordinal)
            && b.Contains("Tolga Kurt", StringComparison.Ordinal));

        var bridge = briefing.ToKickoffBridgeLines();
        Assert.Contains(bridge, l => l.StartsWith("Temiz XI", StringComparison.Ordinal)
            && l.Contains("sakatsız", StringComparison.OrdinalIgnoreCase));
    }

    private static ManagedFixtureSelectionStatusReadModel Fixture(bool approved, int scheduledDay) =>
        new(
            FixtureId: 100,
            SeasonId: 1,
            ManagedClubId: 1,
            OpponentClubId: 2,
            IsHome: true,
            ScheduledDayNumber: scheduledDay,
            ScheduledIsoDate: "2026-08-15",
            IsApproved: approved);
}
