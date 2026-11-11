using FootballCareerSimulator.Application.EventRuleEvaluation.Services;
using FootballCareerSimulator.Domain.EventRuleEvaluation;
using FootballCareerSimulator.Domain.WorldCalendar.Events;

namespace FootballCareerSimulator.Application.EventRuleEvaluation.Reactions;

public interface IReactionRule
{
    string RuleId { get; }

    IReadOnlyList<ReactionIntent> React(
        WorldCalendarDomainEvent domainEvent,
        EvaluatedWorldCalendarEffect appliedEffect);
}
