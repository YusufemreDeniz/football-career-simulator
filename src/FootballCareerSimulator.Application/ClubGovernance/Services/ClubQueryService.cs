namespace FootballCareerSimulator.Application.ClubGovernance.Services;

using FootballCareerSimulator.Application.ClubGovernance.Ports;
using FootballCareerSimulator.Application.ClubGovernance.Queries;
using FootballCareerSimulator.Domain.ClubGovernance;
using FootballCareerSimulator.Domain.Shared;
using FootballCareerSimulator.Simulation.DataPacks;

public sealed class ClubQueryService
{
    private readonly IClubRegistryStore _store;

    public ClubQueryService(IClubRegistryStore store)
    {
        _store = store ?? throw new ArgumentNullException(nameof(store));
    }

    public IReadOnlyList<ClubReadModel> GetAllClubs() =>
        _store.Registry.Clubs
            .Select(ToReadModel)
            .ToArray();

    public ClubReadModel? GetClub(long clubId)
    {
        var club = _store.Registry.Clubs.FirstOrDefault(candidate => candidate.Id.Value == clubId);
        return club is null ? null : ToReadModel(club);
    }

    public ClubReadModel GetClubOrThrow(long clubId) =>
        GetClub(clubId)
        ?? throw new ClubGovernanceQueryException($"Club {clubId} was not found.");

    public string GetDisplayName(ClubId clubId) =>
        _store.Registry.GetClubOrThrow(clubId).DisplayName;

    private static ClubReadModel ToReadModel(Club club)
    {
        if (TurkeySuperLig202627DataPack.TryGetClub(club.Id, out var data)
            && string.Equals(data.OfficialName, club.DisplayName, StringComparison.Ordinal))
        {
            return new(
                club.Id.Value,
                club.DisplayName,
                club.Code.Value,
                club.SportiveStrength,
                club.TransferBudgetLimit,
                club.ReservedTransferFunds,
                club.SpentTransferFunds,
                club.AvailableTransferFunds,
                club.WageBudgetLimit,
                club.ReservedWeeklyWage,
                data.CrestResourcePath,
                data.HomeKitResourcePath,
                data.AwayKitResourcePath,
                data.ThirdKitResourcePath,
                TurkeySuperLig202627DataPack.SnapshotDate);
        }

        return new(
            club.Id.Value,
            club.DisplayName,
            club.Code.Value,
            club.SportiveStrength,
            club.TransferBudgetLimit,
            club.ReservedTransferFunds,
            club.SpentTransferFunds,
            club.AvailableTransferFunds,
            club.WageBudgetLimit,
            club.ReservedWeeklyWage,
            CrestResourcePath: string.Empty,
            HomeKitResourcePath: string.Empty,
            AwayKitResourcePath: string.Empty,
            ThirdKitResourcePath: string.Empty,
            DataSnapshotDate: string.Empty);
    }
}

public sealed class ClubGovernanceQueryException : Exception
{
    public ClubGovernanceQueryException(string message)
        : base(message)
    {
    }
}
