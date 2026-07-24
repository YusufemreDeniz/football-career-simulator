using FootballCareerSimulator.Application.SocialContinuity.Ports;
using FootballCareerSimulator.Application.SocialContinuity.Queries;
using FootballCareerSimulator.Domain.SocialContinuity;

namespace FootballCareerSimulator.Application.SocialContinuity.Services;

/// <summary>
/// Memory salt-okunur sorguları. Relationship / decay / reinforcement yok.
/// </summary>
public sealed class MemoryQueryService
{
    private readonly IMemoryStore _store;

    public MemoryQueryService(IMemoryStore store)
    {
        _store = store ?? throw new ArgumentNullException(nameof(store));
    }

    public ActorMemoriesReadModel GetActiveForActor(
        ActorKind actorKind,
        long actorId,
        int take = 8)
    {
        if (take < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(take), take, "Take must be positive.");
        }

        var remembering = new ActorRef(actorKind, actorId);
        var active = _store.Memories
            .Where(m => m.RememberingActor == remembering && m.Status == MemoryStatus.Active)
            .OrderByDescending(m => m.CurrentInfluence)
            .ThenByDescending(m => m.CreatedOn.DayNumber)
            .ThenByDescending(m => m.MemoryId.Value)
            .ToArray();

        return new ActorMemoriesReadModel(
            actorKind.ToString(),
            actorId,
            active.Length,
            active.Take(take).Select(ToLine).ToArray());
    }

    public IReadOnlyList<MemoryCategoryCountReadModel> GetActiveCategoryCounts() =>
        _store.Memories
            .Where(m => m.Status == MemoryStatus.Active)
            .GroupBy(m => m.Category)
            .OrderBy(g => g.Key)
            .Select(g => new MemoryCategoryCountReadModel(CategoryDisplayName(g.Key), g.Count()))
            .ToArray();

    private static MemoryLineReadModel ToLine(MemoryRecord memory) =>
        new(
            memory.MemoryId.Value,
            CategoryDisplayName(memory.Category),
            ValenceDisplayName(memory.Valence),
            memory.Status.ToString(),
            memory.RememberingActor.Kind.ToString(),
            memory.RememberingActor.Id,
            memory.SubjectKind.ToString(),
            memory.SubjectId,
            memory.BaseImportance,
            memory.CurrentInfluence,
            memory.CreatedOn.DayNumber,
            memory.RuleId,
            memory.RelatedPromiseId?.Value);

    private static string CategoryDisplayName(MemoryCategory category) =>
        category switch
        {
            MemoryCategory.Promise => "Söz",
            MemoryCategory.Selection => "Kadro",
            MemoryCategory.Trust => "Güven",
            MemoryCategory.Transfer => "Transfer",
            MemoryCategory.Career => "Kariyer",
            MemoryCategory.ClubHistory => "Kulüp geçmişi",
            MemoryCategory.MatchPerformance => "Maç performansı",
            _ => category.ToString(),
        };

    private static string ValenceDisplayName(MemoryValence valence) =>
        valence switch
        {
            MemoryValence.Positive => "Olumlu",
            MemoryValence.Negative => "Olumsuz",
            _ => "Nötr",
        };
}
