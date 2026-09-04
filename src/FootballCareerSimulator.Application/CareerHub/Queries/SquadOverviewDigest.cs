using FootballCareerSimulator.Application.TeamPreparation.Queries;

namespace FootballCareerSimulator.Application.CareerHub.Queries;

public sealed record SquadSignalLine(
    string Code,
    string Label,
    int Count,
    bool IsActionable);

/// <summary>
/// Kadro Genel Bakış — birkaç saniyede takım nabzı.
/// </summary>
public sealed record SquadOverviewDigest(
    string Headline,
    string CapacityLine,
    string DepthLine,
    string PitchCaption,
    IReadOnlyList<SquadSignalLine> Signals,
    bool CanCreateTransferNeed)
{
    public const string SignalInjured = "injured";
    public const string SignalFatigue = "fatigue";
    public const string SignalPromise = "promise";
    public const string SignalDepth = "depth";
    public const string SignalOverflow = "overflow";

    public static SquadOverviewDigest Clear() =>
        new(
            "Kadro: kulüp görevi yok.",
            string.Empty,
            string.Empty,
            "Saha için aktif kulüp gerekir.",
            Array.Empty<SquadSignalLine>(),
            false);

    public static SquadOverviewDigest Compose(
        SquadCapacityDigest capacity,
        IReadOnlyList<PlayerManagementLine> players,
        string? scoutNeedLine,
        bool hasDepthGap,
        bool hasMatchBoard,
        bool matchBoardApproved,
        int promiseRiskCount)
    {
        ArgumentNullException.ThrowIfNull(capacity);
        ArgumentNullException.ThrowIfNull(players);

        if (!capacity.IsEmployed)
        {
            return Clear();
        }

        var injured = players.Count(player => player.IsInjured);
        var fatigued = players.Count(player => player.HasFatigueRisk);
        var depthWeak = hasDepthGap;

        var signals = new List<SquadSignalLine>();
        if (injured > 0)
        {
            signals.Add(new SquadSignalLine(SignalInjured, $"{injured} sakat", injured, true));
        }

        if (fatigued > 0)
        {
            signals.Add(new SquadSignalLine(SignalFatigue, $"{fatigued} yüksek yorgunluk", fatigued, true));
        }

        if (promiseRiskCount > 0)
        {
            signals.Add(new SquadSignalLine(
                SignalPromise,
                $"{promiseRiskCount} söz riski",
                promiseRiskCount,
                true));
        }

        if (capacity.IsOverCapacity)
        {
            signals.Add(new SquadSignalLine(
                SignalOverflow,
                $"{capacity.OverflowPlayerIds.Count} taşan sözleşme",
                capacity.OverflowPlayerIds.Count,
                true));
        }

        if (depthWeak)
        {
            signals.Add(new SquadSignalLine(
                SignalDepth,
                string.IsNullOrWhiteSpace(scoutNeedLine) ? "Mevki derinliği zayıf" : scoutNeedLine.Trim(),
                1,
                true));
        }

        if (signals.Count == 0)
        {
            signals.Add(new SquadSignalLine("calm", "Kadro dengeli görünüyor", 0, false));
        }

        var pitchCaption = hasMatchBoard
            ? (matchBoardApproved ? "Sıradaki maç XI (onaylı)" : "Sıradaki maç XI (taslak)")
            : "Kadro sırasına göre önizleme";

        return new SquadOverviewDigest(
            capacity.IsOverCapacity
                ? "Kadro dolu — yer aç veya taşanı çöz."
                : injured > 0 || fatigued > 0
                    ? "Rotasyon ve dinlenme ihtiyacı var."
                    : "Takım hazır — derinliği ve XI'yi kontrol et.",
            capacity.ToDisplayText(),
            string.IsNullOrWhiteSpace(scoutNeedLine) ? "Mevki derinliği: dengeli." : scoutNeedLine.Trim(),
            pitchCaption,
            signals,
            depthWeak || capacity.IsOverCapacity);
    }
}
