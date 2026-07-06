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
        GameDate fallbackStartDate)
    {
        ClubEmployment? employment = employedClubId is null || employmentStartedDayNumber is null
            ? null
            : ClubEmployment.Rehydrate(
                new ClubId(employedClubId.Value),
                GameDate.FromDayNumber(employmentStartedDayNumber.Value));

        if (employment is null)
        {
            return ManagerCareer.StartNewCareer(
                new ManagerId(managerId),
                displayName,
                new ClubId(1),
                fallbackStartDate);
        }

        return ManagerCareer.Rehydrate(new ManagerId(managerId), displayName, employment);
    }
}
