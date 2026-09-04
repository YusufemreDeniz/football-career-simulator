namespace FootballCareerSimulator.Simulation.TeamPreparation;

public enum MvpSquadPositionGroup
{
    Goalkeeper = 1,
    Defender = 2,
    Midfielder = 3,
    Forward = 4,
}

public enum MvpSquadPositionRole
{
    Goalkeeper = 1,
    CentreBack = 2,
    RightBack = 3,
    LeftBack = 4,
    DefensiveMidfielder = 5,
    CentralMidfielder = 6,
    AttackingMidfielder = 7,
    RightMidfielder = 8,
    LeftMidfielder = 9,
    RightWinger = 10,
    LeftWinger = 11,
    Striker = 12,
}

public sealed record MvpSquadPlayerProfile(
    string DisplayName,
    MvpSquadPositionGroup PositionGroup,
    MvpSquadPositionRole? PositionRole = null,
    int? CurrentAbility = null,
    int? PotentialAbility = null,
    int? Age = null)
{
    public MvpSquadPlayerProfile(
        string displayName,
        MvpSquadPositionRole positionRole)
        : this(displayName, positionRole.ToPositionGroup(), positionRole)
    {
    }

    public MvpSquadPlayerProfile(
        string displayName,
        MvpSquadPositionRole positionRole,
        int currentAbility,
        int potentialAbility,
        int age)
        : this(
            displayName,
            positionRole.ToPositionGroup(),
            positionRole,
            currentAbility,
            potentialAbility,
            age)
    {
    }

    public string PositionCode => PositionRole?.ToPositionCode() ?? PositionGroup switch
    {
        MvpSquadPositionGroup.Goalkeeper => "KL",
        MvpSquadPositionGroup.Defender => "DEF",
        MvpSquadPositionGroup.Midfielder => "ORT",
        MvpSquadPositionGroup.Forward => "HÜC",
        _ => "?",
    };

    public string PositionName => PositionRole?.ToTurkishName() ?? PositionGroup switch
    {
        MvpSquadPositionGroup.Goalkeeper => "Kaleci",
        MvpSquadPositionGroup.Defender => "Defans",
        MvpSquadPositionGroup.Midfielder => "Orta saha",
        MvpSquadPositionGroup.Forward => "Hücum",
        _ => "Bilinmiyor",
    };
}

public static class MvpSquadPositionRoleExtensions
{
    public static MvpSquadPositionGroup ToPositionGroup(this MvpSquadPositionRole role) =>
        role switch
        {
            MvpSquadPositionRole.Goalkeeper => MvpSquadPositionGroup.Goalkeeper,
            MvpSquadPositionRole.CentreBack
                or MvpSquadPositionRole.RightBack
                or MvpSquadPositionRole.LeftBack => MvpSquadPositionGroup.Defender,
            MvpSquadPositionRole.DefensiveMidfielder
                or MvpSquadPositionRole.CentralMidfielder
                or MvpSquadPositionRole.AttackingMidfielder
                or MvpSquadPositionRole.RightMidfielder
                or MvpSquadPositionRole.LeftMidfielder => MvpSquadPositionGroup.Midfielder,
            MvpSquadPositionRole.RightWinger
                or MvpSquadPositionRole.LeftWinger
                or MvpSquadPositionRole.Striker => MvpSquadPositionGroup.Forward,
            _ => throw new ArgumentOutOfRangeException(nameof(role), role, "Unknown squad position role."),
        };

    public static string ToPositionCode(this MvpSquadPositionRole role) =>
        role switch
        {
            MvpSquadPositionRole.Goalkeeper => "KL",
            MvpSquadPositionRole.CentreBack => "STP",
            MvpSquadPositionRole.RightBack => "SĞB",
            MvpSquadPositionRole.LeftBack => "SLB",
            MvpSquadPositionRole.DefensiveMidfielder => "DOS",
            MvpSquadPositionRole.CentralMidfielder => "MO",
            MvpSquadPositionRole.AttackingMidfielder => "OOS",
            MvpSquadPositionRole.RightMidfielder => "SĞO",
            MvpSquadPositionRole.LeftMidfielder => "SLO",
            MvpSquadPositionRole.RightWinger => "SĞK",
            MvpSquadPositionRole.LeftWinger => "SLK",
            MvpSquadPositionRole.Striker => "SNT",
            _ => "?",
        };

    public static string ToTurkishName(this MvpSquadPositionRole role) =>
        role switch
        {
            MvpSquadPositionRole.Goalkeeper => "Kaleci",
            MvpSquadPositionRole.CentreBack => "Stoper",
            MvpSquadPositionRole.RightBack => "Sağ bek",
            MvpSquadPositionRole.LeftBack => "Sol bek",
            MvpSquadPositionRole.DefensiveMidfielder => "Defansif orta saha",
            MvpSquadPositionRole.CentralMidfielder => "Merkez orta saha",
            MvpSquadPositionRole.AttackingMidfielder => "Ofansif orta saha",
            MvpSquadPositionRole.RightMidfielder => "Sağ orta saha",
            MvpSquadPositionRole.LeftMidfielder => "Sol orta saha",
            MvpSquadPositionRole.RightWinger => "Sağ kanat",
            MvpSquadPositionRole.LeftWinger => "Sol kanat",
            MvpSquadPositionRole.Striker => "Santrfor",
            _ => "Bilinmiyor",
        };
}
