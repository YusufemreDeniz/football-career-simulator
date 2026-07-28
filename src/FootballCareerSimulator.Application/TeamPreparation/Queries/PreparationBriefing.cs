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
    IReadOnlyList<string> BeatLines)
{
    public const string Brand = "Hazırlık Masası";

    public static PreparationBriefing Unemployed() =>
        new(
            IsEmployed: false,
            Brand,
            "Kulüp yok — hazırlık masası kapalı.",
            "Önce işe dön; sonra antrenman ve taktik seçersin.",
            Array.Empty<string>());

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
            daysUntilNextMatch);
        var headline = ResolveHeadline(training.HasPlan, fatigue, injured, daysUntilNextMatch, advice);

        return new PreparationBriefing(true, Brand, headline, advice, beats);
    }

    public string ToDisplayText()
    {
        var beats = BeatLines.Count == 0
            ? string.Empty
            : "\n" + string.Join("\n", BeatLines.Select(b => "· " + b));
        var advice = string.IsNullOrWhiteSpace(AdviceLine)
            ? string.Empty
            : $"\nÖneri: {AdviceLine}";
        return $"{BrandTitle}\n{Headline}{beats}{advice}";
    }

    private static string ResolveHeadline(
        bool hasPlan,
        int fatigue,
        int injured,
        int? daysUntilNextMatch,
        string advice)
    {
        if (!hasPlan)
        {
            return "Antrenman planı boş — bu haftayı şekillendir.";
        }

        if (injured > 0 && daysUntilNextMatch is <= 2)
        {
            return "Maç yakın ve sakatlık var — temkinli ol.";
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
        int? daysUntilNextMatch)
    {
        if (!hasPlan)
        {
            return "Önce Orta yoğunluk + Genel odak ile başla; sonra ince ayar yap.";
        }

        if (injured > 0 && intensity == (int)TrainingIntensity.High)
        {
            return "Sakatlık varken Yoğun riskli — Hafif'e çek.";
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

    private static string NameIntensity(int? value) => value switch
    {
        (int)TrainingIntensity.Low => "Hafif",
        (int)TrainingIntensity.Medium => "Orta",
        (int)TrainingIntensity.High => "Yoğun",
        _ => "-",
    };

    private static string NameFocus(int? value) => value switch
    {
        (int)TrainingFocus.General => "Genel",
        (int)TrainingFocus.Fitness => "Kondisyon",
        (int)TrainingFocus.Recovery => "Toparlanma",
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
