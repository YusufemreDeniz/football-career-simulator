using FootballCareerSimulator.Domain.Shared;
using FootballCareerSimulator.Domain.WorldCalendar;

namespace FootballCareerSimulator.Domain.TeamPreparation;

public enum TacticalPhase
{
    InPossession = 1,
    OutOfPossession = 2,
}

public enum TacticalPhaseRole
{
    Balanced = 1,
    WideOverloads = 2,
    CentralOverloads = 3,
    DirectRunners = 4,
    CompactBlock = 5,
    AggressivePress = 6,
}

/// <summary>
/// Hücum ve savunma yerleşimini birbirinden ayıran, eski TacticPlan'den bağımsız ek plan.
/// Ayrı tutulması mevcut kariyer kayıtlarının şemasını ve klasik taktik davranışını korur.
/// </summary>
public sealed class DualPhaseTacticPlan
{
    private DualPhaseTacticPlan(
        ClubId clubId,
        Formation inPossessionFormation,
        Formation outOfPossessionFormation,
        TacticalPhaseRole inPossessionRole,
        TacticalPhaseRole outOfPossessionRole,
        GameDate lastUpdatedOn)
    {
        ClubId = clubId;
        InPossessionFormation = inPossessionFormation;
        OutOfPossessionFormation = outOfPossessionFormation;
        InPossessionRole = inPossessionRole;
        OutOfPossessionRole = outOfPossessionRole;
        LastUpdatedOn = lastUpdatedOn;
    }

    public ClubId ClubId { get; }

    public Formation InPossessionFormation { get; }

    public Formation OutOfPossessionFormation { get; }

    public TacticalPhaseRole InPossessionRole { get; }

    public TacticalPhaseRole OutOfPossessionRole { get; }

    public GameDate LastUpdatedOn { get; }

    public static DualPhaseTacticPlan FromLegacy(TacticPlan legacy, GameDate day)
    {
        ArgumentNullException.ThrowIfNull(legacy);
        return Set(
            legacy.ClubId,
            legacy.Formation,
            legacy.Formation,
            TacticalPhaseRole.Balanced,
            TacticalPhaseRole.Balanced,
            day);
    }

    public static DualPhaseTacticPlan Set(
        ClubId clubId,
        Formation inPossessionFormation,
        Formation outOfPossessionFormation,
        TacticalPhaseRole inPossessionRole,
        TacticalPhaseRole outOfPossessionRole,
        GameDate day)
    {
        if (!Enum.IsDefined(inPossessionFormation)
            || !Enum.IsDefined(outOfPossessionFormation))
        {
            throw new TeamPreparationInvariantViolationException("Unknown phase formation.");
        }

        if (!IsRoleAllowed(TacticalPhase.InPossession, inPossessionRole)
            || !IsRoleAllowed(TacticalPhase.OutOfPossession, outOfPossessionRole))
        {
            throw new TeamPreparationInvariantViolationException(
                "Tactical phase role is not valid for its phase.");
        }

        return new DualPhaseTacticPlan(
            clubId,
            inPossessionFormation,
            outOfPossessionFormation,
            inPossessionRole,
            outOfPossessionRole,
            day);
    }

    public static bool IsRoleAllowed(TacticalPhase phase, TacticalPhaseRole role) => phase switch
    {
        TacticalPhase.InPossession => role is TacticalPhaseRole.Balanced
            or TacticalPhaseRole.WideOverloads
            or TacticalPhaseRole.CentralOverloads
            or TacticalPhaseRole.DirectRunners,
        TacticalPhase.OutOfPossession => role is TacticalPhaseRole.Balanced
            or TacticalPhaseRole.CompactBlock
            or TacticalPhaseRole.AggressivePress,
        _ => false,
    };
}
