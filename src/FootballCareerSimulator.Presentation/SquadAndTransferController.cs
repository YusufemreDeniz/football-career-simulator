namespace FootballCareerSimulator.Presentation;

/// <summary>
/// Kadro yönetimi ve transfer pipeline komutlarını tek bağlam altında toplayan ince facade.
/// Tüm iş mantığı <see cref="CareerSessionController"/> üzerinde kalır;
/// bu sınıf yalnızca sorumluluk sınırını çizer ve çağrıyı delege eder.
/// </summary>
public sealed class SquadAndTransferController
{
    private readonly CareerSessionController _session;

    public SquadAndTransferController(CareerSessionController session)
    {
        _session = session ?? throw new ArgumentNullException(nameof(session));
    }

    // ── Kadro Onay & Değişim ─────────────────────────────────────────────────

    /// <summary>Sıradaki vadesi gelmiş maç için varsayılan kadroyu onaylar.</summary>
    public UiActionResult ApproveDefaultSelection() =>
        _session.ApproveDefaultSelectionForNextDueMatch();

    /// <summary>Son ilk 11 oyuncusunu ilk yedekle değiştirir.</summary>
    public UiActionResult SwapLastStarterWithFirstBench() =>
        _session.SwapLastStarterWithFirstBenchForNextDueMatch();

    /// <summary>Belirtilen slot indekslerine göre ilk 11 ile yedek arasında değişim yapar.</summary>
    public UiActionResult SwapStarterWithBench(int starterSlotIndex, int benchSlotIndex) =>
        _session.SwapStarterWithBenchForNextDueMatch(starterSlotIndex, benchSlotIndex);

    // ── Kadro Kapasitesi ─────────────────────────────────────────────────────

    /// <summary>Taşan oyuncuyu A takımına terfi ettirir.</summary>
    public UiActionResult PromoteOverflowPlayer() =>
        _session.PromoteOverflowPlayerToSquad();

    /// <summary>Kadro kapasitesi açmak için öneri oyuncuyu serbest bırakır.</summary>
    public UiActionResult ReleaseToFreeCapacity() =>
        _session.ReleaseToFreeSquadCapacity();

    /// <summary>Kadroda kenar konumundaki bir oyuncuyu satar (otomatik seçim).</summary>
    public UiActionResult SellFringePlayer() =>
        _session.SellFringePlayerFromManagedClub();

    /// <summary>Belirli bir oyuncuyu satar.</summary>
    public UiActionResult SellPlayer(long playerId) =>
        _session.SellManagedClubPlayer(playerId);

    /// <summary>Bir sonraki uygun serbest oyuncuyu imzalar.</summary>
    public UiActionResult SignNextFreeAgent() =>
        _session.SignNextFreeAgentToManagedClub();

    // ── Transfer İhtiyacı ────────────────────────────────────────────────────

    /// <summary>Transfer ihtiyacı önerilerini günceller.</summary>
    public UiActionResult RefreshTransferNeeds() =>
        _session.RefreshTransferNeedSuggestions();

    /// <summary>Pozisyon açığı ihtiyacı tanımlar.</summary>
    public UiActionResult DeclarePositionGapNeed() =>
        _session.DeclarePositionGapNeed();

    /// <summary>En eski açık transfer ihtiyacını kapatır.</summary>
    public UiActionResult CloseOldestNeed() =>
        _session.CloseOldestOpenTransferNeed();

    // ── Scout & Kısa Liste ───────────────────────────────────────────────────

    /// <summary>Scout listesindeki en uygun adayı kısa listeye ekler.</summary>
    public UiActionResult SuggestTarget() =>
        _session.SuggestTransferTarget();

    /// <summary>Belirli bir oyuncuyu kısa listeye ekler.</summary>
    public UiActionResult AddToShortlist(long playerId) =>
        _session.AddScoutCandidateToShortlist(playerId);

    /// <summary>En eski listelenen transfer hedefini düşürür.</summary>
    public UiActionResult DropOldestTarget() =>
        _session.DropOldestListedTransferTarget();

    // ── Transfer Süreci ──────────────────────────────────────────────────────

    /// <summary>En eski hedeften transfer süreci açar.</summary>
    public UiActionResult OpenProcessFromOldestTarget() =>
        _session.OpenTransferProcessFromOldestTarget();

    /// <summary>En eski aktif süreci geri çeker.</summary>
    public UiActionResult WithdrawOldestProcess() =>
        _session.WithdrawOldestActiveTransferProcess();

    /// <summary>En eski aktif süreçte sportif onay talep eder.</summary>
    public UiActionResult RequestSportingApproval() =>
        _session.RequestSportingApprovalForOldestProcess();

    /// <summary>Bekleyen sportif onayı verir.</summary>
    public UiActionResult GrantSportingApproval() =>
        _session.GrantSportingApprovalForOldestPendingProcess();

    /// <summary>Bekleyen sportif onayı reddeder.</summary>
    public UiActionResult RejectSportingApproval() =>
        _session.RejectSportingApprovalForOldestPendingProcess();

    /// <summary>Mali onay talep eder.</summary>
    public UiActionResult RequestFinancialApproval() =>
        _session.RequestFinancialApprovalForOldestProcess();

    /// <summary>Bekleyen mali onayı verir.</summary>
    public UiActionResult GrantFinancialApproval() =>
        _session.GrantFinancialApprovalForOldestPendingProcess();

    /// <summary>Bekleyen mali onayı reddeder.</summary>
    public UiActionResult RejectFinancialApproval() =>
        _session.RejectFinancialApprovalForOldestPendingProcess();

    /// <summary>Mali onaylı en eski süreci tamamlar.</summary>
    public UiActionResult CompleteOldestApprovedProcess() =>
        _session.CompleteOldestFinanciallyApprovedProcess();

    /// <summary>
    /// Transfer masası birincil CTA — en eski aktif süreçte bir adım ilerler.
    /// Bekleyen teklif veya sözleşme varsa önce onu işler.
    /// </summary>
    public UiActionResult AdvanceOldestStep() =>
        _session.AdvanceOldestTransferStep();

    // ── Transfer Penceresi ───────────────────────────────────────────────────

    /// <summary>Transfer penceresini açar.</summary>
    public UiActionResult OpenWindow() =>
        _session.OpenTransferWindow();

    /// <summary>Transfer penceresini kapatır.</summary>
    public UiActionResult CloseWindow() =>
        _session.CloseTransferWindow();

    // ── Kulüp Teklifi ────────────────────────────────────────────────────────

    /// <summary>Varsayılan kulüp teklifi sunar.</summary>
    public UiActionResult SubmitClubOffer() =>
        _session.SubmitDefaultClubOffer();

    /// <summary>Bekleyen kulüp teklifini kabul eder.</summary>
    public UiActionResult AcceptClubOffer() =>
        _session.AcceptPendingClubOffer();

    /// <summary>Bekleyen kulüp teklifini reddeder.</summary>
    public UiActionResult RejectClubOffer() =>
        _session.RejectPendingClubOffer();

    /// <summary>Bekleyen kulüp teklifine karşı teklif yapar.</summary>
    public UiActionResult CounterClubOffer() =>
        _session.CounterPendingClubOffer();

    // ── Sözleşme Teklifi ─────────────────────────────────────────────────────

    /// <summary>Varsayılan sözleşme teklifi sunar.</summary>
    public UiActionResult SubmitContractProposal() =>
        _session.SubmitDefaultContractProposal();

    /// <summary>Bekleyen sözleşme teklifini kabul eder.</summary>
    public UiActionResult AcceptContractProposal() =>
        _session.AcceptPendingContractProposal();

    /// <summary>Bekleyen sözleşme teklifini reddeder.</summary>
    public UiActionResult RejectContractProposal() =>
        _session.RejectPendingContractProposal();

    /// <summary>Bekleyen sözleşme teklifine karşı teklif yapar.</summary>
    public UiActionResult CounterContractProposal() =>
        _session.CounterPendingContractProposal();
}
