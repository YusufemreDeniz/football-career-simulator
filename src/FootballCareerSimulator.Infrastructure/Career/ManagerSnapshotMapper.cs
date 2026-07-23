using FootballCareerSimulator.Domain.Competition;
using FootballCareerSimulator.Domain.ManagerCareer;
using FootballCareerSimulator.Domain.Shared;
using FootballCareerSimulator.Domain.WorldCalendar;

namespace FootballCareerSimulator.Infrastructure.Career;

internal static class ManagerSnapshotMapper
{
    public static ManagerCareer ToDomain(
        long managerId,
        string displayName,
        long? employedClubId,
        int? employmentStartedDayNumber,
        GameDate fallbackStartDate,
        int? seasonExpectation = null,
        int? boardConfidence = null,
        int? riskBand = null,
        long? lastAssessedFixtureId = null,
        string? lastAssessmentReasonCode = null,
        int? employmentStatus = null,
        int? employmentEndReason = null,
        long? lastClubId = null,
        long? dismissedDueToFixtureId = null,
        int? dismissedAtDayNumber = null,
        long? pendingOfferId = null,
        long? pendingOfferClubId = null,
        int? pendingOfferStatus = null,
        int? pendingOfferCreatedDayNumber = null)
    {
        JobOffer? pendingOffer = null;
        if (pendingOfferId is long offerId
            && pendingOfferClubId is long offerClubId
            && pendingOfferStatus is int offerStatus
            && pendingOfferCreatedDayNumber is int offerDay)
        {
            pendingOffer = JobOffer.Rehydrate(
                new JobOfferId(offerId),
                new ClubId(offerClubId),
                (JobOfferStatus)offerStatus,
                GameDate.FromDayNumber(offerDay));
        }

        var status = employmentStatus is int statusValue
            ? (ManagerEmploymentStatus)statusValue
            : employedClubId is null
                ? ManagerEmploymentStatus.Unemployed
                : ManagerEmploymentStatus.Employed;

        if (status == ManagerEmploymentStatus.Unemployed || employedClubId is null)
        {
            return ManagerCareer.Rehydrate(
                new ManagerId(managerId),
                displayName,
                activeEmployment: null,
                ManagerEmploymentStatus.Unemployed,
                terminationReason: employmentEndReason is int endReason
                    ? (EmploymentEndReason)endReason
                    : EmploymentEndReason.Dismissed,
                lastClubId: lastClubId is long club ? new ClubId(club) : null,
                dismissedDueToFixtureId: dismissedDueToFixtureId is long dismissedFixture
                    ? new FixtureId(dismissedFixture)
                    : null,
                dismissedAt: dismissedAtDayNumber is int day
                    ? GameDate.FromDayNumber(day)
                    : null,
                pendingJobOffer: pendingOffer);
        }

        if (employmentStartedDayNumber is null)
        {
            return ManagerCareer.StartNewCareerForClubStrength(
                new ManagerId(managerId),
                displayName,
                new ClubId(employedClubId.Value),
                fallbackStartDate,
                clubSportiveStrength: 50);
        }

        var expectation = seasonExpectation is int expectationValue
            ? (SeasonExpectationTier)expectationValue
            : SeasonExpectationTier.MidTable;
        var confidence = new BoardConfidence(boardConfidence ?? BoardConfidence.DefaultInitialValue);
        var band = riskBand is int riskValue
            ? (EmploymentRiskBand)riskValue
            : EmploymentRisk.FromConfidence(confidence.Value);
        var fixtureId = lastAssessedFixtureId is long fixture
            ? new FixtureId(fixture)
            : (FixtureId?)null;

        var employment = ClubEmployment.Rehydrate(
            new ClubId(employedClubId.Value),
            GameDate.FromDayNumber(employmentStartedDayNumber.Value),
            expectation,
            confidence,
            band,
            fixtureId,
            lastAssessmentReasonCode);

        return ManagerCareer.Rehydrate(
            new ManagerId(managerId),
            displayName,
            employment,
            ManagerEmploymentStatus.Employed,
            terminationReason: null,
            lastClubId: employment.ClubId,
            dismissedDueToFixtureId: null,
            dismissedAt: null,
            pendingJobOffer: null);
    }
}
