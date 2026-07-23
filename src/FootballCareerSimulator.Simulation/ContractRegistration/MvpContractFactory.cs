using FootballCareerSimulator.Domain.ContractRegistration;
using FootballCareerSimulator.Domain.PlayerCareer;
using FootballCareerSimulator.Domain.Shared;
using FootballCareerSimulator.Domain.TeamPreparation;
using FootballCareerSimulator.Domain.WorldCalendar;

namespace FootballCareerSimulator.Simulation.ContractRegistration;

public static class MvpContractFactory
{
    /// <summary>MVP demo: son slot kısa süreli sözleşme → free agency gözlemi.</summary>
    public const int ShortContractSquadSlot = MatchSelection.MaxSquadSlot;

    public static PlayerContract CreateForPlayerCareer(
        Domain.PlayerCareer.PlayerCareer career,
        GameDate startDate)
    {
        ArgumentNullException.ThrowIfNull(career);

        GameDate endDate;
        if (career.SlotIndex == ShortContractSquadSlot)
        {
            endDate = startDate.AddDays(45);
        }
        else
        {
            var years = 2 + (career.SlotIndex % 3); // 2–4 yıl
            endDate = GameDate.FromCalendarDate(startDate.Year + years, startDate.Month, startDate.Day);
        }

        var wage = Math.Max(500, career.CurrentAbility * 120);
        return PlayerContract.Activate(
            career.Id,
            career.OriginClubId,
            startDate,
            endDate,
            wage);
    }
}
