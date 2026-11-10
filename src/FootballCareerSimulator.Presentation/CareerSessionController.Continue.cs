using FootballCareerSimulator.Application.CareerHub.Queries;
using FootballCareerSimulator.Application.WorldCalendar.Queries;

namespace FootballCareerSimulator.Presentation;

/// <summary>
/// Ofis "Devam" döngüsü: tek birincil eylemin durumu ve rutin ilerleme yürütmesi.
/// Karar kuralları <see cref="ContinueFlowGuide"/> içinde (Application, test edilebilir).
/// </summary>
public sealed partial class CareerSessionController
{
    /// <summary>Lig sıradaki planlı maç (vadesi gelmemiş olsa da) — ofis kartı ve geri sayım için.</summary>
    public UpcomingManagedMatch? GetUpcomingManagedMatch()
    {
        var currentDay = GetCurrentGameDate().DayNumber;
        var planned = Host.TeamPreparationModule.SelectionQueries.GetNextPlannedManagedFixture(currentDay);
        if (planned is null)
        {
            return null;
        }

        return new UpcomingManagedMatch(
            planned.FixtureId,
            GetClubDisplayName(planned.OpponentClubId),
            planned.IsHome,
            Math.Max(0, planned.ScheduledDayNumber - currentDay),
            planned.IsApproved);
    }

    public ContinueStep BuildContinueStep()
    {
        var currentDay = GetCurrentGameDate().DayNumber;
        var due = GetNextDueManagedFixture(currentDay);
        var blocker = BuildTimeAdvanceBlockerDigest();
        var upcoming = GetUpcomingManagedMatch();
        var hasMandatoryDecision = string.Equals(
            blocker.PrimaryBlockerCode,
            TimeAdvanceBlockerDigest.CodePendingDecision,
            StringComparison.Ordinal);

        return ContinueFlowGuide.Resolve(new ContinueFlowInput(
            HasOpenDecision: hasMandatoryDecision,
            HasDueContinentalMatch: HasDueContinentalMatch(),
            HasDueManagedMatch: due is not null,
            DueMatchApproved: due is { IsApproved: true },
            SeasonTransitionReady: CanTransitionToNextSeason(),
            SeasonArchivePhase: IsSeasonArchivePhase(),
            CanAdvance: blocker.CanAdvance,
            BlockerAdvice: blocker.AdviceLine,
            PrepSuggestion: BuildPreparationBriefing().Suggestion,
            NextOpponentName: upcoming?.OpponentName,
            NextMatchIsHome: upcoming?.IsHome ?? false,
            DaysToNextMatch: upcoming?.DaysUntil));
    }

    /// <summary>
    /// Devam (ilerleme türü): rutin hafta planını uygular, sonra bir sonraki anlamlı noktaya ilerler.
    /// Diğer türler (maç, karar, sezon) ekran tarafında yönlendirilir.
    /// </summary>
    public UiActionResult RunContinueAdvance(ContinueStep step)
    {
        ArgumentNullException.ThrowIfNull(step);
        if (!string.Equals(step.Kind, ContinueFlowGuide.KindAdvance, StringComparison.Ordinal))
        {
            return UiActionResult.Fail("Devam şu an ilerleme yapamaz — önce bekleyen adımı tamamla.");
        }

        string? planMessage = null;
        if (step.AutoApplyPlan is { } plan)
        {
            _trainingIntensity = plan.Intensity;
            _trainingFocus = plan.Focus;
            _trainingRest = plan.Rest;
            var planResult = ApplyWeeklyTrainingPlan();
            if (!planResult.Succeeded)
            {
                return planResult;
            }

            planMessage = planResult.Message;
        }

        var advanced = AdvanceToNextMeaningfulPoint();
        if (planMessage is null || !advanced.Succeeded)
        {
            return advanced;
        }

        return UiActionResult.Ok(
            $"{planMessage}\n{advanced.Message}",
            advanced.Digest,
            narrativeBridgeLine: advanced.NarrativeBridgeLine,
            nextFocusCode: advanced.NextFocusCode);
    }
}

public sealed record UpcomingManagedMatch(
    long FixtureId,
    string OpponentName,
    bool IsHome,
    int DaysUntil,
    bool IsApproved);
