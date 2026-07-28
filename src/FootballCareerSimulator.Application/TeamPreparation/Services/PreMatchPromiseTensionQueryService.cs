using FootballCareerSimulator.Application.SocialContinuity.Ports;
using FootballCareerSimulator.Application.TeamPreparation.Ports;
using FootballCareerSimulator.Application.TeamPreparation.Queries;
using FootballCareerSimulator.Domain.Competition;
using FootballCareerSimulator.Domain.Shared;
using FootballCareerSimulator.Domain.SocialContinuity;
using FootballCareerSimulator.Domain.TeamPreparation;

namespace FootballCareerSimulator.Application.TeamPreparation.Services;

/// <summary>
/// Maç öncesi: aktif İlk 11 sözü vs mevcut (veya varsayılan) kadro yerleşimi.
/// </summary>
public sealed class PreMatchPromiseTensionQueryService
{
    public const string ToneNone = "None";
    public const string ToneOnTrack = "OnTrack";
    public const string ToneAtRisk = "AtRisk";

    public const string PlacementStarting = "Starting";
    public const string PlacementBench = "Bench";
    public const string PlacementOut = "OutOfMatchday";

    private readonly MatchSelectionQueryService _selectionQueries;
    private readonly IMatchSelectionStore _selectionStore;
    private readonly IClubSquadStore _squadStore;
    private IPromiseStore? _promiseStore;

    public PreMatchPromiseTensionQueryService(
        MatchSelectionQueryService selectionQueries,
        IMatchSelectionStore selectionStore,
        IClubSquadStore squadStore,
        IPromiseStore? promiseStore = null)
    {
        _selectionQueries = selectionQueries ?? throw new ArgumentNullException(nameof(selectionQueries));
        _selectionStore = selectionStore ?? throw new ArgumentNullException(nameof(selectionStore));
        _squadStore = squadStore ?? throw new ArgumentNullException(nameof(squadStore));
        _promiseStore = promiseStore;
    }

    public void BindPromiseStore(IPromiseStore promiseStore) =>
        _promiseStore = promiseStore ?? throw new ArgumentNullException(nameof(promiseStore));

    public PreMatchPromiseTensionReadModel? GetForNextDueMatch(int currentDayNumber)
    {
        var pending = _selectionQueries.GetNextDueManagedFixture(currentDayNumber);
        if (pending is null)
        {
            return null;
        }

        if (_promiseStore is null)
        {
            return new PreMatchPromiseTensionReadModel(
                pending.FixtureId,
                pending.ManagedClubId,
                pending.IsApproved,
                HasTension: false,
                ToneNone,
                "Söz gerilimi: bağlı değil.",
                Array.Empty<PreMatchPromiseTensionLine>());
        }

        var clubId = new ClubId(pending.ManagedClubId);
        var startingPromises = _promiseStore.Promises
            .Where(p =>
                p.IsActive
                && p.Kind == PromiseKind.StartingOpportunity
                && p.ClubId == clubId
                && p.Promisee.Kind == ActorKind.Player)
            .OrderBy(p => p.DeadlineOn.DayNumber)
            .ThenBy(p => p.PromiseId.Value)
            .ToArray();

        if (startingPromises.Length == 0)
        {
            return new PreMatchPromiseTensionReadModel(
                pending.FixtureId,
                pending.ManagedClubId,
                pending.IsApproved,
                HasTension: false,
                ToneNone,
                "Söz gerilimi yok — aktif İlk 11 sözü yok.",
                Array.Empty<PreMatchPromiseTensionLine>());
        }

        var (startingSlots, benchSlots) = ResolveSlots(pending.FixtureId, clubId);
        var squad = _squadStore.Get(clubId);
        var lines = new List<PreMatchPromiseTensionLine>();

        foreach (var promise in startingPromises)
        {
            var member = squad?.Members.FirstOrDefault(m => m.PlayerId.Value == promise.Promisee.Id);
            if (member is null)
            {
                lines.Add(new PreMatchPromiseTensionLine(
                    promise.PromiseId.Value,
                    promise.Promisee.Id,
                    SlotIndex: -1,
                    "İlk 11",
                    PlacementOut,
                    $"Oyuncu#{promise.Promisee.Id} kadroda değil — söz risk altında."));
                continue;
            }

            var placement = startingSlots.Contains(member.SlotIndex)
                ? PlacementStarting
                : benchSlots.Contains(member.SlotIndex)
                    ? PlacementBench
                    : PlacementOut;

            var summary = placement switch
            {
                PlacementStarting =>
                    $"Oyuncu#{promise.Promisee.Id} (slot {member.SlotIndex}) XI'da — söz yolunda.",
                PlacementBench =>
                    $"Oyuncu#{promise.Promisee.Id} (slot {member.SlotIndex}) YEDEKTE — söz risk altında.",
                _ =>
                    $"Oyuncu#{promise.Promisee.Id} (slot {member.SlotIndex}) maç günü kadrosunda değil — söz risk altında.",
            };

            lines.Add(new PreMatchPromiseTensionLine(
                promise.PromiseId.Value,
                promise.Promisee.Id,
                member.SlotIndex,
                "İlk 11",
                placement,
                summary));
        }

        var atRisk = lines.Any(l => l.PlacementCode is PlacementBench or PlacementOut);
        var tone = atRisk ? ToneAtRisk : ToneOnTrack;
        var headline = atRisk
            ? lines.First(l => l.PlacementCode is PlacementBench or PlacementOut).SummaryLine
            : lines[0].SummaryLine;

        return new PreMatchPromiseTensionReadModel(
            pending.FixtureId,
            pending.ManagedClubId,
            pending.IsApproved,
            HasTension: true,
            tone,
            headline,
            lines);
    }

    private (IReadOnlyList<int> Starting, IReadOnlyList<int> Bench) ResolveSlots(
        long fixtureId,
        ClubId clubId)
    {
        var selection = _selectionStore.Get(new FixtureId(fixtureId), clubId);
        if (selection is not null)
        {
            return (selection.StartingSlotIndices, selection.BenchSlotIndices);
        }

        return (
            Enumerable.Range(0, MatchSelection.StartingXiSize).ToArray(),
            Enumerable.Range(MatchSelection.StartingXiSize, MatchSelection.MaxBenchSize).ToArray());
    }
}
