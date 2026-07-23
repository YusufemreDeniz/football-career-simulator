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

    public PlayerCareerDevelopmentService(
        IPlayerCareerStore store,
        ITrainingPhysicalStateStore? trainingStore = null)
    {
        _store = store ?? throw new ArgumentNullException(nameof(store));
        _trainingStore = trainingStore;
    }

    public void EnsureClub(ClubId clubId, int rootSeed, GameDate day)
    {
        var squad = MvpPlayerDevelopmentApplier.EnsureClubSquad(
            clubId,
            rootSeed,
            day,
            _store.ByClubSlot);
        _store.ReplaceClub(clubId, squad);
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
        var updated = new List<Domain.PlayerCareer.PlayerCareer>(squad.Count);
        foreach (var career in squad)
        {
            var physical = _trainingStore?.GetPhysical(clubId, career.SlotIndex);
            updated.Add(MvpPlayerDevelopmentApplier.ApplyWeeklyTraining(career, plan, physical, day));
        }

        _store.ReplaceClub(clubId, updated);
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
