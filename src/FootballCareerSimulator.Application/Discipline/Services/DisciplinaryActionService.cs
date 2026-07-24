using FootballCareerSimulator.Application.Discipline.Ports;
using FootballCareerSimulator.Domain.Discipline;
using FootballCareerSimulator.Domain.Interaction;
using FootballCareerSimulator.Domain.ManagerCareer;
using FootballCareerSimulator.Domain.PlayerCareer;
using FootballCareerSimulator.Domain.Shared;
using FootballCareerSimulator.Domain.WorldCalendar;

namespace FootballCareerSimulator.Application.Discipline.Services;

/// <summary>
/// Disiplin authoritative owner (iskelet). Relationship/Memory yazmaz.
/// </summary>
public sealed class DisciplinaryActionService
{
    private readonly IDisciplinaryActionStore _store;

    public DisciplinaryActionService(IDisciplinaryActionStore store)
    {
        _store = store ?? throw new ArgumentNullException(nameof(store));
    }

    public DisciplinaryAction ApplyFromDecision(DecisionRequest request, DisciplinaryActionKind kind, GameDate day)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (request.Kind != DecisionRequestKind.DisciplineRequest)
        {
            throw new DisciplineInvariantViolationException(
                "Disciplinary action from decision requires DisciplineRequest kind.");
        }

        if (kind == DisciplinaryActionKind.Fine
            && !_store.HasWarningForPlayerAtClub(request.SubjectPlayerId.Value, request.ClubId.Value))
        {
            throw new DisciplineInvariantViolationException(
                "Fine requires a prior warning for the player at this club.");
        }

        var duplicate = _store.Actions.FirstOrDefault(a =>
            a.SourceDecisionRequestId == request.DecisionRequestId);
        if (duplicate is not null)
        {
            return duplicate;
        }

        return Apply(
            kind,
            request.ManagerId,
            request.SubjectPlayerId,
            request.ClubId,
            day,
            request.DecisionRequestId);
    }

    public DisciplinaryAction Apply(
        DisciplinaryActionKind kind,
        ManagerId managerId,
        PlayerId subjectPlayerId,
        ClubId clubId,
        GameDate day,
        DecisionRequestId? sourceDecisionRequestId = null)
    {
        if (kind == DisciplinaryActionKind.Fine
            && !_store.HasWarningForPlayerAtClub(subjectPlayerId.Value, clubId.Value))
        {
            throw new DisciplineInvariantViolationException(
                "Fine requires a prior warning for the player at this club.");
        }

        var nextId = _store.Actions.Count == 0
            ? 1L
            : _store.Actions.Max(a => a.DisciplinaryActionId.Value) + 1;
        var action = DisciplinaryAction.Apply(
            new DisciplinaryActionId(nextId),
            kind,
            managerId,
            subjectPlayerId,
            clubId,
            day,
            sourceDecisionRequestId);
        _store.Upsert(action);
        return action;
    }
}
