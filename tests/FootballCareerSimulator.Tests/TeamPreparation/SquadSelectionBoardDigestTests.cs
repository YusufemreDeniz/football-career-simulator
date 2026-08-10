using FootballCareerSimulator.Application.TeamPreparation.Queries;
using FootballCareerSimulator.Domain.Shared;
using FootballCareerSimulator.Domain.TrainingPhysicalState;
using FootballCareerSimulator.Domain.WorldCalendar;
using FootballCareerSimulator.Simulation.TeamPreparation;

namespace FootballCareerSimulator.Tests.TeamPreparation;

public sealed class SquadSelectionBoardDigestTests
{
    private static readonly ClubId ClubId = new(1);
    private static readonly GameDate Day = GameDate.FromCalendarDate(2026, 8, 10);

    [Fact]
    public void Compose_ExposesLineupBenchAndPlayerCondition()
    {
        var physical = new Dictionary<(long ClubId, int SlotIndex), PlayerPhysicalState>
        {
            [(ClubId.Value, 11)] = PlayerPhysicalState.CreateRested(ClubId, 11)
                .WithLevels(fatigue: 62, fitness: 54),
        };

        var board = SquadSelectionBoardDigest.Compose(
            ClubId,
            Day,
            isApproved: false,
            startingSlots: Enumerable.Range(0, 11).ToArray(),
            benchSlots: Enumerable.Range(11, 7).ToArray(),
            profiles: Profiles(),
            ratingsBySlot: Ratings(),
            physicalBySlot: physical);

        Assert.True(board.HasMatch);
        Assert.False(board.IsApproved);
        Assert.Equal(11, board.StartingXi.Count);
        Assert.Equal(7, board.Bench.Count);
        Assert.All(board.StartingXi, player => Assert.True(player.IsStarter));
        Assert.Equal(54, board.Bench[0].Fitness);
        Assert.Equal(62, board.Bench[0].Fatigue);
        Assert.Contains("FİT %54", board.Bench[0].ButtonLabel);
    }

    [Fact]
    public void Compose_MarksUnavailableBenchPlayer()
    {
        var physical = new Dictionary<(long ClubId, int SlotIndex), PlayerPhysicalState>
        {
            [(ClubId.Value, 11)] = PlayerPhysicalState.CreateRested(ClubId, 11)
                .WithInjury(InjurySeverity.Minor, Day.AddDays(4)),
        };

        var board = SquadSelectionBoardDigest.Compose(
            ClubId,
            Day,
            isApproved: true,
            startingSlots: Enumerable.Range(0, 11).ToArray(),
            benchSlots: Enumerable.Range(11, 7).ToArray(),
            profiles: Profiles(),
            ratingsBySlot: Ratings(),
            physicalBySlot: physical);

        Assert.False(board.Bench[0].IsAvailable);
        Assert.EndsWith("SAKAT", board.Bench[0].ButtonLabel, StringComparison.Ordinal);
    }

    private static IReadOnlyList<MvpSquadPlayerProfile> Profiles() =>
        Enumerable.Range(0, 25)
            .Select(slot => new MvpSquadPlayerProfile(
                $"Oyuncu {slot + 1}",
                slot switch
                {
                    0 or 11 or 12 => MvpSquadPositionGroup.Goalkeeper,
                    >= 1 and <= 4 or >= 13 and <= 16 => MvpSquadPositionGroup.Defender,
                    >= 5 and <= 8 or >= 17 and <= 20 => MvpSquadPositionGroup.Midfielder,
                    _ => MvpSquadPositionGroup.Forward,
                }))
            .ToArray();

    private static IReadOnlyDictionary<int, int> Ratings() =>
        Enumerable.Range(0, 25).ToDictionary(slot => slot, slot => 70 + slot % 10);
}
