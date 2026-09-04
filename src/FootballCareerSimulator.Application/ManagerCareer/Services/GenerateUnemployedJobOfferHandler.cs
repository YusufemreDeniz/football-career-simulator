using FootballCareerSimulator.Application.ClubGovernance.Ports;
using FootballCareerSimulator.Application.ManagerCareer.Commands;
using FootballCareerSimulator.Application.ManagerCareer.Ports;
using FootballCareerSimulator.Application.WorldCalendar.Ports;
using FootballCareerSimulator.Domain.ManagerCareer;
using FootballCareerSimulator.Domain.Shared;

namespace FootballCareerSimulator.Application.ManagerCareer.Services;

public sealed class GenerateUnemployedJobOfferHandler : ICommandIdempotencyReset
{
    private readonly IManagerCareerStore _managerStore;
    private readonly IClubRegistryStore _clubRegistryStore;
    private readonly IWorldTimelineStore _timelineStore;
    private readonly Dictionary<Guid, GenerateUnemployedJobOfferResult> _completed = new();

    public GenerateUnemployedJobOfferHandler(
        IManagerCareerStore managerStore,
        IClubRegistryStore clubRegistryStore,
        IWorldTimelineStore timelineStore)
    {
        _managerStore = managerStore ?? throw new ArgumentNullException(nameof(managerStore));
        _clubRegistryStore = clubRegistryStore ?? throw new ArgumentNullException(nameof(clubRegistryStore));
        _timelineStore = timelineStore ?? throw new ArgumentNullException(nameof(timelineStore));
    }

    public GenerateUnemployedJobOfferResult Handle(GenerateUnemployedJobOfferCommand command)
    {
        ArgumentNullException.ThrowIfNull(command);

        if (_completed.TryGetValue(command.CommandId, out var cached))
        {
            return cached;
        }

        var career = _managerStore.Career;
        if (career.IsEmployed)
        {
            throw new ManagerCareerInvariantViolationException(
                "Job offers can only be generated while unemployed.");
        }

        if (career.PendingJobOffer is { Status: JobOfferStatus.Offered })
        {
            var held = new GenerateUnemployedJobOfferResult(
                true,
                WasAlreadyHeld: true,
                career.PendingJobOffer.Id.Value,
                career.PendingJobOffer.ClubId.Value);
            _completed[command.CommandId] = held;
            return held;
        }

        var clubId = SelectOfferClub(career.LastClubId);
        var day = _timelineStore.Timeline.CurrentDate;
        var offerId = new JobOfferId((day.DayNumber * 1000L) + clubId.Value);
        var offer = JobOffer.CreateOffered(offerId, clubId, day);
        var receive = career.ReceiveJobOffer(offer);
        _managerStore.Replace(receive.Career);

        var result = new GenerateUnemployedJobOfferResult(
            true,
            receive.WasAlreadyHeld,
            receive.OfferId,
            receive.ClubId);
        _completed[command.CommandId] = result;
        return result;
    }

    public void ResetIdempotencyCache() => _completed.Clear();

    private ClubId SelectOfferClub(ClubId? lastClubId)
    {
        var clubs = _clubRegistryStore.Registry.Clubs
            .OrderBy(club => club.Id.Value)
            .ToArray();

        if (clubs.Length == 0)
        {
            throw new ManagerCareerInvariantViolationException("No clubs available for a job offer.");
        }

        var candidates = lastClubId is ClubId last
            ? clubs.Where(club => club.Id != last).ToArray()
            : clubs;

        if (candidates.Length == 0)
        {
            candidates = clubs;
        }

        var dayNumber = _timelineStore.Timeline.CurrentDate.DayNumber;
        var index = Math.Abs(dayNumber) % candidates.Length;
        return candidates[index].Id;
    }
}
