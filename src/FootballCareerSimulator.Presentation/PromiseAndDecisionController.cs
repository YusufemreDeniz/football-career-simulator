namespace FootballCareerSimulator.Presentation;

/// <summary>
/// Söz (Promise) ve Karar (Decision) komutlarını tek bağlam altında toplayan ince facade.
/// Tüm iş mantığı <see cref="CareerSessionController"/> üzerinde kalır;
/// bu sınıf yalnızca sorumluluk sınırını çizer ve çağrıyı delege eder.
/// </summary>
public sealed class PromiseAndDecisionController
{
    private readonly CareerSessionController _session;

    public PromiseAndDecisionController(CareerSessionController session)
    {
        _session = session ?? throw new ArgumentNullException(nameof(session));
    }

    // ── Söz Verme ────────────────────────────────────────────────────────────

    /// <summary>Kadroda en eski oyuncuya ilk 11 fırsatı sözü verir.</summary>
    public UiActionResult PromiseStartingOpportunityToOldestPlayer() =>
        _session.PromiseStartingOpportunityToOldestSquadPlayer();

    /// <summary>Belirtilen oyuncuya ilk 11 fırsatı sözü verir.</summary>
    public UiActionResult PromiseStartingOpportunityToPlayer(long playerId) =>
        _session.PromiseStartingOpportunityToPlayer(playerId);

    /// <summary>Kadroda en eski oyuncuya oyun süresi sözü verir.</summary>
    public UiActionResult PromisePlayingTimeToOldestPlayer() =>
        _session.PromisePlayingTimeToOldestSquadPlayer();

    /// <summary>Belirtilen oyuncuya oyun süresi sözü verir.</summary>
    public UiActionResult PromisePlayingTimeToPlayer(long playerId) =>
        _session.PromisePlayingTimeToPlayer(playerId);

    // ── Karar Açma ───────────────────────────────────────────────────────────

    /// <summary>En eski kadro oyuncusu için forma süresi kararı açar.</summary>
    public UiActionResult OpenPlayingTimeDecisionForOldest() =>
        _session.OpenPlayingTimeDecisionForOldestSquadPlayer();

    /// <summary>Belirtilen oyuncu için forma süresi kararı açar.</summary>
    public UiActionResult OpenPlayingTimeDecisionForPlayer(long playerId) =>
        _session.OpenPlayingTimeDecisionForPlayer(playerId);

    /// <summary>En eski kadro oyuncusu için ilk 11 fırsatı kararı açar.</summary>
    public UiActionResult OpenStartingOpportunityDecisionForOldest() =>
        _session.OpenStartingOpportunityDecisionForOldestSquadPlayer();

    /// <summary>Belirtilen oyuncu için ilk 11 fırsatı kararı açar.</summary>
    public UiActionResult OpenStartingOpportunityDecisionForPlayer(long playerId) =>
        _session.OpenStartingOpportunityDecisionForPlayer(playerId);

    /// <summary>En eski kadro oyuncusu için transfer isteği kararı açar.</summary>
    public UiActionResult OpenTransferDecisionForOldest() =>
        _session.OpenTransferDecisionForOldestSquadPlayer();

    /// <summary>Belirtilen oyuncu için transfer isteği kararı açar.</summary>
    public UiActionResult OpenTransferDecisionForPlayer(long playerId) =>
        _session.OpenTransferDecisionForPlayer(playerId);

    /// <summary>En eski kadro oyuncusu için disiplin görüşmesi kararı açar.</summary>
    public UiActionResult OpenDisciplineDecisionForOldest() =>
        _session.OpenDisciplineDecisionForOldestSquadPlayer();

    /// <summary>Belirtilen oyuncu için disiplin görüşmesi kararı açar.</summary>
    public UiActionResult OpenDisciplineDecisionForPlayer(long playerId) =>
        _session.OpenDisciplineDecisionForPlayer(playerId);

    /// <summary>Yönetim talebi kararı açar.</summary>
    public UiActionResult OpenBoardDemandDecision() =>
        _session.OpenBoardDemandDecision();

    /// <summary>En eski kadro oyuncusu için kritik basın sorusu kararı açar.</summary>
    public UiActionResult OpenPressQuestionDecisionForOldest() =>
        _session.OpenPressQuestionDecisionForOldestSquadPlayer();

    // ── Karar Yanıtlama ──────────────────────────────────────────────────────

    /// <summary>
    /// Bekleyen en eski kararı söz ver / reddet seçeneğiyle yanıtlar.
    /// </summary>
    /// <param name="grantPromise">
    /// <see langword="true"/> → söz ver / kabul et;
    /// <see langword="false"/> → reddet.
    /// </param>
    public UiActionResult AnswerOldestPending(bool grantPromise) =>
        _session.AnswerOldestPendingDecision(grantPromise);

    /// <summary>
    /// Bekleyen en eski kararı belirli bir seçenek kodu ile yanıtlar.
    /// </summary>
    public UiActionResult AnswerOldestPendingWithOption(string optionCode) =>
        _session.AnswerOldestPendingWithOption(optionCode);
}
