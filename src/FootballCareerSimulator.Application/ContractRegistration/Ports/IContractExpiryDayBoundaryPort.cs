using FootballCareerSimulator.Application.ContractRegistration.Queries;
using FootballCareerSimulator.Domain.WorldCalendar;

namespace FootballCareerSimulator.Application.ContractRegistration.Ports;

/// <summary>
/// Gün sınırı consequence'ı — AdvanceSimulationTime'a bağlanır.
/// </summary>
public interface IContractExpiryDayBoundaryPort
{
    FreeAgencyExpiryResult ExpireDueContracts(GameDate day);
}
