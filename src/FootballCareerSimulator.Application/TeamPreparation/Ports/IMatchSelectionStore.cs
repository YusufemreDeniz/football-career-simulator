using FootballCareerSimulator.Domain.Competition;
using FootballCareerSimulator.Domain.Shared;
using FootballCareerSimulator.Domain.TeamPreparation;

namespace FootballCareerSimulator.Application.TeamPreparation.Ports;

public readonly record struct ClubLineupTemplate(
    ClubId ClubId,
    IReadOnlyList<int> StartingSlotIndices,
    IReadOnlyList<int> BenchSlotIndices);

public interface IMatchSelectionStore
{
    IReadOnlyList<MatchSelection> Selections { get; }

    MatchSelection? Get(FixtureId fixtureId, ClubId clubId);

    ClubLineupTemplate? GetLineupTemplate(ClubId clubId);

    void Upsert(MatchSelection selection);

    void Remove(FixtureId fixtureId, ClubId clubId);

    void RemoveForFixture(FixtureId fixtureId);

    void ReplaceAll(IEnumerable<MatchSelection> selections);
}
