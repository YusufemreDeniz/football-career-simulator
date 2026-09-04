using FootballCareerSimulator.Application.ManagerCareer.Ports;
using FootballCareerSimulator.Application.TeamPreparation.Ports;
using FootballCareerSimulator.Application.TeamPreparation.Queries;
using FootballCareerSimulator.Domain.TeamPreparation;

namespace FootballCareerSimulator.Application.TeamPreparation.Services;

public sealed class TacticPlanQueryService
{
    private readonly ITacticPlanStore _store;
    private readonly IManagerCareerStore _managerCareerStore;

    public TacticPlanQueryService(ITacticPlanStore store, IManagerCareerStore managerCareerStore)
    {
        _store = store ?? throw new ArgumentNullException(nameof(store));
        _managerCareerStore = managerCareerStore
            ?? throw new ArgumentNullException(nameof(managerCareerStore));
    }

    public TacticPlanReadModel GetManagedClubPlan()
    {
        if (_managerCareerStore.Career.ActiveEmployment is not { ClubId: var clubId })
        {
            return new TacticPlanReadModel(null, "—", "—", 0, "—", "—", "—");
        }

        var plan = _store.Get(clubId);
        if (plan is null)
        {
            return new TacticPlanReadModel(clubId.Value, "yok", "yok", 0, "yok", "yok", "yok");
        }

        return new TacticPlanReadModel(
            clubId.Value,
            FormatFormation(plan.Formation),
            FormatApproach(plan.Approach),
            plan.LastUpdatedOn.DayNumber,
            FormatPressing(plan.Pressing),
            FormatDefensiveLine(plan.DefensiveLine),
            FormatPassingStyle(plan.PassingStyle),
            plan.Formation,
            plan.Approach,
            plan.Pressing,
            plan.DefensiveLine,
            plan.PassingStyle);
    }

    private static string FormatFormation(Formation formation) => formation switch
    {
        Formation.F442 => "4-4-2",
        Formation.F433 => "4-3-3",
        Formation.F352 => "3-5-2",
        _ => formation.ToString(),
    };

    private static string FormatApproach(TacticalApproach approach) => approach switch
    {
        TacticalApproach.Balanced => "Dengeli",
        TacticalApproach.Attacking => "Hücum",
        TacticalApproach.Defensive => "Defans",
        _ => approach.ToString(),
    };

    private static string FormatPressing(PressingIntensity pressing) => pressing switch
    {
        PressingIntensity.LowBlock => "Geri çekil",
        PressingIntensity.Balanced => "Dengeli pres",
        PressingIntensity.HighPress => "Önde bas",
        _ => pressing.ToString(),
    };

    private static string FormatDefensiveLine(DefensiveLine defensiveLine) => defensiveLine switch
    {
        DefensiveLine.Deep => "Derin hat",
        DefensiveLine.Standard => "Standart hat",
        DefensiveLine.High => "Yüksek hat",
        _ => defensiveLine.ToString(),
    };

    private static string FormatPassingStyle(PassingStyle passingStyle) => passingStyle switch
    {
        PassingStyle.Direct => "Direkt pas",
        PassingStyle.Balanced => "Dengeli pas",
        PassingStyle.Short => "Kısa pas",
        _ => passingStyle.ToString(),
    };
}
