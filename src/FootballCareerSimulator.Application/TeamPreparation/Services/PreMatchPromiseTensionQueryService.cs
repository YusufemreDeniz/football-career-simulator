using FootballCareerSimulator.Application.SocialContinuity.Ports;
using FootballCareerSimulator.Application.TeamPreparation.Ports;
using FootballCareerSimulator.Application.TeamPreparation.Queries;
using FootballCareerSimulator.Domain.Competition;
using FootballCareerSimulator.Domain.Shared;
using FootballCareerSimulator.Domain.SocialContinuity;
using FootballCareerSimulator.Domain.TeamPreparation;

namespace FootballCareerSimulator.Application.TeamPreparation.Services;

/// <summary>
/// Maç öncesi: aktif İlk 11 / forma süresi sözü vs mevcut (veya varsayılan) kadro yerleşimi.
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
        var activePromises = _promiseStore.Promises
            .Where(p =>
                p.IsActive
                && p.Kind is PromiseKind.StartingOpportunity or PromiseKind.PlayingTime
                && p.ClubId == clubId
                && p.Promisee.Kind == ActorKind.Player)
            .OrderBy(p => p.DeadlineOn.DayNumber)
            .ThenBy(p => p.PromiseId.Value)
            .ToArray();

        if (activePromises.Length == 0)
        {
            return new PreMatchPromiseTensionReadModel(
                pending.FixtureId,
                pending.ManagedClubId,
                pending.IsApproved,
                HasTension: false,
                ToneNone,
                "Söz gerilimi yok — aktif forma / İlk 11 sözü yok.",
                Array.Empty<PreMatchPromiseTensionLine>());
        }

        var (startingSlots, benchSlots) = ResolveSlots(pending.FixtureId, clubId);
        var squad = _squadStore.Get(clubId);
        var lines = new List<PreMatchPromiseTensionLine>();

        foreach (var promise in activePromises)
        {
            var kindName = promise.Kind == PromiseKind.StartingOpportunity ? "İlk 11" : "Oyun süresi";
            var member = squad?.Members.FirstOrDefault(m => m.PlayerId.Value == promise.Promisee.Id);
            if (member is null)
            {
                lines.Add(new PreMatchPromiseTensionLine(
                    promise.PromiseId.Value,
                    promise.Promisee.Id,
                    SlotIndex: -1,
                    kindName,
                    PlacementOut,
                    $"Oyuncu#{promise.Promisee.Id} kadroda değil — {kindName} sözü risk altında."));
                continue;
            }

            var placement = startingSlots.Contains(member.SlotIndex)
                ? PlacementStarting
                : benchSlots.Contains(member.SlotIndex)
                    ? PlacementBench
                    : PlacementOut;

            var summary = BuildSummary(promise.Kind, promise.Promisee.Id, member.SlotIndex, placement);
            lines.Add(new PreMatchPromiseTensionLine(
                promise.PromiseId.Value,
                promise.Promisee.Id,
                member.SlotIndex,
                kindName,
                placement,
                summary));
        }

        var atRisk = lines.Any(IsAtRisk);
        var tone = atRisk ? ToneAtRisk : ToneOnTrack;
        var headline = atRisk
            ? lines.First(IsAtRisk).SummaryLine
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

    private static bool IsAtRisk(PreMatchPromiseTensionLine line) =>
        line.KindName == "İlk 11"
            ? line.PlacementCode is PlacementBench or PlacementOut
            : line.PlacementCode == PlacementOut;

    private static string BuildSummary(
        PromiseKind kind,
        long playerId,
        int slotIndex,
        string placement) =>
        kind switch
        {
            PromiseKind.StartingOpportunity => placement switch
            {
                PlacementStarting =>
                    $"Oyuncu#{playerId} (slot {slotIndex}) XI'da — İlk 11 sözü yolunda.",
                PlacementBench =>
                    $"Oyuncu#{playerId} (slot {slotIndex}) YEDEKTE — İlk 11 sözü risk altında.",
                _ =>
                    $"Oyuncu#{playerId} (slot {slotIndex}) maç günü kadrosunda değil — İlk 11 sözü risk altında.",
            },
            PromiseKind.PlayingTime => placement switch
            {
                PlacementStarting =>
                    $"Oyuncu#{playerId} (slot {slotIndex}) XI'da — oyun süresi sözü yolunda.",
                PlacementBench =>
                    $"Oyuncu#{playerId} (slot {slotIndex}) YEDEKTE — oyun süresi sözü yolunda.",
                _ =>
                    $"Oyuncu#{playerId} (slot {slotIndex}) maç günü kadrosunda değil — oyun süresi sözü risk altında.",
            },
            _ => $"Oyuncu#{playerId} söz durumu belirsiz.",
        };

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
