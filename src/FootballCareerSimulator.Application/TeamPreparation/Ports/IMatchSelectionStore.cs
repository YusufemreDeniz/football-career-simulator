using FootballCareerSimulator.Domain.Competition;
using FootballCareerSimulator.Domain.Shared;
using FootballCareerSimulator.Domain.TeamPreparation;

namespace FootballCareerSimulator.Application.TeamPreparation.Ports;

public interface IMatchSelectionStore
{
    IReadOnlyList<MatchSelection> Selections { get; }

    MatchSelection? Get(FixtureId fixtureId, ClubId clubId);

    void Upsert(MatchSelection selection);

    void Remove(FixtureId fixtureId, ClubId clubId);

    void RemoveForFixture(FixtureId fixtureId);

    void ReplaceAll(IEnumerable<MatchSelection> selections);
}
