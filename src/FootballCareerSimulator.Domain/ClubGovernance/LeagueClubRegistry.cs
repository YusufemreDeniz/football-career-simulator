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

    public LeagueClubRegistry WithClub(Club club)
    {
        ArgumentNullException.ThrowIfNull(club);
        if (_clubs.All(existing => existing.Id != club.Id))
        {
            throw new ClubGovernanceInvariantViolationException(
                $"Club {club.Id.Value} was not found.");
        }

        var replaced = _clubs
            .Select(existing => existing.Id == club.Id ? club : existing)
            .ToArray();
        return new LeagueClubRegistry(replaced);
    }

    public static LeagueClubRegistry CreateMvpLeague() =>
        new LeagueClubRegistry(MvpLeagueCatalog.CreateClubs());

    public static LeagueClubRegistry Rehydrate(IEnumerable<Club> clubs) => new(clubs);

    private void Validate()
    {
        if (_clubs.Count > CompetitionMvpConstraints.MaxLeagueTeamCount)
        {
            throw new ClubGovernanceInvariantViolationException(
                $"MVP league cannot contain more than {CompetitionMvpConstraints.MaxLeagueTeamCount} clubs.");
        }

        if (_clubs.Select(club => club.Id).Distinct().Count() != _clubs.Count)
        {
            throw new ClubGovernanceInvariantViolationException("Duplicate club ids are not allowed.");
        }

        if (_clubs.Select(club => club.DisplayName).Distinct(StringComparer.Ordinal).Count() != _clubs.Count)
        {
            throw new ClubGovernanceInvariantViolationException("Duplicate club names are not allowed.");
        }

        if (_clubs.Select(club => club.Code.Value).Distinct(StringComparer.Ordinal).Count() != _clubs.Count)
        {
            throw new ClubGovernanceInvariantViolationException("Duplicate club codes are not allowed.");
        }
    }
}
