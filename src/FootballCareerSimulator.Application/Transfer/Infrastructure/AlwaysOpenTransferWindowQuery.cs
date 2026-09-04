using FootballCareerSimulator.Application.Transfer.Ports;

namespace FootballCareerSimulator.Application.Transfer.Infrastructure;

public sealed class AlwaysOpenTransferWindowQuery : ITransferWindowQuery
{
    public static AlwaysOpenTransferWindowQuery Instance { get; } = new();

    public bool IsOpen => true;
}
