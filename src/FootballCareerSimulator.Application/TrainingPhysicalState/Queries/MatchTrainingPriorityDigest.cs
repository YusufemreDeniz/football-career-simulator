namespace FootballCareerSimulator.Application.TrainingPhysicalState.Queries;

using FootballCareerSimulator.Application.Competition.Queries;
using FootballCareerSimulator.Domain.TrainingPhysicalState;

/// <summary>
/// Gerçek XI fiziksel ölçümleri ile sıradaki rakibin tehdidini birleştirerek
/// oyuncuya sıralı, maç-özel antrenman seçenekleri sunar.
/// </summary>
public sealed record MatchTrainingPriorityDigest(
    bool IsAvailable,
    string BrandTitle,
    string Headline,
    string SquadStatusLine,
    string StaffFeedback,
    int DaysUntilMatch,
    bool HasPhysicalData,
    MatchTrainingPriority? RecommendedPriority,
    IReadOnlyList<MatchTrainingPriorityOptionReadModel> Options)
{
    public const string Brand = "Maça Özel Antrenman";

    public MatchTrainingPriorityOptionReadModel? RecommendedOption =>
        Options.FirstOrDefault(option => option.IsRecommended);

    public static MatchTrainingPriorityDigest Compose(
        ClubTrainingSummaryReadModel summary,
        OpponentDossierDigest? opponent,
        int daysUntilMatch,
        bool hasPlannedMatch = true)
    {
        ArgumentNullException.ThrowIfNull(summary);
        if (daysUntilMatch < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(daysUntilMatch),
                daysUntilMatch,
                "Days until match cannot be negative.");
        }

        if (summary.ClubId is null)
        {
            return new MatchTrainingPriorityDigest(
                IsAvailable: false,
                Brand,
                "Kulüp yok — maça özel çalışma kapalı.",
                "Fiziksel ekip raporu bulunmuyor.",
                "Staff: Önce bir kulüpte göreve başla; ardından maç planını kurarız.",
                daysUntilMatch,
                HasPhysicalData: false,
                RecommendedPriority: null,
                Options: Array.Empty<MatchTrainingPriorityOptionReadModel>());
        }

        if (!hasPlannedMatch)
        {
            return new MatchTrainingPriorityDigest(
                IsAvailable: false,
                Brand,
                "Planlı maç yok — maça özel çalışma beklemede.",
                "Fiziksel ekip haftalık planı sürdürüyor.",
                "Staff: Fikstür netleşince rakibe özel öncelikleri açacağız.",
                daysUntilMatch,
                HasPhysicalData: summary.AverageFatigue is not null
                    && summary.AverageFitness is not null,
                RecommendedPriority: null,
                Options: Array.Empty<MatchTrainingPriorityOptionReadModel>());
        }

        var hasPhysicalData = summary.AverageFatigue is not null
            && summary.AverageFitness is not null;
        var context = new ResolverContext(
            Fatigue: Math.Clamp(
                summary.AverageFatigue ?? PlayerPhysicalState.DefaultFatigue,
                PlayerPhysicalState.MinLevel,
                PlayerPhysicalState.MaxLevel),
            Fitness: Math.Clamp(
                summary.AverageFitness ?? PlayerPhysicalState.DefaultFitness,
                PlayerPhysicalState.MinLevel,
                PlayerPhysicalState.MaxLevel),
            Injured: Math.Max(0, summary.InjuredSlotCount),
            Unavailable: Math.Max(0, summary.UnavailableSlotCount),
            DaysUntilMatch: daysUntilMatch,
            HasPhysicalData: hasPhysicalData,
            ThreatKind: opponent?.ThreatKind ?? OpponentThreatKind.Neutral,
            ManagedIsHome: opponent?.ManagedIsHome ?? false,
            StrengthDifference: opponent?.StrengthDifference ?? 0);

        var candidates = Enum.GetValues<MatchTrainingPriority>()
            .Select(priority => new PriorityCandidate(
                priority,
                Score(priority, context),
                BuildEffect(priority, context)))
            .OrderByDescending(candidate => candidate.Score)
            .ThenBy(candidate => (int)candidate.Priority)
            .ToArray();
        var recommended = candidates[0].Priority;
        var options = candidates
            .Select((candidate, index) => BuildOption(
                candidate,
                context,
                rank: index + 1,
                isRecommended: candidate.Priority == recommended))
            .ToArray();

        var recommendedTitle = Title(recommended);
        return new MatchTrainingPriorityDigest(
            IsAvailable: true,
            Brand,
            $"Maça {daysUntilMatch} gün kala öncelik: {recommendedTitle}.",
            BuildSquadStatusLine(summary, context),
            BuildStaffFeedback(recommended, context, opponent is not null),
            daysUntilMatch,
            hasPhysicalData,
            recommended,
            options);
    }

    /// <summary>
    /// Seçilen önceliğin sıradaki maça ait geçici sonucunu döndürür.
    /// Kalıcı state değiştirmez; caller sonucu maç bağlamında saklayıp uygulayabilir.
    /// </summary>
    public static MatchTrainingPrioritySelectionOutcome ResolveSelection(
        MatchTrainingPriority priority,
        MatchTrainingPriorityDigest digest)
    {
        ArgumentNullException.ThrowIfNull(digest);
        return digest.ResolveSelection(priority);
    }

    public MatchTrainingPrioritySelectionOutcome ResolveSelection(
        MatchTrainingPriority priority)
    {
        if (!Enum.IsDefined(priority))
        {
            throw new ArgumentOutOfRangeException(nameof(priority), priority, "Unknown match training priority.");
        }

        if (!IsAvailable)
        {
            throw new InvalidOperationException(
                "A match training priority cannot be selected without an active club.");
        }

        var option = Options.SingleOrDefault(candidate => candidate.Priority == priority)
            ?? throw new InvalidOperationException($"Priority {priority} is not present in this digest.");
        var recommendationNote = option.IsRecommended
            ? " Staff ekibi de bu seçimi öneriyor."
            : " Staff önerisinden farklı bir maç planı seçtin.";
        var outcome = $"{option.Title} seçildi — yalnız sıradaki maç için "
            + $"{FormatSigned(option.TemporaryMatchModifier)} geçici güç; "
            + $"tahmini yorgunluk {FormatSigned(option.ProjectedFatigueDelta)}, "
            + $"sakatlık riski {FormatSigned(option.InjuryRiskDeltaPercent)} yüzde puan."
            + recommendationNote;

        return new MatchTrainingPrioritySelectionOutcome(
            option.Priority,
            option.StableCode,
            option.Title,
            outcome,
            option.BoostLine,
            option.RiskLine,
            option.TemporaryMatchModifier,
            option.ProjectedFatigueDelta,
            option.InjuryRiskDeltaPercent,
            option.IsRecommended);
    }

    private static int Score(MatchTrainingPriority priority, ResolverContext context)
    {
        var score = 20;
        switch (priority)
        {
            case MatchTrainingPriority.Recovery:
                score += context.Fatigue switch
                {
                    >= 70 => 135,
                    >= 60 => 100,
                    >= 55 => 70,
                    >= 50 => 30,
                    _ => 0,
                };
                score += Math.Min(72, context.Injured * 18);
                score += Math.Min(88, context.Unavailable * 22);
                if (context.DaysUntilMatch <= 1 && context.Fatigue >= 45)
                {
                    score += 25;
                }

                break;

            case MatchTrainingPriority.MatchSharpness:
                score += context.Fitness switch
                {
                    < 55 => 100,
                    < 70 => 60,
                    < 80 => 25,
                    _ => 0,
                };
                score += context.DaysUntilMatch is >= 2 and <= 5 ? 20 : 0;
                score -= context.DaysUntilMatch <= 1 ? 15 : 0;
                score -= context.Fatigue >= 60 ? 85 : context.Fatigue >= 55 ? 35 : 0;
                score -= context.Unavailable >= 2 ? 25 : 0;
                break;

            case MatchTrainingPriority.PressResistance:
                score += context.ThreatKind switch
                {
                    OpponentThreatKind.WinningStreak => 105,
                    OpponentThreatKind.SquadQuality => 85,
                    OpponentThreatKind.TopZoneTempo => 80,
                    _ => 0,
                };
                score += context.StrengthDifference >= 7 ? 25
                    : context.StrengthDifference >= 3 ? 12
                    : 0;
                score += context.ManagedIsHome ? 0 : 12;
                score -= context.Fatigue >= 60 ? 75 : context.Fatigue >= 55 ? 30 : 0;
                score -= context.DaysUntilMatch <= 1 ? 10 : 0;
                break;

            case MatchTrainingPriority.DefensiveTransitions:
                score += context.ThreatKind switch
                {
                    OpponentThreatKind.ProductiveAttack => 115,
                    OpponentThreatKind.SquadQuality => 55,
                    OpponentThreatKind.WinningStreak => 40,
                    OpponentThreatKind.TopZoneTempo => 35,
                    _ => 0,
                };
                score += context.StrengthDifference >= 3 ? 15 : 0;
                score += context.ManagedIsHome ? 0 : 8;
                score -= context.Fatigue >= 60 ? 70 : context.Fatigue >= 55 ? 25 : 0;
                score -= context.DaysUntilMatch <= 1 ? 10 : 0;
                break;

            case MatchTrainingPriority.AttackingPatterns:
                score += context.ThreatKind switch
                {
                    OpponentThreatKind.DefensiveResistance => 115,
                    OpponentThreatKind.LosingStreak => 110,
                    OpponentThreatKind.Neutral => 25,
                    _ => 0,
                };
                score += context.StrengthDifference <= -7 ? 25
                    : context.StrengthDifference <= -3 ? 12
                    : 0;
                score += context.ManagedIsHome ? 10 : 0;
                score -= context.Fatigue >= 60 ? 80 : context.Fatigue >= 55 ? 35 : 0;
                score -= context.DaysUntilMatch <= 1 ? 15 : 0;
                break;

            default:
                throw new ArgumentOutOfRangeException(nameof(priority), priority, null);
        }

        return score;
    }

    private static PriorityEffect BuildEffect(
        MatchTrainingPriority priority,
        ResolverContext context)
    {
        var effect = priority switch
        {
            MatchTrainingPriority.Recovery => new PriorityEffect(0, -12, -7),
            MatchTrainingPriority.MatchSharpness => new PriorityEffect(2, 7, 4),
            MatchTrainingPriority.PressResistance => new PriorityEffect(2, 6, 3),
            MatchTrainingPriority.DefensiveTransitions => new PriorityEffect(2, 5, 2),
            MatchTrainingPriority.AttackingPatterns => new PriorityEffect(2, 8, 5),
            _ => throw new ArgumentOutOfRangeException(nameof(priority), priority, null),
        };

        if (priority == MatchTrainingPriority.Recovery)
        {
            var freshnessModifier = context.Fatigue >= 70 || context.Unavailable >= 2
                ? 2
                : context.Fatigue >= 55 || context.Injured > 0
                    ? 1
                    : 0;
            if (context.DaysUntilMatch <= 1 && context.Fatigue >= 45)
            {
                freshnessModifier = Math.Max(1, freshnessModifier);
            }

            return effect with { TemporaryMatchModifier = freshnessModifier };
        }

        if (context.Fatigue >= 65)
        {
            effect = effect with
            {
                TemporaryMatchModifier = Math.Max(0, effect.TemporaryMatchModifier - 1),
                ProjectedFatigueDelta = effect.ProjectedFatigueDelta + 3,
                InjuryRiskDeltaPercent = effect.InjuryRiskDeltaPercent + 5,
            };
        }
        else if (context.Fatigue >= 55)
        {
            effect = effect with
            {
                ProjectedFatigueDelta = effect.ProjectedFatigueDelta + 2,
                InjuryRiskDeltaPercent = effect.InjuryRiskDeltaPercent + 3,
            };
        }

        if (context.Injured > 0)
        {
            effect = effect with
            {
                InjuryRiskDeltaPercent = effect.InjuryRiskDeltaPercent + Math.Min(4, context.Injured),
            };
        }

        if (context.Unavailable >= 2)
        {
            effect = effect with
            {
                TemporaryMatchModifier = Math.Max(0, effect.TemporaryMatchModifier - 1),
                InjuryRiskDeltaPercent = effect.InjuryRiskDeltaPercent + 2,
            };
        }

        if (context.DaysUntilMatch <= 1)
        {
            effect = effect with
            {
                ProjectedFatigueDelta = effect.ProjectedFatigueDelta + 2,
                InjuryRiskDeltaPercent = effect.InjuryRiskDeltaPercent + 2,
            };
        }

        if (context.Fitness < 55)
        {
            effect = effect with
            {
                ProjectedFatigueDelta = effect.ProjectedFatigueDelta + 1,
                InjuryRiskDeltaPercent = effect.InjuryRiskDeltaPercent + 2,
            };
        }

        return effect;
    }

    private static MatchTrainingPriorityOptionReadModel BuildOption(
        PriorityCandidate candidate,
        ResolverContext context,
        int rank,
        bool isRecommended)
    {
        var priority = candidate.Priority;
        var effect = candidate.Effect;
        var loadPlan = SuggestedLoad(priority);
        var risk = RiskTradeOff(priority);
        if (priority != MatchTrainingPriority.Recovery && context.Fatigue >= 65)
        {
            risk += " Ağır bacaklar geçici katkıyı bir kademe düşürüyor.";
        }

        if (!context.HasPhysicalData)
        {
            risk += " XI ölçümü henüz oluşmadığı için sağlık ekibi kontrolü gerekir.";
        }

        return new MatchTrainingPriorityOptionReadModel(
            priority,
            StableCode(priority),
            rank,
            Title(priority),
            Description(priority),
            $"Geçici etki (yalnız sıradaki maç): "
                + $"{FormatSigned(effect.TemporaryMatchModifier)} güç · "
                + $"yorgunluk {FormatSigned(effect.ProjectedFatigueDelta)}.",
            $"Risk: sakatlık olasılığı {FormatSigned(effect.InjuryRiskDeltaPercent)} yüzde puan. {risk}",
            $"Haftalık eşlik: {NameIntensity(loadPlan.Intensity)} · "
                + $"{NameFocus(loadPlan.Focus)} · {NameRest(loadPlan.Rest)}.",
            effect.TemporaryMatchModifier,
            effect.ProjectedFatigueDelta,
            effect.InjuryRiskDeltaPercent,
            loadPlan.Focus,
            loadPlan.Intensity,
            loadPlan.Rest,
            isRecommended);
    }

    private static string BuildSquadStatusLine(
        ClubTrainingSummaryReadModel summary,
        ResolverContext context)
    {
        if (!context.HasPhysicalData)
        {
            return "XI ölçümü henüz oluşmadı"
                + (context.Injured > 0 ? $" · sakat {context.Injured}" : string.Empty)
                + (context.Unavailable > 0 ? $" · kullanılamaz {context.Unavailable}" : string.Empty)
                + ".";
        }

        var planNote = summary.HasPlan ? string.Empty : " · haftalık plan yok";
        return $"Gerçek XI ölçümü: yorgunluk {context.Fatigue} · fitness {context.Fitness}"
            + $" · sakat {context.Injured} · kullanılamaz {context.Unavailable}{planNote}.";
    }

    private static string BuildStaffFeedback(
        MatchTrainingPriority recommended,
        ResolverContext context,
        bool hasOpponent)
    {
        if (recommended == MatchTrainingPriority.Recovery)
        {
            var absence = context.Unavailable > 0
                ? $", {context.Unavailable} oyuncu kullanılamıyor"
                : string.Empty;
            return $"Sağlık ekibi: XI yorgunluğu {context.Fatigue}{absence}; "
                + "önce yükü indir, mevcut sakatları hazır sayma.";
        }

        if (recommended == MatchTrainingPriority.MatchSharpness)
        {
            return $"Performans ekibi: XI fitnessı {context.Fitness}; "
                + "kontrollü maç temposu çalış, son gün gereksiz yük bindirme.";
        }

        if (!hasOpponent)
        {
            return "Yardımcı antrenör: Rakip raporu yok; dengeli hücum tekrarını kısa ve temiz tut.";
        }

        return recommended switch
        {
            MatchTrainingPriority.PressResistance =>
                "Yardımcı antrenör: Rakibin erken baskısını ilk pas ve destek açısıyla kırmaya hazırlan.",
            MatchTrainingPriority.DefensiveTransitions =>
                "Yardımcı antrenör: Top kaybı sonrası ilk beş saniyeyi ve merkez güvenliğini çalış.",
            MatchTrainingPriority.AttackingPatterns =>
                "Yardımcı antrenör: Rakip blok yerleşmeden üçüncü bölge koşularını tekrar et.",
            _ => "Yardımcı antrenör: Maç planını kısa, ölçülü ve rakibe özel tut.",
        };
    }

    private static SuggestedTrainingLoad SuggestedLoad(MatchTrainingPriority priority) =>
        priority switch
        {
            MatchTrainingPriority.Recovery =>
                new(TrainingFocus.Recovery, TrainingIntensity.Low, RestApproach.Heavy),
            MatchTrainingPriority.MatchSharpness =>
                new(TrainingFocus.Fitness, TrainingIntensity.Medium, RestApproach.Normal),
            MatchTrainingPriority.PressResistance =>
                new(TrainingFocus.General, TrainingIntensity.Medium, RestApproach.Normal),
            MatchTrainingPriority.DefensiveTransitions =>
                new(TrainingFocus.General, TrainingIntensity.Medium, RestApproach.Normal),
            MatchTrainingPriority.AttackingPatterns =>
                new(TrainingFocus.General, TrainingIntensity.High, RestApproach.Normal),
            _ => throw new ArgumentOutOfRangeException(nameof(priority), priority, null),
        };

    private static string Title(MatchTrainingPriority priority) => priority switch
    {
        MatchTrainingPriority.Recovery => "Toparlanma",
        MatchTrainingPriority.MatchSharpness => "Maç Keskinliği",
        MatchTrainingPriority.PressResistance => "Baskıdan Çıkış",
        MatchTrainingPriority.DefensiveTransitions => "Geçiş Savunması",
        MatchTrainingPriority.AttackingPatterns => "Hücum Otomasyonları",
        _ => throw new ArgumentOutOfRangeException(nameof(priority), priority, null),
    };

    private static string StableCode(MatchTrainingPriority priority) => priority switch
    {
        MatchTrainingPriority.Recovery => "recovery",
        MatchTrainingPriority.MatchSharpness => "match_sharpness",
        MatchTrainingPriority.PressResistance => "press_resistance",
        MatchTrainingPriority.DefensiveTransitions => "defensive_transitions",
        MatchTrainingPriority.AttackingPatterns => "attacking_patterns",
        _ => throw new ArgumentOutOfRangeException(nameof(priority), priority, null),
    };

    private static string Description(MatchTrainingPriority priority) => priority switch
    {
        MatchTrainingPriority.Recovery =>
            "Maç öncesi yükü indirir; fit oyuncuların tazeliğini korur.",
        MatchTrainingPriority.MatchSharpness =>
            "Kontrollü maç temposuyla düşük fitnessı ve ritmi toparlar.",
        MatchTrainingPriority.PressResistance =>
            "İlk pas, destek açısı ve baskı altında top saklamayı tekrarlar.",
        MatchTrainingPriority.DefensiveTransitions =>
            "Top kaybı sonrası geri koşu ve merkez güvenliğini keskinleştirir.",
        MatchTrainingPriority.AttackingPatterns =>
            "Üçüncü bölge koşuları ve son pas zamanlamasını tekrarlar.",
        _ => throw new ArgumentOutOfRangeException(nameof(priority), priority, null),
    };

    private static string RiskTradeOff(MatchTrainingPriority priority) => priority switch
    {
        MatchTrainingPriority.Recovery =>
            "Mevcut sakatlar iyileşmiş sayılmaz; rakibe özel taktik tekrar azalır.",
        MatchTrainingPriority.MatchSharpness =>
            "Yorgun kadroda veya son günde tempo yükü ters tepebilir.",
        MatchTrainingPriority.PressResistance =>
            "Tekrar yükü bacakları ağırlaştırabilir; hücum sonlandırması daha az çalışılır.",
        MatchTrainingPriority.DefensiveTransitions =>
            "Geri koşu yükü artar; yerleşik hücum tekrarına daha az süre kalır.",
        MatchTrainingPriority.AttackingPatterns =>
            "En yüksek saha yüküdür; top kaybı ve sakatlık riski büyür.",
        _ => throw new ArgumentOutOfRangeException(nameof(priority), priority, null),
    };

    private static string NameIntensity(TrainingIntensity intensity) => intensity switch
    {
        TrainingIntensity.Low => "Hafif",
        TrainingIntensity.Medium => "Orta",
        TrainingIntensity.High => "Yoğun",
        _ => "-",
    };

    private static string NameFocus(TrainingFocus focus) => focus switch
    {
        TrainingFocus.General => "Dengeli",
        TrainingFocus.Fitness => "Kondisyon",
        TrainingFocus.Recovery => "Toparlanma",
        TrainingFocus.Tactical => "Taktik",
        _ => "-",
    };

    private static string NameRest(RestApproach rest) => rest switch
    {
        RestApproach.Light => "Az dinlenme",
        RestApproach.Normal => "Normal dinlenme",
        RestApproach.Heavy => "Bol dinlenme",
        _ => "-",
    };

    private static string FormatSigned(int value) => value switch
    {
        > 0 => $"+{value}",
        0 => "±0",
        _ => value.ToString(),
    };

    private readonly record struct ResolverContext(
        int Fatigue,
        int Fitness,
        int Injured,
        int Unavailable,
        int DaysUntilMatch,
        bool HasPhysicalData,
        OpponentThreatKind ThreatKind,
        bool ManagedIsHome,
        int StrengthDifference);

    private readonly record struct PriorityCandidate(
        MatchTrainingPriority Priority,
        int Score,
        PriorityEffect Effect);

    private sealed record PriorityEffect(
        int TemporaryMatchModifier,
        int ProjectedFatigueDelta,
        int InjuryRiskDeltaPercent);

    private readonly record struct SuggestedTrainingLoad(
        TrainingFocus Focus,
        TrainingIntensity Intensity,
        RestApproach Rest);
}

public sealed record MatchTrainingPriorityOptionReadModel(
    MatchTrainingPriority Priority,
    string StableCode,
    int Rank,
    string Title,
    string Description,
    string BoostLine,
    string RiskLine,
    string SuggestedLoadLine,
    int TemporaryMatchModifier,
    int ProjectedFatigueDelta,
    int InjuryRiskDeltaPercent,
    TrainingFocus SuggestedFocus,
    TrainingIntensity SuggestedIntensity,
    RestApproach SuggestedRest,
    bool IsRecommended)
{
    public int NumericCode => (int)Priority;
}

public sealed record MatchTrainingPrioritySelectionOutcome(
    MatchTrainingPriority Priority,
    string StableCode,
    string Title,
    string OutcomeText,
    string BoostLine,
    string RiskLine,
    int TemporaryMatchModifier,
    int ProjectedFatigueDelta,
    int InjuryRiskDeltaPercent,
    bool WasRecommended);
