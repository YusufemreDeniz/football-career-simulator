using FootballCareerSimulator.Application.ClubGovernance.Ports;
using FootballCareerSimulator.Application.ManagerCareer.Commands;
using FootballCareerSimulator.Application.ManagerCareer.Ports;
using FootballCareerSimulator.Application.SocialContinuity.Services;
using FootballCareerSimulator.Application.WorldCalendar.Ports;
using FootballCareerSimulator.Domain.ManagerCareer;
using FootballCareerSimulator.Domain.Shared;

namespace FootballCareerSimulator.Application.ManagerCareer.Services;

public sealed class AcceptPendingJobOfferHandler : ICommandIdempotencyReset
{
    private readonly IManagerCareerStore _managerStore;
    private readonly IClubRegistryStore _clubRegistryStore;
    private readonly IWorldTimelineStore _timelineStore;
    private CareerMemoryService? _careerMemory;
    private ClubHistoryMemoryService? _clubHistoryMemory;
    private readonly Dictionary<Guid, AcceptPendingJobOfferResult> _completed = new();

    public AcceptPendingJobOfferHandler(
        IManagerCareerStore managerStore,
        IClubRegistryStore clubRegistryStore,
        IWorldTimelineStore timelineStore)
    {
        _managerStore = managerStore ?? throw new ArgumentNullException(nameof(managerStore));
        _clubRegistryStore = clubRegistryStore ?? throw new ArgumentNullException(nameof(clubRegistryStore));
        _timelineStore = timelineStore ?? throw new ArgumentNullException(nameof(timelineStore));
    }

    public void BindCareerMemory(CareerMemoryService careerMemory) =>
        _careerMemory = careerMemory ?? throw new ArgumentNullException(nameof(careerMemory));

    public void BindClubHistoryMemory(ClubHistoryMemoryService clubHistoryMemory) =>
        _clubHistoryMemory = clubHistoryMemory ?? throw new ArgumentNullException(nameof(clubHistoryMemory));

    public AcceptPendingJobOfferResult Handle(AcceptPendingJobOfferCommand command)
    {
        ArgumentNullException.ThrowIfNull(command);

        if (_completed.TryGetValue(command.CommandId, out var cached))
        {
            return cached;
        }

        var career = _managerStore.Career;
        var offer = career.PendingJobOffer
            ?? throw new ManagerCareerInvariantViolationException("No pending job offer.");
        var isReturn = career.LastClubId is { } last && last == offer.ClubId;

        var club = _clubRegistryStore.Registry.GetClubOrThrow(offer.ClubId);
        var startedAt = _timelineStore.Timeline.CurrentDate;
        var accepted = career.AcceptPendingJobOffer(
            startedAt,
            SeasonExpectation.FromSportiveStrength(club.SportiveStrength));

        _managerStore.Replace(accepted.Career);
        var hiredClub = new ClubId(accepted.ClubId);
        var offerId = new JobOfferId(accepted.OfferId);
        _careerMemory?.RecordHiring(
            accepted.Career.ManagerId,
            hiredClub,
            offerId,
            startedAt);
        if (isReturn)
        {
            _clubHistoryMemory?.RecordManagerReturned(
                accepted.Career.ManagerId,
                hiredClub,
                offerId,
                startedAt);
        }

        var result = new AcceptPendingJobOfferResult(true, accepted.OfferId, accepted.ClubId);
        _completed[command.CommandId] = result;
        return result;
    }

    public void ResetIdempotencyCache() => _completed.Clear();
}
