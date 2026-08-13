using FootballCareerSimulator.Domain.WorldCalendar;
using System.Security.Cryptography;

namespace FootballCareerSimulator.Presentation;

public sealed record CareerStartConfiguration(
    string ManagerName,
    long StartingClubId,
    GameDate StartingDate,
    int RootSeed)
{
    public static CareerStartConfiguration Create(
        string managerName,
        long startingClubId,
        GameDate? startingDate = null,
        int? rootSeed = null)
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
            startingDate ?? Today(),
            rootSeed ?? RandomNumberGenerator.GetInt32(1, int.MaxValue));
    }

    public static GameDate Today()
    {
        var today = DateTime.Today;
        return GameDate.FromCalendarDate(today.Year, today.Month, today.Day);
    }
}
