using FootballCareerSimulator.Application.CareerHub.Queries;
using FootballCareerSimulator.Domain.Match;
using FootballCareerSimulator.Simulation.Continental;
using FootballCareerSimulator.Simulation.Match;
using Xunit;

namespace FootballCareerSimulator.Tests;

public sealed class ContinentalTournamentEngineTests
{
    [Fact]
    public void Engine_Initializes16TeamsAcross4Groups_WithDomesticAndEuropeanGiants()
    {
        var otherClubs = new List<(long ClubId, string Name, int Strength)>
        {
            (2L, "Fenerbahçe", 80),
            (3L, "Beşiktaş", 78),
            (4L, "Trabzonspor", 76),
            (5L, "Başakşehir", 74),
        };

        var engine = new ContinentalTournamentEngine(
            worldSeed: 42,
            managedClubId: 1L,
            managedClubName: "Galatasaray",
            managedClubStrength: 82,
            otherDomesticClubs: otherClubs,
            startDayNumber: 15);

        Assert.Equal(16, engine.Participants.Count);

        // 4 Türk Temsilcisi
        var domestic = engine.Participants.Where(p => p.Country == "Türkiye").ToList();
        Assert.Equal(4, domestic.Count);
        Assert.Contains(domestic, p => p.ClubId == 1L && p.ClubName == "Galatasaray");

        // 12 Avrupa Devi
        var european = engine.Participants.Where(p => p.Country != "Türkiye").ToList();
        Assert.Equal(12, european.Count);

        // Her grupta tam 4 takım ve 1 Türk temsilcisi
        foreach (var groupCode in new[] { ContinentalGroupCode.A, ContinentalGroupCode.B, ContinentalGroupCode.C, ContinentalGroupCode.D })
        {
            var groupTeams = engine.Participants.Where(p => p.GroupCode == groupCode).ToList();
            Assert.Equal(4, groupTeams.Count);
            Assert.Single(groupTeams, t => t.Country == "Türkiye");
            Assert.Equal(3, groupTeams.Count(t => t.Country != "Türkiye"));

            var standings = engine.GetGroupStandings(groupCode);
            Assert.Equal(4, standings.Count);
        }
    }

    [Fact]
    public void Engine_Schedules48GroupStageFixtures_Across6Rounds()
    {
        var engine = CreateTestEngine();

        // 4 grup * 6 hafta * 2 maç/hafta = 48 maç
        var groupFixtures = engine.AllFixtures
            .Where(f => f.Stage == ContinentalTournamentStage.GroupStage)
            .ToList();

        Assert.Equal(48, groupFixtures.Count);

        // Menajerin kulübü tam 6 grup maçı oynamalı
        var managedFixtures = groupFixtures
            .Where(f => f.HomeClubId == engine.ManagedClubId || f.AwayClubId == engine.ManagedClubId)
            .ToList();

        Assert.Equal(6, managedFixtures.Count);

        // Maçlar zaman çizelgesine göre sıralı olmalı
        for (var i = 0; i < managedFixtures.Count - 1; i++)
        {
            Assert.True(managedFixtures[i].ScheduledDayNumber < managedFixtures[i + 1].ScheduledDayNumber);
        }
    }

    [Fact]
    public void RecordManagedMatchResult_UpdatesStandingsAndCalculatesPrizeMoney()
    {
        var engine = CreateTestEngine();
        var nextManaged = engine.GetNextManagedFixture(currentDayNumber: 20);
        Assert.NotNull(nextManaged);

        var managedIsHome = nextManaged.HomeClubId == engine.ManagedClubId;

        // Galibiyet: Menajer 2 - 1 Rakip
        var homeGoals = managedIsHome ? 2 : 1;
        var awayGoals = managedIsHome ? 1 : 2;

        engine.RecordManagedMatchResult(
            nextManaged.FixtureId,
            homeGoals,
            awayGoals,
            Array.Empty<MatchKeyMoment>());

        Assert.True(nextManaged.IsPlayed);
        Assert.Equal(ContinentalTournamentEngine.WinPrizeMoney, engine.TotalPrizeMoneyWonByManagedClub);

        var groupStandings = engine.GetGroupStandings(nextManaged.GroupCode!.Value);
        var managedEntry = groupStandings.First(s => s.ClubId == engine.ManagedClubId);

        Assert.Equal(1, managedEntry.Played);
        Assert.Equal(1, managedEntry.Won);
        Assert.Equal(0, managedEntry.Drawn);
        Assert.Equal(0, managedEntry.Lost);
        Assert.Equal(2, managedEntry.GoalsFor);
        Assert.Equal(1, managedEntry.GoalsAgainst);
        Assert.Equal(1, managedEntry.GoalDifference);
        Assert.Equal(3, managedEntry.Points);
    }

    [Fact]
    public void SimulatePendingAiMatches_SimulatesRemainingGroupMatches()
    {
        var engine = CreateTestEngine();
        var initialUnplayed = engine.AllFixtures.Count(f => !f.IsPlayed);

        var simulated = engine.SimulatePendingAiMatches(upToDayNumber: 50);

        var playedCount = engine.AllFixtures.Count(f => f.IsPlayed);
        Assert.True(simulated > 0);
        Assert.True(playedCount > 0);
        Assert.True(engine.AllFixtures.Count(f => !f.IsPlayed) < initialUnplayed);
    }

    [Fact]
    public void ContinentalTournamentDigest_ComposesCorrectly_ForPresentation()
    {
        var engine = CreateTestEngine();
        var digest = ContinentalTournamentDigest.Compose(engine, currentDayNumber: 15);

        Assert.True(digest.IsActive);
        Assert.Equal("Avrupa Şampiyonlar Kupası", digest.TournamentTitle);
        Assert.Equal(4, digest.AllGroups.Count);
        Assert.NotNull(digest.UpcomingManagedFixture);
        Assert.Equal(0, digest.TotalPrizeMoneyWon);

        // Her grupta 4 takım puan tablosunda bulunmalı
        foreach (var grp in digest.AllGroups)
        {
            Assert.Equal(4, grp.Standings.Count);
        }
    }

    [Fact]
    public void ContinentalTournamentEngine_WhenInitializedWithCalendarSeasonDate_DoesNotTriggerPrematureMatchInJuly()
    {
        var otherClubs = new List<(long ClubId, string Name, int Strength)>
        {
            (2L, "Fenerbahçe", 80),
            (3L, "Beşiktaş", 78),
            (4L, "Trabzonspor", 76),
        };

        var julyFirst = FootballCareerSimulator.Domain.WorldCalendar.GameDate.FromCalendarDate(2026, 7, 1).DayNumber;
        var septemberFifteenth = FootballCareerSimulator.Domain.WorldCalendar.GameDate.FromCalendarDate(2026, 9, 15).DayNumber;

        var engine = new ContinentalTournamentEngine(
            worldSeed: 12345,
            managedClubId: 1L,
            managedClubName: "Galatasaray",
            managedClubStrength: 82,
            otherDomesticClubs: otherClubs,
            startDayNumber: septemberFifteenth);

        // 1 Temmuz'da henüz vadesi gelmiş kıtasal maç olmamalıdır
        var dueFixture = engine.GetNextDueFixture(julyFirst);
        Assert.Null(dueFixture);

        var nextManaged = engine.GetNextManagedFixture(julyFirst);
        Assert.NotNull(nextManaged);
        Assert.True(nextManaged.ScheduledDayNumber > julyFirst);
    }

    private static ContinentalTournamentEngine CreateTestEngine()
    {
        var otherClubs = new List<(long ClubId, string Name, int Strength)>
        {
            (2L, "Fenerbahçe", 80),
            (3L, "Beşiktaş", 78),
            (4L, "Trabzonspor", 76),
        };

        return new ContinentalTournamentEngine(
            worldSeed: 12345,
            managedClubId: 1L,
            managedClubName: "Galatasaray",
            managedClubStrength: 82,
            otherDomesticClubs: otherClubs,
            startDayNumber: 15);
    }
}
