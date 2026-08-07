namespace FootballCareerSimulator.Application.Competition.Queries;

using FootballCareerSimulator.Application.Competition.Commands;
using FootballCareerSimulator.Domain.Match;

/// <summary>
/// Maç içindeki skor ve kırmızı kart kırılmalarına tribünün deterministik cevabı.
/// Tepkiler yönetilen kulübün ev/deplasman perspektifinden üretilir.
/// </summary>
public sealed record CrowdReactionDigest(
    string BrandTitle,
    IReadOnlyList<CrowdReactionBeat> MomentBeats,
    string HalfTimeBeat)
{
    public const string Brand = "Tribün Tepkisi";
    private const int HalfTimeMinute = 45;

    public static CrowdReactionDigest Compose(
        bool managedIsHome,
        IReadOnlyList<MatchKeyMomentReadModel>? keyMoments)
    {
        var reactions = new List<CrowdReactionBeat>();
        var homeGoals = 0;
        var awayGoals = 0;
        var halfTimeHomeGoals = 0;
        var halfTimeAwayGoals = 0;

        foreach (var moment in (keyMoments ?? Array.Empty<MatchKeyMomentReadModel>())
                     .OrderBy(moment => moment.Minute))
        {
            var marginBefore = ManagedMargin(managedIsHome, homeGoals, awayGoals);
            if (string.Equals(moment.Kind, nameof(MatchKeyMomentKind.Goal), StringComparison.Ordinal))
            {
                if (moment.IsHomeSide)
                {
                    homeGoals++;
                    if (moment.Minute <= HalfTimeMinute)
                    {
                        halfTimeHomeGoals++;
                    }
                }
                else
                {
                    awayGoals++;
                    if (moment.Minute <= HalfTimeMinute)
                    {
                        halfTimeAwayGoals++;
                    }
                }
            }

            var marginAfter = ManagedMargin(managedIsHome, homeGoals, awayGoals);
            var line = ResolveMomentReaction(
                moment,
                managedIsHome,
                marginBefore,
                marginAfter);
            if (line is not null)
            {
                reactions.Add(new CrowdReactionBeat(
                    moment.Minute,
                    $"{moment.Minute}' Tribün · {line}"));
            }
        }

        return new CrowdReactionDigest(
            Brand,
            reactions.ToArray(),
            $"45' Tribün · {ResolveHalfTimeReaction(
                managedIsHome,
                halfTimeHomeGoals,
                halfTimeAwayGoals)}");
    }

    private static string? ResolveMomentReaction(
        MatchKeyMomentReadModel moment,
        bool managedIsHome,
        int marginBefore,
        int marginAfter)
    {
        var isManagedSide = moment.IsHomeSide == managedIsHome;
        if (string.Equals(moment.Kind, nameof(MatchKeyMomentKind.Goal), StringComparison.Ordinal))
        {
            return ResolveGoalReaction(
                managedIsHome,
                isManagedSide,
                marginBefore,
                marginAfter);
        }

        if (!string.Equals(moment.Kind, nameof(MatchKeyMomentKind.RedCard), StringComparison.Ordinal))
        {
            return null;
        }

        return (managedIsHome, isManagedSide) switch
        {
            (true, true) => "Kırmızı kart tribünü hakeme çevirdi — sabır bitti.",
            (true, false) => "Rakibin kırmızısıyla tribün galibiyet kokusunu aldı.",
            (false, true) => "Ev sahibi tribün kırmızı kartla baskıyı katladı.",
            _ => "Rakibin kırmızısında deplasman köşesi cesaretlendi.",
        };
    }

    private static string ResolveGoalReaction(
        bool managedIsHome,
        bool isManagedSide,
        int marginBefore,
        int marginAfter)
    {
        if (isManagedSide)
        {
            if (marginAfter == 0)
            {
                return managedIsHome
                    ? "Beraberlik golü tribünü ayağa kaldırdı."
                    : "Deplasman köşesi patladı — stat bir an sustu.";
            }

            if (marginBefore <= 0 && marginAfter > 0)
            {
                return managedIsHome
                    ? "Öne geçişte stat koptu."
                    : "Deplasman golü ev tribününü susturdu.";
            }

            return managedIsHome
                ? "Ev tribünü golle birlikte şarkıyı yükseltti."
                : "Deplasman köşesi sesi ele geçirdi.";
        }

        if (marginAfter == 0)
        {
            return managedIsHome
                ? "Konukların beraberlik golü tribünün sesini kesti."
                : "Ev sahibi tribün beraberlikle yeniden oyunda.";
        }

        if (marginBefore >= 0 && marginAfter < 0)
        {
            return managedIsHome
                ? "Ev tribünü bir anda sustu — baskı kulübeye döndü."
                : "Ev sahibi tribün golle birlikte maçı üzerine aldı.";
        }

        return managedIsHome
            ? "Tribünde homurtu büyüyor."
            : "Ev sahibi tribün farkla daha da yükseliyor.";
    }

    private static string ResolveHalfTimeReaction(
        bool managedIsHome,
        int homeGoals,
        int awayGoals)
    {
        var margin = ManagedMargin(managedIsHome, homeGoals, awayGoals);
        return (managedIsHome, Math.Sign(margin)) switch
        {
            (true, 1) => "Ev tribünü devreye ayakta giriyor — üstünlüğü korumanı bekliyor.",
            (true, -1) => "Devre düdüğü uğultuyla karşılandı — sabır daralıyor.",
            (true, _) => "Beraberlikte ses dinmedi — ikinci yarıda kıvılcım bekleniyor.",
            (false, 1) => "Deplasman köşesi ayakta; ev tribünü huzursuz.",
            (false, -1) => "Ev sahibi tribün skoru sahiplenmiş durumda.",
            _ => "Ev sahibi tribün baskıyı artırıyor; deplasman köşesi direniyor.",
        };
    }

    private static int ManagedMargin(bool managedIsHome, int homeGoals, int awayGoals) =>
        managedIsHome ? homeGoals - awayGoals : awayGoals - homeGoals;
}

public sealed record CrowdReactionBeat(int Minute, string Line);
