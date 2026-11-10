using FootballCareerSimulator.Application.CareerHub.Queries;
using FootballCareerSimulator.Simulation.DataPacks;
using FootballCareerSimulator.Simulation.TeamPreparation;
using Xunit;

namespace FootballCareerSimulator.Tests;

public sealed class EuropeanEliteDataPackTests
{
    [Fact]
    public void DataPack_Contains18EliteClubs_WithRealisticStarsAndWonderkids()
    {
        var clubs = EuropeanElite202627DataPack.AllClubs;
        Assert.Equal(65, clubs.Count);
        Assert.Equal(20, EuropeanElite202627DataPack.GetClubsByLeague("Premier League").Count);
        Assert.Equal(20, EuropeanElite202627DataPack.GetClubsByLeague("La Liga").Count);
        Assert.Equal(18, EuropeanElite202627DataPack.GetClubsByLeague("Bundesliga").Count);

        Assert.True(EuropeanElite202627DataPack.TryGetClub(101, out var realMadrid));
        Assert.Equal("REAL MADRID CF", realMadrid.OfficialName);
        Assert.Equal("La Liga", realMadrid.LeagueName);
        Assert.Equal(89, realMadrid.SportiveStrength);
        Assert.Contains(realMadrid.Players, p => p.DisplayName == "Kylian Mbappé" && p.CurrentAbility == 91);
        Assert.Contains(realMadrid.Players, p => p.DisplayName == "Arda Güler" && p.PotentialAbility >= 89);

        Assert.True(EuropeanElite202627DataPack.TryGetClub(102, out var manCity));
        Assert.Equal("Premier League", manCity.LeagueName);
        Assert.Contains(manCity.Players, p => p.DisplayName == "Erling Haaland" && p.CurrentAbility == 91);

        Assert.True(EuropeanElite202627DataPack.TryGetClub(105, out var barcelona));
        Assert.Contains(barcelona.Players, p => p.DisplayName == "Lamine Yamal" && p.Age == 19 && p.PotentialAbility >= 93);
    }

    [Fact]
    public void DataPack_PlayerIdEncoding_ResolvesPlayerAndClubProperly()
    {
        var mbappeId = EuropeanElite202627DataPack.MakePlayerId(101, 0);
        Assert.Equal(101001L, mbappeId);

        var resolved = EuropeanElite202627DataPack.TryResolvePlayer(
            mbappeId,
            out var playerName,
            out var clubName,
            out var clubData,
            out var profile);

        Assert.True(resolved);
        Assert.Equal("Kylian Mbappé", playerName);
        Assert.Equal("REAL MADRID CF", clubName);
        Assert.NotNull(clubData);
        Assert.Equal(101, clubData.ClubId);
        Assert.NotNull(profile);
        Assert.Equal(91, profile.CurrentAbility);
    }

    [Fact]
    public void FreeAgents_ContainRecognizableGlobalFreeAgents()
    {
        var freeAgents = EuropeanElite202627DataPack.FreeAgents;
        Assert.NotEmpty(freeAgents);
        Assert.Contains(freeAgents, fa => fa.DisplayName == "Memphis Depay");
        Assert.Contains(freeAgents, fa => fa.DisplayName == "Sergio Ramos");
        Assert.Contains(freeAgents, fa => fa.DisplayName == "Adrien Rabiot");

        var resolved = EuropeanElite202627DataPack.TryResolvePlayer(
            9001,
            out var name,
            out var club,
            out var clubData,
            out var profile);

        Assert.True(resolved);
        Assert.Equal("Memphis Depay", name);
        Assert.Equal("Serbest Oyuncu", club);
        Assert.Null(clubData);
        Assert.NotNull(profile);
    }

    [Fact]
    public void ScoutValuation_FreeAgentHasZeroMarketValueAndZeroTransferFee()
    {
        var valuation = ScoutTransferValuationModel.Evaluate(
            currentAbility: 81,
            potentialAbility: 81,
            age: 32,
            managedClubStrength: 80,
            sourceClubStrength: 65,
            positionalAverage: 76,
            transferBudgetAvailable: 500_000,
            isFreeAgent: true);

        Assert.Equal(0, valuation.MarketValue);
        Assert.Equal(0, valuation.SuggestedOpeningFee);
        Assert.True(valuation.IsAffordable);
        Assert.True(valuation.SuggestedWeeklyWage > 0);
        Assert.True(valuation.InterestPercent >= 50);
    }

    [Fact]
    public void ScoutTransferDigest_Compose_FiltersByScopeProperly()
    {
        var profiles = Enumerable.Range(0, 25)
            .Select(slot => new MvpSquadPlayerProfile(
                $"Oyuncu {slot}",
                slot < 2
                    ? MvpSquadPositionRole.Goalkeeper
                    : slot < 10
                        ? MvpSquadPositionRole.CentreBack
                        : slot < 18
                            ? MvpSquadPositionRole.CentralMidfielder
                            : MvpSquadPositionRole.Striker))
            .ToArray();
        var ratings = Enumerable.Range(0, 25).ToDictionary(slot => slot, _ => 75);

        var candidates = new[]
        {
            new ScoutCandidateSource(2001, 2, "Trabzonspor", "Yerli Kaleci", MvpSquadPositionGroup.Goalkeeper, "KL", 77, 24, 82, 78, 76, null, false, "Trendyol Süper Lig", false),
            new ScoutCandidateSource(101011, 101, "REAL MADRID CF", "Thibaut Courtois", MvpSquadPositionGroup.Goalkeeper, "KL", 89, 34, 89, 78, 89, null, false, "La Liga", true),
            new ScoutCandidateSource(9006, 0, "Serbest Oyuncu", "Keylor Navas", MvpSquadPositionGroup.Goalkeeper, "KL", 80, 39, 80, 78, 65, null, false, "Serbest Oyuncular", true),
        };

        // 1. Domestic Scope
        var domestic = ScoutTransferDigest.Compose("Kulübüm", 1, profiles, ratings, candidates, scope: TransferMarketScope.Domestic);
        Assert.Single(domestic.Candidates);
        Assert.Equal("Yerli Kaleci", domestic.Candidates[0].DisplayName);

        // 2. European Elite Scope
        var euro = ScoutTransferDigest.Compose("Kulübüm", 1, profiles, ratings, candidates, scope: TransferMarketScope.EuropeanElite);
        Assert.Single(euro.Candidates);
        Assert.Equal("Thibaut Courtois", euro.Candidates[0].DisplayName);

        // 3. Free Agents Scope
        var free = ScoutTransferDigest.Compose("Kulübüm", 1, profiles, ratings, candidates, scope: TransferMarketScope.FreeAgents);
        Assert.Single(free.Candidates);
        Assert.Equal("Keylor Navas", free.Candidates[0].DisplayName);
        Assert.Equal(0, free.Candidates[0].MarketValue);
        Assert.Contains("SERBEST", free.Candidates[0].ToListLabel(), StringComparison.Ordinal);

        // 4. All Scope
        var all = ScoutTransferDigest.Compose("Kulübüm", 1, profiles, ratings, candidates, scope: TransferMarketScope.All);
        Assert.Equal(3, all.Candidates.Count);
    }
}
