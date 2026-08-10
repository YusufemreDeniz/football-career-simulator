using FootballCareerSimulator.Domain.TeamPreparation;

namespace FootballCareerSimulator.Simulation.TeamPreparation;

public sealed record MvpLineupRoleFit(
    int Score,
    int NaturalFitCount,
    int MatchStrengthModifier,
    IReadOnlyList<bool> PlayerNaturalFits,
    IReadOnlyList<MvpSquadPositionRole> RequiredRoles,
    IReadOnlyList<int> PlayerByRoleIndex);

public static class MvpLineupRoleFitCalculator
{
    public static MvpLineupRoleFit Evaluate(
        Formation formation,
        IReadOnlyList<MvpSquadPlayerProfile> players)
    {
        ArgumentNullException.ThrowIfNull(players);

        var requiredRoles = RequiredRolesFor(formation);
        var playerByRoleIndex = MatchPlayersToRoles(players, requiredRoles);
        var matchedPlayerIndices = playerByRoleIndex.Where(index => index >= 0).ToHashSet();
        var naturalFitCount = matchedPlayerIndices.Count;
        var score = players.Count == 0
            ? 0
            : (int)Math.Round(
                naturalFitCount * 100.0 / requiredRoles.Count,
                MidpointRounding.AwayFromZero);

        return new MvpLineupRoleFit(
            score,
            naturalFitCount,
            ComputeMatchStrengthModifier(score),
            players.Select((_, index) => matchedPlayerIndices.Contains(index)).ToArray(),
            requiredRoles,
            playerByRoleIndex);
    }

    public static int ComputeMatchStrengthModifier(int score) =>
        score switch
        {
            >= 100 => 2,
            >= 91 => 1,
            >= 82 => 0,
            >= 73 => -1,
            >= 64 => -2,
            _ => -4,
        };

    public static IReadOnlyList<MvpSquadPositionRole> RequiredRolesFor(Formation formation) =>
        formation switch
        {
            Formation.F442 =>
            [
                MvpSquadPositionRole.Goalkeeper,
                MvpSquadPositionRole.RightBack,
                MvpSquadPositionRole.CentreBack,
                MvpSquadPositionRole.CentreBack,
                MvpSquadPositionRole.LeftBack,
                MvpSquadPositionRole.RightMidfielder,
                MvpSquadPositionRole.CentralMidfielder,
                MvpSquadPositionRole.CentralMidfielder,
                MvpSquadPositionRole.LeftMidfielder,
                MvpSquadPositionRole.Striker,
                MvpSquadPositionRole.Striker,
            ],
            Formation.F433 =>
            [
                MvpSquadPositionRole.Goalkeeper,
                MvpSquadPositionRole.RightBack,
                MvpSquadPositionRole.CentreBack,
                MvpSquadPositionRole.CentreBack,
                MvpSquadPositionRole.LeftBack,
                MvpSquadPositionRole.DefensiveMidfielder,
                MvpSquadPositionRole.CentralMidfielder,
                MvpSquadPositionRole.AttackingMidfielder,
                MvpSquadPositionRole.RightWinger,
                MvpSquadPositionRole.LeftWinger,
                MvpSquadPositionRole.Striker,
            ],
            Formation.F352 =>
            [
                MvpSquadPositionRole.Goalkeeper,
                MvpSquadPositionRole.CentreBack,
                MvpSquadPositionRole.CentreBack,
                MvpSquadPositionRole.CentreBack,
                MvpSquadPositionRole.RightMidfielder,
                MvpSquadPositionRole.DefensiveMidfielder,
                MvpSquadPositionRole.CentralMidfielder,
                MvpSquadPositionRole.AttackingMidfielder,
                MvpSquadPositionRole.LeftMidfielder,
                MvpSquadPositionRole.Striker,
                MvpSquadPositionRole.Striker,
            ],
            _ => throw new ArgumentOutOfRangeException(nameof(formation), formation, "Unknown formation."),
        };

    private static int[] MatchPlayersToRoles(
        IReadOnlyList<MvpSquadPlayerProfile> players,
        IReadOnlyList<MvpSquadPositionRole> requiredRoles)
    {
        var playerByRoleIndex = Enumerable.Repeat(-1, requiredRoles.Count).ToArray();

        bool TryAssign(int playerIndex, bool[] visited)
        {
            for (var roleIndex = 0; roleIndex < requiredRoles.Count; roleIndex++)
            {
                if (visited[roleIndex]
                    || !CanPlay(players[playerIndex], requiredRoles[roleIndex]))
                {
                    continue;
                }

                visited[roleIndex] = true;
                if (playerByRoleIndex[roleIndex] < 0
                    || TryAssign(playerByRoleIndex[roleIndex], visited))
                {
                    playerByRoleIndex[roleIndex] = playerIndex;
                    return true;
                }
            }

            return false;
        }

        for (var playerIndex = 0; playerIndex < players.Count; playerIndex++)
        {
            TryAssign(playerIndex, new bool[requiredRoles.Count]);
        }

        return playerByRoleIndex;
    }

    private static bool CanPlay(MvpSquadPlayerProfile player, MvpSquadPositionRole requiredRole)
    {
        if (player.PositionRole is null)
        {
            return player.PositionGroup == requiredRole.ToPositionGroup();
        }

        var role = player.PositionRole.Value;
        return role == requiredRole
            || (requiredRole == MvpSquadPositionRole.CentralMidfielder
                && role is MvpSquadPositionRole.DefensiveMidfielder
                    or MvpSquadPositionRole.AttackingMidfielder)
            || (requiredRole == MvpSquadPositionRole.DefensiveMidfielder
                && role == MvpSquadPositionRole.CentralMidfielder)
            || (requiredRole == MvpSquadPositionRole.AttackingMidfielder
                && role == MvpSquadPositionRole.CentralMidfielder)
            || (requiredRole == MvpSquadPositionRole.RightMidfielder
                && role is MvpSquadPositionRole.RightWinger
                    or MvpSquadPositionRole.RightBack)
            || (requiredRole == MvpSquadPositionRole.LeftMidfielder
                && role is MvpSquadPositionRole.LeftWinger
                    or MvpSquadPositionRole.LeftBack)
            || (requiredRole == MvpSquadPositionRole.RightWinger
                && role == MvpSquadPositionRole.RightMidfielder)
            || (requiredRole == MvpSquadPositionRole.LeftWinger
                && role == MvpSquadPositionRole.LeftMidfielder);
    }
}
