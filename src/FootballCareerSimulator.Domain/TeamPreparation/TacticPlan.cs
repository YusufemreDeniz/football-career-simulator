using FootballCareerSimulator.Domain.Shared;
using FootballCareerSimulator.Domain.WorldCalendar;

namespace FootballCareerSimulator.Domain.TeamPreparation;

/// <summary>
/// Kulüp bazlı yeniden kullanılabilir taktik planı (MVP: formasyon + yaklaşım).
/// </summary>
public sealed class TacticPlan
{
    private TacticPlan(
        ClubId clubId,
        Formation formation,
        TacticalApproach approach,
        GameDate lastUpdatedOn)
    {
        ClubId = clubId;
        Formation = formation;
        Approach = approach;
        LastUpdatedOn = lastUpdatedOn;
    }

    public ClubId ClubId { get; }

    public Formation Formation { get; }

    public TacticalApproach Approach { get; }

    public GameDate LastUpdatedOn { get; }

    public static TacticPlan CreateDefault(ClubId clubId, GameDate day) =>
        Set(clubId, Formation.F442, TacticalApproach.Balanced, day);

    public static TacticPlan Set(
        ClubId clubId,
        Formation formation,
        TacticalApproach approach,
        GameDate day)
    {
        if (!Enum.IsDefined(formation))
        {
            throw new TeamPreparationInvariantViolationException(
                $"Unknown formation: {formation}.");
        }

        if (!Enum.IsDefined(approach))
        {
            throw new TeamPreparationInvariantViolationException(
                $"Unknown tactical approach: {approach}.");
        }

        return new TacticPlan(clubId, formation, approach, day);
    }

    public static TacticPlan Rehydrate(
        ClubId clubId,
        Formation formation,
        TacticalApproach approach,
        GameDate lastUpdatedOn) =>
        Set(clubId, formation, approach, lastUpdatedOn);

    public TacticPlan WithFormation(Formation formation, GameDate day) =>
        Set(ClubId, formation, Approach, day);

    public TacticPlan WithApproach(TacticalApproach approach, GameDate day) =>
        Set(ClubId, Formation, approach, day);
}
