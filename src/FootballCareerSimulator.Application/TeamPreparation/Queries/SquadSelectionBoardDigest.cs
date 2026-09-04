using FootballCareerSimulator.Domain.Shared;
using FootballCareerSimulator.Domain.TrainingPhysicalState;
using FootballCareerSimulator.Domain.WorldCalendar;
using FootballCareerSimulator.Simulation.TeamPreparation;

namespace FootballCareerSimulator.Application.TeamPreparation.Queries;

public sealed record SquadSelectionPlayerDigest(
    int SlotIndex,
    string DisplayName,
    string PositionCode,
    int Rating,
    int Fitness,
    int Fatigue,
    bool IsAvailable,
    bool IsStarter,
    string PositionName = "")
{
    public string ButtonLabel =>
        $"{DisplayName}\n{PositionCode} · {PlayerPhysicalState.FatigueBandLabel(Fatigue)}"
        + (IsAvailable ? string.Empty : " · SAKAT");
}

public sealed record SquadSelectionBoardDigest(
    bool HasMatch,
    bool IsApproved,
    IReadOnlyList<SquadSelectionPlayerDigest> StartingXi,
    IReadOnlyList<SquadSelectionPlayerDigest> Bench)
{
    public static SquadSelectionBoardDigest Clear() =>
        new(
            false,
            false,
            Array.Empty<SquadSelectionPlayerDigest>(),
            Array.Empty<SquadSelectionPlayerDigest>());

    public static SquadSelectionBoardDigest Compose(
        ClubId clubId,
        GameDate day,
        bool isApproved,
        IReadOnlyList<int> startingSlots,
        IReadOnlyList<int> benchSlots,
        IReadOnlyList<MvpSquadPlayerProfile> profiles,
        IReadOnlyDictionary<int, int> ratingsBySlot,
        IReadOnlyDictionary<(long ClubId, int SlotIndex), PlayerPhysicalState> physicalBySlot)
    {
        ArgumentNullException.ThrowIfNull(startingSlots);
        ArgumentNullException.ThrowIfNull(benchSlots);
        ArgumentNullException.ThrowIfNull(profiles);
        ArgumentNullException.ThrowIfNull(ratingsBySlot);
        ArgumentNullException.ThrowIfNull(physicalBySlot);

        var starting = startingSlots
            .Select(slot => ComposePlayer(
                clubId,
                day,
                slot,
                isStarter: true,
                profiles,
                ratingsBySlot,
                physicalBySlot))
            .ToArray();
        var bench = benchSlots
            .Select(slot => ComposePlayer(
                clubId,
                day,
                slot,
                isStarter: false,
                profiles,
                ratingsBySlot,
                physicalBySlot))
            .ToArray();

        return new SquadSelectionBoardDigest(true, isApproved, starting, bench);
    }

    private static SquadSelectionPlayerDigest ComposePlayer(
        ClubId clubId,
        GameDate day,
        int slot,
        bool isStarter,
        IReadOnlyList<MvpSquadPlayerProfile> profiles,
        IReadOnlyDictionary<int, int> ratingsBySlot,
        IReadOnlyDictionary<(long ClubId, int SlotIndex), PlayerPhysicalState> physicalBySlot)
    {
        if (slot < 0 || slot >= profiles.Count)
        {
            throw new ArgumentOutOfRangeException(nameof(slot), slot, "Squad slot has no player profile.");
        }

        var profile = profiles[slot];
        var state = physicalBySlot.TryGetValue((clubId.Value, slot), out var physical)
            ? physical
            : PlayerPhysicalState.CreateRested(clubId, slot);
        if (!ratingsBySlot.TryGetValue(slot, out var rating))
        {
            throw new ArgumentException($"Squad slot {slot} has no rating.", nameof(ratingsBySlot));
        }

        return new SquadSelectionPlayerDigest(
            slot,
            profile.DisplayName,
            profile.PositionCode,
            rating,
            state.Fitness,
            state.Fatigue,
            state.IsAvailableOn(day),
            isStarter,
            profile.PositionName);
    }
}
