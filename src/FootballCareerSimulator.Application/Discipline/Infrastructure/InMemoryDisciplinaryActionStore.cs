using FootballCareerSimulator.Application.Discipline.Ports;
using FootballCareerSimulator.Domain.Discipline;

namespace FootballCareerSimulator.Application.Discipline.Infrastructure;

public sealed class InMemoryDisciplinaryActionStore : IDisciplinaryActionStore
{
    private readonly Dictionary<long, DisciplinaryAction> _actions = new();

    public IReadOnlyList<DisciplinaryAction> Actions =>
        _actions.Values.OrderBy(a => a.DisciplinaryActionId.Value).ToArray();

    public DisciplinaryAction? Get(DisciplinaryActionId id) =>
        _actions.TryGetValue(id.Value, out var action) ? action : null;

    public bool HasWarningForPlayerAtClub(long playerId, long clubId) =>
        _actions.Values.Any(a =>
            a.Kind == DisciplinaryActionKind.Warning
            && a.SubjectPlayerId.Value == playerId
            && a.ClubId.Value == clubId);

    public void Upsert(DisciplinaryAction action)
    {
        ArgumentNullException.ThrowIfNull(action);
        _actions[action.DisciplinaryActionId.Value] = action;
    }

    public void ReplaceAll(IEnumerable<DisciplinaryAction> actions)
    {
        ArgumentNullException.ThrowIfNull(actions);
        _actions.Clear();
        foreach (var action in actions)
        {
            Upsert(action);
        }
    }
}
