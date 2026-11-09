using FootballCareerSimulator.Application.ClubGovernance.Ports;
using FootballCareerSimulator.Application.ContractRegistration.Services;
using FootballCareerSimulator.Application.PlayerCareer.Ports;
using FootballCareerSimulator.Application.TrainingPhysicalState.Ports;
using FootballCareerSimulator.Domain.Shared;
using FootballCareerSimulator.Domain.TrainingPhysicalState;
using FootballCareerSimulator.Domain.WorldCalendar;
using FootballCareerSimulator.Simulation.PlayerCareer;

namespace FootballCareerSimulator.Application.PlayerCareer.Services;

/// <summary>
/// Training/Match/World orkestrasyonundan çağrılan PlayerCareer yazma kapısı.
/// </summary>
public sealed class PlayerCareerDevelopmentService
{
    private readonly IPlayerCareerStore _store;
    private readonly ITrainingPhysicalStateStore? _trainingStore;
    private readonly ContractRegistrationService? _contracts;
    private readonly IClubRegistryStore? _clubRegistry;

    public PlayerCareerDevelopmentService(
        IPlayerCareerStore store,
        ITrainingPhysicalStateStore? trainingStore = null,
        ContractRegistrationService? contracts = null,
        IClubRegistryStore? clubRegistry = null)
    {
        _store = store ?? throw new ArgumentNullException(nameof(store));
        _trainingStore = trainingStore;
        _contracts = contracts;
        _clubRegistry = clubRegistry;
    }

    /// <summary>
    /// Kulübün tesis seviyesine bağlı antrenman gelişim çarpanı (seviye 1 → 1,0x … seviye 5 → 1,4x).
    /// Kulüp kaydı bağlı değilse veya kulüp bulunamazsa nötr 1,0 döner.
    /// </summary>
    private double FacilityMultiplier(ClubId clubId)
    {
        if (_clubRegistry is null)
        {
            return 1.0;
        }

        var club = _clubRegistry.Registry.Clubs.FirstOrDefault(candidate => candidate.Id == clubId);
        return club?.FacilityTrainingMultiplier ?? 1.0;
    }

    public void EnsureClub(ClubId clubId, int rootSeed, GameDate day)
    {
        var squad = MvpPlayerDevelopmentApplier.EnsureClubSquad(
            clubId,
            rootSeed,
            day,
            _store.ByClubSlot);
        _store.ReplaceClub(clubId, squad);
        _contracts?.EnsureClubContracts(clubId, day);
    }

    public int ApplyDueAging(GameDate day)
    {
        if (_store.Careers.Count == 0)
        {
            return 0;
        }

        var before = _store.Careers.ToDictionary(c => c.Id.Value, c => c.CurrentAbility);
        var aged = MvpAgingApplier.ApplyDueAging(_store.Careers, day);
        _store.ReplaceAll(aged);
        return aged.Count(c => before.TryGetValue(c.Id.Value, out var prior) && prior != c.CurrentAbility);
    }

    public void EnsureAndApplyWeeklyTraining(
        ClubId clubId,
        WeeklyTrainingPlan plan,
        int rootSeed,
        GameDate day)
    {
        ArgumentNullException.ThrowIfNull(plan);

        ApplyDueAging(day);
        var squad = MvpPlayerDevelopmentApplier.EnsureClubSquad(
            clubId,
            rootSeed,
            day,
            _store.ByClubSlot);
        var facilityMultiplier = FacilityMultiplier(clubId);
        var updated = new List<Domain.PlayerCareer.PlayerCareer>(squad.Count);
        foreach (var career in squad)
        {
            if (_contracts is not null && _contracts.IsFreeAgent(career.Id))
            {
                updated.Add(career);
                continue;
            }

            var physical = _trainingStore?.GetPhysical(clubId, career.SlotIndex);
            updated.Add(MvpPlayerDevelopmentApplier.ApplyWeeklyTraining(career, plan, physical, day, facilityMultiplier));
        }

        _store.ReplaceClub(clubId, updated);
        _contracts?.EnsureClubContracts(clubId, day);
        var expiry = _contracts?.ExpireDueContracts(day);
        if (expiry is { ExpiredCount: > 0 })
        {
            _contracts!.RestorePopulationContinuity(day);
        }
    }

    public void EnsureAndApplyMatchAppearances(
        ClubId clubId,
        IReadOnlyList<int> startingSlotIndices,
        int rootSeed,
        GameDate day)
    {
        ArgumentNullException.ThrowIfNull(startingSlotIndices);

        ApplyDueAging(day);
        var squad = MvpPlayerDevelopmentApplier.EnsureClubSquad(
                clubId,
                rootSeed,
                day,
                _store.ByClubSlot)
            .ToDictionary(c => c.SlotIndex);

        foreach (var slot in startingSlotIndices)
        {
            if (!squad.TryGetValue(slot, out var career))
            {
                continue;
            }

            if (_contracts is not null && _contracts.IsFreeAgent(career.Id))
            {
                continue;
            }

            var physical = _trainingStore?.GetPhysical(clubId, slot);
            if (physical is not null && !physical.IsAvailableOn(day))
            {
                continue;
            }

            squad[slot] = MvpPlayerDevelopmentApplier.ApplyMatchAppearance(career, day);
        }

        _store.ReplaceClub(clubId, squad.Values.OrderBy(c => c.SlotIndex));
    }
}
