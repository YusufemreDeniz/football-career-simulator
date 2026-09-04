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
        PressingIntensity pressing,
        DefensiveLine defensiveLine,
        PassingStyle passingStyle,
        GameDate lastUpdatedOn)
    {
        ClubId = clubId;
        Formation = formation;
        Approach = approach;
        Pressing = pressing;
        DefensiveLine = defensiveLine;
        PassingStyle = passingStyle;
        LastUpdatedOn = lastUpdatedOn;
    }

    public ClubId ClubId { get; }

    public Formation Formation { get; }

    public TacticalApproach Approach { get; }

    public PressingIntensity Pressing { get; }

    public DefensiveLine DefensiveLine { get; }

    public PassingStyle PassingStyle { get; }

    public GameDate LastUpdatedOn { get; }

    public static TacticPlan CreateDefault(ClubId clubId, GameDate day) =>
        Set(
            clubId,
            Formation.F442,
            TacticalApproach.Balanced,
            PressingIntensity.Balanced,
            DefensiveLine.Standard,
            PassingStyle.Balanced,
            day);

    public static TacticPlan Set(
        ClubId clubId,
        Formation formation,
        TacticalApproach approach,
        PressingIntensity pressing,
        DefensiveLine defensiveLine,
        PassingStyle passingStyle,
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

        if (!Enum.IsDefined(pressing)
            || !Enum.IsDefined(defensiveLine)
            || !Enum.IsDefined(passingStyle))
        {
            throw new TeamPreparationInvariantViolationException(
                "Unknown team instruction value.");
        }

        return new TacticPlan(
            clubId,
            formation,
            approach,
            pressing,
            defensiveLine,
            passingStyle,
            day);
    }

    public static TacticPlan Set(
        ClubId clubId,
        Formation formation,
        TacticalApproach approach,
        GameDate day) =>
        Set(
            clubId,
            formation,
            approach,
            PressingIntensity.Balanced,
            DefensiveLine.Standard,
            PassingStyle.Balanced,
            day);

    public static TacticPlan Rehydrate(
        ClubId clubId,
        Formation formation,
        TacticalApproach approach,
        PressingIntensity pressing,
        DefensiveLine defensiveLine,
        PassingStyle passingStyle,
        GameDate lastUpdatedOn) =>
        Set(
            clubId,
            formation,
            approach,
            pressing,
            defensiveLine,
            passingStyle,
            lastUpdatedOn);

    public static TacticPlan Rehydrate(
        ClubId clubId,
        Formation formation,
        TacticalApproach approach,
        GameDate lastUpdatedOn) =>
        Set(clubId, formation, approach, lastUpdatedOn);

    public TacticPlan WithFormation(Formation formation, GameDate day) =>
        Set(ClubId, formation, Approach, Pressing, DefensiveLine, PassingStyle, day);

    public TacticPlan WithApproach(TacticalApproach approach, GameDate day) =>
        Set(ClubId, Formation, approach, Pressing, DefensiveLine, PassingStyle, day);

    public TacticPlan WithPlan(
        Formation formation,
        TacticalApproach approach,
        GameDate day) =>
        Set(ClubId, formation, approach, Pressing, DefensiveLine, PassingStyle, day);

    public TacticPlan WithPressing(PressingIntensity pressing, GameDate day) =>
        Set(ClubId, Formation, Approach, pressing, DefensiveLine, PassingStyle, day);

    public TacticPlan WithDefensiveLine(DefensiveLine defensiveLine, GameDate day) =>
        Set(ClubId, Formation, Approach, Pressing, defensiveLine, PassingStyle, day);

    public TacticPlan WithPassingStyle(PassingStyle passingStyle, GameDate day) =>
        Set(ClubId, Formation, Approach, Pressing, DefensiveLine, passingStyle, day);
}
