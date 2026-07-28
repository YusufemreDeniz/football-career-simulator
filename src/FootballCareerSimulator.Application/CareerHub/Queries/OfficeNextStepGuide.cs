namespace FootballCareerSimulator.Application.CareerHub.Queries;

using FootballCareerSimulator.Application.Competition.Queries;
using FootballCareerSimulator.Application.TeamPreparation.Queries;
using FootballCareerSimulator.Application.Transfer.Queries;
using FootballCareerSimulator.Application.WorldCalendar.Queries;

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
    public const string ActionTransitionSeason = "TransitionSeason";
    public const string ActionApplyPrepSuggestion = "ApplyPrepSuggestion";
    public const string ActionSellFringe = "SellFringe";
    public const string ActionOpenTransferWindow = "OpenTransferWindow";

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
            TodayPulseDigest.FocusSeason => new OfficeNextStep(
                "Sezonu Bitir → Yeni Sezon",
                TargetToday,
                TodayPulseDigest.FocusSeason,
                ActionTransitionSeason),
            _ => null,
        };
    }

    /// <summary>
    /// Nabız + maç/ilerleme/engel/sezon/hazırlık/lig/transfer — Bugün ekranının canlı birincil CTA'sı.
    /// </summary>
    public static OfficeNextStep? ResolveFromPulse(
        string focusCode,
        bool hasDueUnapprovedMatch,
        bool hasDuePlayableMatch,
        bool canAdvanceDay,
        string? primaryBlockerCode = null,
        bool seasonTransitionReady = false,
        bool seasonArchivePhase = false,
        PrepPlanSuggestion? prepSuggestion = null,
        LeagueNextStep? leagueNextStep = null,
        TransferNextStep? transferNextStep = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(focusCode);

        if (!canAdvanceDay)
        {
            var unblock = ResolveBlocker(
                primaryBlockerCode,
                hasDueUnapprovedMatch,
                hasDuePlayableMatch);
            if (unblock is not null)
            {
                return unblock;
            }
        }

        if (seasonTransitionReady
            || string.Equals(focusCode, TodayPulseDigest.FocusSeason, StringComparison.Ordinal))
        {
            return new OfficeNextStep(
                seasonArchivePhase ? "Yeni Sezona Geç" : "Sezonu Bitir → Yeni Sezon",
                TargetToday,
                TodayPulseDigest.FocusSeason,
                ActionTransitionSeason);
        }

        if (string.Equals(focusCode, TodayPulseDigest.FocusPrep, StringComparison.Ordinal)
            && prepSuggestion is not null)
        {
            return new OfficeNextStep(
                prepSuggestion.ButtonLabel,
                TargetPrep,
                TodayPulseDigest.FocusPrep,
                ActionApplyPrepSuggestion);
        }

        if (string.Equals(focusCode, TodayPulseDigest.FocusLeague, StringComparison.Ordinal)
            && leagueNextStep is not null)
        {
            return new OfficeNextStep(
                leagueNextStep.ButtonLabel,
                leagueNextStep.TargetPageCode,
                TodayPulseDigest.FocusLeague,
                MapLeagueAction(leagueNextStep.ActionCode));
        }

        if (string.Equals(focusCode, TodayPulseDigest.FocusTransfer, StringComparison.Ordinal)
            && transferNextStep is not null)
        {
            return new OfficeNextStep(
                transferNextStep.ButtonLabel,
                transferNextStep.TargetPageCode,
                TodayPulseDigest.FocusTransfer,
                MapTransferAction(transferNextStep.ActionCode));
        }

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

        if (string.Equals(focusCode, TodayPulseDigest.FocusDesk, StringComparison.Ordinal))
        {
            return new OfficeNextStep(
                "Zorunlu kararı yanıtla",
                TargetToday,
                TodayPulseDigest.FocusDesk,
                ActionNavigate);
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

    private static OfficeNextStep? ResolveBlocker(
        string? primaryBlockerCode,
        bool hasDueUnapprovedMatch,
        bool hasDuePlayableMatch)
    {
        if (string.IsNullOrWhiteSpace(primaryBlockerCode))
        {
            return null;
        }

        if (string.Equals(
                primaryBlockerCode,
                TimeAdvanceBlockerDigest.CodeUnplayedFixtures,
                StringComparison.Ordinal))
        {
            if (hasDueUnapprovedMatch)
            {
                return new OfficeNextStep(
                    "Kadro Onayla (engel)",
                    TargetToday,
                    TodayPulseDigest.FocusMatch,
                    ActionApproveSelection);
            }

            return new OfficeNextStep(
                "Bugünün Maçlarını Oyna (engel)",
                TargetToday,
                TodayPulseDigest.FocusMatch,
                ActionPlayMatches);
        }

        if (string.Equals(
                primaryBlockerCode,
                TimeAdvanceBlockerDigest.CodePendingDecision,
                StringComparison.Ordinal))
        {
            return new OfficeNextStep(
                "Zorunlu kararı yanıtla (engel)",
                TargetToday,
                TodayPulseDigest.FocusDesk,
                ActionNavigate);
        }

        return new OfficeNextStep(
            "İlerleme engelini çöz",
            TargetToday,
            TodayPulseDigest.FocusDesk,
            ActionNavigate);
    }

    private static string MapLeagueAction(string leagueActionCode) =>
        string.Equals(leagueActionCode, LeagueNextStep.ActionAdvanceDay, StringComparison.Ordinal)
            ? ActionAdvanceDay
            : ActionNavigate;

    private static string MapTransferAction(string transferActionCode)
    {
        if (string.Equals(transferActionCode, TransferNextStep.ActionSellFringe, StringComparison.Ordinal))
        {
            return ActionSellFringe;
        }

        if (string.Equals(
                transferActionCode,
                TransferNextStep.ActionOpenTransferWindow,
                StringComparison.Ordinal))
        {
            return ActionOpenTransferWindow;
        }

        return ActionNavigate;
    }
}

public sealed record OfficeNextStep(
    string ButtonLabel,
    string TargetPageCode,
    string FocusCode,
    string ActionCode);
