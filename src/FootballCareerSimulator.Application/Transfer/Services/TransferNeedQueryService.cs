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
    private readonly IManagerCareerStore _managerCareerStore;

    public TransferNeedQueryService(
        ITransferNeedStore store,
        IShortlistStore shortlistStore,
        ITransferTargetStore targetStore,
        IManagerCareerStore managerCareerStore)
    {
        _store = store ?? throw new ArgumentNullException(nameof(store));
        _shortlistStore = shortlistStore ?? throw new ArgumentNullException(nameof(shortlistStore));
        _targetStore = targetStore ?? throw new ArgumentNullException(nameof(targetStore));
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
}
