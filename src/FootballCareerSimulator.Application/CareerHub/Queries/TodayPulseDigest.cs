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
        bool seasonArchivePhase = false,
        InjuryRecoveryPathDigest? recoveryPath = null,
        WeekStoryDigest? weekStory = null,
        WeekMoodDigest? weekMood = null,
        int dayNumber = 0)
    {
        ArgumentNullException.ThrowIfNull(desk);
        ArgumentNullException.ThrowIfNull(match);
        ArgumentNullException.ThrowIfNull(prep);
        ArgumentNullException.ThrowIfNull(league);

        squad ??= SquadCapacityDigest.Unemployed();
        transfer ??= TransferDeskBriefing.Unemployed();
        recoveryPath ??= InjuryRecoveryPathDigest.Clear();
        weekStory ??= WeekStoryDigest.Clear();
        weekMood ??= WeekMoodDigest.Clear();

        var lines = new List<string>();
        if (weekStory.IsActive)
        {
            lines.Add(weekStory.ToPulseLine());
        }
        else if (weekMood.IsActive)
        {
            lines.Add(weekMood.ToPulseLine());
            var calmNote = StaffWhisper.Compose(match, prep, league, weekMood.MoodCode, dayNumber);
            if (!string.IsNullOrWhiteSpace(calmNote))
            {
                lines.Add(calmNote);
            }
        }
        else if (recoveryPath.IsActive)
        {
            lines.Add(
                string.Equals(
                    recoveryPath.CurrentStepCode,
                    InjuryRecoveryPathDigest.StepCleared,
                    StringComparison.Ordinal)
                    ? recoveryPath.Headline
                    : $"İyileşme: {recoveryPath.Headline}");
        }

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
        else if (transfer is { IsEmployed: true, WindowRhythmLine: { } rhythm })
        {
            lines.Add($"Transfer: {rhythm}");
        }

        if (prep.InjuredNames.Count > 0)
        {
            lines.Add("Sakat: " + string.Join(", ", prep.InjuredNames.Take(3)));
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
            var autoSwapHint = match.BeatLines
                .FirstOrDefault(b => b.StartsWith("Sakat XI'de:", StringComparison.Ordinal));
            return (
                FocusMatch,
                autoSwapHint is not null
                    ? autoSwapHint
                    : match.HasInjuryPressure
                        ? "Sakatlık kadroyu düşürdü — sakatsız XI onayla."
                        : "Maç kapıda — kadroyu kilitle.");
        }

        if (match is { HasMatch: true, HasInjuryPressure: true })
        {
            return (FocusMatch, "Sakatlar listede — XI'yi kontrol et / Maç Gününe git.");
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

        if (match.HasMatch && match.IsReadyToKickOff)
        {
            return (FocusMatch, "Hazırsın — düdük için Bugün'de kal.");
        }

        // Forma Sözü / ayrılma kabulü sonrası satış CTA'sı hazırlık-lig sakin nabzına gömülmesin.
        // Maç günü (yukarıdaki düdük) hâlâ önceliklidir.
        if (transfer.NextStep is { } exitStep
            && (string.Equals(exitStep.ReasonCode, TransferNextStep.ReasonSellFringe, StringComparison.Ordinal)
                || string.Equals(exitStep.ReasonCode, TransferNextStep.ReasonPromiseExit, StringComparison.Ordinal)))
        {
            return (FocusTransfer, exitStep.PulseHeadline);
        }

        if (prep is { IsEmployed: true, DemandsAttention: true })
        {
            var prepHeadline = prep.Suggestion?.ActionCode switch
            {
                PrepPlanSuggestion.SeedWeek => "Haftalık plan boş — birincil düğmeyle kur.",
                PrepPlanSuggestion.ApplyRecovery when prep.HasInjuryPressure =>
                    prep.InjuredNames.Count > 0
                        ? $"{prep.InjuredNames[0]} sakat — Toparlanma Uygula."
                        : "Sakatlık var — Toparlanma Uygula.",
                PrepPlanSuggestion.ApplyRecovery => "Kadro yorgun — Toparlanma Uygula.",
                PrepPlanSuggestion.ApplyFitness => "Fitness düşük — Kondisyon Uygula.",
                PrepPlanSuggestion.SoftenLoad => "Yük ağır — Yükü Hafiflet.",
                _ => "Hazırlık Masası çağırıyor.",
            };
            return (FocusPrep, prepHeadline);
        }

        if (league is { HasSeason: true, DemandsAttention: true })
        {
            return (
                FocusLeague,
                league.NextStep?.PulseHeadline ?? "Lig Masası'na bir bak — sıralama konuşuyor.");
        }

        if (transfer.DemandsAttention)
        {
            return (
                FocusTransfer,
                transfer.NextStep?.PulseHeadline
                    ?? "Transfer Masası çağırıyor — pencere ve çıkışa bak.");
        }

        if (squad.IsFull)
        {
            return (FocusSquad, "Kadro dolu — Yer Aç ile slot aç, sonra imza.");
        }

        return (FocusCalm, "Sakin bir gün — nabız dengede.");
    }
}
