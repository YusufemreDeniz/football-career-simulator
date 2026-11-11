using FootballCareerSimulator.Application.TeamPreparation.Ports;
using FootballCareerSimulator.Domain.Shared;
using FootballCareerSimulator.Domain.TeamPreparation;
using FootballCareerSimulator.Domain.WorldCalendar;

namespace FootballCareerSimulator.Application.TeamPreparation.Services;

public sealed class TacticPlanService
{
    private readonly ITacticPlanStore _store;

    public TacticPlanService(ITacticPlanStore store)
    {
        _store = store ?? throw new ArgumentNullException(nameof(store));
    }

    public TacticPlan EnsureDefault(ClubId clubId, GameDate day)
    {
        var existing = _store.Get(clubId);
        if (existing is not null)
        {
            return existing;
        }

        var plan = TacticPlan.CreateDefault(clubId, day);
        _store.Upsert(plan);
        return plan;
    }

    public TacticPlan SetFormation(ClubId clubId, Formation formation, GameDate day)
    {
        var current = EnsureDefault(clubId, day);
        var next = current.WithFormation(formation, day);
        _store.Upsert(next);
        return next;
    }

    public TacticPlan SetApproach(ClubId clubId, TacticalApproach approach, GameDate day)
    {
        var current = EnsureDefault(clubId, day);
        var next = current.WithApproach(approach, day);
        _store.Upsert(next);
        return next;
    }

    public TacticPlan SetPlan(
        ClubId clubId,
        Formation formation,
        TacticalApproach approach,
        GameDate day)
    {
        var current = EnsureDefault(clubId, day);
        var next = current.WithPlan(formation, approach, day);
        _store.Upsert(next);
        return next;
    }

    public TacticPlan SetPressing(ClubId clubId, PressingIntensity pressing, GameDate day)
    {
        var next = EnsureDefault(clubId, day).WithPressing(pressing, day);
        _store.Upsert(next);
        return next;
    }

    public TacticPlan SetDefensiveLine(ClubId clubId, DefensiveLine defensiveLine, GameDate day)
    {
        var next = EnsureDefault(clubId, day).WithDefensiveLine(defensiveLine, day);
        _store.Upsert(next);
        return next;
    }

    public TacticPlan SetPassingStyle(ClubId clubId, PassingStyle passingStyle, GameDate day)
    {
        var next = EnsureDefault(clubId, day).WithPassingStyle(passingStyle, day);
        _store.Upsert(next);
        return next;
    }
}
