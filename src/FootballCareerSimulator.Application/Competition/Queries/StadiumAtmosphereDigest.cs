namespace FootballCareerSimulator.Application.Competition.Queries;

/// <summary>
/// Stadyum — maç gecesinin giriş kapısı: sahaya göre (Ev/Dep) ve lig konumuna göre
/// tribünün deterministik sesi. Sonuç önsezisi vermez, yalnızca sahneyi kurar.
/// </summary>
public sealed record StadiumAtmosphereDigest(
    string BrandTitle,
    string Headline,
    string CrowdLine)
{
    public const string Brand = "Stadyum";

    public static StadiumAtmosphereDigest Compose(
        bool isHome,
        int? managedRank,
        int clubCount)
    {
        var headline = isHome
            ? "Stadyum — ev gecesi."
            : "Stadyum — deplasman gecesi.";

        var hasValidRank = managedRank is int rank
            && clubCount > 0
            && rank >= 1
            && rank <= clubCount;
        var topZone = hasValidRank
            && managedRank <= Math.Max(2, clubCount / 4);
        var bottomZone = hasValidRank
            && managedRank > clubCount - Math.Max(2, clubCount / 5);

        var crowd = (isHome, topZone, bottomZone) switch
        {
            (true, true, _) => "Tribün bu akşam senin adını söylüyor — dolu, coşkulu, zirve havası.",
            (true, false, true) => "Ev tribünü gergin — sıralama konuşuluyor, ilk düdükte sabır sınanacak.",
            (true, false, false) => "Ev tribünü dolu — taraftar oyunun kontrolünü senden bekliyor.",
            (false, true, _) => "Ev sahibi tribün, zirve yarışındaki misafiri bozmak için sesini yükseltiyor.",
            (false, false, true) => "Ev sahibi tribün sende kırılganlık görüyor — ilk baskıyı atlatman gerek.",
            _ => "Deplasman tribünü gürültülü — oyuna erken tutunman gerek.",
        };

        return new StadiumAtmosphereDigest(Brand, headline, crowd);
    }
}
