namespace FootballCareerSimulator.Simulation.TeamPreparation;

public enum MvpSquadPositionGroup
{
    Goalkeeper = 1,
    Defender = 2,
    Midfielder = 3,
    Forward = 4,
}

public sealed record MvpSquadPlayerProfile(
    string DisplayName,
    MvpSquadPositionGroup PositionGroup)
{
    public string PositionCode => PositionGroup switch
    {
        MvpSquadPositionGroup.Goalkeeper => "KL",
        MvpSquadPositionGroup.Defender => "DEF",
        MvpSquadPositionGroup.Midfielder => "ORT",
        MvpSquadPositionGroup.Forward => "HÜC",
        _ => "?",
    };
}
