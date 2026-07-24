using FootballCareerSimulator.Application.Interaction.Ports;
using FootballCareerSimulator.Application.ManagerCareer.Ports;
using FootballCareerSimulator.Application.SocialContinuity.Services;
using FootballCareerSimulator.Domain.Interaction;
using FootballCareerSimulator.Domain.ManagerCareer;
using FootballCareerSimulator.Domain.PlayerCareer;
using FootballCareerSimulator.Domain.Shared;
using FootballCareerSimulator.Domain.WorldCalendar;

namespace FootballCareerSimulator.Application.Interaction.Services;

/// <summary>
/// DecisionRequest owner (iskelet). Forma süresi talebi → Promise entegrasyonu.
/// </summary>
public sealed class DecisionRequestService
{
    public const int DefaultPlayingTimeTargetAppearances = 3;
    public const int DefaultDeadlineDays = 14;

    private readonly IDecisionRequestStore _store;
    private readonly IManagerCareerStore _managerCareerStore;
    private readonly PlayingTimePromiseService? _playingTime;

    public DecisionRequestService(
        IDecisionRequestStore store,
        IManagerCareerStore managerCareerStore,
        PlayingTimePromiseService? playingTime = null)
    {
        _store = store ?? throw new ArgumentNullException(nameof(store));
        _managerCareerStore = managerCareerStore
            ?? throw new ArgumentNullException(nameof(managerCareerStore));
        _playingTime = playingTime;
    }

    public DecisionRequest OpenPlayingTimeRequest(
        PlayerId subjectPlayerId,
        GameDate day,
        int? deadlineDays = null,
        bool isHardBlocker = true)
    {
        var career = _managerCareerStore.Career;
        if (!career.IsEmployed || career.ActiveEmployment is null)
        {
            throw new InteractionInvariantViolationException(
                "Manager must be employed to open a playing-time decision request.");
        }

        var clubId = career.ActiveEmployment.ClubId;
        var hasOpen = _store.Requests.Any(r =>
            r.IsOpen
            && r.Kind == DecisionRequestKind.PlayingTimeRequest
            && r.SubjectPlayerId == subjectPlayerId
            && r.ClubId == clubId);
        if (hasOpen)
        {
            throw new InteractionInvariantViolationException(
                $"Player {subjectPlayerId.Value} already has an open playing-time decision request.");
        }

        var days = deadlineDays ?? DefaultDeadlineDays;
        if (days < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(deadlineDays), days, "Deadline days must be positive.");
        }

        var nextId = _store.Requests.Count == 0
            ? 1L
            : _store.Requests.Max(r => r.DecisionRequestId.Value) + 1;
        var request = DecisionRequest.OpenPlayingTimeRequest(
            new DecisionRequestId(nextId),
            career.ManagerId,
            subjectPlayerId,
            clubId,
            day,
            day.AddDays(days),
            isHardBlocker);
        _store.Upsert(request);
        return request;
    }

    public DecisionRequest Answer(
        DecisionRequestId decisionRequestId,
        string optionCode,
        GameDate day,
        int playingTimeTargetAppearances = DefaultPlayingTimeTargetAppearances)
    {
        var current = Require(decisionRequestId);
        var answered = current.Answer(optionCode, day);
        _store.Upsert(answered);

        if (answered.Kind == DecisionRequestKind.PlayingTimeRequest
            && answered.SelectedOptionCode == DecisionRequest.OptionGrantPlayingTimePromise)
        {
            if (_playingTime is null)
            {
                throw new InteractionInvariantViolationException(
                    "Playing-time promise service is required to grant a playing-time decision.");
            }

            _playingTime.Create(
                answered.ManagerId,
                answered.SubjectPlayerId,
                answered.ClubId,
                playingTimeTargetAppearances,
                answered.DeadlineOn,
                day);
        }

        return answered;
    }

    public int ExpireDue(GameDate day)
    {
        var expired = 0;
        foreach (var request in _store.Requests.Where(r => r.IsOpen).ToArray())
        {
            var next = request.ExpireIfDue(day);
            if (next.Status != request.Status)
            {
                _store.Upsert(next);
                expired++;
            }
        }

        return expired;
    }

    private DecisionRequest Require(DecisionRequestId id) =>
        _store.Get(id)
        ?? throw new InteractionInvariantViolationException(
            $"Decision request #{id.Value} not found.");
}
