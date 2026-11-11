using FootballCareerSimulator.Application.TeamPreparation.Infrastructure;
using FootballCareerSimulator.Application.TeamPreparation.Queries;
using FootballCareerSimulator.Application.TeamPreparation.Services;
using FootballCareerSimulator.Domain.Shared;
using FootballCareerSimulator.Domain.TeamPreparation;
using FootballCareerSimulator.Domain.WorldCalendar;
using FootballCareerSimulator.Simulation.TeamPreparation;

namespace FootballCareerSimulator.Tests.TeamPreparation;

public sealed class DualPhaseTacticPlanTests
{
    private static readonly GameDate Day = GameDate.FromCalendarDate(2026, 8, 31);

    [Fact]
    public void FromLegacy_IsBackwardCompatibleAndAddsNoModifier()
    {
        var legacy = TacticPlan.CreateDefault(new ClubId(1), Day);
        var phase = DualPhaseTacticPlan.FromLegacy(legacy, Day);

        Assert.Equal(legacy.Formation, phase.InPossessionFormation);
        Assert.Equal(legacy.Formation, phase.OutOfPossessionFormation);
        Assert.Equal(0, DualPhaseTacticMatchModifier.Compute(legacy, phase));
        Assert.Equal(0, DualPhaseTacticMatchModifier.Compute(legacy, null));
    }

    [Fact]
    public void Set_RejectsRoleAssignedToWrongPhase()
    {
        Assert.Throws<TeamPreparationInvariantViolationException>(() =>
            DualPhaseTacticPlan.Set(
                new ClubId(1),
                Formation.F433,
                Formation.F442,
                TacticalPhaseRole.CompactBlock,
                TacticalPhaseRole.WideOverloads,
                Day));
    }

    [Fact]
    public void CompatibleTransition_ProducesDeterministicCappedBenefitAndStaffDigest()
    {
        var legacy = TacticPlan.CreateDefault(new ClubId(1), Day)
            .WithFormation(Formation.F433, Day)
            .WithPressing(PressingIntensity.Balanced, Day);
        var phase = DualPhaseTacticPlan.Set(
            new ClubId(1),
            Formation.F433,
            Formation.F442,
            TacticalPhaseRole.WideOverloads,
            TacticalPhaseRole.CompactBlock,
            Day);

        var first = DualPhaseTacticMatchModifier.Compute(legacy, phase);
        var second = DualPhaseTacticMatchModifier.Compute(legacy, phase);
        var digest = DualPhaseTacticStaffDigest.Compose(legacy, phase);

        Assert.Equal(2, first);
        Assert.Equal(first, second);
        Assert.Equal(first, digest.MatchModifier);
        Assert.Equal(DualPhaseTacticRisk.Low, digest.Risk);
        Assert.Contains("+2", digest.StaffNote, StringComparison.Ordinal);
    }

    [Fact]
    public void Service_UsesLegacyAsDefaultThenStoresExplicitPlan()
    {
        var legacyStore = new InMemoryTacticPlanStore();
        var phaseStore = new InMemoryDualPhaseTacticPlanStore();
        var clubId = new ClubId(7);
        legacyStore.Upsert(
            TacticPlan.CreateDefault(clubId, Day).WithFormation(Formation.F352, Day));
        var service = new DualPhaseTacticPlanService(phaseStore, legacyStore);

        var inherited = service.EnsureFromLegacy(clubId, Day);
        var explicitPlan = service.SetPlan(
            clubId,
            Formation.F352,
            Formation.F442,
            TacticalPhaseRole.CentralOverloads,
            TacticalPhaseRole.CompactBlock,
            Day.AddDays(1));

        Assert.Equal(Formation.F352, inherited.InPossessionFormation);
        Assert.Equal(Formation.F352, inherited.OutOfPossessionFormation);
        Assert.Same(explicitPlan, phaseStore.Get(clubId));
        Assert.Equal(Day.AddDays(1), explicitPlan.LastUpdatedOn);
    }
}
