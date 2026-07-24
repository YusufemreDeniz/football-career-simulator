using FootballCareerSimulator.Application.ManagerCareer.Ports;
using FootballCareerSimulator.Application.Transfer.Ports;
using FootballCareerSimulator.Application.Transfer.Queries;
using FootballCareerSimulator.Domain.Transfer;

namespace FootballCareerSimulator.Application.Transfer.Services;

public sealed class TransferNeedQueryService
{
    private readonly ITransferNeedStore _store;
    private readonly IShortlistStore _shortlistStore;
    private readonly ITransferTargetStore _targetStore;
    private readonly ITransferProcessStore _processStore;
    private readonly IClubOfferStore _offerStore;
    private readonly IPlayerContractProposalStore _proposalStore;
    private readonly IManagerCareerStore _managerCareerStore;

    public TransferNeedQueryService(
        ITransferNeedStore store,
        IShortlistStore shortlistStore,
        ITransferTargetStore targetStore,
        ITransferProcessStore processStore,
        IClubOfferStore offerStore,
        IPlayerContractProposalStore proposalStore,
        IManagerCareerStore managerCareerStore)
    {
        _store = store ?? throw new ArgumentNullException(nameof(store));
        _shortlistStore = shortlistStore ?? throw new ArgumentNullException(nameof(shortlistStore));
        _targetStore = targetStore ?? throw new ArgumentNullException(nameof(targetStore));
        _processStore = processStore ?? throw new ArgumentNullException(nameof(processStore));
        _offerStore = offerStore ?? throw new ArgumentNullException(nameof(offerStore));
        _proposalStore = proposalStore ?? throw new ArgumentNullException(nameof(proposalStore));
        _managerCareerStore = managerCareerStore
            ?? throw new ArgumentNullException(nameof(managerCareerStore));
    }

    public ManagedClubTransferNeedsReadModel GetManagedClubNeeds()
    {
        if (_managerCareerStore.Career.ActiveEmployment is not { ClubId: var clubId })
        {
            return new ManagedClubTransferNeedsReadModel(null, 0, Array.Empty<TransferNeedLineReadModel>());
        }

        var open = _store.GetForClub(clubId)
            .Where(n => n.IsOpen)
            .Select(ToNeedLine)
            .ToArray();

        return new ManagedClubTransferNeedsReadModel(clubId.Value, open.Length, open);
    }

    public ManagedClubShortlistTargetsReadModel GetManagedClubShortlistTargets()
    {
        if (_managerCareerStore.Career.ActiveEmployment is not { ClubId: var clubId })
        {
            return new ManagedClubShortlistTargetsReadModel(
                null,
                0,
                0,
                Array.Empty<ShortlistLineReadModel>(),
                Array.Empty<TransferTargetLineReadModel>());
        }

        var shortlist = _shortlistStore.GetForClub(clubId)
            .Where(e => e.IsActive)
            .Select(e => new ShortlistLineReadModel(
                e.EntryId.Value,
                e.PlayerId.Value,
                e.NeedId?.Value,
                e.Priority,
                "Aktif"))
            .ToArray();

        var targets = _targetStore.GetForClub(clubId)
            .Where(t => t.IsListed)
            .Select(t => new TransferTargetLineReadModel(
                t.TargetId.Value,
                t.NeedId.Value,
                t.PlayerId.Value,
                t.ShortlistEntryId?.Value,
                "Listede"))
            .ToArray();

        return new ManagedClubShortlistTargetsReadModel(
            clubId.Value,
            shortlist.Length,
            targets.Length,
            shortlist,
            targets);
    }

    public ManagedClubTransferProcessesReadModel GetManagedClubProcesses()
    {
        if (_managerCareerStore.Career.ActiveEmployment is not { ClubId: var clubId })
        {
            return new ManagedClubTransferProcessesReadModel(
                null,
                0,
                Array.Empty<TransferProcessLineReadModel>());
        }

        var active = _processStore.GetForBuyingClub(clubId)
            .Where(p => p.IsActive)
            .Select(p => new TransferProcessLineReadModel(
                p.ProcessId.Value,
                p.TargetId.Value,
                p.PlayerId.Value,
                (int)p.Status,
                TranslateProcessStatus(p.Status),
                p.FailureReasonCode,
                p.IsFreeAgent))
            .ToArray();

        return new ManagedClubTransferProcessesReadModel(clubId.Value, active.Length, active);
    }

    public ManagedClubOffersReadModel GetManagedClubOffers()
    {
        if (_managerCareerStore.Career.ActiveEmployment is not { ClubId: var clubId })
        {
            return new ManagedClubOffersReadModel(null, 0, Array.Empty<ClubOfferLineReadModel>());
        }

        var processIds = _processStore.GetForBuyingClub(clubId)
            .Select(p => p.ProcessId.Value)
            .ToHashSet();
        var offers = _offerStore.Offers
            .Where(o => processIds.Contains(o.ProcessId.Value))
            .OrderByDescending(o => o.OfferId.Value)
            .Take(5)
            .Select(o => new ClubOfferLineReadModel(
                o.OfferId.Value,
                o.ProcessId.Value,
                o.Round,
                o.OfferedFee,
                TranslateOfferStatus(o.Status)))
            .ToArray();
        var pending = _offerStore.Offers.Count(o =>
            processIds.Contains(o.ProcessId.Value) && o.IsPending);

        return new ManagedClubOffersReadModel(clubId.Value, pending, offers);
    }

    public ManagedClubContractProposalsReadModel GetManagedContractProposals()
    {
        if (_managerCareerStore.Career.ActiveEmployment is not { ClubId: var clubId })
        {
            return new ManagedClubContractProposalsReadModel(
                null,
                0,
                Array.Empty<ContractProposalLineReadModel>());
        }

        var processIds = _processStore.GetForBuyingClub(clubId)
            .Select(p => p.ProcessId.Value)
            .ToHashSet();
        var proposals = _proposalStore.Proposals
            .Where(p => processIds.Contains(p.ProcessId.Value))
            .OrderByDescending(p => p.ProposalId.Value)
            .Take(5)
            .Select(p => new ContractProposalLineReadModel(
                p.ProposalId.Value,
                p.ProcessId.Value,
                p.Round,
                p.WeeklyWage,
                p.ContractYears,
                TranslateProposalStatus(p.Status)))
            .ToArray();
        var pending = _proposalStore.Proposals.Count(p =>
            processIds.Contains(p.ProcessId.Value) && p.IsPending);

        return new ManagedClubContractProposalsReadModel(clubId.Value, pending, proposals);
    }

    private static TransferNeedLineReadModel ToNeedLine(TransferNeed need) =>
        new(
            need.NeedId.Value,
            TranslateKind(need.Kind),
            need.Status == TransferNeedStatus.Open ? "Açık" : "Kapalı",
            need.Priority,
            need.ReasonCode,
            need.IdentifiedOn.DayNumber);

    private static string TranslateKind(TransferNeedKind kind) =>
        kind switch
        {
            TransferNeedKind.PositionGap => "Pozisyon açığı",
            TransferNeedKind.SquadDepth => "Kadro derinliği",
            TransferNeedKind.Aging => "Yaşlanma",
            TransferNeedKind.InjuryCover => "Sakatlık kapağı",
            TransferNeedKind.ExpiringContract => "Sözleşme bitişi",
            TransferNeedKind.TacticalRequirement => "Taktik gereksinim",
            _ => kind.ToString(),
        };

    private static string TranslateProcessStatus(TransferProcessStatus status) =>
        status switch
        {
            TransferProcessStatus.UnderEvaluation => "Değerlendirmede",
            TransferProcessStatus.SportingApprovalPending => "Sportif onay bekliyor",
            TransferProcessStatus.SportingApproved => "Sportif onaylı",
            TransferProcessStatus.ClubNegotiation => "Kulüp müzakeresi",
            TransferProcessStatus.ClubAgreementReached => "Kulüp anlaşması",
            TransferProcessStatus.PlayerNegotiation => "Oyuncu müzakeresi",
            TransferProcessStatus.PlayerAgreementReached => "Oyuncu anlaşması",
            TransferProcessStatus.FinancialApprovalPending => "Mali onay bekliyor",
            TransferProcessStatus.FinancialApproved => "Mali onaylı",
            TransferProcessStatus.CompletionPending => "Tamamlanıyor",
            TransferProcessStatus.Completed => "Tamamlandı",
            TransferProcessStatus.Rejected => "Reddedildi",
            TransferProcessStatus.Withdrawn => "Geri çekildi",
            TransferProcessStatus.Failed => "Başarısız",
            TransferProcessStatus.Expired => "Süresi doldu",
            TransferProcessStatus.Archived => "Arşiv",
            _ => status.ToString(),
        };

    private static string TranslateOfferStatus(ClubOfferStatus status) =>
        status switch
        {
            ClubOfferStatus.Pending => "Bekliyor",
            ClubOfferStatus.Accepted => "Kabul",
            ClubOfferStatus.Rejected => "Ret",
            ClubOfferStatus.Superseded => "Geçersiz",
            _ => status.ToString(),
        };

    private static string TranslateProposalStatus(PlayerContractProposalStatus status) =>
        status switch
        {
            PlayerContractProposalStatus.Pending => "Bekliyor",
            PlayerContractProposalStatus.Accepted => "Kabul",
            PlayerContractProposalStatus.Rejected => "Ret",
            PlayerContractProposalStatus.Superseded => "Geçersiz",
            _ => status.ToString(),
        };
}
