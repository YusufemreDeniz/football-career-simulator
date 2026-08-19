using FootballCareerSimulator.Application.CareerHub.Queries;
using FootballCareerSimulator.Application.TeamPreparation.Queries;

namespace FootballCareerSimulator.Application.Competition.Queries;

/// <summary>
/// Düdük anı — maç nabzı: düdük sonrası sahaya giriş özeti.
/// Ofisteki tempo flash'ını ve maç brifingi köprüsünü maçın başlangıcına taşır.
/// </summary>
public sealed record MatchKickoffMoment(
    bool HasMatch,
    bool IsReadyToKickOff,
    string BrandTitle,
    string Headline,
    string FixtureLine,
    IReadOnlyList<string> BeatLines)
{
    public const string Brand = "Maç Nabzı";

    public static MatchKickoffMoment Clear() =>
        new(false, false, Brand, "Düdük kapalı.", string.Empty, Array.Empty<string>());

    /// <summary>
    /// Düdük anı köprüsü — brifingden "maça böyle girdin" satırları + tempo flash'ı.
    /// </summary>
    public static MatchKickoffMoment Compose(
        PreMatchBriefing briefing,
        MatchDayTempoFlash.Flash? tempoFlash = null,
        string? formMomentumCode = null)
    {
        ArgumentNullException.ThrowIfNull(briefing);
        if (!briefing.HasMatch)
        {
            return Clear();
        }

        var beats = new List<string>();
        if (tempoFlash is not null)
        {
            beats.Add(tempoFlash.BeatLine);
        }

        if (string.Equals(
                formMomentumCode,
                DressingRoomEchoDigest.MomentumWinningStreak,
                StringComparison.Ordinal))
        {
            beats.Add("Üç maçlık galibiyet serisi sahaya çıktı — dördüncü zafer peşinde.");
        }
        else if (string.Equals(
                     formMomentumCode,
                     DressingRoomEchoDigest.MomentumLosingStreak,
                     StringComparison.Ordinal))
        {
            beats.Add("Üç maçlık mağlubiyet serisi — bugün kırılma maçı.");
        }

        foreach (var line in briefing.ToKickoffBridgeLines())
        {
            if (string.Equals(line, briefing.FixtureLine, StringComparison.Ordinal))
            {
                continue;
            }

            beats.Add(line);
        }

        return new MatchKickoffMoment(
            HasMatch: true,
            IsReadyToKickOff: briefing.IsReadyToKickOff,
            Brand,
            briefing.IsReadyToKickOff
                ? "Düdük çaldı — maç başladı."
                : "Düdük kapalı — kadro önce.",
            briefing.FixtureLine,
            beats);
    }

    public string ToDisplayText()
    {
        if (!HasMatch)
        {
            return $"{BrandTitle}\n{Headline}";
        }

        var beats = BeatLines.Count == 0
            ? string.Empty
            : "\n" + string.Join("\n", BeatLines.Select(b => "· " + b));
        return $"{BrandTitle}\n{Headline}\n{FixtureLine}{beats}";
    }
}
