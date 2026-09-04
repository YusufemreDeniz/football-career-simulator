using FootballCareerSimulator.Application.SocialContinuity.Ports;
using FootballCareerSimulator.Domain.Interaction;
using FootballCareerSimulator.Domain.PlayerCareer;
using FootballCareerSimulator.Domain.SocialContinuity;
using FootballCareerSimulator.Domain.WorldCalendar;

namespace FootballCareerSimulator.Application.Interaction.Services;

/// <summary>
/// Maç sonrası forma süresi talebi: SelectionBenched / SelectionOmitted hafızası
/// <see cref="DecisionRequestService.DefaultPlayingTimeTargetAppearances"/> eşiğine
/// ulaşınca (GDD “üç maç yedek”; yeni formül yok) yönetilen kulüpte PlayingTimeRequest açar.
/// Her yeni eşik diliminde en fazla bir talep; açık talep veya aktif forma sözü varken açılmaz.
/// </summary>
public sealed class PostMatchPlayingTimeDemandTrigger
{
    /// <summary>
    /// Oturum dışı / yedek birikimi eşiği — mevcut DefaultPlayingTimeTargetAppearances ile aynı (3).
    /// </summary>
    public const int SittingOutDemandThreshold =
        DecisionRequestService.DefaultPlayingTimeTargetAppearances;

    private readonly DecisionRequestService _decisions;
    private readonly IMemoryStore _memories;
    private readonly IPromiseStore? _promises;

    public PostMatchPlayingTimeDemandTrigger(
        DecisionRequestService decisions,
        IMemoryStore memories,
        IPromiseStore? promises = null)
    {
        _decisions = decisions ?? throw new ArgumentNullException(nameof(decisions));
        _memories = memories ?? throw new ArgumentNullException(nameof(memories));
        _promises = promises;
    }

    public DecisionRequest? TryOpenAfterManagedSittingOut(
        IReadOnlyList<PlayerId> benchedOrOmittedThisMatch,
        GameDate day)
    {
        ArgumentNullException.ThrowIfNull(benchedOrOmittedThisMatch);

        if (benchedOrOmittedThisMatch.Count == 0)
        {
            return null;
        }

        var candidates = benchedOrOmittedThisMatch
            .Distinct()
            .Select(id => (
                PlayerId: id,
                Pressure: CountSittingOutEvents(id),
                RequiredPressure: RequiredPressureForNextRequest(id)))
            .Where(c => c.Pressure >= c.RequiredPressure)
            .Where(c => !HasBlockingOpenRequest(c.PlayerId))
            .Where(c => !HasActivePlayingTimePromise(c.PlayerId))
            .OrderByDescending(c => c.Pressure - c.RequiredPressure)
            .ThenByDescending(c => c.Pressure)
            .ThenBy(c => c.PlayerId.Value)
            .ToArray();

        if (candidates.Length == 0)
        {
            return null;
        }

        var subject = candidates[0].PlayerId;
        try
        {
            return _decisions.OpenPlayingTimeRequest(subject, day);
        }
        catch (InteractionInvariantViolationException)
        {
            return null;
        }
    }

    public static int CountSittingOutEvents(IReadOnlyList<MemoryRecord> memories, PlayerId playerId)
    {
        ArgumentNullException.ThrowIfNull(memories);
        var remembering = new ActorRef(ActorKind.Player, playerId.Value);
        var benched = memories.Count(m =>
            m.Status == MemoryStatus.Active
            && m.RememberingActor == remembering
            && m.RuleId == MemoryRecord.SelectionBenchedRuleId
            && m.RuleVersion == MemoryRecord.SelectionBenchedRuleVersion);

        var omitted = memories
            .Where(m =>
                m.Status == MemoryStatus.Active
                && m.RememberingActor == remembering
                && m.RuleId == MemoryRecord.SelectionOmittedRuleId
                && m.RuleVersion == MemoryRecord.SelectionOmittedRuleVersion)
            .OrderByDescending(m => m.LastReinforcedOn.DayNumber)
            .ThenByDescending(m => m.MemoryId.Value)
            .FirstOrDefault();

        var omittedEvents = omitted is null ? 0 : omitted.ReinforcementCount + 1;
        return benched + omittedEvents;
    }

    private int CountSittingOutEvents(PlayerId playerId) =>
        CountSittingOutEvents(_memories.Memories, playerId);

    private int RequiredPressureForNextRequest(PlayerId playerId)
    {
        var priorRequestCount = _decisions.CountPlayerRequests(
            playerId,
            DecisionRequestKind.PlayingTimeRequest);
        return checked((priorRequestCount + 1) * SittingOutDemandThreshold);
    }

    private bool HasBlockingOpenRequest(PlayerId playerId) =>
        _decisions.HasOpenPlayerRequest(
            playerId,
            DecisionRequestKind.PlayingTimeRequest,
            DecisionRequestKind.StartingOpportunityRequest,
            DecisionRequestKind.TransferRequest);

    private bool HasActivePlayingTimePromise(PlayerId playerId)
    {
        if (_promises is null)
        {
            return false;
        }

        return _promises.Promises.Any(p =>
            p.IsActive
            && p.Kind == PromiseKind.PlayingTime
            && p.Promisee.Kind == ActorKind.Player
            && p.Promisee.Id == playerId.Value);
    }
}
