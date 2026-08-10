using FootballCareerSimulator.Domain.ClubGovernance;
using FootballCareerSimulator.Domain.Shared;
using FootballCareerSimulator.Simulation.DataPacks;

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
            Assert.StartsWith("res://assets/clubs/turkey/super-lig-2026-27/", data.CrestResourcePath);
            Assert.EndsWith(".png", data.HomeKitResourcePath);
            Assert.EndsWith(".png", data.AwayKitResourcePath);
            Assert.EndsWith(".png", data.ThirdKitResourcePath);
        }
    }
}
