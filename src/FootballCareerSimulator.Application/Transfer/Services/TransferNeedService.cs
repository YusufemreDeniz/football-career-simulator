using FootballCareerSimulator.Application.ContractRegistration.Ports;
using FootballCareerSimulator.Application.TeamPreparation.Ports;
using FootballCareerSimulator.Application.Transfer.Ports;
using FootballCareerSimulator.Domain.Shared;
using FootballCareerSimulator.Domain.Transfer;
using FootballCareerSimulator.Domain.WorldCalendar;

namespace FootballCareerSimulator.Application.Transfer.Services;

/// <summary>
/// Transfer Need iskeleti: ihtiyaç belirleme / kapatma. Target ve Process yok.
/// </summary>
public sealed class TransferNeedService
{
    private readonly ITransferNeedStore _store;
    private readonly IContractStore _contractStore;
    private readonly IClubSquadStore _squadStore;

    public TransferNeedService(
        ITransferNeedStore store,
        IContractStore contractStore,
        IClubSquadStore squadStore)
    {
        _store = store ?? throw new ArgumentNullException(nameof(store));
        _contractStore = contractStore ?? throw new ArgumentNullException(nameof(contractStore));
        _squadStore = squadStore ?? throw new ArgumentNullException(nameof(squadStore));
    }

    public TransferNeed Declare(
        ClubId clubId,
        TransferNeedKind kind,
        int priority,
        string reasonCode,
        GameDate day) =>
        EnsureOpen(clubId, kind, priority, reasonCode, day);

    public IReadOnlyList<TransferNeed> RefreshSuggestions(ClubId clubId, GameDate day)
    {
        var createdOrKept = new List<TransferNeed>();

        var yearEnd = GameDate.FromCalendarDate(day.Year + 1, day.Month, day.Day);
        var expiring = _contractStore.GetForClub(clubId)
            .Count(c => c.IsActiveOn(day) && c.EndDate.DayNumber <= yearEnd.DayNumber);
        if (expiring > 0)
        {
            createdOrKept.Add(EnsureOpen(
                clubId,
                TransferNeedKind.ExpiringContract,
                priority: 3,
                "ExpiringContracts",
                day));
        }

        var squad = _squadStore.Get(clubId);
        var memberCount = squad?.Members.Count ?? 0;
        if (memberCount > 0 && memberCount < 18)
        {
            createdOrKept.Add(EnsureOpen(
                clubId,
                TransferNeedKind.SquadDepth,
                priority: 2,
                "ThinSquad",
                day));
        }

        return createdOrKept;
    }

    public TransferNeed Close(TransferNeedId needId, GameDate day)
    {
        var existing = _store.Get(needId)
            ?? throw new TransferInvariantViolationException($"Transfer need #{needId.Value} not found.");

        var closed = existing.Close(day);
        _store.Upsert(closed);
        return closed;
    }

    private TransferNeed EnsureOpen(
        ClubId clubId,
        TransferNeedKind kind,
        int priority,
        string reasonCode,
        GameDate day)
    {
        var openSameKind = _store.GetForClub(clubId)
            .FirstOrDefault(n => n.IsOpen && n.Kind == kind);
        if (openSameKind is not null)
        {
            return openSameKind;
        }

        var maxId = _store.Needs.Select(n => n.NeedId.Value).DefaultIfEmpty(0).Max();
        var nextId = new TransferNeedId(maxId + 1);
        var need = TransferNeed.Identify(nextId, clubId, kind, priority, reasonCode, day);
        _store.Upsert(need);
        return need;
    }
}
