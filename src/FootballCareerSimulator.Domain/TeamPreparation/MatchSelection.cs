using FootballCareerSimulator.Domain.Competition;
using FootballCareerSimulator.Domain.Shared;

namespace FootballCareerSimulator.Domain.TeamPreparation;

public enum MatchSelectionStatus
{
    Approved = 1,
}

/// <summary>
/// Maç öncesi onaylanmış kadro seçimi (ilk 11 + yedek slot indeksleri).
/// </summary>
public sealed class MatchSelection
{
    public const int StartingXiSize = 11;
    public const int MaxBenchSize = 7;
    public const int MinSquadSlot = 0;
    public const int MaxSquadSlot = 24;

    private MatchSelection(
        FixtureId fixtureId,
        ClubId clubId,
        IReadOnlyList<int> startingSlotIndices,
        IReadOnlyList<int> benchSlotIndices,
        MatchSelectionStatus status)
    {
        FixtureId = fixtureId;
        ClubId = clubId;
        StartingSlotIndices = startingSlotIndices;
        BenchSlotIndices = benchSlotIndices;
        Status = status;
    }

    public FixtureId FixtureId { get; }

    public ClubId ClubId { get; }

    public IReadOnlyList<int> StartingSlotIndices { get; }

    public IReadOnlyList<int> BenchSlotIndices { get; }

    public MatchSelectionStatus Status { get; }

    public static MatchSelection Approve(
        FixtureId fixtureId,
        ClubId clubId,
        IReadOnlyList<int> startingSlotIndices,
        IReadOnlyList<int> benchSlotIndices)
    {
        ArgumentNullException.ThrowIfNull(startingSlotIndices);
        ArgumentNullException.ThrowIfNull(benchSlotIndices);

        if (startingSlotIndices.Count != StartingXiSize)
        {
            throw new TeamPreparationInvariantViolationException(
                $"Starting XI must contain exactly {StartingXiSize} players.");
        }

        if (benchSlotIndices.Count > MaxBenchSize)
        {
            throw new TeamPreparationInvariantViolationException(
                $"Bench cannot exceed {MaxBenchSize} players.");
        }

        var allSlots = startingSlotIndices.Concat(benchSlotIndices).ToArray();
        if (allSlots.Distinct().Count() != allSlots.Length)
        {
            throw new TeamPreparationInvariantViolationException(
                "Starting XI and bench slots must be unique.");
        }

        foreach (var slot in allSlots)
        {
            if (slot is < MinSquadSlot or > MaxSquadSlot)
            {
                throw new TeamPreparationInvariantViolationException(
                    $"Squad slot {slot} is out of range ({MinSquadSlot}-{MaxSquadSlot}).");
            }
        }

        return new MatchSelection(
            fixtureId,
            clubId,
            startingSlotIndices.ToArray(),
            benchSlotIndices.ToArray(),
            MatchSelectionStatus.Approved);
    }

    public static MatchSelection ApproveDefault(FixtureId fixtureId, ClubId clubId) =>
        Approve(
            fixtureId,
            clubId,
            Enumerable.Range(0, StartingXiSize).ToArray(),
            Enumerable.Range(StartingXiSize, MaxBenchSize).ToArray());

    public static MatchSelection Rehydrate(
        FixtureId fixtureId,
        ClubId clubId,
        IReadOnlyList<int> startingSlotIndices,
        IReadOnlyList<int> benchSlotIndices,
        MatchSelectionStatus status)
    {
        if (status != MatchSelectionStatus.Approved)
        {
            throw new TeamPreparationInvariantViolationException(
                $"Unsupported match selection status: {status}.");
        }

        return Approve(fixtureId, clubId, startingSlotIndices, benchSlotIndices);
    }
}
