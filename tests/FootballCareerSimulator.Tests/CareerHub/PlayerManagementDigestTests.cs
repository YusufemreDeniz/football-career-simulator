using FootballCareerSimulator.Application.CareerHub.Queries;
using FootballCareerSimulator.Application.TeamPreparation.Queries;
using FootballCareerSimulator.Domain.ContractRegistration;
using FootballCareerSimulator.Domain.ManagerCareer;
using FootballCareerSimulator.Domain.PlayerCareer;
using FootballCareerSimulator.Domain.Shared;
using FootballCareerSimulator.Domain.SocialContinuity;
using FootballCareerSimulator.Domain.TeamPreparation;
using FootballCareerSimulator.Domain.TrainingPhysicalState;
using FootballCareerSimulator.Domain.WorldCalendar;
using FootballCareerSimulator.Simulation.TeamPreparation;
using PlayerCareerAggregate = FootballCareerSimulator.Domain.PlayerCareer.PlayerCareer;

namespace FootballCareerSimulator.Tests.CareerHub;

public sealed class PlayerManagementDigestTests
{
    [Fact]
    public void Compose_JoinsCareerPhysicalContractRelationshipAndPromiseByPlayer()
    {
        var clubId = new ClubId(1);
        var managerId = new ManagerId(7);
        var day = GameDate.FromCalendarDate(2026, 8, 12);
        var playerId = PlayerId.FromClubSlot(clubId.Value, slotIndex: 0);
        var squad = ClubSquad.Empty(clubId).EnsureMember(playerId, slotIndex: 0, day);
        var career = PlayerCareerAggregate.CreateForSlot(
            clubId,
            slotIndex: 0,
            currentAbility: 74,
            potentialAbility: 82,
            birthYear: 2004);
        var physical = PlayerPhysicalState.CreateRested(clubId, slotIndex: 0)
            .WithLevels(fatigue: 38, fitness: 91);
        var contract = PlayerContract.Activate(
            playerId,
            clubId,
            day,
            day.AddDays(300),
            weeklyWage: 125_000);
        var relationship = RelationshipRecord.CreatePlayerToManager(
            new RelationshipId(1),
            playerId,
            managerId,
            day);
        var promise = Promise.CreateStartingOpportunity(
            new PromiseId(1),
            managerId,
            playerId,
            clubId,
            targetStarts: 3,
            deadlineOn: day.AddDays(30),
            createdOn: day);

        var digest = PlayerManagementDigest.Compose(
            clubId,
            managerId.Value,
            day,
            [new MvpSquadPlayerProfile("Gerçek Oyuncu", MvpSquadPositionRole.Goalkeeper)],
            [new SquadPlayerReadModel(1, "Gerçek Oyuncu", 0, 74)],
            squad,
            [career],
            new Dictionary<(long ClubId, int SlotIndex), PlayerPhysicalState>
            {
                [(clubId.Value, 0)] = physical,
            },
            [contract],
            [relationship],
            [promise]);

        var player = Assert.Single(digest.Players);
        Assert.True(digest.HasClub);
        Assert.Equal(playerId.Value, player.PlayerId);
        Assert.Equal("KL", player.PositionCode);
        Assert.Equal(22, player.Age);
        Assert.Equal(82, player.PotentialAbility);
        Assert.Equal(91, player.Fitness);
        Assert.Equal(125_000, player.WeeklyWage);
        Assert.Equal(50, player.Trust);
        Assert.Contains("İlk 11 0/3", player.PromiseSummary, StringComparison.Ordinal);
        Assert.Contains("nötr başlangıç", player.CausalitySummary, StringComparison.Ordinal);
        Assert.Contains("Gerçek Oyuncu", player.ToDetailText(), StringComparison.Ordinal);
        Assert.Contains("Nedensellik:", player.ToDetailText(), StringComparison.Ordinal);
    }
}
