using FootballCareerSimulator.Domain.ManagerCareer;
using FootballCareerSimulator.Domain.WorldCalendar;
using System.Security.Cryptography;

namespace FootballCareerSimulator.Presentation;

public sealed record CareerStartConfiguration(
    string ManagerName,
    long StartingClubId,
    GameDate StartingDate,
    int RootSeed,
    StartingBackground? StartingBackground = null)
{
    public static CareerStartConfiguration Create(
        string managerName,
        long startingClubId,
        GameDate? startingDate = null,
        int? rootSeed = null,
        StartingBackground? startingBackground = null)
    {
        var normalizedName = managerName?.Trim() ?? string.Empty;
        if (normalizedName.Length is < 2 or > 48)
        {
            throw new ArgumentException("Manager name must contain 2 to 48 characters.", nameof(managerName));
        }

        if (startingClubId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(startingClubId));
        }

        return new CareerStartConfiguration(
            normalizedName,
            startingClubId,
            startingDate ?? ProductionCareerWorldConstraints.DefaultOpeningDate,
            rootSeed ?? RandomNumberGenerator.GetInt32(1, int.MaxValue),
            startingBackground);
    }
}
