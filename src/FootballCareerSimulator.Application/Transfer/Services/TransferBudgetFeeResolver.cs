using FootballCareerSimulator.Application.Transfer.Ports;
using FootballCareerSimulator.Domain.Transfer;

namespace FootballCareerSimulator.Application.Transfer.Services;

internal static class TransferBudgetFeeResolver
{
    public static int ResolveActiveFee(IClubOfferStore offerStore, TransferProcessId processId)
    {
        var offers = offerStore.GetForProcess(processId);
        var accepted = offers.LastOrDefault(o => o.Status == ClubOfferStatus.Accepted);
        if (accepted is not null)
        {
            return accepted.OfferedFee;
        }

        var pending = offers.LastOrDefault(o => o.IsPending);
        return pending?.OfferedFee ?? 0;
    }
}
