namespace FootballCareerSimulator.Application.Competition.Queries;

/// <summary>
/// Soyunma Odası — maç sonrası kaptanın sesi: sonuca ve farka göre deterministik
/// kapanış satırı. Kovulmada koridor sessizliği ile biter.
/// </summary>
public sealed record CaptainReactionDigest(
    string BrandTitle,
    string VoiceLine)
{
    public const string Brand = "Soyunma Odası";

    /// <summary>
    /// Yönetilen maç yoksa veya sonuç yoksa null döner — sadece gerçek maç geceleri ses verir.
    /// </summary>
    public static CaptainReactionDigest? Compose(
        int? managedGoalMargin,
        bool dismissed)
    {
        if (managedGoalMargin is not int margin)
        {
            return null;
        }

        if (dismissed)
        {
            return new CaptainReactionDigest(
                Brand,
                "Soyunma odası boş — koridor sessiz. Koltuk gitti, söz yok.");
        }

        var line = margin switch
        {
            >= 3 => "Kaptan: “Bugün saha bizimdi — bu oyun herkese lazım.”",
            >= 1 => "Kaptan: “Kazandık ama sallanarak — bu yetmez, sıkı tutun.”",
            0 => "Kaptan: “Puan güven değil, uyarı. İş bitmedi.”",
            >= -2 => "Kaptan: “Son dakikalara bıraktık — bu kayıp bizim.”",
            _ => "Soyunma odasında sessizlik. Kaptan sözü yutmuş — herkes bakıştı.",
        };

        return new CaptainReactionDigest(Brand, line);
    }
}
