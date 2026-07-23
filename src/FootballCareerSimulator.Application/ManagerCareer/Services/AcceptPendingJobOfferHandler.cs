using FootballCareerSimulator.Application.ClubGovernance.Ports;
using FootballCareerSimulator.Application.ManagerCareer.Commands;
using FootballCareerSimulator.Application.ManagerCareer.Ports;
using FootballCareerSimulator.Application.WorldCalendar.Ports;
using FootballCareerSimulator.Domain.ManagerCareer;

namespace FootballCareerSimulator.Application.ManagerCareer.Services;

public sealed class AcceptPendingJobOfferHandler : ICommandIdempotencyReset
{
    private readonly IManagerCareerStore _managerStore;
    private readonly IClubRegistryStore _clubRegistryStore;
    private readonly IWorldTimelineStore _timelineStore;
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

        var club = _clubRegistryStore.Registry.GetClubOrThrow(offer.ClubId);
        var startedAt = _timelineStore.Timeline.CurrentDate;
        var accepted = career.AcceptPendingJobOffer(
            startedAt,
            SeasonExpectation.FromSportiveStrength(club.SportiveStrength));

        _managerStore.Replace(accepted.Career);

        var result = new AcceptPendingJobOfferResult(true, accepted.OfferId, accepted.ClubId);
        _completed[command.CommandId] = result;
        return result;
    }

    public void ResetIdempotencyCache() => _completed.Clear();
}
