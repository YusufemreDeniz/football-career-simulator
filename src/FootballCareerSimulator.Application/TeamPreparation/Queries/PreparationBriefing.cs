using FootballCareerSimulator.Application.TrainingPhysicalState.Queries;
using FootballCareerSimulator.Domain.TrainingPhysicalState;

namespace FootballCareerSimulator.Application.TeamPreparation.Queries;

/// <summary>
/// Hazırlık sayfası brifingi — antrenman + taktik + sıradaki maça göre öneri.
/// </summary>
public sealed record PreparationBriefing(
    bool IsEmployed,
    string BrandTitle,
    string Headline,
    string AdviceLine,
    IReadOnlyList<string> BeatLines,
    bool DemandsAttention = false,
    PrepPlanSuggestion? Suggestion = null,
    IReadOnlyList<string>? InjuredPlayerNames = null,
    string? GroundLine = null)
{
    public const string Brand = "Hazırlık Masası";

    public IReadOnlyList<string> InjuredNames =>
        InjuredPlayerNames ?? Array.Empty<string>();

    public bool HasInjuryPressure => InjuredNames.Count > 0;

    public static PreparationBriefing Unemployed() =>
        new(
            IsEmployed: false,
            Brand,
            "Kulüp yok — hazırlık masası kapalı.",
            "Önce işe dön; sonra antrenman ve taktik seçersin.",
            Array.Empty<string>(),
            DemandsAttention: false,
            Suggestion: null,
            InjuredPlayerNames: Array.Empty<string>());

    public static PreparationBriefing Compose(
        ClubTrainingSummaryReadModel training,
        TacticPlanReadModel tactic,
        string tacticModifierLabel,
        string? nextMatchFixtureLine = null,
        int? daysUntilNextMatch = null)
    {
        ArgumentNullException.ThrowIfNull(training);
        ArgumentNullException.ThrowIfNull(tactic);
        ArgumentException.ThrowIfNullOrWhiteSpace(tacticModifierLabel);

        if (training.ClubId is null)
        {
            return Unemployed();
        }

        var fatigue = training.AverageFatigue ?? 0;
        var fitness = training.AverageFitness ?? 0;
        var injured = training.InjuredSlotCount;
        var intensity = training.Intensity;
        var focus = training.Focus;
        var rest = training.RestApproach;

        var injuredNames = training.InjuredNames;
        var beats = new List<string>();
        if (!training.HasPlan)
        {
            beats.Add("Antrenman planı yok — yoğunluk / odak / dinlenme seç.");
        }
        else
        {
            beats.Add(
                $"Antrenman: {NameIntensity(intensity)} / {NameFocus(focus)} · {NameRest(rest)}");
            beats.Add($"XI yorgunluk {fatigue} · fitness {fitness}"
                + (injured > 0 ? $" · sakat {injured}" : string.Empty));
        }

        if (injuredNames.Count > 0)
        {
            beats.Add("Sakat: " + string.Join(", ", injuredNames.Take(3)));
        }

        if (!string.Equals(tactic.FormationName, "yok", StringComparison.Ordinal)
            && !string.Equals(tactic.FormationName, "—", StringComparison.Ordinal))
        {
            beats.Add($"Taktik: {tactic.FormationName} · {tactic.ApproachName} (maç {tacticModifierLabel})");
        }
        else
        {
            beats.Add("Taktik planı yok — formasyon seç.");
        }

        if (!string.IsNullOrWhiteSpace(nextMatchFixtureLine))
        {
            beats.Add($"Sıradaki: {nextMatchFixtureLine}");
        }

        var advice = ResolveAdvice(
            training.HasPlan,
            fatigue,
            fitness,
            injured,
            intensity,
            focus,
            daysUntilNextMatch,
            injuredNames);
        var headline = ResolveHeadline(
            training.HasPlan,
            fatigue,
            injured,
            daysUntilNextMatch,
            advice,
            injuredNames);
        var suggestion = ResolveSuggestion(
            training.HasPlan,
            fatigue,
            fitness,
            injured,
            intensity,
            focus,
            rest,
            daysUntilNextMatch);

        return new PreparationBriefing(
            true,
            Brand,
            headline,
            advice,
            beats,
            DemandsAttention: suggestion is not null,
            Suggestion: suggestion,
            InjuredPlayerNames: injuredNames,
            GroundLine: TrainingGroundDigest.Compose(training, daysUntilNextMatch)?.VoiceLine);
    }

    public string ToDisplayText()
    {
        var beats = BeatLines.Count == 0
            ? string.Empty
            : "\n" + string.Join("\n", BeatLines.Select(b => "· " + b));
        var advice = string.IsNullOrWhiteSpace(AdviceLine)
            ? string.Empty
            : $"\nÖneri: {AdviceLine}";
        var ground = string.IsNullOrWhiteSpace(GroundLine)
            ? string.Empty
            : $"\nSahadan: {GroundLine}";
        return $"{BrandTitle}\n{Headline}{beats}{advice}{ground}";
    }

    private static string ResolveHeadline(
        bool hasPlan,
        int fatigue,
        int injured,
        int? daysUntilNextMatch,
        string advice,
        IReadOnlyList<string> injuredNames)
    {
        if (!hasPlan)
        {
            return "Antrenman planı boş — bu haftayı şekillendir.";
        }

        if (injured > 0)
        {
            var who = injuredNames.Count > 0 ? injuredNames[0] : null;
            if (daysUntilNextMatch is <= 2)
            {
                return who is null
                    ? "Maç yakın ve sakatlık var — temkinli ol."
                    : $"{who} sakat — maç öncesi yükü düşür.";
            }

            return who is null
                ? "Sakatlık var — Toparlanma düşün."
                : $"{who} sakat — Toparlanma uygula.";
        }

        if (fatigue >= 60 && daysUntilNextMatch is <= 2)
        {
            return "Maç yakın, kadro yorgun — toparlanma zamanı.";
        }

        if (fatigue >= 60)
        {
            return "Yorgunluk birikmiş — yükü düşür.";
        }

        if (advice.Contains("Kondisyon", StringComparison.Ordinal))
        {
            return "Fitness düşük — kondisyon penceresi.";
        }

        return "Haftalık hazırlık masası açık.";
    }

    private static string ResolveAdvice(
        bool hasPlan,
        int fatigue,
        int fitness,
        int injured,
        int? intensity,
        int? focus,
        int? daysUntilNextMatch,
        IReadOnlyList<string> injuredNames)
    {
        if (!hasPlan)
        {
            return "Önce Orta yoğunluk + Genel odak ile başla; sonra ince ayar yap.";
        }

        if (injured > 0 && intensity == (int)TrainingIntensity.High)
        {
            return "Sakatlık varken Yoğun riskli — Hafif'e çek.";
        }

        if (injured > 0)
        {
            var who = injuredNames.Count > 0 ? injuredNames[0] + " — " : string.Empty;
            if (focus != (int)TrainingFocus.Recovery || intensity == (int)TrainingIntensity.High)
            {
                return who + "Sakat kadro için Toparlanma + Hafif / Bol dinlenme.";
            }

            return who + "Toparlanma doğru — XI'de sakatı oynatma.";
        }

        if (fatigue >= 55)
        {
            if (focus != (int)TrainingFocus.Recovery || intensity == (int)TrainingIntensity.High)
            {
                return "Yorgunluk yüksek — Toparlanma + Hafif / Bol dinlenme düşün.";
            }

            return "Toparlanma doğru yönde — yorgunluk düşünce normale dön.";
        }

        if (fitness < 50 && daysUntilNextMatch is <= 3)
        {
            return "Maç yaklaşıyor, fitness düşük — Kondisyon odağı.";
        }

        if (daysUntilNextMatch is <= 1 && intensity == (int)TrainingIntensity.High)
        {
            return "Düdük arifesinde Yoğun ağır — yarın için yükü düşür.";
        }

        return "Plan tutarlı — Sıradaki Maç brifingine de göz at.";
    }

    private static PrepPlanSuggestion? ResolveSuggestion(
        bool hasPlan,
        int fatigue,
        int fitness,
        int injured,
        int? intensity,
        int? focus,
        int? rest,
        int? daysUntilNextMatch)
    {
        if (!hasPlan)
        {
            return PrepPlanSuggestion.SeedWeekPlan();
        }

        if (injured > 0 && intensity == (int)TrainingIntensity.High)
        {
            return PrepPlanSuggestion.SoftenLoadPlan(focus);
        }

        if (injured > 0)
        {
            if (focus != (int)TrainingFocus.Recovery || intensity == (int)TrainingIntensity.High)
            {
                return PrepPlanSuggestion.RecoveryPlan();
            }

            return null;
        }

        if (fatigue >= 55)
        {
            if (focus != (int)TrainingFocus.Recovery || intensity == (int)TrainingIntensity.High)
            {
                return PrepPlanSuggestion.RecoveryPlan();
            }

            return null;
        }

        if (fitness < 50 && daysUntilNextMatch is <= 3)
        {
            if (focus == (int)TrainingFocus.Fitness
                && intensity != (int)TrainingIntensity.High
                && rest != (int)RestApproach.Light)
            {
                return null;
            }

            return PrepPlanSuggestion.FitnessPlan();
        }

        if (daysUntilNextMatch is <= 1 && intensity == (int)TrainingIntensity.High)
        {
            return PrepPlanSuggestion.SoftenLoadPlan(focus);
        }

        return null;
    }

    private static string NameIntensity(int? value) => value switch
    {
        (int)TrainingIntensity.Low => "Hafif",
        (int)TrainingIntensity.Medium => "Orta",
        (int)TrainingIntensity.High => "Yoğun",
        _ => "-",
    };

    private static string NameFocus(int? value) => value switch
    {
        (int)TrainingFocus.General => "Dengeli",
        (int)TrainingFocus.Fitness => "Kondisyon",
        (int)TrainingFocus.Recovery => "Toparlanma",
        (int)TrainingFocus.Tactical => "Taktik",
        _ => "-",
    };

    private static string NameRest(int? value) => value switch
    {
        (int)RestApproach.Light => "Az dinlenme",
        (int)RestApproach.Normal => "Normal dinlenme",
        (int)RestApproach.Heavy => "Bol dinlenme",
        _ => "-",
    };
}

/// <summary>
/// Bugün birincil CTA için tek tık hazırlık önerisi (yoğunluk + odak + dinlenme).
/// </summary>
public sealed record PrepPlanSuggestion(
    string ActionCode,
    string ButtonLabel,
    TrainingIntensity Intensity,
    TrainingFocus Focus,
    RestApproach Rest)
{
    public const string SeedWeek = "SeedWeek";
    public const string ApplyRecovery = "ApplyRecovery";
    public const string ApplyFitness = "ApplyFitness";
    public const string SoftenLoad = "SoftenLoad";

    public static PrepPlanSuggestion SeedWeekPlan() =>
        new(SeedWeek, "Haftalık Planı Kur", TrainingIntensity.Medium, TrainingFocus.General, RestApproach.Normal);

    public static PrepPlanSuggestion RecoveryPlan() =>
        new(ApplyRecovery, "Toparlanma Uygula", TrainingIntensity.Low, TrainingFocus.Recovery, RestApproach.Heavy);

    public static PrepPlanSuggestion FitnessPlan() =>
        new(ApplyFitness, "Kondisyon Uygula", TrainingIntensity.Medium, TrainingFocus.Fitness, RestApproach.Normal);

    public static PrepPlanSuggestion SoftenLoadPlan(int? currentFocus) =>
        new(
            SoftenLoad,
            "Yükü Hafiflet",
            TrainingIntensity.Low,
            currentFocus switch
            {
                (int)TrainingFocus.Fitness => TrainingFocus.Fitness,
                (int)TrainingFocus.Recovery => TrainingFocus.Recovery,
                (int)TrainingFocus.Tactical => TrainingFocus.Tactical,
                _ => TrainingFocus.General,
            },
            RestApproach.Heavy);
}
