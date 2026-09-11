namespace FootballCareerSimulator.Application.CareerHub.Queries;

public sealed record FanPulsePost(
    string Handle,
    string Tag,
    string Message,
    bool IsPositive);

/// <summary>
/// Oyuncunun sonuçlarının tribün ve spor medyasındaki karşılığını tek, açıklanabilir
/// bir puana dönüştürür. Aynı kariyer durumu her zaman aynı akışı üretir.
/// </summary>
public sealed record FanPulseDigest(
    int ApprovalRating,
    string Trend,
    string Headline,
    IReadOnlyList<FanPulsePost> Posts)
{
    public static FanPulseDigest Empty { get; } = new(
        50,
        "SABİT",
        "Taraftar nabzı kulüp göreviyle başlayacak.",
        Array.Empty<FanPulsePost>());

    public static FanPulseDigest Compose(
        string clubName,
        int rank,
        int clubCount,
        int recentPoints,
        int recentMatchCount,
        int goalDifference,
        int lastGoalMargin,
        int managerReputation,
        int boardConfidence)
    {
        if (string.IsNullOrWhiteSpace(clubName) || clubCount <= 0 || rank <= 0)
        {
            return Empty;
        }

        var formMaximum = Math.Max(1, recentMatchCount * 3);
        var formScore = (int)Math.Round((recentPoints / (double)formMaximum) * 30d) - 15;
        var rankScore = (int)Math.Round(((clubCount - rank) / (double)Math.Max(1, clubCount - 1)) * 24d) - 6;
        var rating = Math.Clamp(
            50
            + formScore
            + rankScore
            + Math.Clamp(goalDifference, -10, 10)
            + Math.Clamp(lastGoalMargin * 4, -12, 12)
            + Math.Clamp((boardConfidence - 50) / 5, -10, 10)
            + Math.Clamp((managerReputation - 50) / 10, -5, 5),
            5,
            99);

        var trend = lastGoalMargin switch
        {
            > 0 => "YÜKSELİYOR",
            < 0 => "DÜŞÜYOR",
            _ => "SABİT",
        };
        var tag = "#" + new string(clubName.Where(char.IsLetterOrDigit).ToArray());
        var resultPost = lastGoalMargin switch
        {
            >= 3 => "Bu takım bugün rakibine nefes aldırmadı. Aynı cesareti istiyoruz!",
            > 0 => "Üç puan geldi; tribün yeniden takıma inanıyor.",
            0 => "Mücadele var ama son paslarda daha fazlasını bekliyoruz.",
            <= -3 => "Bu sonuç kabul edilemez. Bir sonraki maçta güçlü tepki şart.",
            _ => "Skor can yakıyor; şimdi menajerin cevabını görmek istiyoruz.",
        };
        var tablePost = rank <= Math.Max(2, clubCount / 4)
            ? $"{rank}. sıradayız. Hedef artık sadece iyi futbol değil, kupa yarışı."
            : rank > clubCount - Math.Max(2, clubCount / 5)
                ? $"{rank}. sıra alarm veriyor. Her puan sezonun kaderini değiştirecek."
                : $"{rank}. sıra yetmez; takımın yukarı tırmanacak gücü var.";
        var pressurePost = rating >= 70
            ? "Şehir hocanın arkasında. Bu enerji soyunma odasına taşınmalı."
            : rating <= 35
                ? "Krediler azalıyor. Yönetim ve tribün artık net bir oyun görmek istiyor."
                : "Sabır sürüyor ama sahadaki kimlik daha belirgin olmalı.";

        return new FanPulseDigest(
            rating,
            trend,
            rating switch
            {
                >= 80 => "Şehir takımın etrafında kenetlendi.",
                >= 60 => "Tribünde güven ve beklenti birlikte yükseliyor.",
                >= 40 => "Taraftar desteği temkinli; sıradaki sonuç belirleyici.",
                _ => "Kamuoyu baskısı kritik seviyede.",
            },
            new[]
            {
                new FanPulsePost("@TribunNabzi", tag, resultPost, lastGoalMargin >= 0),
                new FanPulsePost("@LigMasasi", tag, tablePost, rank <= clubCount / 2),
                new FanPulsePost("@SehirSpor", tag, pressurePost, rating >= 50),
            });
    }
}
