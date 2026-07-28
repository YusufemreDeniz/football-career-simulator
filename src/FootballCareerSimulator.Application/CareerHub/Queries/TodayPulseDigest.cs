using FootballCareerSimulator.Application.Competition.Queries;
using FootballCareerSimulator.Application.Interaction.Queries;
using FootballCareerSimulator.Application.TeamPreparation.Queries;
using FootballCareerSimulator.Application.Transfer.Queries;

namespace FootballCareerSimulator.Application.CareerHub.Queries;

/// <summary>
/// Bugün nabzı — ofis, maç, hazırlık, lig, kadro, transfer ve sezon döngüsünü bağlar.
/// </summary>
public sealed record TodayPulseDigest(
    string BrandTitle,
    string Headline,
    string PrimaryFocusCode,
    IReadOnlyList<string> PulseLines)
{
    public const string Brand = "Günün Nabzı";

    public const string FocusDesk = "Desk";
    public const string FocusMatch = "Match";
    public const string FocusPrep = "Prep";
    public const string FocusLeague = "League";
    public const string FocusSquad = "Squad";
    public const string FocusTransfer = "Transfer";
    public const string FocusSeason = "Season";
    public const string FocusCalm = "Calm";

    public static TodayPulseDigest Compose(
        DecisionDeskDigest desk,
        PreMatchBriefing match,
        PreparationBriefing prep,
        LeagueWorldBriefing league,
        SquadCapacityDigest? squad = null,
        TransferDeskBriefing? transfer = null,
        bool seasonTransitionReady = false,
        bool seasonArchivePhase = false)
    {
        ArgumentNullException.ThrowIfNull(desk);
        ArgumentNullException.ThrowIfNull(match);
        ArgumentNullException.ThrowIfNull(prep);
        ArgumentNullException.ThrowIfNull(league);

        squad ??= SquadCapacityDigest.Unemployed();
        transfer ??= TransferDeskBriefing.Unemployed();

        var lines = new List<string>();
        if (desk.HasOpenDecision)
        {
            lines.Add($"Masada: {desk.Headline}");
        }

        if (squad.IsOverCapacity)
        {
            lines.Add($"Kadro: {squad.Headline}");
        }
        else if (squad.IsFull)
        {
            lines.Add($"Kadro: {squad.Headline}");
        }

        if (seasonTransitionReady)
        {
            lines.Add(seasonArchivePhase
                ? "Sezon: arşiv + yeni sezon hazır"
                : "Sezon: kapanışa hazır — tüm maçlar işlendi");
        }

        if (transfer is { IsEmployed: true, DemandsAttention: true })
        {
            lines.Add($"Transfer: {transfer.Headline}");
        }

        if (match.HasMatch)
        {
            lines.Add($"Maç: {match.Headline}");
        }

        if (prep.IsEmployed)
        {
            lines.Add($"Hazırlık: {prep.Headline}");
        }

        if (league.HasSeason)
        {
            lines.Add($"Lig: {league.Headline}");
        }

        var (focus, headline) = ResolveFocus(
            desk,
            match,
            prep,
            league,
            squad,
            transfer,
            seasonTransitionReady,
            seasonArchivePhase);
        return new TodayPulseDigest(Brand, headline, focus, lines.Take(4).ToArray());
    }

    public string ToDisplayText()
    {
        var pulses = PulseLines.Count == 0
            ? string.Empty
            : "\n" + string.Join("\n", PulseLines.Select(l => "· " + l));
        return $"{BrandTitle}\n{Headline}{pulses}";
    }

    private static (string Focus, string Headline) ResolveFocus(
        DecisionDeskDigest desk,
        PreMatchBriefing match,
        PreparationBriefing prep,
        LeagueWorldBriefing league,
        SquadCapacityDigest squad,
        TransferDeskBriefing transfer,
        bool seasonTransitionReady,
        bool seasonArchivePhase)
    {
        if (desk.IsHardBlocker)
        {
            return (FocusDesk, "Önce Masada — zaman burada kilitli.");
        }

        if (desk.HasOpenDecision)
        {
            return (FocusDesk, "Masada iş var — ofisi temizle.");
        }

        if (squad.IsOverCapacity)
        {
            return (FocusSquad, "Kadro taştı — Kulüp'te Yer Aç veya Taşanı Kadroya Al.");
        }

        if (match.HasMatch && !match.IsReadyToKickOff)
        {
            return (FocusMatch, "Maç kapıda — kadroyu kilitle.");
        }

        if (match is { HasMatch: true, HasPromiseRisk: true })
        {
            return (FocusMatch, "Söz riski var — XI↔Yedek düşün.");
        }

        // Çok sezon dikey kesiti: sezon geçişi sakin günde kaybolmasın.
        if (seasonTransitionReady)
        {
            return (
                FocusSeason,
                seasonArchivePhase
                    ? "Sezon arşive hazır — yeni sezona geç."
                    : "Sezon bitti — kapanışı tamamla.");
        }

        if (prep.IsEmployed
            && (prep.Headline.Contains("yorgun", StringComparison.OrdinalIgnoreCase)
                || prep.Headline.Contains("sakat", StringComparison.OrdinalIgnoreCase)
                || prep.Headline.Contains("boş", StringComparison.OrdinalIgnoreCase)))
        {
            return (FocusPrep, "Hazırlık Masası çağırıyor.");
        }

        if (league.HasSeason
            && (league.Headline.Contains("Alt sıralar", StringComparison.Ordinal)
                || league.Headline.Contains("Zirvedesin", StringComparison.Ordinal)))
        {
            return (FocusLeague, "Lig Masası'na bir bak — sıralama konuşuyor.");
        }

        if (match.HasMatch && match.IsReadyToKickOff)
        {
            return (FocusMatch, "Hazırsın — düdük için Bugün'de kal.");
        }

        if (transfer.DemandsAttention)
        {
            return (FocusTransfer, "Transfer Masası çağırıyor — pencere ve çıkışa bak.");
        }

        if (squad.IsFull)
        {
            return (FocusSquad, "Kadro dolu — Yer Aç ile slot aç, sonra imza.");
        }

        return (FocusCalm, "Sakin bir gün — nabız dengede.");
    }
}
