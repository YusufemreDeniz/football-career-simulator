using FootballCareerSimulator.Domain.ContractRegistration;
using FootballCareerSimulator.Domain.PlayerCareer;
using FootballCareerSimulator.Domain.Shared;
using FootballCareerSimulator.Domain.WorldCalendar;

namespace FootballCareerSimulator.Simulation.ContractRegistration;

public static class MvpContractFactory
{
    public static PlayerContract CreateForPlayerCareer(
        Domain.PlayerCareer.PlayerCareer career,
        GameDate startDate)
    {
        ArgumentNullException.ThrowIfNull(career);

        var years = 2 + (career.SlotIndex % 3); // 2–4 yıl
        var endDate = GameDate.FromCalendarDate(startDate.Year + years, startDate.Month, startDate.Day);
        var wage = Math.Max(500, career.CurrentAbility * 120);
        return PlayerContract.Activate(
            career.Id,
            career.OriginClubId,
            startDate,
            endDate,
            wage);
    }
}
