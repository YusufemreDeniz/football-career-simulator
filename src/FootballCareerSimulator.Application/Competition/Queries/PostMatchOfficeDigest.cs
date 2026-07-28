using FootballCareerSimulator.Application.Interaction.Queries;

namespace FootballCareerSimulator.Application.Competition.Queries;

/// <summary>
/// Maç gecesinden ofise dönüş — "şimdi masanda ne var?" özeti.
/// </summary>
public sealed record PostMatchOfficeDigest(
    string BrandTitle,
    string Headline,
    IReadOnlyList<string> BeatLines)
{
    public const string Brand = "Ofiste";

    public static PostMatchOfficeDigest Quiet() =>
        new(Brand, "Ofis sakin — sıradaki güne bak.", Array.Empty<string>());

    public static PostMatchOfficeDigest Compose(
        MatchNightNarrative? narrative,
        DecisionDeskDigest desk,
        bool hasManagedMatch)
    {
        ArgumentNullException.ThrowIfNull(desk);

        if (narrative is null || !hasManagedMatch)
        {
            return Quiet();
        }

        var beats = new List<string>();
        foreach (var line in narrative.AfterWhistleLines.Take(3))
        {
            beats.Add(line);
        }

        if (desk.HasOpenDecision)
        {
            beats.Add(
                desk.IsHardBlocker
                    ? $"Masada zorunlu: {desk.Headline}"
                    : $"Masada: {desk.Headline}");
        }
        else
        {
            beats.Add("Masada yeni zorunlu dosya yok.");
        }

        if (narrative.KickoffLines.Count > 0
            && narrative.KickoffLines.Any(l =>
                l.Contains("söz riski", StringComparison.OrdinalIgnoreCase)))
        {
            beats.Add("Maça söz gerilimiyle girmiştin — sonuçlar ofise yansıdı.");
        }

        var headline = ResolveHeadline(narrative, desk);
        return new PostMatchOfficeDigest(Brand, headline, beats.Take(5).ToArray());
    }

    public string ToStatusMessage()
    {
        var beats = BeatLines.Count == 0
            ? string.Empty
            : "\n" + string.Join("\n", BeatLines.Select(b => "· " + b));
        return $"{BrandTitle}\n{Headline}{beats}";
    }

    public string ToDisplayText() => ToStatusMessage();

    private static string ResolveHeadline(MatchNightNarrative narrative, DecisionDeskDigest desk)
    {
        if (narrative.AfterWhistleLines.Any(l =>
                l.Contains("işten çıkardı", StringComparison.OrdinalIgnoreCase)))
        {
            return "Koltuk gitti — ofis artık senin değil.";
        }

        if (desk.IsHardBlocker)
        {
            return "Ofiste kriz — cevap vermeden ilerleyemezsin.";
        }

        if (narrative.AfterWhistleLines.Any(l =>
                l.Contains("Basın sorusu", StringComparison.OrdinalIgnoreCase)))
        {
            return "Basın ofise üşüştü.";
        }

        if (narrative.AfterWhistleLines.Any(l =>
                l.Contains("Kritik", StringComparison.OrdinalIgnoreCase)
                || l.Contains("İncelemede", StringComparison.OrdinalIgnoreCase)))
        {
            return "Yönetim masası ısınıyor.";
        }

        if (narrative.AfterWhistleLines.Any(l =>
                l.Contains("Sakatlık", StringComparison.OrdinalIgnoreCase)))
        {
            return "Soyunma odası endişeli — sakatlık var.";
        }

        if (desk.HasOpenDecision)
        {
            return "Ofiste iş birikti — Masada bak.";
        }

        if (narrative.OutcomeTone.Contains("kazandın", StringComparison.OrdinalIgnoreCase)
            || narrative.OutcomeTone.Contains("galibiyet", StringComparison.OrdinalIgnoreCase)
            || narrative.OutcomeTone.Contains("domine", StringComparison.OrdinalIgnoreCase)
            || narrative.OutcomeTone.Contains("Üç puan", StringComparison.OrdinalIgnoreCase))
        {
            return "Ofis rahatladı — gece senindi.";
        }

        return "Ofise döndün — geceyi değerlendirdin.";
    }
}
