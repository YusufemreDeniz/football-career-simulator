namespace FootballCareerSimulator.Application.Competition.Queries;

/// <summary>
/// Maç öncesindeki üç maçlık form geriliminin düdük sonrasındaki anlatısal hükmü.
/// Yeni state tutmaz; giriş momentumu ve yönetilen maç sonucundan deterministik türetilir.
/// </summary>
public sealed record FormStreakVerdictDigest(
    string BrandTitle,
    string Headline,
    string VerdictCode)
{
    public const string Brand = "FORM HÜKMÜ";
    public const string WinningExtended = "WinningExtended";
    public const string WinningEnded = "WinningEnded";
    public const string LosingDeepened = "LosingDeepened";
    public const string LosingBroken = "LosingBroken";
    public const string StreakBattleWon = "StreakBattleWon";
    public const string StreakBattleDrawn = "StreakBattleDrawn";
    public const string StreakBattleLost = "StreakBattleLost";
    public const string RivalStreakStopped = "RivalStreakStopped";
    public const string RivalStreakExtended = "RivalStreakExtended";

    public static FormStreakVerdictDigest? Compose(
        string? enteringMomentumCode,
        int? managedGoalMargin,
        int enteringMomentumLength = 0,
        int opponentWinningStreakLength = 0)
    {
        if (managedGoalMargin is null)
        {
            return null;
        }

        var managedWinningStreak = string.Equals(
            enteringMomentumCode,
            DressingRoomEchoDigest.MomentumWinningStreak,
            StringComparison.Ordinal);
        var managedLosingStreak = string.Equals(
            enteringMomentumCode,
            DressingRoomEchoDigest.MomentumLosingStreak,
            StringComparison.Ordinal);
        if (opponentWinningStreakLength >= 3)
        {
            return ComposeAgainstWinningOpponent(
                managedWinningStreak,
                managedLosingStreak,
                Math.Max(3, enteringMomentumLength),
                opponentWinningStreakLength,
                managedGoalMargin.Value);
        }

        if (managedWinningStreak)
        {
            var nextLength = Math.Max(3, enteringMomentumLength) + 1;
            return managedGoalMargin > 0
                ? new(Brand, $"Seri büyüdü — üst üste {nextLength}. galibiyet.", WinningExtended)
                : new(Brand, "Galibiyet serisi sona erdi — yeni ritim bu sonuçtan kurulacak.", WinningEnded);
        }

        if (!managedLosingStreak)
        {
            return null;
        }

        if (managedGoalMargin < 0)
        {
            var nextLength = Math.Max(3, enteringMomentumLength) + 1;
            return new(Brand, $"Kriz derinleşti — üst üste {nextLength}. mağlubiyet.", LosingDeepened);
        }

        return managedGoalMargin > 0
            ? new(Brand, "Kriz kırıldı — üç maçlık mağlubiyet serisi galibiyetle bitti.", LosingBroken)
            : new(Brand, "Mağlubiyet serisi durdu — ilk nefes alındı.", LosingBroken);
    }

    private static FormStreakVerdictDigest ComposeAgainstWinningOpponent(
        bool managedWinningStreak,
        bool managedLosingStreak,
        int managedStreakLength,
        int opponentStreakLength,
        int managedGoalMargin)
    {
        if (managedGoalMargin > 0)
        {
            if (managedWinningStreak)
            {
                return new(
                    Brand,
                    $"Seri savaşını kazandın — {managedStreakLength} maçlık serin"
                    + $" {managedStreakLength + 1} galibiyete çıktı,"
                    + $" rakibin {opponentStreakLength} maçlık serisi bitti.",
                    StreakBattleWon);
            }

            if (managedLosingStreak)
            {
                return new(
                    Brand,
                    $"Kriz kırıldı — {managedStreakLength} yenilgin ve rakibin"
                    + $" {opponentStreakLength} galibiyeti aynı gece sona erdi.",
                    LosingBroken);
            }

            return new(
                Brand,
                $"Rakibin {opponentStreakLength} maçlık galibiyet serisini bitirdin.",
                RivalStreakStopped);
        }

        if (managedGoalMargin == 0)
        {
            if (managedWinningStreak)
            {
                return new(
                    Brand,
                    $"Seri savaşı kilitlendi — senin {managedStreakLength} maçlık,"
                    + $" rakibin {opponentStreakLength} maçlık galibiyet serisi sona erdi.",
                    StreakBattleDrawn);
            }

            if (managedLosingStreak)
            {
                return new(
                    Brand,
                    $"Krizde ilk nefes — {managedStreakLength} yenilgin durdu,"
                    + $" rakibin {opponentStreakLength} galibiyetlik serisi de bitti.",
                    LosingBroken);
            }

            return new(
                Brand,
                $"Rakibin {opponentStreakLength} maçlık galibiyet serisini beraberlikle durdurdun.",
                RivalStreakStopped);
        }

        if (managedWinningStreak)
        {
            return new(
                Brand,
                $"Seri savaşını rakip kazandı — {managedStreakLength} maçlık serin bitti,"
                + $" rakibinki {opponentStreakLength + 1} galibiyete çıktı.",
                StreakBattleLost);
        }

        if (managedLosingStreak)
        {
            return new(
                Brand,
                $"Kriz derinleşti — {managedStreakLength + 1}. mağlubiyet;"
                + $" rakibin serisi {opponentStreakLength + 1} galibiyete çıktı.",
                LosingDeepened);
        }

        return new(
            Brand,
            $"Rakibin galibiyet serisi {opponentStreakLength + 1} maça çıktı.",
            RivalStreakExtended);
    }
}
