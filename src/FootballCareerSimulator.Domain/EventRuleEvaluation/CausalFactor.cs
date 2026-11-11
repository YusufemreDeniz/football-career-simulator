using FootballCareerSimulator.Domain.SocialContinuity;

namespace FootballCareerSimulator.Domain.EventRuleEvaluation;

/// <summary>
/// Bir sonuca etki eden tekil neden faktörü (docs/20 §6 / §29).
/// </summary>
public sealed record CausalFactor(
    string FactorCode,
    CausalFactorWeight Weight,
    string SummaryKey,
    ActorRef? SourceActor,
    int? ScoreDelta,
    int OccurredAtDayNumber);
