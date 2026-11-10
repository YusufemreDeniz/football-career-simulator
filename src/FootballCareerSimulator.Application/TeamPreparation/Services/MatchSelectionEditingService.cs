using FootballCareerSimulator.Application.TeamPreparation.Ports;
using FootballCareerSimulator.Application.TrainingPhysicalState.Ports;
using FootballCareerSimulator.Application.WorldCalendar.Ports;
using FootballCareerSimulator.Domain.Competition;
using FootballCareerSimulator.Domain.Shared;
using FootballCareerSimulator.Domain.TeamPreparation;
using FootballCareerSimulator.Simulation.TrainingPhysicalState;

namespace FootballCareerSimulator.Application.TeamPreparation.Services;

/// <summary>
/// Maç öncesi kadro düzenleme komutları (ilk 11 içi yer değişimi, yedek sıralaması, diziliş yerleşimi).
/// Presentation katmanı seçim store'una doğrudan yazmaz (docs/17 §335); tüm düzenlemeler buradan geçer ve
/// ilk 11 için sakatlık/uygunluk kuralı her yazımda yeniden doğrulanır.
/// </summary>
public sealed class MatchSelectionEditingService
{
    private readonly IMatchSelectionStore _selections;
    private readonly IClubSquadStore _squads;
    private readonly ITrainingPhysicalStateStore? _training;
    private readonly IWorldTimelineStore? _timeline;

    public MatchSelectionEditingService(
        IMatchSelectionStore selections,
        IClubSquadStore squads,
        ITrainingPhysicalStateStore? training = null,
        IWorldTimelineStore? timeline = null)
    {
        _selections = selections ?? throw new ArgumentNullException(nameof(selections));
        _squads = squads ?? throw new ArgumentNullException(nameof(squads));
        _training = training;
        _timeline = timeline;
    }

    /// <summary>
    /// Kayıtlı seçimi, yoksa uygunluğu gözeten varsayılan seçimi döndürür (store'a yazmaz).
    /// </summary>
    public MatchSelection GetOrDefault(FixtureId fixtureId, ClubId clubId)
    {
        var current = _selections.Get(fixtureId, clubId);
        if (current is not null)
        {
            return current;
        }

        var squad = _squads.Get(clubId);
        return _training is not null && _timeline is not null
            ? MvpAvailabilityAwareSelection.ApproveDefaultPreferringAvailable(
                fixtureId,
                clubId,
                _timeline.Timeline.CurrentDate,
                _training.PhysicalBySlot,
                squad)
            : MatchSelection.ApproveDefault(fixtureId, clubId, squad);
    }

    public MatchSelection SwapStarters(FixtureId fixtureId, ClubId clubId, int starterSlotA, int starterSlotB)
    {
        var current = GetOrDefault(fixtureId, clubId);
        var starting = current.StartingSlotIndices.ToList();
        Swap(starting, starterSlotA, starterSlotB, "Seçilen oyuncular ilk 11 listesinde bulunamadı.");
        return Save(current, starting, current.BenchSlotIndices);
    }

    public MatchSelection SwapBench(FixtureId fixtureId, ClubId clubId, int benchSlotA, int benchSlotB)
    {
        var current = GetOrDefault(fixtureId, clubId);
        var bench = current.BenchSlotIndices.ToList();
        Swap(bench, benchSlotA, benchSlotB, "Seçilen oyuncular yedek kulübesinde bulunamadı.");
        return Save(current, current.StartingSlotIndices, bench);
    }

    /// <summary>
    /// İlk 11'i yeni bir saha sırasına yerleştirir; yeni sıra mevcut ilk 11'in permütasyonu olmalıdır
    /// (bu komut oyuncu değiştirmez, yalnız diziliş rollerine yerleştirir).
    /// </summary>
    public MatchSelection ReorderStartingXi(FixtureId fixtureId, ClubId clubId, IReadOnlyList<int> newStartingOrder)
    {
        ArgumentNullException.ThrowIfNull(newStartingOrder);
        var current = GetOrDefault(fixtureId, clubId);
        if (newStartingOrder.Count != current.StartingSlotIndices.Count
            || !newStartingOrder.OrderBy(slot => slot).SequenceEqual(current.StartingSlotIndices.OrderBy(slot => slot)))
        {
            throw new TeamPreparationInvariantViolationException(
                "Yeni diziliş mevcut ilk 11'in yeniden sıralanması olmalı.");
        }

        return Save(current, newStartingOrder, current.BenchSlotIndices);
    }

    /// <summary>Sezon geçişinde fikstür kimlikleri yeniden kullanıldığı için eski onaylar temizlenir.</summary>
    public void ClearAll() => _selections.ReplaceAll(Array.Empty<MatchSelection>());

    private MatchSelection Save(
        MatchSelection current,
        IReadOnlyList<int> starting,
        IReadOnlyList<int> bench)
    {
        var squad = _squads.Get(current.ClubId);
        if (_training is not null && _timeline is not null)
        {
            MvpAvailabilityAwareSelection.EnsureStartingXiAvailable(
                current.ClubId,
                starting,
                _timeline.Timeline.CurrentDate,
                _training.PhysicalBySlot,
                squad);
        }

        var updated = MatchSelection.Approve(current.FixtureId, current.ClubId, starting, bench, squad);
        _selections.Upsert(updated);
        return updated;
    }

    private static void Swap(List<int> slots, int a, int b, string missingMessage)
    {
        var indexA = slots.IndexOf(a);
        var indexB = slots.IndexOf(b);
        if (indexA < 0 || indexB < 0)
        {
            throw new TeamPreparationInvariantViolationException(missingMessage);
        }

        slots[indexA] = b;
        slots[indexB] = a;
    }
}
