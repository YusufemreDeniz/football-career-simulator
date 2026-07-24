using FootballCareerSimulator.Domain.ClubGovernance;
using FootballCareerSimulator.Domain.Competition;
using FootballCareerSimulator.Domain.Shared;

namespace FootballCareerSimulator.Tests.ClubGovernance;

public sealed class ClubRegistryTests
{
    [Fact]
    public void CreateMvpLeague_ContainsTwentyClubsWithUniqueIds()
    {
        var registry = LeagueClubRegistry.CreateMvpLeague();

        Assert.Equal(CompetitionMvpConstraints.LeagueTeamCount, registry.Clubs.Count);
        Assert.Equal(
            CompetitionMvpConstraints.LeagueTeamCount,
            registry.Clubs.Select(club => club.Id.Value).Distinct().Count());
    }

    [Fact]
    public void GetClubOrThrow_ReturnsClubForKnownId()
    {
        var registry = LeagueClubRegistry.CreateMvpLeague();
        var club = registry.GetClubOrThrow(new ClubId(5));

        Assert.Equal(5, club.Id.Value);
        Assert.Equal("AKD", club.Code.Value);
        Assert.Equal("Akdeniz United", club.DisplayName);
        Assert.Equal(Club.DefaultTransferBudgetLimit(club.SportiveStrength), club.TransferBudgetLimit);
        Assert.Equal(0, club.ReservedTransferFunds);
        Assert.Equal(club.TransferBudgetLimit, club.AvailableTransferFunds);
        Assert.Equal(Club.DefaultWageBudgetLimit(club.SportiveStrength), club.WageBudgetLimit);
        Assert.Equal(0, club.ReservedWeeklyWage);
    }

    [Fact]
    public void ReserveAndRelease_TransferFunds_RoundTrip()
    {
        var registry = LeagueClubRegistry.CreateMvpLeague();
        var club = registry.GetClubOrThrow(new ClubId(1));
        var reserved = club.ReserveTransferFunds(500_000);
        var released = reserved.ReleaseTransferReservation(500_000);

        Assert.Equal(500_000, reserved.ReservedTransferFunds);
        Assert.Equal(0, released.ReservedTransferFunds);
        Assert.Equal(club.AvailableTransferFunds, released.AvailableTransferFunds);
    }

    [Fact]
    public void ClubCode_RejectsInvalidLength()
    {
        Assert.Throws<ClubGovernanceInvariantViolationException>(() => new ClubCode("A"));
    }
}
