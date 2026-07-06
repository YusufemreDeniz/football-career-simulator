namespace FootballCareerSimulator.Application.ClubGovernance.Services;

using FootballCareerSimulator.Application.ClubGovernance.Ports;
using FootballCareerSimulator.Application.ClubGovernance.Queries;
using FootballCareerSimulator.Domain.Shared;

public sealed class ClubQueryService
{
    private readonly IClubRegistryStore _store;

    public ClubQueryService(IClubRegistryStore store)
    {
        _store = store ?? throw new ArgumentNullException(nameof(store));
    }

    public IReadOnlyList<ClubReadModel> GetAllClubs() =>
        _store.Registry.Clubs
            .Select(club => new ClubReadModel(
                club.Id.Value,
                club.DisplayName,
                club.Code.Value,
                club.SportiveStrength))
            .ToArray();

    public ClubReadModel? GetClub(long clubId)
    {
        var club = _store.Registry.Clubs.FirstOrDefault(candidate => candidate.Id.Value == clubId);
        return club is null
            ? null
            : new ClubReadModel(club.Id.Value, club.DisplayName, club.Code.Value, club.SportiveStrength);
    }

    public ClubReadModel GetClubOrThrow(long clubId) =>
        GetClub(clubId)
        ?? throw new ClubGovernanceQueryException($"Club {clubId} was not found.");

    public string GetDisplayName(ClubId clubId) =>
        _store.Registry.GetClubOrThrow(clubId).DisplayName;
}

public sealed class ClubGovernanceQueryException : Exception
{
    public ClubGovernanceQueryException(string message)
        : base(message)
    {
    }
}
