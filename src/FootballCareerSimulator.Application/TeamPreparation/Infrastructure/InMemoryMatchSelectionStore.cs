using FootballCareerSimulator.Application.TeamPreparation.Ports;
using FootballCareerSimulator.Domain.Competition;
using FootballCareerSimulator.Domain.Shared;
using FootballCareerSimulator.Domain.TeamPreparation;

namespace FootballCareerSimulator.Application.TeamPreparation.Infrastructure;

public sealed class InMemoryMatchSelectionStore : IMatchSelectionStore
{
    private readonly Dictionary<(long FixtureId, long ClubId), MatchSelection> _selections = new();
    private readonly Dictionary<long, ClubLineupTemplate> _templates = new();

    public IReadOnlyList<MatchSelection> Selections =>
        _selections.Values
            .OrderBy(s => s.FixtureId.Value)
            .ThenBy(s => s.ClubId.Value)
            .ToArray();

    public MatchSelection? Get(FixtureId fixtureId, ClubId clubId) =>
        _selections.TryGetValue((fixtureId.Value, clubId.Value), out var selection)
            ? selection
            : null;

    public ClubLineupTemplate? GetLineupTemplate(ClubId clubId) =>
        _templates.TryGetValue(clubId.Value, out var template)
            ? template
            : null;

    public void Upsert(MatchSelection selection)
    {
        ArgumentNullException.ThrowIfNull(selection);
        _selections[(selection.FixtureId.Value, selection.ClubId.Value)] = selection;
        _templates[selection.ClubId.Value] = new ClubLineupTemplate(
            selection.ClubId,
            selection.StartingSlotIndices.ToArray(),
            selection.BenchSlotIndices.ToArray());
    }

    public void Remove(FixtureId fixtureId, ClubId clubId) =>
        _selections.Remove((fixtureId.Value, clubId.Value));

    public void RemoveForFixture(FixtureId fixtureId)
    {
        var keys = _selections.Keys
            .Where(key => key.FixtureId == fixtureId.Value)
            .ToArray();

        foreach (var key in keys)
        {
            _selections.Remove(key);
        }
    }

    public void ReplaceAll(IEnumerable<MatchSelection> selections)
    {
        ArgumentNullException.ThrowIfNull(selections);
        _selections.Clear();
        _templates.Clear();
        foreach (var selection in selections)
        {
            Upsert(selection);
        }
    }
}
