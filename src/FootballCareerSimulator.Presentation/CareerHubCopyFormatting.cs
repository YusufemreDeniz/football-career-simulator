using FootballCareerSimulator.Application.CareerHub.Queries;
using FootballCareerSimulator.Application.Competition.Queries;
using Godot;
using System.Text.RegularExpressions;

namespace FootballCareerSimulator.Presentation;

internal static class CareerHubCopyFormatting
{
    private static readonly Regex InternalIdentifierPattern =
        new(@"\(?[A-Za-z]*#\d+\)?", RegexOptions.Compiled);

    internal static string ToPlayerFacingText(string text) =>
        Regex.Replace(InternalIdentifierPattern.Replace(text, string.Empty), @"[ ]{2,}", " ").Trim();

    internal static string CompactOfficeText(PostMatchOfficeDigest digest)
    {
        var body = string.IsNullOrWhiteSpace(digest.AdviceLine)
            ? digest.Headline
            : $"{digest.Headline}\n{digest.AdviceLine}";
        if (digest.BeatLines.Count > 0)
        {
            var beats = string.Join(" · ", digest.BeatLines.Take(2));
            body = $"{body}\n{beats}";
        }

        return CompactText(body, maxLines: 3, maxCharacters: 300);
    }

    internal static string CompactOfficeText(CareerResumeDigest digest) =>
        CompactSummary(digest.Headline, digest.AdviceLine);

    internal static string CompactSummary(string headline, string supportingLine) =>
        CompactText(
            string.IsNullOrWhiteSpace(supportingLine)
                ? headline
                : $"{headline}\n{supportingLine}",
            maxLines: 2,
            maxCharacters: 220);

    internal static string CompactText(string text, int maxLines, int maxCharacters = 180)
    {
        var compact = string.Join(
            "\n",
            text.Replace("\r", string.Empty, StringComparison.Ordinal)
                .Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Take(maxLines));
        return compact.Length <= maxCharacters
            ? compact
            : compact[..(maxCharacters - 3)].TrimEnd() + "...";
    }

    internal static void SanitizeScreenText(Node node)
    {
        if (node is Label label)
        {
            label.Text = ToPlayerFacingText(label.Text);
            ShortenSecondaryCopy(label);
        }
        else if (node is Button button)
        {
            button.Text = ToPlayerFacingText(button.Text);
        }
        else if (node is ItemList list)
        {
            for (var index = 0; index < list.ItemCount; index++)
            {
                list.SetItemText(index, ToPlayerFacingText(list.GetItemText(index)));
            }
        }

        foreach (var child in node.GetChildren())
        {
            SanitizeScreenText(child);
        }
    }

    /// <summary>En fazla bu kadar karakterlik ikincil (soluk) açıklama ekranda kalır.</summary>
    internal const int SecondaryCopyMaxCharacters = 90;

    /// <summary>
    /// Soluk renkli ikincil açıklamalar ekranı metin duvarına çeviriyordu. Yalnız ilk cümle
    /// (en fazla <see cref="SecondaryCopyMaxCharacters"/>) görünür; tamamı ipucu olarak kalır.
    /// Birincil metinlere (veri, başlık, uyarı) dokunulmaz.
    /// </summary>
    private static void ShortenSecondaryCopy(Label label)
    {
        if (label.AutowrapMode == TextServer.AutowrapMode.Off
            || !label.HasThemeColorOverride("font_color")
            || label.GetThemeColor("font_color") != CareerUiTheme.InkMuted)
        {
            return;
        }

        var text = label.Text;
        var shortened = ShortenToFirstSentence(text, SecondaryCopyMaxCharacters);
        if (!string.Equals(shortened, text, StringComparison.Ordinal))
        {
            label.TooltipText = text;
            label.Text = shortened;
        }
    }

    internal static string ShortenToFirstSentence(string text, int maxCharacters)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return text;
        }

        var firstLine = text.Replace("\r", string.Empty, StringComparison.Ordinal)
            .Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .FirstOrDefault() ?? string.Empty;
        if (firstLine.Length <= maxCharacters)
        {
            return firstLine;
        }

        var window = firstLine[..maxCharacters];
        var cut = new[] { ". ", " — ", " · ", "; " }
            .Select(separator => window.LastIndexOf(separator, StringComparison.Ordinal))
            .Max();
        if (cut >= maxCharacters / 3)
        {
            return window[..cut].TrimEnd(' ', '·', '—', ';') + ".";
        }

        var space = window.LastIndexOf(' ');
        return (space > 0 ? window[..space] : window).TrimEnd() + "…";
    }

    internal static string TranslateExpectation(string? code) =>
        code switch
        {
            "TitleChallenge" => "Şampiyonluk",
            "UpperHalf" => "Üst yarı",
            "MidTable" => "Orta sıra",
            "LowerHalf" => "Alt yarı",
            "Survival" => "Küme kurtarma",
            _ => code ?? "—",
        };

    internal static string TranslateRisk(string? code) =>
        code switch
        {
            "Secure" => "güvende",
            "Stable" => "stabil",
            "UnderReview" => "incelemede",
            "Critical" => "kritik",
            _ => code ?? "—",
        };

    internal static string TranslateReason(string? code) =>
        code switch
        {
            "WinOnTrack" => "galibiyet (hedefte)",
            "WinBehindExpectation" => "galibiyet (hedefin gerisinde)",
            "DrawOnTrack" => "beraberlik (hedefte)",
            "DrawBehindExpectation" => "beraberlik (hedefin gerisinde)",
            "LossOnTrack" => "mağlubiyet (hedefte)",
            "LossBehindExpectation" => "mağlubiyet (hedefin gerisinde)",
            _ => code ?? "—",
        };
}
