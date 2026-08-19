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

    public static FormStreakVerdictDigest? Compose(
        string? enteringMomentumCode,
        int? managedGoalMargin)
    {
        if (managedGoalMargin is null)
        {
            return null;
        }

        if (string.Equals(
                enteringMomentumCode,
                DressingRoomEchoDigest.MomentumWinningStreak,
                StringComparison.Ordinal))
        {
            return managedGoalMargin > 0
                ? new(Brand, "Seri büyüdü — üst üste dördüncü galibiyet.", WinningExtended)
                : new(Brand, "Galibiyet serisi sona erdi — yeni ritim bu sonuçtan kurulacak.", WinningEnded);
        }

        if (!string.Equals(
                enteringMomentumCode,
                DressingRoomEchoDigest.MomentumLosingStreak,
                StringComparison.Ordinal))
        {
            return null;
        }

        if (managedGoalMargin < 0)
        {
            return new(Brand, "Kriz derinleşti — üst üste dördüncü mağlubiyet.", LosingDeepened);
        }

        return managedGoalMargin > 0
            ? new(Brand, "Kriz kırıldı — üç maçlık mağlubiyet serisi galibiyetle bitti.", LosingBroken)
            : new(Brand, "Mağlubiyet serisi durdu — ilk nefes alındı.", LosingBroken);
    }
}
