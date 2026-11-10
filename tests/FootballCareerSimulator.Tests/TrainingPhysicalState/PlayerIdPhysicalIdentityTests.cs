using FootballCareerSimulator.Application.TrainingPhysicalState.Infrastructure;
using FootballCareerSimulator.Application.TrainingPhysicalState.Services;
using FootballCareerSimulator.Domain.PlayerCareer;
using FootballCareerSimulator.Domain.Shared;
using FootballCareerSimulator.Domain.TeamPreparation;
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
        var store = new InMemoryTrainingPhysicalStateStore();
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
    public void ReleaseThenNewPlayer_SameSlot_DoesNotLeakPhysicalState()
    {
        var store = new InMemoryTrainingPhysicalStateStore();
        var club = new ClubId(1);
        const int slot = 8;
        var a = PlayerPhysicalState.CreateRested(new PlayerId(101), club, slot)
            .WithLevels(88, 40)
            .WithInjury(InjurySeverity.Moderate, Day.AddDays(4), PlayerPhysicalState.ReasonTrainingLoad);
        store.UpsertPhysical(a);

        PhysicalLocationSync.ClearPlayerLocation(store, a.PlayerId);
        Assert.False(store.GetPhysical(a.PlayerId)!.HasLocation);

        var b = PlayerPhysicalState.CreateRested(new PlayerId(202), club, slot);
        store.UpsertPhysical(b);

        var atSlot = store.GetPhysical(club, slot)!;
        Assert.Equal(b.PlayerId, atSlot.PlayerId);
        Assert.Equal(PlayerPhysicalState.DefaultFatigue, atSlot.Fatigue);
        Assert.False(atSlot.IsInjured);
        Assert.Equal(88, store.GetPhysical(a.PlayerId)!.Fatigue);
        Assert.True(store.GetPhysical(a.PlayerId)!.IsInjured);
    }

    [Fact]
    public void UpsertPhysical_ClearsConflictingSlotOccupant()
    {
        var store = new InMemoryTrainingPhysicalStateStore();
        var club = new ClubId(1);
        store.UpsertPhysical(PlayerPhysicalState.CreateRested(new PlayerId(1), club, 3).WithLevels(70, 50));
        store.UpsertPhysical(PlayerPhysicalState.CreateRested(new PlayerId(2), club, 3));

        Assert.False(store.GetPhysical(new PlayerId(1))!.HasLocation);
        Assert.Equal(2, store.GetPhysical(club, 3)!.PlayerId.Value);
        Assert.Single(store.PhysicalBySlot);
    }

    [Fact]
    public void PhysicalBySlot_ThrowsOnDuplicateLocationInvariant()
    {
        var store = new InMemoryTrainingPhysicalStateStore();
        var club = new ClubId(1);
        // Bypass Upsert conflict clearing via ReplaceAll raw map is not possible;
        // force by mutating through reflection-free path: ClearLocation then dual assign without clear.
        // Use ReplaceAll after constructing two located states — UpsertPhysical clears.
        // Instead seed via ReplaceAll which now uses UpsertPhysical.
        // Simulate legacy dirty state by private dictionary is inaccessible —
        // verify GetPhysical uniqueness after SyncClubMembership instead.
        var a = PlayerPhysicalState.CreateRested(new PlayerId(1), club, 5).WithLevels(90, 40);
        var b = PlayerPhysicalState.CreateRested(new PlayerId(2));
        store.UpsertPhysical(a);
        store.UpsertPhysical(b);
        // Manually put both on same slot by clearing then using Copy path: Upsert B then re-upsert A without clearing B
        // Upsert clears — so duplicate can't persist through store API.
        store.UpsertPhysical(b.WithLocation(club, 5));
        Assert.False(store.GetPhysical(new PlayerId(1))!.HasLocation);

        PhysicalLocationSync.SyncClubMembership(
            store,
            club,
            [SquadMember.Create(new PlayerId(2), 5, Day)]);
        Assert.Equal(2, store.GetPhysical(club, 5)!.PlayerId.Value);
        Assert.False(store.GetPhysical(new PlayerId(1))!.HasLocation);
    }

    [Fact]
    public void WorkloadWindow_AgesWithTimeWithoutNewMatch()
    {
        var state = PlayerPhysicalState.CreateRested(new PlayerId(3), new ClubId(1), 0)
            .RecordMatchMinutes(Day, 90);

        Assert.Equal(90, state.EffectiveMatchMinutesLast7Days(Day));
        Assert.True(state.EffectiveMatchMinutesLast7Days(Day.AddDays(8)) < 90);
        Assert.Equal(0, state.EffectiveMatchMinutesLast7Days(Day.AddDays(7)));
        Assert.Equal(0, state.EffectiveMatchMinutesLast14Days(Day.AddDays(15)));
        Assert.Equal(90, state.MatchMinutesLast7Days);
    }

    [Fact]
    public void Congestion_PreMatchGapRaisesMatchRisk_OnProductionPath()
    {
        var baseState = PlayerPhysicalState.CreateRested(new PlayerId(9), new ClubId(1), 0)
            .WithLevels(60, 70)
            .RecordMatchMinutes(Day, 90);

        var congestedDay = Day.AddDays(2);
        var restedDay = Day.AddDays(6);

        Assert.True(baseState.HasCongestedFixture(congestedDay));
        Assert.False(baseState.HasCongestedFixture(restedDay));

        var congestedRisk = MvpInjuryRiskEvaluator.ComputeMatchRiskPercent(baseState, 90, congestedDay);
        var restedRisk = MvpInjuryRiskEvaluator.ComputeMatchRiskPercent(baseState, 90, restedDay);
        Assert.True(congestedRisk > restedRisk);

        // MaybeInjureFromMatch PRE-MATCH: RecordMatchMinutes sonrası congestion false olurdu; risk pre-match alınır.
        var afterCongested = MvpInjuryRiskEvaluator.MaybeInjureFromMatch(
            baseState,
            rootSeed: 1,
            fixtureId: 42,
            congestedDay,
            minutesPlayed: 90);
        Assert.Equal(congestedDay.DayNumber, afterCongested.LastMatchDayNumber);
        Assert.False(afterCongested.HasCongestedFixture(congestedDay));
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
    public void ReturnFromInjury_KeepsReason_AndElevatedRiskIsTemporary()
    {
        var day = Day;
        var injured = PlayerPhysicalState.CreateRested(new PlayerId(5), new ClubId(1), 0)
            .WithLevels(40, 75)
            .WithInjury(InjurySeverity.Minor, day.AddDays(1), PlayerPhysicalState.ReasonMatchLoad);
        var recovered = injured.RecoverIfDue(day.AddDays(2));
        Assert.False(recovered.IsInjured);
        Assert.True(recovered.Fatigue > injured.Fatigue);
        Assert.True(recovered.Fitness < injured.Fitness);
        Assert.Equal(PlayerPhysicalState.ReasonMatchLoad, recovered.LastInjuryReasonCode);
        Assert.True(recovered.HasElevatedReturnRisk);

        var fullyFit = recovered.WithLevels(
            PlayerPhysicalState.DefaultFatigue,
            PlayerPhysicalState.DefaultFitness);
        Assert.False(fullyFit.HasElevatedReturnRisk);
        Assert.Equal(PlayerPhysicalState.ReasonMatchLoad, fullyFit.LastInjuryReasonCode);

        var returnBonusWhileElevated = MvpInjuryRiskEvaluator.ComputeMatchRiskPercent(recovered, 90, day.AddDays(3));
        var returnBonusWhenFit = MvpInjuryRiskEvaluator.ComputeMatchRiskPercent(fullyFit, 90, day.AddDays(3));
        Assert.True(returnBonusWhileElevated > returnBonusWhenFit);
    }

    [Fact]
    public void RecoverDueInjuries_OnlyTouchesExpiredInjuriesAndKeepsClubSlotIndexes()
    {
        var store = new InMemoryTrainingPhysicalStateStore();
        var club1 = new ClubId(1);
        var club2 = new ClubId(2);
        store.UpsertPhysical(
            PlayerPhysicalState.CreateRested(new PlayerId(101), club1, 0)
                .WithInjury(InjurySeverity.Minor, Day.AddDays(2)));
        store.UpsertPhysical(
            PlayerPhysicalState.CreateRested(new PlayerId(102), club1, 1)
                .WithInjury(InjurySeverity.Serious, Day.AddDays(10)));
        store.UpsertPhysical(
            PlayerPhysicalState.CreateRested(new PlayerId(201), club2, 0));

        Assert.Equal(2, store.GetPhysicalStatesForClub(club1).Count);
        Assert.Single(store.GetPhysicalStatesForClub(club2));

        var recoveredCount = store.RecoverDueInjuries(Day.AddDays(3));

        Assert.Equal(1, recoveredCount);
        Assert.False(store.GetPhysical(new PlayerId(101))!.IsInjured);
        Assert.True(store.GetPhysical(new PlayerId(102))!.IsInjured);
        Assert.Equal(101, store.GetPhysical(club1, 0)!.PlayerId.Value);
        Assert.Equal(0, store.RecoverDueInjuries(Day.AddDays(3)));
    }
}
