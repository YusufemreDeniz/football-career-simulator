namespace FootballCareerSimulator.Domain.ManagerCareer;

using FootballCareerSimulator.Domain.Shared;
using FootballCareerSimulator.Domain.WorldCalendar;

public sealed class ManagerCareer
{
    private ManagerCareer(ManagerId managerId, string displayName, ClubEmployment? activeEmployment)
    {
        ManagerId = managerId;
        DisplayName = displayName;
        ActiveEmployment = activeEmployment;
    }

    public ManagerId ManagerId { get; }

    public string DisplayName { get; }

    public ClubEmployment? ActiveEmployment { get; }

    public static ManagerCareer StartNewCareer(
        ManagerId managerId,
        string displayName,
        ClubId startingClubId,
        GameDate startedAt)
    {
        if (string.IsNullOrWhiteSpace(displayName))
        {
            throw new ManagerCareerInvariantViolationException("Manager display name cannot be empty.");
        }

        return new ManagerCareer(
            managerId,
            displayName.Trim(),
            ClubEmployment.Create(startingClubId, startedAt));
    }

    public static ManagerCareer Rehydrate(
        ManagerId managerId,
        string displayName,
        ClubEmployment? activeEmployment) =>
        new(managerId, displayName, activeEmployment);
}
