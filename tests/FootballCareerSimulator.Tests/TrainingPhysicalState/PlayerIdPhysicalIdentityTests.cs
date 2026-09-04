using FootballCareerSimulator.Domain.PlayerCareer;
using FootballCareerSimulator.Domain.Shared;
using FootballCareerSimulator.Domain.TrainingPhysicalState;
using FootballCareerSimulator.Domain.WorldCalendar;
using FootballCareerSimulator.Simulation.TrainingPhysicalState;

namespace FootballCareerSimulator.Tests.TrainingPhysicalState;

public sealed class PlayerIdPhysicalIdentityTests
{
    private static readonly GameDate Day = GameDate.FromCalendarDate(2026, 8, 1);

    [Fact]
    public void SlotRelocate_KeepsSamePlayerIdFatigueAndInjury()
    {
        var playerId = new PlayerId(9001);
        var seller = new ClubId(1);
        var buyer = new ClubId(2);
        var injured = PlayerPhysicalState.CreateRested(playerId, seller, 4)
            .WithLevels(70, 60)
            .WithInjury(InjurySeverity.Moderate, Day.AddDays(5), PlayerPhysicalState.ReasonMatchLoad);

        var relocated = injured.WithLocation(buyer, 11);

        Assert.Equal(playerId, relocated.PlayerId);
        Assert.Equal(70, relocated.Fatigue);
        Assert.Equal(InjurySeverity.Moderate, relocated.InjurySeverity);
        Assert.Equal(buyer, relocated.ClubId);
        Assert.Equal(11, relocated.SlotIndex);
    }

    [Fact]
    public void Store_KeyedByPlayerId_DoesNotLeakSlotStateToAnotherPlayer()
    {
        var store = new Application.TrainingPhysicalState.Infrastructure.InMemoryTrainingPhysicalStateStore();
        var club = new ClubId(1);
        var a = PlayerPhysicalState.CreateRested(new PlayerId(1), club, 0).WithLevels(80, 50);
        var b = PlayerPhysicalState.CreateRested(new PlayerId(2), club, 0);
        store.UpsertPhysical(a);
        store.UpsertPhysical(b.WithLocation(club, 1));

        Assert.Equal(80, store.GetPhysical(new PlayerId(1))!.Fatigue);
        Assert.Equal(PlayerPhysicalState.DefaultFatigue, store.GetPhysical(new PlayerId(2))!.Fatigue);
        Assert.Equal(a.PlayerId, store.GetPhysical(club, 0)!.PlayerId);
    }

    [Fact]
    public void Workload_IncreasesMatchInjuryRisk()
    {
        var fresh = PlayerPhysicalState.CreateRested(new PlayerId(3), new ClubId(1), 0);
        var loaded = fresh
            .RecordMatchMinutes(Day, 90)
            .RecordMatchMinutes(Day.AddDays(1), 90)
            .RecordMatchMinutes(Day.AddDays(2), 90);
        Assert.True(loaded.MatchMinutesLast7Days >= 180);
        Assert.True(
            MvpInjuryRiskEvaluator.ComputeMatchRiskPercent(loaded, 90, Day.AddDays(3))
            > MvpInjuryRiskEvaluator.ComputeMatchRiskPercent(fresh, 90, Day.AddDays(3)));
    }

    [Fact]
    public void SameSeed_ProducesSameInjuryRoll()
    {
        var state = PlayerPhysicalState.CreateRested(new PlayerId(44), new ClubId(1), 2)
            .WithLevels(85, 55)
            .RecordMatchMinutes(Day, 90)
            .RecordMatchMinutes(Day.AddDays(2), 90);

        var a = MvpInjuryRiskEvaluator.MaybeInjureFromMatch(state, rootSeed: 123, fixtureId: 9, Day.AddDays(3));
        var b = MvpInjuryRiskEvaluator.MaybeInjureFromMatch(state, rootSeed: 123, fixtureId: 9, Day.AddDays(3));
        Assert.Equal(a.IsInjured, b.IsInjured);
        Assert.Equal(a.Fatigue, b.Fatigue);
        Assert.Equal(a.MatchMinutesLast7Days, b.MatchMinutesLast7Days);
    }

    [Fact]
    public void RestDay_ReducesFatigueWithoutInjuryRoll()
    {
        var club = new ClubId(1);
        var playerId = new PlayerId(77);
        var sunday = GameDate.FromCalendarDate(2026, 8, 2); // Sunday
        Assert.False(MvpTrainingLoadApplier.IsCalendarTrainingDay(sunday));

        var plan = WeeklyTrainingPlan.Set(
            club,
            TrainingFocus.Recovery,
            TrainingIntensity.High,
            RestApproach.Heavy,
            sunday);
        var tired = PlayerPhysicalState.CreateRested(playerId, club, 0).WithLevels(70, 60);
        var after = MvpTrainingLoadApplier.ApplyDailyTickToMembers(
            plan,
            sunday,
            rootSeed: 1,
            [(playerId, 0)],
            new Dictionary<long, PlayerPhysicalState> { [playerId.Value] = tired },
            isMatchDay: false);

        Assert.Single(after);
        Assert.True(after[0].Fatigue < tired.Fatigue);
        Assert.False(after[0].IsInjured);
    }

    [Fact]
    public void ReturnFromInjury_AppliesDampenedFitness()
    {
        var day = Day;
        var injured = PlayerPhysicalState.CreateRested(new PlayerId(5), new ClubId(1), 0)
            .WithLevels(40, 75)
            .WithInjury(InjurySeverity.Minor, day.AddDays(1));
        var recovered = injured.RecoverIfDue(day.AddDays(2));
        Assert.False(recovered.IsInjured);
        Assert.True(recovered.Fatigue > injured.Fatigue);
        Assert.True(recovered.Fitness < injured.Fitness);
        Assert.Equal(PlayerPhysicalState.ReasonReturnFromInjury, recovered.LastInjuryReasonCode);
    }
}
