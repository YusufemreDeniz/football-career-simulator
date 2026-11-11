using FootballCareerSimulator.Application.Competition.Commands;
using FootballCareerSimulator.Application.Competition.Queries;
using FootballCareerSimulator.Domain.Match;

namespace FootballCareerSimulator.Tests.Competition;

public sealed class MatchMomentStoryboardTests
{
    [Fact]
    public void Build_OrdersByMinuteAndPreservesSourceOrderForTies()
    {
        MatchKeyMomentReadModel[] moments =
        [
            Moment(MatchKeyMomentKind.RedCard, 70, slot: 4),
            Moment(MatchKeyMomentKind.Goal, 12, slot: 9),
            Moment(MatchKeyMomentKind.YellowCard, 70, slot: 6),
        ];

        var storyboard = MatchMomentStoryboard.Build(moments, sequenceSeed: 42);

        Assert.Collection(
            storyboard.Frames,
            frame => Assert.Equal(nameof(MatchKeyMomentKind.Goal), frame.Kind),
            frame => Assert.Equal(nameof(MatchKeyMomentKind.RedCard), frame.Kind),
            frame => Assert.Equal(nameof(MatchKeyMomentKind.YellowCard), frame.Kind));
        Assert.Equal([0, 1, 2], storyboard.Frames.Select(frame => frame.SequenceIndex));
    }

    [Fact]
    public void Build_IsDeterministicForTheSameSeedAndInput()
    {
        MatchKeyMomentReadModel[] moments =
        [
            new(
                nameof(MatchKeyMomentKind.Goal),
                36,
                IsHomeSide: true,
                PrimarySlotIndex: 10,
                AssistSlotIndex: 7,
                PrimaryPlayerName: "Ada Kaya",
                AssistPlayerName: "Ece Demir"),
            Moment(MatchKeyMomentKind.Injury, 64, slot: 3, isHomeSide: false),
        ];

        var first = MatchMomentStoryboard.Build(moments, sequenceSeed: 913);
        var second = MatchMomentStoryboard.Build(moments, sequenceSeed: 913);

        Assert.True(first.Frames.SequenceEqual(second.Frames));
    }

    [Fact]
    public void GoalFrame_AttacksTheCorrectGoalAndUsesAssistAsBallOrigin()
    {
        var home = Assert.Single(MatchMomentStoryboard.Build(
            [new MatchKeyMomentReadModel(
                nameof(MatchKeyMomentKind.Goal),
                21,
                IsHomeSide: true,
                PrimarySlotIndex: 9,
                AssistSlotIndex: 6)],
            sequenceSeed: 7).Frames);
        var away = Assert.Single(MatchMomentStoryboard.Build(
            [Moment(MatchKeyMomentKind.Goal, 52, slot: 10, isHomeSide: false)],
            sequenceSeed: 7).Frames);

        Assert.Equal(home.SupportPosition, home.BallStart);
        Assert.True(home.BallEnd.X > 0.95f);
        Assert.True(away.BallEnd.X < 0.05f);
    }

    [Fact]
    public void ResolvePlayerPosition_MirrorsSidesAndKeepsCoordinatesInBounds()
    {
        for (var slot = -2; slot <= 14; slot++)
        {
            var home = MatchMomentStoryboard.ResolvePlayerPosition(slot, isHomeSide: true);
            var away = MatchMomentStoryboard.ResolvePlayerPosition(slot, isHomeSide: false);

            Assert.InRange(home.X, 0f, 1f);
            Assert.InRange(home.Y, 0f, 1f);
            Assert.Equal(1f - home.X, away.X, precision: 5);
            Assert.Equal(home.Y, away.Y, precision: 5);
        }
    }

    [Fact]
    public void Build_NullMomentsReturnsReusableEmptyStoryboard()
    {
        var storyboard = MatchMomentStoryboard.Build(null);

        Assert.Same(MatchMomentStoryboard.Empty, storyboard);
        Assert.Empty(storyboard.Frames);
    }

    private static MatchKeyMomentReadModel Moment(
        MatchKeyMomentKind kind,
        int minute,
        int slot,
        bool isHomeSide = true) =>
        new(kind.ToString(), minute, isHomeSide, slot);
}
