using FootballCareerSimulator.Domain.TrainingPhysicalState;

namespace FootballCareerSimulator.Application.TrainingPhysicalState.Queries;

public enum CongestionRiskLevel
{
    Normal = 0,
    Heavy = 1,
    Critical = 2,
}

public sealed record FixtureCongestionForecastDigest(
    bool IsAvailable,
    CongestionRiskLevel RiskLevel,
    string RiskBadgeText,
    int MatchesNext7Days,
    int MatchesNext14Days,
    int MatchesNext30Days,
    int MinRestGapDays,
    int AverageFitness,
    int AverageFatigue,
    int InjuredCount,
    int HighRiskStarterCount,
    string SummaryLine,
    string RecommendationLine)
{
    public static FixtureCongestionForecastDigest Unavailable() =>
        new(
            IsAvailable: false,
            RiskLevel: CongestionRiskLevel.Normal,
            RiskBadgeText: "Takvim Sakin",
            MatchesNext7Days: 0,
            MatchesNext14Days: 0,
            MatchesNext30Days: 0,
            MinRestGapDays: 7,
            AverageFitness: PlayerPhysicalState.DefaultFitness,
            AverageFatigue: PlayerPhysicalState.DefaultFatigue,
            InjuredCount: 0,
            HighRiskStarterCount: 0,
            SummaryLine: "Aktif fikstür yoğunluğu verisi yok.",
            RecommendationLine: "Standart haftalık idman planıyla devam edebilirsin.");

    public static FixtureCongestionForecastDigest Compose(
        int currentDayNumber,
        IReadOnlyList<int> upcomingScheduledMatchDays,
        IReadOnlyList<PlayerPhysicalState> squadStates)
    {
        ArgumentNullException.ThrowIfNull(upcomingScheduledMatchDays);
        ArgumentNullException.ThrowIfNull(squadStates);

        var futureDays = upcomingScheduledMatchDays
            .Where(day => day >= currentDayNumber)
            .Distinct()
            .OrderBy(day => day)
            .ToArray();

        var matches7 = futureDays.Count(day => day <= currentDayNumber + 7);
        var matches14 = futureDays.Count(day => day <= currentDayNumber + 14);
        var matches30 = futureDays.Count(day => day <= currentDayNumber + 30);

        var minGap = 7;
        if (futureDays.Length >= 2)
        {
            minGap = int.MaxValue;
            for (var i = 1; i < futureDays.Length; i++)
            {
                var gap = futureDays[i] - futureDays[i - 1];
                if (gap < minGap)
                {
                    minGap = gap;
                }
            }
        }
        else if (futureDays.Length == 1)
        {
            minGap = Math.Max(1, futureDays[0] - currentDayNumber);
        }

        var avgFitness = squadStates.Count == 0
            ? PlayerPhysicalState.DefaultFitness
            : (int)Math.Round(squadStates.Average(s => s.Fitness), MidpointRounding.AwayFromZero);
        var avgFatigue = squadStates.Count == 0
            ? PlayerPhysicalState.DefaultFatigue
            : (int)Math.Round(squadStates.Average(s => s.Fatigue), MidpointRounding.AwayFromZero);
        var injuredCount = squadStates.Count(s => s.IsInjured);
        var highRiskCount = squadStates.Count(s => !s.IsInjured && (s.Fatigue >= 65 || s.Fitness <= 62));

        var riskLevel = ResolveRiskLevel(
            matches7,
            matches14,
            minGap,
            avgFitness,
            avgFatigue,
            injuredCount,
            highRiskCount);

        var badgeText = riskLevel switch
        {
            CongestionRiskLevel.Critical => "🚨 Kritik Fikstür Yükü",
            CongestionRiskLevel.Heavy => "⚠️ Yoğun Takvim",
            _ => "✓ Takvim Dengeli",
        };

        var summaryLine =
            $"7G: {matches7} maç · 14G: {matches14} maç · 30G: {matches30} maç · En kısa ara: {minGap} gün";

        var recommendationLine = riskLevel switch
        {
            CongestionRiskLevel.Critical =>
                $"Sakatlık riski kritik ({highRiskCount} yorgun, {injuredCount} sakat) — Ağır Dinlenme ve rotasyon 11'i önerilir.",
            CongestionRiskLevel.Heavy =>
                $"Takvim sıkışıyor (14 günde {matches14} maç) — Toparlanma odaklı hafif idman ve yedek rotasyonu planla.",
            _ =>
                "Maç aralıkları dengeli — tam kadro ve standart taktik yüklemesiyle çıkabilirsin.",
        };

        return new FixtureCongestionForecastDigest(
            IsAvailable: true,
            RiskLevel: riskLevel,
            RiskBadgeText: badgeText,
            MatchesNext7Days: matches7,
            MatchesNext14Days: matches14,
            MatchesNext30Days: matches30,
            MinRestGapDays: minGap,
            AverageFitness: avgFitness,
            AverageFatigue: avgFatigue,
            InjuredCount: injuredCount,
            HighRiskStarterCount: highRiskCount,
            SummaryLine: summaryLine,
            RecommendationLine: recommendationLine);
    }

    private static CongestionRiskLevel ResolveRiskLevel(
        int matches7,
        int matches14,
        int minGap,
        int avgFitness,
        int avgFatigue,
        int injuredCount,
        int highRiskCount)
    {
        if (matches7 >= 3
            || minGap <= 2
            || avgFatigue >= 65
            || avgFitness <= 60
            || highRiskCount >= 4
            || (matches7 >= 2 && (avgFatigue >= 50 || injuredCount >= 3)))
        {
            return CongestionRiskLevel.Critical;
        }

        if (matches7 >= 2
            || matches14 >= 4
            || minGap <= 4
            || avgFatigue >= 45
            || avgFitness <= 70
            || highRiskCount >= 2
            || injuredCount >= 2)
        {
            return CongestionRiskLevel.Heavy;
        }

        return CongestionRiskLevel.Normal;
    }
}
