namespace FootballCareerSimulator.Application.ClubGovernance.Infrastructure;

using FootballCareerSimulator.Application.ClubGovernance.Ports;
using FootballCareerSimulator.Domain.ClubGovernance;

public sealed class InMemoryClubRegistryStore : IClubRegistryStore
{
    public InMemoryClubRegistryStore(LeagueClubRegistry registry)
    {
        Registry = registry ?? throw new ArgumentNullException(nameof(registry));
    }

    public LeagueClubRegistry Registry { get; private set; }

    public void Replace(LeagueClubRegistry registry) =>
        Registry = registry ?? throw new ArgumentNullException(nameof(registry));
}
