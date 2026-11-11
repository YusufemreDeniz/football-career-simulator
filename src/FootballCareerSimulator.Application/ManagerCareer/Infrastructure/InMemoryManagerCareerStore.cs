namespace FootballCareerSimulator.Application.ManagerCareer.Infrastructure;

using FootballCareerSimulator.Application.ManagerCareer.Ports;
using FootballCareerSimulator.Domain.ManagerCareer;

public sealed class InMemoryManagerCareerStore : IManagerCareerStore
{
    public InMemoryManagerCareerStore(ManagerCareer career)
    {
        Career = career ?? throw new ArgumentNullException(nameof(career));
    }

    public ManagerCareer Career { get; private set; }

    public void Replace(ManagerCareer career) =>
        Career = career ?? throw new ArgumentNullException(nameof(career));
}
