namespace FootballCareerSimulator.Application.Transfer.Services;

/// <summary>
/// Transfer komutunun kim adına yürütüldüğü (D-140: AI kulüp aynı domain kurallarına tabi).
/// </summary>
public enum TransferActingParty
{
    HumanManager = 0,
    SimulatedClub = 1,
}
