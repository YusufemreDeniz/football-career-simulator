using FootballCareerSimulator.Domain.ClubGovernance;
using FootballCareerSimulator.Domain.Shared;
using FootballCareerSimulator.Simulation.DataPacks;
using FootballCareerSimulator.Simulation.TeamPreparation;

namespace FootballCareerSimulator.Tests.DataPacks;

public sealed class TurkeySuperLig202627DataPackTests
{
    [Fact]
    public void AllClubs_MatchesLeagueCatalogAndProvidesCompleteSquadsAndBranding()
    {
        var catalog = MvpLeagueCatalog.CreateClubs();

        Assert.Equal(18, TurkeySuperLig202627DataPack.AllClubs.Count);
        foreach (var club in catalog)
        {
            var data = TurkeySuperLig202627DataPack.GetClub(new ClubId(club.Id.Value));

            Assert.Equal(club.DisplayName, data.OfficialName);
            Assert.Equal(25, data.PlayerNames.Count);
            Assert.Equal(25, data.PlayerNames.Distinct(StringComparer.OrdinalIgnoreCase).Count());
            Assert.Equal(25, data.Players.Count);
            Assert.All(data.Players, player => Assert.True(Enum.IsDefined(player.PositionGroup)));
            Assert.All(data.Players, player =>
            {
                Assert.InRange(player.CurrentAbility!.Value, 45, 90);
                Assert.InRange(player.PotentialAbility!.Value, player.CurrentAbility.Value, 99);
                Assert.InRange(player.Age!.Value, 16, 40);
            });
            Assert.Equal(
                (int)Math.Round(
                    data.Players.Take(11).Average(player => player.CurrentAbility!.Value),
                    MidpointRounding.AwayFromZero),
                data.SquadStrength);
            Assert.Contains("ea.com", data.AbilitySourceUrl, StringComparison.OrdinalIgnoreCase);
            Assert.Equal(
                new[]
                {
                    MvpSquadPositionGroup.Goalkeeper,
                    MvpSquadPositionGroup.Defender,
                    MvpSquadPositionGroup.Defender,
                    MvpSquadPositionGroup.Defender,
                    MvpSquadPositionGroup.Defender,
                    MvpSquadPositionGroup.Midfielder,
                    MvpSquadPositionGroup.Midfielder,
                    MvpSquadPositionGroup.Midfielder,
                    MvpSquadPositionGroup.Midfielder,
                    MvpSquadPositionGroup.Forward,
                    MvpSquadPositionGroup.Forward,
                },
                data.Players.Take(11).Select(player => player.PositionGroup));
            Assert.StartsWith("res://assets/clubs/turkey/super-lig-2026-27/", data.CrestResourcePath);
            Assert.EndsWith(".png", data.HomeKitResourcePath);
            Assert.EndsWith(".png", data.AwayKitResourcePath);
            Assert.EndsWith(".png", data.ThirdKitResourcePath);
        }
    }

    [Fact]
    public void GalatasarayRoster_PreservesDetailedRolesFromLiveSource()
    {
        var players = TurkeySuperLig202627DataPack.GetClub(new ClubId(1)).Players;

        Assert.Contains(players, player => player.PositionRole == MvpSquadPositionRole.Goalkeeper);
        Assert.Contains(players, player => player.PositionRole == MvpSquadPositionRole.CentreBack);
        Assert.Contains(players, player => player.PositionRole is
            MvpSquadPositionRole.RightBack or MvpSquadPositionRole.LeftBack);
        Assert.Contains(players, player => player.PositionRole is
            MvpSquadPositionRole.DefensiveMidfielder or
            MvpSquadPositionRole.CentralMidfielder or
            MvpSquadPositionRole.AttackingMidfielder);
        Assert.Contains(players, player => player.PositionRole is
            MvpSquadPositionRole.RightWinger or
            MvpSquadPositionRole.LeftWinger or
            MvpSquadPositionRole.Striker);
    }

    [Fact]
    public void GalatasarayRoster_UsesPlayerSpecificRatingsInsteadOfSquadSlotRandomness()
    {
        var players = TurkeySuperLig202627DataPack.GetClub(new ClubId(1)).Players;
        var osimhen = Assert.Single(players, player => player.DisplayName == "Victor Osimhen");
        var youthPlayer = Assert.Single(players, player => player.DisplayName == "B. Luş");

        Assert.Equal(85, osimhen.CurrentAbility);
        Assert.True(osimhen.CurrentAbility > youthPlayer.CurrentAbility);
        Assert.True(youthPlayer.PotentialAbility > youthPlayer.CurrentAbility);
    }
}
