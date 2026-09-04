using FootballCareerSimulator.Domain.Discipline;

namespace FootballCareerSimulator.Application.Discipline.Ports;

public interface IDisciplinaryActionStore
{
    IReadOnlyList<DisciplinaryAction> Actions { get; }

    DisciplinaryAction? Get(DisciplinaryActionId id);

    bool HasWarningForPlayerAtClub(long playerId, long clubId);

    void Upsert(DisciplinaryAction action);

    void ReplaceAll(IEnumerable<DisciplinaryAction> actions);
}
