using FootballCareerSimulator.Application.TeamPreparation.Ports;
using FootballCareerSimulator.Domain.Shared;
using FootballCareerSimulator.Domain.TeamPreparation;
using FootballCareerSimulator.Domain.WorldCalendar;

namespace FootballCareerSimulator.Application.TeamPreparation.Services;

public sealed class DualPhaseTacticPlanService
{
    private readonly IDualPhaseTacticPlanStore _store;
    private readonly ITacticPlanStore _legacyStore;

    public DualPhaseTacticPlanService(
        IDualPhaseTacticPlanStore store,
        ITacticPlanStore legacyStore)
    {
        _store = store ?? throw new ArgumentNullException(nameof(store));
        _legacyStore = legacyStore ?? throw new ArgumentNullException(nameof(legacyStore));
    }

    public DualPhaseTacticPlan EnsureFromLegacy(ClubId clubId, GameDate day)
    {
        var existing = _store.Get(clubId);
        if (existing is not null)
        {
            return existing;
        }

        var legacy = _legacyStore.Get(clubId) ?? TacticPlan.CreateDefault(clubId, day);
        var next = DualPhaseTacticPlan.FromLegacy(legacy, day);
        _store.Upsert(next);
        return next;
    }

    public DualPhaseTacticPlan SetPlan(
        ClubId clubId,
        Formation inPossessionFormation,
        Formation outOfPossessionFormation,
        TacticalPhaseRole inPossessionRole,
        TacticalPhaseRole outOfPossessionRole,
        GameDate day)
    {
        var next = DualPhaseTacticPlan.Set(
            clubId,
            inPossessionFormation,
            outOfPossessionFormation,
            inPossessionRole,
            outOfPossessionRole,
            day);
        _store.Upsert(next);
        return next;
    }
}
