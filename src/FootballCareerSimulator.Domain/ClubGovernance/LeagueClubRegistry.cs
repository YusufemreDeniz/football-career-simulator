namespace FootballCareerSimulator.Domain.ClubGovernance;

using FootballCareerSimulator.Domain.Competition;
using FootballCareerSimulator.Domain.Shared;

/// <summary>
/// MVP lig kulüp kaydı; 20 kulüplük sabit lig (docs/02_MVP_SCOPE.md Bölüm 17.2).
/// </summary>
public sealed class LeagueClubRegistry
{
    private readonly List<Club> _clubs = new();

    private LeagueClubRegistry(IEnumerable<Club> clubs)
    {
        _clubs.AddRange(clubs.OrderBy(club => club.Id.Value));
        Validate();
    }

    public IReadOnlyList<Club> Clubs => _clubs;

    public Club GetClubOrThrow(ClubId clubId) =>
        _clubs.FirstOrDefault(club => club.Id == clubId)
        ?? throw new ClubGovernanceInvariantViolationException($"Club {clubId.Value} was not found.");

    public static LeagueClubRegistry CreateMvpLeague()
    {
        var clubs = new List<Club>(CompetitionMvpConstraints.LeagueTeamCount);
        for (var index = 1; index <= CompetitionMvpConstraints.LeagueTeamCount; index++)
        {
            var id = new ClubId(index);
            var strength = 45 + (index * 2 % 46);
            clubs.Add(Club.Create(
                id,
                displayName: $"Kulüp {index:D2}",
                code: new ClubCode($"K{index:D2}"),
                sportiveStrength: strength));
        }

        return new LeagueClubRegistry(clubs);
    }

    public static LeagueClubRegistry Rehydrate(IEnumerable<Club> clubs) => new(clubs);

    private void Validate()
    {
        if (_clubs.Count > CompetitionMvpConstraints.LeagueTeamCount)
        {
            throw new ClubGovernanceInvariantViolationException(
                $"MVP league cannot contain more than {CompetitionMvpConstraints.LeagueTeamCount} clubs.");
        }

        if (_clubs.Select(club => club.Id).Distinct().Count() != _clubs.Count)
        {
            throw new ClubGovernanceInvariantViolationException("Duplicate club ids are not allowed.");
        }
    }
}
