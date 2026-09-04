using FootballCareerSimulator.Application.ManagerCareer.Composition;
using FootballCareerSimulator.Application.PlayerCareer.Composition;
using FootballCareerSimulator.Application.PlayerCareer.Services;
using FootballCareerSimulator.Application.TrainingPhysicalState.Infrastructure;
using FootballCareerSimulator.Application.WorldCalendar.Commands;
using FootballCareerSimulator.Application.WorldCalendar.Composition;
using FootballCareerSimulator.Domain.Shared;
using FootballCareerSimulator.Domain.WorldCalendar;
using PlayerCareerAggregate = FootballCareerSimulator.Domain.PlayerCareer.PlayerCareer;

namespace FootballCareerSimulator.Tests.PlayerCareer;

public sealed class PlayerAgingDayBoundaryConsequenceTests
{
    [Fact]
    public void Advance_AcrossNewYear_AgesDecliningPlayerViaDayBoundary()
    {
        var start = GameDate.FromCalendarDate(2026, 12, 30);
        var world = WorldCalendarModule.Create(start, rootSeed: 51);
        var manager = ManagerCareerModule.CreateNewCareer(start, startingClubId: 1);
        var players = PlayerCareerModule.Create(
            manager.Store,
            world.TimelineStore,
            new InMemoryTrainingPhysicalStateStore());

        players.Store.Upsert(
            PlayerCareerAggregate.CreateForSlot(
                    new ClubId(1),
                    slotIndex: 3,
                    currentAbility: 70,
                    potentialAbility: 72,
                    birthYear: 1994)
                .ApplyAnnualAging(start));

        world.AdvanceSimulationTime.BindPlayerAgingConsequences(
            new PlayerAgingDayBoundaryApplier(
                players.Development,
                world.EventRuleEvaluation!.Gate));

        var advance = world.AdvanceSimulationTime.Handle(
            new AdvanceSimulationTimeCommand(
                Guid.NewGuid(),
                GameDate.FromCalendarDate(2027, 1, 1).DayNumber));

        Assert.True(advance.Succeeded);
        Assert.Equal(1, advance.PlayersAgedCount);
        var aged = players.Store.Get(new ClubId(1), 3)!;
        Assert.Equal(67, aged.CurrentAbility);
        Assert.Equal(2027, aged.LastAgedCalendarYear);
    }
}
