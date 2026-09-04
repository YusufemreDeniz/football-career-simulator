namespace FootballCareerSimulator.Application.Transfer.Ports;

/// <summary>
/// Transfer'in World &amp; Calendar pencere durumunu okuması (authority World'de).
/// </summary>
public interface ITransferWindowQuery
{
    bool IsOpen { get; }
}
