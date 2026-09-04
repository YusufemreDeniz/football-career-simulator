using FootballCareerSimulator.Application.ClubGovernance.Ports;
using FootballCareerSimulator.Application.ContractRegistration.Ports;
using FootballCareerSimulator.Domain.ClubGovernance;
using FootballCareerSimulator.Domain.Shared;
using FootballCareerSimulator.Domain.WorldCalendar;

namespace FootballCareerSimulator.Application.ClubGovernance.Services;

/// <summary>
/// Haftalık maaş bütçesi: limit + aktif sözleşme taahhüdü + rezervasyon. Ledger yok.
/// </summary>
public sealed class ClubWageBudgetService
{
    private readonly IClubRegistryStore _store;
    private readonly IContractStore _contractStore;

    public ClubWageBudgetService(IClubRegistryStore store, IContractStore contractStore)
    {
        _store = store ?? throw new ArgumentNullException(nameof(store));
        _contractStore = contractStore ?? throw new ArgumentNullException(nameof(contractStore));
    }

    public ClubWageBudgetSnapshot Get(ClubId clubId, GameDate day)
    {
        var club = _store.Registry.GetClubOrThrow(clubId);
        var committed = SumCommittedWeeklyWage(clubId, day);
        return new ClubWageBudgetSnapshot(
            club.Id.Value,
            club.WageBudgetLimit,
            committed,
            club.ReservedWeeklyWage,
            club.AvailableWeeklyWageHeadroom(committed));
    }

    public void Reserve(ClubId clubId, int weeklyWage, GameDate day)
    {
        var committed = SumCommittedWeeklyWage(clubId, day);
        Replace(clubId, club => club.ReserveWeeklyWage(weeklyWage, committed));
    }

    public void Release(ClubId clubId, int weeklyWage) =>
        Replace(clubId, club => club.ReleaseWeeklyWageReservation(weeklyWage));

    public bool CanAfford(ClubId clubId, int weeklyWage, GameDate day) =>
        weeklyWage <= 0 || Get(clubId, day).Available >= weeklyWage;

    private int SumCommittedWeeklyWage(ClubId clubId, GameDate day) =>
        _contractStore.GetForClub(clubId)
            .Where(c => c.IsActiveOn(day))
            .Sum(c => c.WeeklyWage);

    private void Replace(ClubId clubId, Func<Club, Club> mutate)
    {
        var current = _store.Registry.GetClubOrThrow(clubId);
        _store.Replace(_store.Registry.WithClub(mutate(current)));
    }
}

public sealed record ClubWageBudgetSnapshot(
    long ClubId,
    int Limit,
    int Committed,
    int Reserved,
    int Available);
