using FootballCareerSimulator.Application.CareerHub.Queries;
using FootballCareerSimulator.Application.Competition.Queries;
using FootballCareerSimulator.Application.WorldCalendar.Queries;

namespace FootballCareerSimulator.Presentation;

/// <summary>
/// Sezon yönetimi, kayıt/yükleme ve planlama dönemi komutlarını tek bağlam altında toplayan ince facade.
/// Tüm iş mantığı <see cref="CareerSessionController"/> üzerinde kalır;
/// bu sınıf yalnızca sorumluluk sınırını çizer ve çağrıyı delege eder.
/// </summary>
public sealed class SeasonAndSaveController
{
    private readonly CareerSessionController _session;

    public SeasonAndSaveController(CareerSessionController session)
    {
        _session = session ?? throw new ArgumentNullException(nameof(session));
    }

    // ── Lig Kurulumu ─────────────────────────────────────────────────────────

    /// <summary>Lig ve sezon başlangıç koşullarını doğrular, eksikse tamamlar.</summary>
    public UiActionResult EnsureLeagueReady() =>
        _session.EnsureLeagueReady();

    /// <summary>İş teklifi oluşturur (işsiz menajer için).</summary>
    public UiActionResult GenerateJobOffer() =>
        _session.GenerateJobOffer();

    /// <summary>Bekleyen iş teklifini kabul eder.</summary>
    public UiActionResult AcceptJobOffer() =>
        _session.AcceptJobOffer();

    // ── Sezon Geçişi ─────────────────────────────────────────────────────────

    /// <summary>Mevcut sezonu tamamlar (emeklilik, yeni oyuncu nesli, sözleşme yenileme).</summary>
    public UiActionResult CompleteSeason() =>
        _session.CompleteSeason();

    /// <summary>Tamamlanmış sezonu arşive taşır.</summary>
    public UiActionResult ArchiveSeason() =>
        _session.ArchiveSeason();

    /// <summary>Yeni sezonu başlatır ve fikstürleri planlar.</summary>
    public UiActionResult StartNewSeason() =>
        _session.StartNewSeason();

    /// <summary>Bir sonraki sezona hazır geçiş yapılıp yapılamayacağını kontrol eder.</summary>
    public bool CanTransitionToNextSeason() =>
        _session.CanTransitionToNextSeason();

    /// <summary>
    /// Sezon geçişini tek adımda yönetir:
    /// sezon tamamlama → arşivleme → yeni sezon başlatma.
    /// </summary>
    public UiActionResult TransitionToNextSeason() =>
        _session.TransitionToNextSeason();

    /// <summary>Sezonun arşiv fazında olup olmadığını döner.</summary>
    public bool IsSeasonArchivePhase() =>
        _session.IsSeasonArchivePhase();

    // ── Zaman İlerleme ───────────────────────────────────────────────────────

    /// <summary>Oyun zamanını belirtilen gün sayısı kadar ilerletir.</summary>
    public UiActionResult AdvanceDays(int dayCount) =>
        _session.AdvanceDays(dayCount);

    /// <summary>Zaman ilerleme engelleyicilerinin özetini oluşturur.</summary>
    public TimeAdvanceBlockerDigest BuildTimeAdvanceBlockerDigest() =>
        _session.BuildTimeAdvanceBlockerDigest();

    /// <summary>Aktif engelleyici özetini biçimlendirir.</summary>
    public string FormatActiveBlockerSummary() =>
        _session.FormatActiveBlockerSummary();

    // ── Kayıt / Yükleme ──────────────────────────────────────────────────────

    /// <summary>Kayıt masası özetini oluşturur (yol, zaman damgası, sezon durumu).</summary>
    public SaveDeskDigest BuildSaveDeskDigest() =>
        _session.BuildSaveDeskDigest();

    /// <summary>Kariyeri diske kaydeder.</summary>
    public UiActionResult SaveGame() =>
        _session.SaveGame();

    /// <summary>Kariyeri diskten yükler.</summary>
    public UiActionResult LoadGame() =>
        _session.LoadGame();

    /// <summary>Kayıt dosyasının var olup olmadığını döner.</summary>
    public bool SaveFileExists() =>
        _session.SaveFileExists();

    /// <summary>Yükleme sonrası kariyer özeti (nabız doğrulama).</summary>
    public CareerResumeDigest? LastCareerResume =>
        _session.LastCareerResume;

    /// <summary>Kariyer devam özetini oluşturur.</summary>
    public CareerResumeDigest BuildCareerResumeDigest(bool wasMigrated) =>
        _session.BuildCareerResumeDigest(wasMigrated);

    // ── Planlama Dönemi ──────────────────────────────────────────────────────

    /// <summary>Planlama dönemini açar.</summary>
    public UiActionResult OpenPlanningPeriod() =>
        _session.OpenPlanningPeriod();

    /// <summary>Planlama dönemini tamamlar.</summary>
    public UiActionResult CompletePlanningPeriod() =>
        _session.CompletePlanningPeriod();

    // ── Kariyer Sonrası Özetler ───────────────────────────────────────────────

    /// <summary>Kariyer miras özetini oluşturur (görev rekoru, geliştirilen oyuncular vb.).</summary>
    public CareerLegacyDigest BuildCareerLegacyDigest() =>
        _session.BuildCareerLegacyDigest();

    /// <summary>Kayıt masası kariyer geçmişini oluşturur.</summary>
    public SaveDeskDigest BuildSaveDesk() =>
        _session.BuildSaveDeskDigest();
}
