namespace FootballCareerSimulator.Application.ClubGovernance.Ports;

using FootballCareerSimulator.Domain.ClubGovernance;

public interface IClubRegistryStore
{
    LeagueClubRegistry Registry { get; }

    void Replace(LeagueClubRegistry registry);
}
