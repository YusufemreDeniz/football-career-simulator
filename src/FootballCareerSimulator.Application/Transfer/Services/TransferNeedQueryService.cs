using FootballCareerSimulator.Application.ManagerCareer.Ports;
using FootballCareerSimulator.Application.Transfer.Ports;
using FootballCareerSimulator.Application.Transfer.Queries;
using FootballCareerSimulator.Domain.Transfer;

namespace FootballCareerSimulator.Application.Transfer.Services;

public sealed class TransferNeedQueryService
{
    private readonly ITransferNeedStore _store;
    private readonly IManagerCareerStore _managerCareerStore;

    public TransferNeedQueryService(ITransferNeedStore store, IManagerCareerStore managerCareerStore)
    {
        _store = store ?? throw new ArgumentNullException(nameof(store));
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
            .Select(ToLine)
            .ToArray();

        return new ManagedClubTransferNeedsReadModel(clubId.Value, open.Length, open);
    }

    private static TransferNeedLineReadModel ToLine(TransferNeed need) =>
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
