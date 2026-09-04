namespace FootballCareerSimulator.Application.CareerHub.Queries;

public sealed record FirstWeekGuideStep(
    string Code,
    string Title,
    string Body,
    string TargetPageCode,
    string ButtonLabel);

/// <summary>
/// Oyuncuyu soyut açıklamalarda tutmak yerine ilk haftanın gerçek ekranlarına
/// götüren kısa, kapatılabilir görev dizisi.
/// </summary>
public sealed record FirstWeekGuideDigest(
    bool IsVisible,
    bool IsComplete,
    int StepNumber,
    int TotalStepCount,
    FirstWeekGuideStep? CurrentStep)
{
    public const string PageToday = "today";
    public const string PageClub = "club";
    public const string PageTransfer = "transfer";
    public const string PagePrep = "prep";
    public const string PageWorld = "world";

    private static readonly FirstWeekGuideStep[] Steps =
    [
        new("read-pulse", "1. Kulübün nabzını oku", "Merkezde Bugün kartını ve sıradaki maçı incele.", PageToday, "Merkezi Göster"),
        new("meet-squad", "2. Takımını tanı", "Kadro ekranında XI, fizik durumu ve sözleşme risklerine bak.", PageClub, "Kadroya Git"),
        new("prepare-match", "3. Maça özel hazırlan", "Staff önerisini okuyup bu maça uygun antrenman önceliğini seç.", PagePrep, "Antrenmana Git"),
        new("shape-plan", "4. Oyun planını kur", "Rakip dosyasına göre formasyon ve yaklaşımını kontrol et.", PagePrep, "Taktiği Aç"),
        new("scan-market", "5. Pazarı tanı", "Scout ekibinin kadrodaki zayıf bölge için sunduğu adaylara göz at.", PageTransfer, "Scout Merkezine Git"),
        new("read-league", "6. Dünyayı oku", "Puan durumu, form ve fikstürden rakiplerinin ritmini gör.", PageWorld, "Lig Merkezine Git"),
        new("enter-match", "7. Kararını sahaya taşı", "Merkeze dön, kadroyu onayla ve maç merkezine gir.", PageToday, "Maça Hazırlan"),
    ];

    public static int TotalSteps => Steps.Length;

    public static FirstWeekGuideDigest Compose(bool enabled, int stepIndex, int daysSinceCareerStart)
    {
        var normalizedStep = Math.Clamp(stepIndex, 0, TotalSteps);
        var inFirstWeek = daysSinceCareerStart is >= 0 and <= 7;
        var complete = normalizedStep >= TotalSteps;
        var visible = enabled && inFirstWeek && !complete;
        return new FirstWeekGuideDigest(
            visible,
            complete,
            Math.Min(normalizedStep + 1, TotalSteps),
            TotalSteps,
            visible ? Steps[normalizedStep] : null);
    }
}
