namespace FootballCareerSimulator.Application.CareerHub.Queries;

/// <summary>
/// Bugün / Ofis birincil CTA — nabız fokusundan hedef sayfa veya aksiyon.
/// Diyalog/gazeteci açmaz; dikey kesit haftalık döngüyü sıkılaştırır.
/// </summary>
public static class OfficeNextStepGuide
{
    public const string TargetToday = "Today";
    public const string TargetClub = "Club";
    public const string TargetTransfer = "Transfer";
    public const string TargetPrep = "Prep";
    public const string TargetWorld = "World";

    public const string ActionNavigate = "Navigate";
    public const string ActionApproveSelection = "ApproveSelection";
    public const string ActionPlayMatches = "PlayMatches";
    public const string ActionAdvanceDay = "AdvanceDay";

    public static OfficeNextStep? Resolve(string? focusCode)
    {
        if (string.IsNullOrWhiteSpace(focusCode))
        {
            return null;
        }

        return focusCode switch
        {
            TodayPulseDigest.FocusDesk => new OfficeNextStep(
                "Masada'ya Git",
                TargetToday,
                TodayPulseDigest.FocusDesk,
                ActionNavigate),
            TodayPulseDigest.FocusMatch => new OfficeNextStep(
                "Bugün / Sıradaki Maç",
                TargetToday,
                TodayPulseDigest.FocusMatch,
                ActionNavigate),
            TodayPulseDigest.FocusSquad => new OfficeNextStep(
                "Kulüp / Kadro",
                TargetClub,
                TodayPulseDigest.FocusSquad,
                ActionNavigate),
            TodayPulseDigest.FocusTransfer => new OfficeNextStep(
                "Transfer Masası",
                TargetTransfer,
                TodayPulseDigest.FocusTransfer,
                ActionNavigate),
            TodayPulseDigest.FocusPrep => new OfficeNextStep(
                "Hazırlık Masası",
                TargetPrep,
                TodayPulseDigest.FocusPrep,
                ActionNavigate),
            TodayPulseDigest.FocusLeague => new OfficeNextStep(
                "Lig Masası",
                TargetWorld,
                TodayPulseDigest.FocusLeague,
                ActionNavigate),
            _ => null,
        };
    }

    /// <summary>
    /// Nabız + maç/ilerleme durumu — Bugün ekranının canlı birincil CTA'sı.
    /// </summary>
    public static OfficeNextStep? ResolveFromPulse(
        string focusCode,
        bool hasDueUnapprovedMatch,
        bool hasDuePlayableMatch,
        bool canAdvanceDay)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(focusCode);

        if (string.Equals(focusCode, TodayPulseDigest.FocusMatch, StringComparison.Ordinal))
        {
            if (hasDueUnapprovedMatch)
            {
                return new OfficeNextStep(
                    "Kadro Onayla",
                    TargetToday,
                    TodayPulseDigest.FocusMatch,
                    ActionApproveSelection);
            }

            if (hasDuePlayableMatch)
            {
                return new OfficeNextStep(
                    "Bugünün Maçlarını Oyna",
                    TargetToday,
                    TodayPulseDigest.FocusMatch,
                    ActionPlayMatches);
            }
        }

        if (string.Equals(focusCode, TodayPulseDigest.FocusCalm, StringComparison.Ordinal)
            && canAdvanceDay)
        {
            return new OfficeNextStep(
                "1 Gün İlerlet",
                TargetToday,
                TodayPulseDigest.FocusCalm,
                ActionAdvanceDay);
        }

        return Resolve(focusCode);
    }
}

public sealed record OfficeNextStep(
    string ButtonLabel,
    string TargetPageCode,
    string FocusCode,
    string ActionCode);
