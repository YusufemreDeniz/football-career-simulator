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
            Formation.F4231 =>
            [
                MvpSquadPositionRole.Goalkeeper,
                MvpSquadPositionRole.RightBack,
                MvpSquadPositionRole.CentreBack,
                MvpSquadPositionRole.CentreBack,
                MvpSquadPositionRole.LeftBack,
                MvpSquadPositionRole.DefensiveMidfielder,
                MvpSquadPositionRole.DefensiveMidfielder,
                MvpSquadPositionRole.RightWinger,
                MvpSquadPositionRole.AttackingMidfielder,
                MvpSquadPositionRole.LeftWinger,
                MvpSquadPositionRole.Striker,
            ],
            Formation.F4312 =>
            [
                MvpSquadPositionRole.Goalkeeper,
                MvpSquadPositionRole.RightBack,
                MvpSquadPositionRole.CentreBack,
                MvpSquadPositionRole.CentreBack,
                MvpSquadPositionRole.LeftBack,
                MvpSquadPositionRole.DefensiveMidfielder,
                MvpSquadPositionRole.CentralMidfielder,
                MvpSquadPositionRole.CentralMidfielder,
                MvpSquadPositionRole.AttackingMidfielder,
                MvpSquadPositionRole.Striker,
                MvpSquadPositionRole.Striker,
            ],
            Formation.F4222 =>
            [
                MvpSquadPositionRole.Goalkeeper,
                MvpSquadPositionRole.RightBack,
                MvpSquadPositionRole.CentreBack,
                MvpSquadPositionRole.CentreBack,
                MvpSquadPositionRole.LeftBack,
                MvpSquadPositionRole.DefensiveMidfielder,
                MvpSquadPositionRole.DefensiveMidfielder,
                MvpSquadPositionRole.AttackingMidfielder,
                MvpSquadPositionRole.AttackingMidfielder,
                MvpSquadPositionRole.Striker,
                MvpSquadPositionRole.Striker,
            ],
            Formation.F343 =>
            [
                MvpSquadPositionRole.Goalkeeper,
                MvpSquadPositionRole.CentreBack,
                MvpSquadPositionRole.CentreBack,
                MvpSquadPositionRole.CentreBack,
                MvpSquadPositionRole.RightMidfielder,
                MvpSquadPositionRole.CentralMidfielder,
                MvpSquadPositionRole.CentralMidfielder,
                MvpSquadPositionRole.LeftMidfielder,
                MvpSquadPositionRole.RightWinger,
                MvpSquadPositionRole.Striker,
                MvpSquadPositionRole.LeftWinger,
            ],
            Formation.F3241 =>
            [
                MvpSquadPositionRole.Goalkeeper,
                MvpSquadPositionRole.CentreBack,
                MvpSquadPositionRole.CentreBack,
                MvpSquadPositionRole.CentreBack,
                MvpSquadPositionRole.DefensiveMidfielder,
                MvpSquadPositionRole.DefensiveMidfielder,
                MvpSquadPositionRole.RightWinger,
                MvpSquadPositionRole.AttackingMidfielder,
                MvpSquadPositionRole.AttackingMidfielder,
                MvpSquadPositionRole.LeftWinger,
                MvpSquadPositionRole.Striker,
            ],
            Formation.F3331 =>
            [
                MvpSquadPositionRole.Goalkeeper,
                MvpSquadPositionRole.CentreBack,
                MvpSquadPositionRole.CentreBack,
                MvpSquadPositionRole.CentreBack,
                MvpSquadPositionRole.DefensiveMidfielder,
                MvpSquadPositionRole.CentralMidfielder,
                MvpSquadPositionRole.DefensiveMidfielder,
                MvpSquadPositionRole.RightWinger,
                MvpSquadPositionRole.AttackingMidfielder,
                MvpSquadPositionRole.LeftWinger,
                MvpSquadPositionRole.Striker,
            ],
            Formation.F4141 =>
            [
                MvpSquadPositionRole.Goalkeeper,
                MvpSquadPositionRole.RightBack,
                MvpSquadPositionRole.CentreBack,
                MvpSquadPositionRole.CentreBack,
                MvpSquadPositionRole.LeftBack,
                MvpSquadPositionRole.DefensiveMidfielder,
                MvpSquadPositionRole.RightMidfielder,
                MvpSquadPositionRole.CentralMidfielder,
                MvpSquadPositionRole.CentralMidfielder,
                MvpSquadPositionRole.LeftMidfielder,
                MvpSquadPositionRole.Striker,
            ],
            Formation.F532 =>
            [
                MvpSquadPositionRole.Goalkeeper,
                MvpSquadPositionRole.RightBack,
                MvpSquadPositionRole.CentreBack,
                MvpSquadPositionRole.CentreBack,
                MvpSquadPositionRole.CentreBack,
                MvpSquadPositionRole.LeftBack,
                MvpSquadPositionRole.DefensiveMidfielder,
                MvpSquadPositionRole.CentralMidfielder,
                MvpSquadPositionRole.CentralMidfielder,
                MvpSquadPositionRole.Striker,
                MvpSquadPositionRole.Striker,
            ],
            Formation.F523 =>
            [
                MvpSquadPositionRole.Goalkeeper,
                MvpSquadPositionRole.RightBack,
                MvpSquadPositionRole.CentreBack,
                MvpSquadPositionRole.CentreBack,
                MvpSquadPositionRole.CentreBack,
                MvpSquadPositionRole.LeftBack,
                MvpSquadPositionRole.CentralMidfielder,
                MvpSquadPositionRole.CentralMidfielder,
                MvpSquadPositionRole.RightWinger,
                MvpSquadPositionRole.Striker,
                MvpSquadPositionRole.LeftWinger,
            ],
            Formation.F424 =>
            [
                MvpSquadPositionRole.Goalkeeper,
                MvpSquadPositionRole.RightBack,
                MvpSquadPositionRole.CentreBack,
                MvpSquadPositionRole.CentreBack,
                MvpSquadPositionRole.LeftBack,
                MvpSquadPositionRole.CentralMidfielder,
                MvpSquadPositionRole.CentralMidfielder,
                MvpSquadPositionRole.RightWinger,
                MvpSquadPositionRole.Striker,
                MvpSquadPositionRole.Striker,
                MvpSquadPositionRole.LeftWinger,
            ],
            Formation.F451 =>
            [
                MvpSquadPositionRole.Goalkeeper,
                MvpSquadPositionRole.RightBack,
                MvpSquadPositionRole.CentreBack,
                MvpSquadPositionRole.CentreBack,
                MvpSquadPositionRole.LeftBack,
                MvpSquadPositionRole.RightMidfielder,
                MvpSquadPositionRole.CentralMidfielder,
                MvpSquadPositionRole.DefensiveMidfielder,
                MvpSquadPositionRole.CentralMidfielder,
                MvpSquadPositionRole.LeftMidfielder,
                MvpSquadPositionRole.Striker,
            ],
            _ =>
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

    public static IReadOnlyList<MvpSquadPositionRole> PitchRolesFor(Formation formation) =>
        formation switch
        {
            Formation.F433 =>
            [
                MvpSquadPositionRole.Goalkeeper,
                MvpSquadPositionRole.LeftBack,
                MvpSquadPositionRole.CentreBack,
                MvpSquadPositionRole.CentreBack,
                MvpSquadPositionRole.RightBack,
                MvpSquadPositionRole.CentralMidfielder,
                MvpSquadPositionRole.DefensiveMidfielder,
                MvpSquadPositionRole.CentralMidfielder,
                MvpSquadPositionRole.LeftWinger,
                MvpSquadPositionRole.Striker,
                MvpSquadPositionRole.RightWinger,
            ],
            Formation.F352 =>
            [
                MvpSquadPositionRole.Goalkeeper,
                MvpSquadPositionRole.CentreBack,
                MvpSquadPositionRole.CentreBack,
                MvpSquadPositionRole.CentreBack,
                MvpSquadPositionRole.LeftMidfielder,
                MvpSquadPositionRole.CentralMidfielder,
                MvpSquadPositionRole.DefensiveMidfielder,
                MvpSquadPositionRole.CentralMidfielder,
                MvpSquadPositionRole.RightMidfielder,
                MvpSquadPositionRole.Striker,
                MvpSquadPositionRole.Striker,
            ],
            Formation.F4231 =>
            [
                MvpSquadPositionRole.Goalkeeper,
                MvpSquadPositionRole.LeftBack,
                MvpSquadPositionRole.CentreBack,
                MvpSquadPositionRole.CentreBack,
                MvpSquadPositionRole.RightBack,
                MvpSquadPositionRole.DefensiveMidfielder,
                MvpSquadPositionRole.DefensiveMidfielder,
                MvpSquadPositionRole.LeftWinger,
                MvpSquadPositionRole.AttackingMidfielder,
                MvpSquadPositionRole.RightWinger,
                MvpSquadPositionRole.Striker,
            ],
            Formation.F4312 =>
            [
                MvpSquadPositionRole.Goalkeeper,
                MvpSquadPositionRole.LeftBack,
                MvpSquadPositionRole.CentreBack,
                MvpSquadPositionRole.CentreBack,
                MvpSquadPositionRole.RightBack,
                MvpSquadPositionRole.CentralMidfielder,
                MvpSquadPositionRole.DefensiveMidfielder,
                MvpSquadPositionRole.CentralMidfielder,
                MvpSquadPositionRole.AttackingMidfielder,
                MvpSquadPositionRole.Striker,
                MvpSquadPositionRole.Striker,
            ],
            Formation.F4222 =>
            [
                MvpSquadPositionRole.Goalkeeper,
                MvpSquadPositionRole.LeftBack,
                MvpSquadPositionRole.CentreBack,
                MvpSquadPositionRole.CentreBack,
                MvpSquadPositionRole.RightBack,
                MvpSquadPositionRole.DefensiveMidfielder,
                MvpSquadPositionRole.DefensiveMidfielder,
                MvpSquadPositionRole.AttackingMidfielder,
                MvpSquadPositionRole.AttackingMidfielder,
                MvpSquadPositionRole.Striker,
                MvpSquadPositionRole.Striker,
            ],
            Formation.F343 =>
            [
                MvpSquadPositionRole.Goalkeeper,
                MvpSquadPositionRole.CentreBack,
                MvpSquadPositionRole.CentreBack,
                MvpSquadPositionRole.CentreBack,
                MvpSquadPositionRole.LeftMidfielder,
                MvpSquadPositionRole.CentralMidfielder,
                MvpSquadPositionRole.CentralMidfielder,
                MvpSquadPositionRole.RightMidfielder,
                MvpSquadPositionRole.LeftWinger,
                MvpSquadPositionRole.Striker,
                MvpSquadPositionRole.RightWinger,
            ],
            Formation.F3241 =>
            [
                MvpSquadPositionRole.Goalkeeper,
                MvpSquadPositionRole.CentreBack,
                MvpSquadPositionRole.CentreBack,
                MvpSquadPositionRole.CentreBack,
                MvpSquadPositionRole.DefensiveMidfielder,
                MvpSquadPositionRole.DefensiveMidfielder,
                MvpSquadPositionRole.LeftMidfielder,
                MvpSquadPositionRole.AttackingMidfielder,
                MvpSquadPositionRole.AttackingMidfielder,
                MvpSquadPositionRole.RightMidfielder,
                MvpSquadPositionRole.Striker,
            ],
            Formation.F3331 =>
            [
                MvpSquadPositionRole.Goalkeeper,
                MvpSquadPositionRole.CentreBack,
                MvpSquadPositionRole.CentreBack,
                MvpSquadPositionRole.CentreBack,
                MvpSquadPositionRole.DefensiveMidfielder,
                MvpSquadPositionRole.CentralMidfielder,
                MvpSquadPositionRole.DefensiveMidfielder,
                MvpSquadPositionRole.LeftWinger,
                MvpSquadPositionRole.AttackingMidfielder,
                MvpSquadPositionRole.RightWinger,
                MvpSquadPositionRole.Striker,
            ],
            Formation.F4141 =>
            [
                MvpSquadPositionRole.Goalkeeper,
                MvpSquadPositionRole.LeftBack,
                MvpSquadPositionRole.CentreBack,
                MvpSquadPositionRole.CentreBack,
                MvpSquadPositionRole.RightBack,
                MvpSquadPositionRole.DefensiveMidfielder,
                MvpSquadPositionRole.LeftMidfielder,
                MvpSquadPositionRole.CentralMidfielder,
                MvpSquadPositionRole.CentralMidfielder,
                MvpSquadPositionRole.RightMidfielder,
                MvpSquadPositionRole.Striker,
            ],
            Formation.F532 =>
            [
                MvpSquadPositionRole.Goalkeeper,
                MvpSquadPositionRole.LeftBack,
                MvpSquadPositionRole.CentreBack,
                MvpSquadPositionRole.CentreBack,
                MvpSquadPositionRole.CentreBack,
                MvpSquadPositionRole.RightBack,
                MvpSquadPositionRole.CentralMidfielder,
                MvpSquadPositionRole.DefensiveMidfielder,
                MvpSquadPositionRole.CentralMidfielder,
                MvpSquadPositionRole.Striker,
                MvpSquadPositionRole.Striker,
            ],
            Formation.F523 =>
            [
                MvpSquadPositionRole.Goalkeeper,
                MvpSquadPositionRole.LeftBack,
                MvpSquadPositionRole.CentreBack,
                MvpSquadPositionRole.CentreBack,
                MvpSquadPositionRole.CentreBack,
                MvpSquadPositionRole.RightBack,
                MvpSquadPositionRole.CentralMidfielder,
                MvpSquadPositionRole.CentralMidfielder,
                MvpSquadPositionRole.LeftWinger,
                MvpSquadPositionRole.Striker,
                MvpSquadPositionRole.RightWinger,
            ],
            Formation.F424 =>
            [
                MvpSquadPositionRole.Goalkeeper,
                MvpSquadPositionRole.LeftBack,
                MvpSquadPositionRole.CentreBack,
                MvpSquadPositionRole.CentreBack,
                MvpSquadPositionRole.RightBack,
                MvpSquadPositionRole.CentralMidfielder,
                MvpSquadPositionRole.CentralMidfielder,
                MvpSquadPositionRole.LeftWinger,
                MvpSquadPositionRole.Striker,
                MvpSquadPositionRole.Striker,
                MvpSquadPositionRole.RightWinger,
            ],
            Formation.F451 =>
            [
                MvpSquadPositionRole.Goalkeeper,
                MvpSquadPositionRole.LeftBack,
                MvpSquadPositionRole.CentreBack,
                MvpSquadPositionRole.CentreBack,
                MvpSquadPositionRole.RightBack,
                MvpSquadPositionRole.LeftMidfielder,
                MvpSquadPositionRole.DefensiveMidfielder,
                MvpSquadPositionRole.CentralMidfielder,
                MvpSquadPositionRole.CentralMidfielder,
                MvpSquadPositionRole.RightMidfielder,
                MvpSquadPositionRole.Striker,
            ],
            _ => // 4-4-2
            [
                MvpSquadPositionRole.Goalkeeper,
                MvpSquadPositionRole.LeftBack,
                MvpSquadPositionRole.CentreBack,
                MvpSquadPositionRole.CentreBack,
                MvpSquadPositionRole.RightBack,
                MvpSquadPositionRole.LeftMidfielder,
                MvpSquadPositionRole.CentralMidfielder,
                MvpSquadPositionRole.CentralMidfielder,
                MvpSquadPositionRole.RightMidfielder,
                MvpSquadPositionRole.Striker,
                MvpSquadPositionRole.Striker,
            ],
        };

    /// <summary>
    /// Computes the optimal assignment of the 11 starters to pitch slots (0..10) for the given formation,
    /// maximizing natural position and role fit (Hungarian / Kuhn-Munkres maximum weight matching).
    /// Returns an array of 11 indices, where result[slot] is the index in players that should play at formation pitch slot.
    /// </summary>
    public static int[] ComputeOptimalPitchSlots(
        Formation formation,
        IReadOnlyList<MvpSquadPlayerProfile> players)
    {
        ArgumentNullException.ThrowIfNull(players);
        if (players.Count != 11)
        {
            return Enumerable.Range(0, players.Count).ToArray();
        }

        var targetRoles = PitchRolesFor(formation);
        const int n = 11;
        var cost = new int[n, n];
        for (var i = 0; i < n; i++)
        {
            for (var j = 0; j < n; j++)
            {
                cost[i, j] = EvaluatePitchRoleFit(players[i], targetRoles[j]);
            }
        }

        return HungarianMaxWeight(cost, n);
    }

    public static int EvaluatePitchRoleFit(MvpSquadPlayerProfile player, MvpSquadPositionRole targetRole)
    {
        var isGk = player.PositionRole == MvpSquadPositionRole.Goalkeeper || player.PositionGroup == MvpSquadPositionGroup.Goalkeeper;
        var targetIsGk = targetRole == MvpSquadPositionRole.Goalkeeper;

        if (isGk && targetIsGk)
        {
            return 1000;
        }

        if (isGk != targetIsGk)
        {
            return -10000;
        }

        var role = player.PositionRole;
        if (role == targetRole)
        {
            return 100;
        }

        if (role.HasValue)
        {
            var r = role.Value;
            if ((targetRole is MvpSquadPositionRole.LeftMidfielder && r is MvpSquadPositionRole.LeftBack or MvpSquadPositionRole.LeftWinger)
                || (targetRole is MvpSquadPositionRole.RightMidfielder && r is MvpSquadPositionRole.RightBack or MvpSquadPositionRole.RightWinger))
            {
                return 92;
            }

            if ((targetRole is MvpSquadPositionRole.LeftBack && r is MvpSquadPositionRole.LeftMidfielder or MvpSquadPositionRole.LeftWinger)
                || (targetRole is MvpSquadPositionRole.RightBack && r is MvpSquadPositionRole.RightMidfielder or MvpSquadPositionRole.RightWinger))
            {
                return 88;
            }

            if ((targetRole is MvpSquadPositionRole.LeftWinger && r is MvpSquadPositionRole.LeftMidfielder or MvpSquadPositionRole.Striker)
                || (targetRole is MvpSquadPositionRole.RightWinger && r is MvpSquadPositionRole.RightMidfielder or MvpSquadPositionRole.Striker))
            {
                return 88;
            }

            if (targetRole is MvpSquadPositionRole.CentralMidfielder
                && r is MvpSquadPositionRole.DefensiveMidfielder or MvpSquadPositionRole.AttackingMidfielder)
            {
                return 90;
            }

            if (targetRole is MvpSquadPositionRole.DefensiveMidfielder
                && r is MvpSquadPositionRole.CentralMidfielder)
            {
                return 88;
            }

            if (targetRole is MvpSquadPositionRole.AttackingMidfielder
                && r is MvpSquadPositionRole.CentralMidfielder)
            {
                return 88;
            }

            if (targetRole is MvpSquadPositionRole.CentreBack
                && r is MvpSquadPositionRole.LeftBack or MvpSquadPositionRole.RightBack)
            {
                return 80;
            }

            if (targetRole is MvpSquadPositionRole.LeftBack or MvpSquadPositionRole.RightBack
                && r is MvpSquadPositionRole.CentreBack)
            {
                return 75;
            }

            if (targetRole is MvpSquadPositionRole.Striker
                && r is MvpSquadPositionRole.LeftWinger or MvpSquadPositionRole.RightWinger)
            {
                return 82;
            }

            if (targetRole is MvpSquadPositionRole.Striker
                && r is MvpSquadPositionRole.AttackingMidfielder)
            {
                return 72;
            }
        }

        var targetGroup = targetRole.ToPositionGroup();
        if (player.PositionGroup == targetGroup)
        {
            return 60;
        }

        if ((player.PositionGroup == MvpSquadPositionGroup.Defender && targetGroup == MvpSquadPositionGroup.Midfielder)
            || (player.PositionGroup == MvpSquadPositionGroup.Midfielder && targetGroup == MvpSquadPositionGroup.Defender)
            || (player.PositionGroup == MvpSquadPositionGroup.Midfielder && targetGroup == MvpSquadPositionGroup.Forward)
            || (player.PositionGroup == MvpSquadPositionGroup.Forward && targetGroup == MvpSquadPositionGroup.Midfielder))
        {
            return 30;
        }

        return 10;
    }

    private static int[] HungarianMaxWeight(int[,] cost, int n)
    {
        var maxWeight = int.MinValue;
        for (var i = 0; i < n; i++)
        {
            for (var j = 0; j < n; j++)
            {
                if (cost[i, j] > maxWeight)
                {
                    maxWeight = cost[i, j];
                }
            }
        }

        var a = new int[n + 1, n + 1];
        for (var i = 1; i <= n; i++)
        {
            for (var j = 1; j <= n; j++)
            {
                a[i, j] = maxWeight - cost[i - 1, j - 1];
            }
        }

        var u = new int[n + 1];
        var v = new int[n + 1];
        var p = new int[n + 1];
        var way = new int[n + 1];

        for (var i = 1; i <= n; i++)
        {
            p[0] = i;
            var minv = new int[n + 1];
            Array.Fill(minv, int.MaxValue);
            var used = new bool[n + 1];
            var j0 = 0;

            do
            {
                used[j0] = true;
                int i0 = p[j0], delta = int.MaxValue, j1 = 0;

                for (var j = 1; j <= n; j++)
                {
                    if (!used[j])
                    {
                        var cur = a[i0, j] - u[i0] - v[j];
                        if (cur < minv[j])
                        {
                            minv[j] = cur;
                            way[j] = j0;
                        }
                        if (minv[j] < delta)
                        {
                            delta = minv[j];
                            j1 = j;
                        }
                    }
                }

                for (var j = 0; j <= n; j++)
                {
                    if (used[j])
                    {
                        u[p[j]] += delta;
                        v[j] -= delta;
                    }
                    else
                    {
                        minv[j] -= delta;
                    }
                }

                j0 = j1;
            } while (p[j0] != 0);

            do
            {
                var j1 = way[j0];
                p[j0] = p[j1];
                j0 = j1;
            } while (j0 != 0);
        }

        var result = new int[n];
        for (var j = 1; j <= n; j++)
        {
            result[j - 1] = p[j] - 1;
        }

        return result;
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
