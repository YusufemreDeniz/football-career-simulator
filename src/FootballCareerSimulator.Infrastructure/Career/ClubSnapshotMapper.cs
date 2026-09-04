using FootballCareerSimulator.Domain.ClubGovernance;
using FootballCareerSimulator.Domain.Shared;

namespace FootballCareerSimulator.Infrastructure.Career;

internal static class ClubSnapshotMapper
{
    public static LeagueClubRegistry ToRegistry(IReadOnlyList<ClubSnapshotRow> rows)
    {
        if (rows.Count == 0)
        {
            return LeagueClubRegistry.CreateMvpLeague();
        }

        var clubs = rows
            .OrderBy(row => row.ClubId)
            .Select(row => Club.Rehydrate(
                new ClubId(row.ClubId),
                row.DisplayName,
                new ClubCode(row.ClubCode),
                row.SportiveStrength,
                row.TransferBudgetLimit,
                row.ReservedTransferFunds,
                row.SpentTransferFunds,
                row.WageBudgetLimit,
                row.ReservedWeeklyWage))
            .ToArray();

        return LeagueClubRegistry.Rehydrate(clubs);
    }

    internal sealed record ClubSnapshotRow(
        long ClubId,
        string DisplayName,
        string ClubCode,
        int SportiveStrength,
        int TransferBudgetLimit,
        int ReservedTransferFunds,
        int SpentTransferFunds,
        int WageBudgetLimit,
        int ReservedWeeklyWage);
}
