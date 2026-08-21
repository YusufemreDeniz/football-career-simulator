using FootballCareerSimulator.Application.CareerHub.Queries;
using FootballCareerSimulator.Application.Competition.Queries;
using FootballCareerSimulator.Application.TeamPreparation.Queries;
using FootballCareerSimulator.Application.TrainingPhysicalState.Queries;
using FootballCareerSimulator.Application.Transfer.Queries;
using FootballCareerSimulator.Domain.TeamPreparation;
using FootballCareerSimulator.Domain.TrainingPhysicalState;

namespace FootballCareerSimulator.Presentation;

/// <summary>
/// Maç günü akışı, taktik yönetimi ve antrenman komutlarını tek bağlam altında toplayan ince facade.
/// Tüm iş mantığı <see cref="CareerSessionController"/> üzerinde kalır;
/// bu sınıf yalnızca sorumluluk sınırını çizer ve çağrıyı delege eder.
/// </summary>
public sealed class MatchDayController
{
    private readonly CareerSessionController _session;

    public MatchDayController(CareerSessionController session)
    {
        _session = session ?? throw new ArgumentNullException(nameof(session));
    }

    // ── Taktik Bilgi ─────────────────────────────────────────────────────────

    /// <summary>TD not defterini (son üç maç planı) oluşturur.</summary>
    public TechnicalDirectorNotebookDigest BuildNotebook() =>
        _session.BuildTechnicalDirectorNotebook();

    /// <summary>Tekrarlanan örüntü uyarısını oluşturur.</summary>
    public RepeatedPatternWarningDigest BuildPatternWarning() =>
        _session.BuildRepeatedPatternWarning();

    /// <summary>Alternatif plan reçetesini oluşturur.</summary>
    public AlternativePlanPrescriptionDigest BuildAlternativePrescription() =>
        _session.BuildAlternativePlanPrescription();

    /// <summary>Alternatif plan reçetesini uygular.</summary>
    public UiActionResult ApplyAlternativePrescription() =>
        _session.ApplyAlternativePlanPrescription();

    /// <summary>Rakip dosyasını oluşturur.</summary>
    public OpponentDossierDigest? BuildOpponentDossier() =>
        _session.BuildOpponentDossier();

    /// <summary>Eşleşme planını oluşturur.</summary>
    public MatchupPlanDigest? BuildMatchupPlan() =>
        _session.BuildMatchupPlan();

    // ── Maç Günü Sunumu ──────────────────────────────────────────────────────

    /// <summary>Maç günü tempo flash'ını oluşturur.</summary>
    public MatchDayTempoFlash.Flash? BuildTempoFlash() =>
        _session.BuildMatchDayTempoFlash();

    /// <summary>Düdük anı verilerini oluşturur.</summary>
    public MatchKickoffMoment BuildKickoffMoment() =>
        _session.BuildMatchKickoffMoment();

    /// <summary>Maç günü XI şeridini oluşturur.</summary>
    public MatchDayLineupStrip BuildLineupStrip() =>
        _session.BuildMatchDayLineupStrip();

    /// <summary>Kadro uyumu önizlemesini oluşturur.</summary>
    public LineupCompatibilityDigest BuildLineupCompatibility() =>
        _session.BuildLineupCompatibility();

    /// <summary>Kadro kurma tahtasını oluşturur.</summary>
    public SquadSelectionBoardDigest BuildSquadSelectionBoard() =>
        _session.BuildSquadSelectionBoard();

    // ── Devre Arası ──────────────────────────────────────────────────────────

    /// <summary>Yönetilen takımın devre arası özetini oluşturur.</summary>
    public MatchHalfTimeDigest BuildHalfTimeDigest() =>
        _session.BuildManagedHalfTimeDigest();

    // ── Maç Oynama ───────────────────────────────────────────────────────────

    /// <summary>
    /// Vadesi gelmiş tüm maçları oynatır.
    /// </summary>
    /// <param name="managedSecondHalfDelta">Devre arası taktik kararından gelen ikinci yarı güç farkı.</param>
    /// <param name="halfTime">Devre arası skoru (forced half-time için).</param>
    /// <param name="halfTimeDecisionLabel">Devre arası taktik kararı etiketi.</param>
    /// <param name="halfTimeSubstitutionLabel">Devre arası oyuncu değişikliği etiketi.</param>
    public PlayMatchesUiResult PlayDueMatches(
        int managedSecondHalfDelta = 0,
        MatchHalfTimeDigest? halfTime = null,
        string? halfTimeDecisionLabel = null,
        string? halfTimeSubstitutionLabel = null) =>
        _session.PlayDueMatches(
            managedSecondHalfDelta,
            halfTime,
            halfTimeDecisionLabel,
            halfTimeSubstitutionLabel);

    // ── Taktik Ayarları ──────────────────────────────────────────────────────

    /// <summary>Yönetilen kulübün taktik planını getirir.</summary>
    public TacticPlanReadModel GetManagedTacticPlan() =>
        _session.GetManagedTacticPlan();

    /// <summary>Taktik yaklaşımı ayarlar.</summary>
    public UiActionResult SetApproach(TacticalApproach approach) =>
        _session.SetTacticApproach(approach);

    /// <summary>Formasyon ayarlar.</summary>
    public UiActionResult SetFormation(Formation formation) =>
        _session.SetTacticFormation(formation);

    /// <summary>Pres yoğunluğunu ayarlar.</summary>
    public UiActionResult SetPressing(PressingIntensity pressing) =>
        _session.SetTacticPressing(pressing);

    /// <summary>Savunma hattını ayarlar.</summary>
    public UiActionResult SetDefensiveLine(DefensiveLine defensiveLine) =>
        _session.SetTacticDefensiveLine(defensiveLine);

    /// <summary>Pas stilini ayarlar.</summary>
    public UiActionResult SetPassingStyle(PassingStyle passingStyle) =>
        _session.SetTacticPassingStyle(passingStyle);

    /// <summary>Taktik modifier etiketini getirir (örn. "+2").</summary>
    public string GetTacticModifierLabel() =>
        _session.GetManagedTacticModifierLabel();

    // ── Antrenman ────────────────────────────────────────────────────────────

    /// <summary>Haftalık antrenman yoğunluğunu ayarlar.</summary>
    public UiActionResult SetWeeklyTraining(TrainingIntensity intensity) =>
        _session.SetWeeklyTraining(intensity);

    /// <summary>Haftalık antrenman odağını ayarlar.</summary>
    public UiActionResult SetWeeklyTrainingFocus(TrainingFocus focus) =>
        _session.SetWeeklyTrainingFocus(focus);

    /// <summary>Haftalık dinlenme yaklaşımını ayarlar.</summary>
    public UiActionResult SetWeeklyTrainingRest(RestApproach rest) =>
        _session.SetWeeklyTrainingRest(rest);

    /// <summary>Hazırlık brifinginin önerdiği antrenman planını uygular.</summary>
    public UiActionResult ApplySuggestedPreparationPlan() =>
        _session.ApplySuggestedPreparationPlan();

    /// <summary>Maça özel antrenman önceliğini ayarlar.</summary>
    public UiActionResult SelectMatchTrainingPriority(MatchTrainingPriority priority) =>
        _session.SelectMatchTrainingPriority(priority);

    /// <summary>Maça özel antrenman önceliği özetini oluşturur.</summary>
    public MatchTrainingPriorityDigest BuildMatchTrainingPriorityDigest() =>
        _session.BuildMatchTrainingPriorityDigest();

    // ── Maç Sonrası Ofis ─────────────────────────────────────────────────────

    /// <summary>Maç sonrası ofis dönüşü özetini oluşturur.</summary>
    public PostMatchOfficeDigest BuildPostMatchOfficeReturn(PlayMatchesUiResult results) =>
        _session.BuildPostMatchOfficeReturn(results);
}
