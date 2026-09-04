using FootballCareerSimulator.Application.TeamPreparation.Ports;
using FootballCareerSimulator.Application.TrainingPhysicalState.Ports;
using FootballCareerSimulator.Domain.Shared;
using FootballCareerSimulator.Domain.WorldCalendar;
using FootballCareerSimulator.Simulation.TrainingPhysicalState;

namespace FootballCareerSimulator.Application.TeamPreparation.Services;

/// <summary>
/// Onaylı kadroda müsait olmayan starter varsa seçimi düşürür (yeniden onay gerekir).
/// </summary>
public sealed class MatchSelectionAvailabilityRevalidationService
{
    private readonly IMatchSelectionStore _selectionStore;
    private readonly ITrainingPhysicalStateStore _trainingStore;

    public MatchSelectionAvailabilityRevalidationService(
        IMatchSelectionStore selectionStore,
        ITrainingPhysicalStateStore trainingStore)
    {
        _selectionStore = selectionStore ?? throw new ArgumentNullException(nameof(selectionStore));
        _trainingStore = trainingStore ?? throw new ArgumentNullException(nameof(trainingStore));
    }

    public int InvalidateUnavailableForClub(ClubId clubId, GameDate day)
    {
        var physical = _trainingStore.PhysicalBySlot;
        var removed = 0;

        foreach (var selection in _selectionStore.Selections
                     .Where(candidate => candidate.ClubId == clubId)
                     .ToArray())
        {
            if (!MvpAvailabilityAwareSelection.HasUnavailableStarter(
                    clubId,
                    selection.StartingSlotIndices,
                    day,
                    physical))
            {
                continue;
            }

            _selectionStore.Remove(selection.FixtureId, selection.ClubId);
            removed++;
        }

        return removed;
    }
}
