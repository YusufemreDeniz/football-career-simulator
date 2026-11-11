using FootballCareerSimulator.Application.SocialContinuity.Composition;
using FootballCareerSimulator.Domain.Competition;
using FootballCareerSimulator.Domain.ManagerCareer;
using FootballCareerSimulator.Domain.PlayerCareer;
using FootballCareerSimulator.Domain.Shared;
using FootballCareerSimulator.Domain.SocialContinuity;
using FootballCareerSimulator.Domain.WorldCalendar;

namespace FootballCareerSimulator.Tests.SocialContinuity;

public sealed class MemoryDecayTests
{
    private static readonly GameDate Day = GameDate.FromCalendarDate(2026, 8, 1);

    [Fact]
    public void SelectionMemory_DecaysFasterThanCareerMemory()
    {
        var social = SocialContinuityModule.Create();
        social.SelectionMemory.RecordStarts(new FixtureId(1), [new PlayerId(10)], Day);
        social.CareerMemory.RecordDismissal(
            new ManagerId(1),
            new ClubId(2),
            new FixtureId(9),
            Day);

        var asOf = Day.AddDays(28);
        Assert.Equal(2, social.MemoryDecay.ApplyDue(asOf));

        var selection = Assert.Single(
            social.MemoryStore.Memories,
            m => m.Category == MemoryCategory.Selection);
        var career = Assert.Single(
            social.MemoryStore.Memories,
            m => m.Category == MemoryCategory.Career);

        // Selection: 35 - (28/7)*8 = 35 - 32 = 3
        Assert.Equal(3, selection.CurrentInfluence);
        // Career: 85 - (28/28)*3 = 82
        Assert.Equal(82, career.CurrentInfluence);
        Assert.True(selection.CurrentInfluence < career.CurrentInfluence);
    }

    [Fact]
    public void ApplyDue_IsIdempotentForSameDay()
    {
        var social = SocialContinuityModule.Create();
        social.SelectionMemory.RecordStarts(new FixtureId(2), [new PlayerId(11)], Day);

        var asOf = Day.AddDays(7);
        Assert.Equal(1, social.MemoryDecay.ApplyDue(asOf));
        Assert.Equal(0, social.MemoryDecay.ApplyDue(asOf));
        Assert.Equal(27, social.MemoryStore.Memories.Single().CurrentInfluence);
    }

    [Fact]
    public void LargeTimeJump_AppliesFullElapsedDecay()
    {
        var social = SocialContinuityModule.Create();
        social.SelectionMemory.RecordStarts(new FixtureId(3), [new PlayerId(12)], Day);

        // 5 periods * 8 = 40 → floor MinImportance (1)
        Assert.Equal(1, social.MemoryDecay.ApplyDue(Day.AddDays(35)));
        Assert.Equal(1, social.MemoryStore.Memories.Single().CurrentInfluence);
        Assert.Equal(
            1,
            MemoryTimeDecay.ComputeCurrentInfluence(
                MemoryCategory.Selection,
                baseImportance: 35,
                Day,
                Day.AddDays(35)));
    }

    [Fact]
    public void SameDay_DoesNotDecay()
    {
        var social = SocialContinuityModule.Create();
        social.CareerMemory.RecordHiring(
            new ManagerId(1),
            new ClubId(3),
            new JobOfferId(7),
            Day);

        Assert.Equal(0, social.MemoryDecay.ApplyDue(Day));
        Assert.Equal(
            social.MemoryStore.Memories.Single().BaseImportance,
            social.MemoryStore.Memories.Single().CurrentInfluence);
    }
}
