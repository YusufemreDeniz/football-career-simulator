using FootballCareerSimulator.Application.TeamPreparation.Queries;
using FootballCareerSimulator.Domain.Shared;
using FootballCareerSimulator.Domain.TrainingPhysicalState;
using FootballCareerSimulator.Domain.WorldCalendar;
using FootballCareerSimulator.Simulation.TrainingPhysicalState;

namespace FootballCareerSimulator.Tests.TeamPreparation;

public sealed class MatchDayLineupStripTests
{
    private static readonly GameDate Day = GameDate.FromCalendarDate(2026, 7, 1);

    [Fact]
    public void Compose_MarksInAndOutChipsFromSwaps()
    {
        var names = Enumerable.Range(0, 25).Select(i => $"Oyuncu{i} Soyad{i}").ToArray();
        var swaps = new MvpAvailabilityAwareSelection.AvailabilityAutoSwap[]
        {
            new(0, 11),
            new(1, 12),
        };

        var strip = MatchDayLineupStrip.Compose(
            hasMatch: true,
            isApproved: false,
            displayStartingSlots: Enumerable.Range(2, 11).ToArray(),
            swaps: swaps,
            playerNames: names);

        Assert.Contains("Taslak XI", strip.Caption, StringComparison.Ordinal);
        Assert.Equal(11, strip.StartingXi.Count);
        Assert.Contains(strip.StartingXi, c => c.IsIn && c.SlotIndex == 11);
        Assert.Contains(strip.StartingXi, c => c.IsIn && c.SlotIndex == 12);
        Assert.Equal(2, strip.OutPlayers.Count);
        Assert.All(strip.OutPlayers, c => Assert.True(c.IsOut));
        Assert.StartsWith("↑", strip.StartingXi.First(c => c.IsIn).ChipLabel, StringComparison.Ordinal);
        Assert.StartsWith("×", strip.OutPlayers[0].ChipLabel, StringComparison.Ordinal);
    }

    [Fact]
    public void PreferredPreview_FeedsStripWithInjuredOut()
    {
        var clubId = new ClubId(1);
        var physical = new Dictionary<(long, int), PlayerPhysicalState>
        {
            [(1, 0)] = PlayerPhysicalState.CreateRested(clubId, 0)
                .WithInjury(InjurySeverity.Serious, Day.AddDays(14)),
        };

        Assert.True(MvpAvailabilityAwareSelection.TryPreviewPreferredStartingXi(
            clubId,
            Day,
            physical,
            clubSquad: null,
            out var starting,
            out var swaps));

        Assert.DoesNotContain(0, starting);
        Assert.Single(swaps);
        Assert.Equal(0, swaps[0].OutSlotIndex);
        Assert.Equal(11, swaps[0].InSlotIndex);

        var names = Enumerable.Range(0, 25).Select(i => $"P{i} N{i}").ToArray();
        var strip = MatchDayLineupStrip.Compose(true, true, starting, swaps, names);
        Assert.Contains("Onaylı XI", strip.Caption, StringComparison.Ordinal);
        Assert.Contains(strip.StartingXi, c => c.IsIn && c.SlotIndex == 11);
        Assert.Contains(strip.OutPlayers, c => c.SlotIndex == 0 && c.IsOut);
        Assert.Contains("Sahaya bu XI ile çıktın", strip.ResultBridgeCaption, StringComparison.Ordinal);
        Assert.Contains("Böyle çıktın:", strip.ResultBridgeBeatLine(), StringComparison.Ordinal);
        Assert.Contains("Sahadaki XI", strip.HalfTimeBridgeCaption, StringComparison.Ordinal);
        Assert.Contains("değişiklik düşün", strip.HalfTimeBridgeCaption, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void CleanReturn_MarksTemizXiBridge()
    {
        var names = Enumerable.Range(0, 25).Select(i => $"Oyuncu{i} Soyad{i}").ToArray();
        var strip = MatchDayLineupStrip.Compose(
            hasMatch: true,
            isApproved: true,
            displayStartingSlots: Enumerable.Range(0, 11).ToArray(),
            swaps: Array.Empty<MvpAvailabilityAwareSelection.AvailabilityAutoSwap>(),
            playerNames: names,
            cleanReturnNames: ["Tolga Kurt"]);

        Assert.True(strip.HasCleanReturn);
        Assert.Contains("Temiz XI", strip.Caption, StringComparison.Ordinal);
        Assert.Contains("Kurt", strip.Caption, StringComparison.Ordinal);
        Assert.Contains("temiz XI", strip.ResultBridgeCaption, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Temiz XI:", strip.ResultBridgeBeatLine(), StringComparison.Ordinal);
        Assert.Contains("Temiz XI", strip.HalfTimeBridgeCaption, StringComparison.Ordinal);
    }
}
