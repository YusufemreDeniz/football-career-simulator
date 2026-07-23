namespace FootballCareerSimulator.Application.ManagerCareer.Services;

using FootballCareerSimulator.Application.ManagerCareer.Ports;
using FootballCareerSimulator.Application.ManagerCareer.Queries;

public sealed class ManagerCareerQueryService
{
    private readonly IManagerCareerStore _store;

    public ManagerCareerQueryService(IManagerCareerStore store)
    {
        _store = store ?? throw new ArgumentNullException(nameof(store));
    }

    public ManagerCareerReadModel GetCareer()
    {
        var career = _store.Career;
        var employment = career.ActiveEmployment;
        var offer = career.PendingJobOffer;
        return new ManagerCareerReadModel(
            career.ManagerId.Value,
            career.DisplayName,
            career.EmploymentStatus.ToString(),
            employment?.ClubId.Value,
            employment?.StartedAt.DayNumber,
            employment?.SeasonExpectation.ToString(),
            employment?.BoardConfidence.Value,
            employment?.RiskBand.ToString(),
            employment?.LastAssessmentReasonCode,
            employment?.LastAssessedFixtureId?.Value,
            career.TerminationReason?.ToString(),
            career.LastClubId?.Value,
            career.DismissedDueToFixtureId?.Value,
            career.DismissedAt?.DayNumber,
            offer?.Id.Value,
            offer?.ClubId.Value,
            offer?.Status.ToString());
    }
}
