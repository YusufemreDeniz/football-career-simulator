using FootballCareerSimulator.Application.ClubGovernance.Ports;
using FootballCareerSimulator.Domain.ClubGovernance;
using FootballCareerSimulator.Domain.Shared;

namespace FootballCareerSimulator.Application.ClubGovernance.Services;

/// <summary>
/// Transfer bütçe rezervasyonu / release / spend — Club & Governance sahibi.
/// </summary>
public sealed class ClubTransferBudgetService
{
    private readonly IClubRegistryStore _store;

    public ClubTransferBudgetService(IClubRegistryStore store)
    {
        _store = store ?? throw new ArgumentNullException(nameof(store));
    }

    public ClubTransferBudgetSnapshot Get(ClubId clubId)
    {
        var club = _store.Registry.GetClubOrThrow(clubId);
        return new ClubTransferBudgetSnapshot(
            club.Id.Value,
            club.TransferBudgetLimit,
            club.ReservedTransferFunds,
            club.SpentTransferFunds,
            club.AvailableTransferFunds);
    }

    public void Reserve(ClubId clubId, int amount) =>
        Replace(clubId, club => club.ReserveTransferFunds(amount));

    public void Release(ClubId clubId, int amount) =>
        Replace(clubId, club => club.ReleaseTransferReservation(amount));

    public void ApplyReservedSpend(ClubId clubId, int amount) =>
        Replace(clubId, club => club.ApplyReservedTransferSpend(amount));

    private void Replace(ClubId clubId, Func<Club, Club> mutate)
    {
        var current = _store.Registry.GetClubOrThrow(clubId);
        _store.Replace(_store.Registry.WithClub(mutate(current)));
    }
}

public sealed record ClubTransferBudgetSnapshot(
    long ClubId,
    int Limit,
    int Reserved,
    int Spent,
    int Available);
