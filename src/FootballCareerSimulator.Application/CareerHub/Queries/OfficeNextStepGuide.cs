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
    public const string ActionOpenMatchDay = "OpenMatchDay";
    public const string ActionAdvanceDay = "AdvanceDay";
    public const string ActionTransitionSeason = "TransitionSeason";
    public const string ActionApplyPrepSuggestion = "ApplyPrepSuggestion";
    public const string ActionSellFringe = "SellFringe";
    public const string ActionOpenTransferWindow = "OpenTransferWindow";
    public const string ActionScanNeeds = "ScanNeeds";
    public const string ActionPickTarget = "PickTarget";
    public const string ActionStartProcess = "StartProcess";
    public const string ActionAdvanceProcess = "AdvanceProcess";
    public const string ActionAnswerOffers = "AnswerOffers";

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
        TransferNextStep? transferNextStep = null,
        bool hasInjuryPressure = false,
        InjuryRecoveryPathDigest? recoveryPath = null,
        WeekStoryDigest? weekStory = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(focusCode);

        if (!canAdvanceDay)
        {
            var unblock = ResolveBlocker(
                primaryBlockerCode,
                hasDueUnapprovedMatch,
                hasDuePlayableMatch,
                hasInjuryPressure);
            if (unblock is not null)
            {
                // İyileşme 1/3: engel kadro dese bile önce Toparlanma.
                if (recoveryPath is { IsActive: true, CurrentStepCode: InjuryRecoveryPathDigest.StepRecovery }
                    || weekStory is { IsActive: true, PhaseCode: WeekStoryDigest.PhaseInjury })
                {
                    return ResolveRecoveryPathStep(
                        recoveryPath ?? InjuryRecoveryPathDigest.Clear(),
                        canAdvanceDay)
                        ?? ResolveWeekStoryStep(
                            weekStory,
                            hasDueUnapprovedMatch,
                            hasDuePlayableMatch,
                            canAdvanceDay);
                }

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

        if (recoveryPath is { IsActive: true })
        {
            var recoveryStep = ResolveRecoveryPathStep(recoveryPath, canAdvanceDay);
            if (recoveryStep is not null)
            {
                return recoveryStep;
            }
        }

        if (weekStory is { IsActive: true })
        {
            var storyStep = ResolveWeekStoryStep(
                weekStory,
                hasDueUnapprovedMatch,
                hasDuePlayableMatch,
                canAdvanceDay);
            if (storyStep is not null)
            {
                return storyStep;
            }
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
                    hasInjuryPressure ? "Sakatsız Kadro Onayla" : "Kadro Onayla",
                    TargetToday,
                    TodayPulseDigest.FocusMatch,
                    ActionApproveSelection);
            }

            if (hasDuePlayableMatch)
            {
                return new OfficeNextStep(
                    hasInjuryPressure ? "Maç Günü — XI Kontrol" : "Maç Gününe Git",
                    TargetToday,
                    TodayPulseDigest.FocusMatch,
                    ActionOpenMatchDay);
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

    private static OfficeNextStep? ResolveRecoveryPathStep(
        InjuryRecoveryPathDigest recoveryPath,
        bool canAdvanceDay)
    {
        if (!recoveryPath.IsActive)
        {
            return null;
        }

        return recoveryPath.CurrentStepCode switch
        {
            InjuryRecoveryPathDigest.StepRecovery => new OfficeNextStep(
                PrepPlanSuggestion.RecoveryPlan().ButtonLabel,
                TargetPrep,
                TodayPulseDigest.FocusPrep,
                ActionApplyPrepSuggestion),
            InjuryRecoveryPathDigest.StepXi => new OfficeNextStep(
                "Sakatsız Kadro Onayla",
                TargetToday,
                TodayPulseDigest.FocusMatch,
                ActionApproveSelection),
            InjuryRecoveryPathDigest.StepKickoff => new OfficeNextStep(
                "Maç Gününe Git",
                TargetToday,
                TodayPulseDigest.FocusMatch,
                ActionOpenMatchDay),
            InjuryRecoveryPathDigest.StepHold when canAdvanceDay => new OfficeNextStep(
                "1 Gün İlerlet",
                TargetToday,
                TodayPulseDigest.FocusCalm,
                ActionAdvanceDay),
            _ => null,
        };
    }

    /// <summary>
    /// Haftanın Hikâyesi — kariyere dönüşte / Temiz XI / hüküm fazlarında birincil CTA.
    /// </summary>
    public static OfficeNextStep? ResolveWeekStoryStep(
        WeekStoryDigest? weekStory,
        bool hasDueUnapprovedMatch,
        bool hasDuePlayableMatch,
        bool canAdvanceDay)
    {
        if (weekStory is not { IsActive: true })
        {
            return null;
        }

        return weekStory.PhaseCode switch
        {
            WeekStoryDigest.PhaseInjury or WeekStoryDigest.PhaseRecovery => new OfficeNextStep(
                PrepPlanSuggestion.RecoveryPlan().ButtonLabel,
                TargetPrep,
                TodayPulseDigest.FocusPrep,
                ActionApplyPrepSuggestion),
            WeekStoryDigest.PhaseXi => new OfficeNextStep(
                "Sakatsız Kadro Onayla",
                TargetToday,
                TodayPulseDigest.FocusMatch,
                ActionApproveSelection),
            WeekStoryDigest.PhaseKickoff or WeekStoryDigest.PhaseCleanXi or WeekStoryDigest.PhaseCleared
                when hasDueUnapprovedMatch => new OfficeNextStep(
                "Temiz XI Onayla",
                TargetToday,
                TodayPulseDigest.FocusMatch,
                ActionApproveSelection),
            WeekStoryDigest.PhaseKickoff or WeekStoryDigest.PhaseCleanXi or WeekStoryDigest.PhaseCleared
                when hasDuePlayableMatch => new OfficeNextStep(
                "Temiz XI — Maç Gününe Git",
                TargetToday,
                TodayPulseDigest.FocusMatch,
                ActionOpenMatchDay),
            WeekStoryDigest.PhaseVerdict when canAdvanceDay => new OfficeNextStep(
                "Hikâyeyi kapat — 1 Gün İlerlet",
                TargetToday,
                TodayPulseDigest.FocusCalm,
                ActionAdvanceDay),
            WeekStoryDigest.PhaseCleared when canAdvanceDay => new OfficeNextStep(
                "1 Gün İlerlet",
                TargetToday,
                TodayPulseDigest.FocusCalm,
                ActionAdvanceDay),
            _ => null,
        };
    }

    private static OfficeNextStep? ResolveBlocker(
        string? primaryBlockerCode,
        bool hasDueUnapprovedMatch,
        bool hasDuePlayableMatch,
        bool hasInjuryPressure = false)
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
                    hasInjuryPressure ? "Sakatsız Kadro Onayla (engel)" : "Kadro Onayla (engel)",
                    TargetToday,
                    TodayPulseDigest.FocusMatch,
                    ActionApproveSelection);
            }

            return new OfficeNextStep(
                hasInjuryPressure ? "Maç Günü — XI Kontrol (engel)" : "Maç Gününe Git (engel)",
                TargetToday,
                TodayPulseDigest.FocusMatch,
                ActionOpenMatchDay);
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

        if (string.Equals(transferActionCode, TransferNextStep.ActionScanNeeds, StringComparison.Ordinal))
        {
            return ActionScanNeeds;
        }

        if (string.Equals(transferActionCode, TransferNextStep.ActionPickTarget, StringComparison.Ordinal))
        {
            return ActionPickTarget;
        }

        if (string.Equals(transferActionCode, TransferNextStep.ActionStartProcess, StringComparison.Ordinal))
        {
            return ActionStartProcess;
        }

        if (string.Equals(transferActionCode, TransferNextStep.ActionAdvanceProcess, StringComparison.Ordinal))
        {
            return ActionAdvanceProcess;
        }

        if (string.Equals(transferActionCode, TransferNextStep.ActionAnswerOffers, StringComparison.Ordinal))
        {
            return ActionAnswerOffers;
        }

        return ActionNavigate;
    }
}

public sealed record OfficeNextStep(
    string ButtonLabel,
    string TargetPageCode,
    string FocusCode,
    string ActionCode);
