using FootballCareerSimulator.Application.ManagerCareer.Ports;
using FootballCareerSimulator.Domain.Shared;
using FootballCareerSimulator.Domain.Transfer;

namespace FootballCareerSimulator.Application.Transfer.Services;

internal static class TransferActorGuard
{
    public static void EnsureBuyingClubActor(
        IManagerCareerStore managerCareerStore,
        ClubId buyingClubId,
        TransferActingParty actor,
        string humanActionDescription)
    {
        if (actor == TransferActingParty.SimulatedClub)
        {
            if (managerCareerStore.Career.ActiveEmployment is { ClubId: var managed }
                && managed.Value == buyingClubId.Value)
            {
                throw new TransferInvariantViolationException(
                    "Simulated club actor cannot act for the human-managed club.");
            }

            return;
        }

        if (managerCareerStore.Career.ActiveEmployment is not { ClubId: var clubId }
            || clubId.Value != buyingClubId.Value)
        {
            throw new TransferInvariantViolationException(humanActionDescription);
        }
    }
}
