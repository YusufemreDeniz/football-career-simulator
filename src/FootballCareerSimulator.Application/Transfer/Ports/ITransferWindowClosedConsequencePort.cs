using FootballCareerSimulator.Domain.WorldCalendar;
using FootballCareerSimulator.Application.Transfer.Services;

namespace FootballCareerSimulator.Application.Transfer.Ports;

/// <summary>
/// Transfer penceresi kapandı consequence'ı — World Calendar Close handler'a bağlanır.
/// </summary>
public interface ITransferWindowClosedConsequencePort
{
    TransferWindowCloseOutcome ApplyWindowClosed(GameDate day);
}
