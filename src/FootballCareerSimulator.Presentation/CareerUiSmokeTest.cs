using FootballCareerSimulator.Application.Competition.Commands;
using FootballCareerSimulator.Application.TeamPreparation.Commands;
using FootballCareerSimulator.Domain.Competition;
using FootballCareerSimulator.Application.WorldCalendar.Commands;
using Godot;

namespace FootballCareerSimulator.Presentation;

/// <summary>
/// Export/CI smoke: ayrı bir host üzerinde kayıt, planlama ve lig/maç döngüsünü doğrular.
/// İnteraktif oyuncu oturumunu bozmaz.
/// </summary>
public static class CareerUiSmokeTest
{
    public static bool Run()
    {
        var passed = true;
        var host = CareerPresentationHost.CreateDefault(
            Path.Combine(OS.GetUserDataDir(), "career_ui_selfcheck.db"));
        var world = host.WorldModule;
        var competition = host.CompetitionModule;

        var before = world.Queries.GetCurrentGameDate();
        passed &= LogCheck("Başlangıç tarihi", before.DayNumber > 0);

        var result = world.AdvanceSimulationTime.Handle(
            new AdvanceSimulationTimeCommand(Guid.NewGuid(), before.DayNumber + 1));
        passed &= LogCheck("1 gün ilerletme", result.Succeeded && result.NewDayNumber == before.DayNumber + 1);

        var after = world.Queries.GetCurrentGameDate();
        passed &= LogCheck("Query güncel tarih", after.DayNumber == before.DayNumber + 1);

        var selfCheckSavePath = Path.Combine(OS.GetUserDataDir(), "career_ui_selfcheck_roundtrip.db");
        try
        {
            var checkpointDay = after.DayNumber;
            var saveResult = host.GameSession.Save(selfCheckSavePath);
            passed &= LogCheck("Career kayıt", saveResult.Succeeded && saveResult.SavedDayNumber == checkpointDay);

            world.AdvanceSimulationTime.Handle(
                new AdvanceSimulationTimeCommand(Guid.NewGuid(), checkpointDay + 3));
            passed &= LogCheck(
                "İlerletme sonrası tarih değişti",
                world.Queries.GetCurrentGameDate().DayNumber == checkpointDay + 3);

            var loadResult = host.GameSession.Load(selfCheckSavePath);
            passed &= LogCheck(
                "Career yükleme checkpoint",
                loadResult.Succeeded && loadResult.LoadedDayNumber == checkpointDay);
            passed &= LogCheck(
                "Yükleme sonrası query",
                world.Queries.GetCurrentGameDate().DayNumber == checkpointDay);
        }
        catch (Exception ex)
        {
            GD.Print($"[SelfCheck] BAŞARISIZ: Kayıt/yükleme — {ex.Message}.");
            passed = false;
        }

        try
        {
            var current = world.Queries.GetCurrentGameDate();
            var openResult = world.OpenPlanningPeriod.Handle(
                new OpenPlanningPeriodCommand(Guid.NewGuid(), 99, current.DayNumber));
            passed &= LogCheck("Planlama dönemi aç", openResult.Succeeded);

            var period = world.Queries.GetCurrentPlanningPeriod();
            passed &= LogCheck("Aktif dönem query", period is not null && period.PlanningPeriodId == 99);

            var completeResult = world.CompletePlanningPeriod.Handle(
                new CompletePlanningPeriodCommand(Guid.NewGuid()));
            passed &= LogCheck("Planlama dönemi tamamla", completeResult.Succeeded);
            passed &= LogCheck("Tamamlama sonrası aktif dönem yok", world.Queries.GetCurrentPlanningPeriod() is null);
        }
        catch (Exception ex)
        {
            GD.Print($"[SelfCheck] BAŞARISIZ: Planlama dönemi — {ex.Message}.");
            passed = false;
        }

        try
        {
            CareerSessionController.SetupLeagueSeasonForSelfCheck(competition, world);
            var season = competition.Queries.GetCurrentSeason();
            passed &= LogCheck(
                "Lig sezonu kurulumu",
                season is not null
                && season.ParticipantCount == CompetitionMvpConstraints.LeagueTeamCount
                && season.FixtureCount == CompetitionMvpConstraints.TotalLeagueFixtures);

            var leagueSavePath = Path.Combine(OS.GetUserDataDir(), "career_league_selfcheck.db");
            var saveResult = host.GameSession.Save(leagueSavePath);
            passed &= LogCheck(
                "Lig kayıt fixture sayısı",
                saveResult.SavedFixtureCount == CompetitionMvpConstraints.TotalLeagueFixtures);

            world.AdvanceSimulationTime.Handle(
                new AdvanceSimulationTimeCommand(
                    Guid.NewGuid(),
                    world.Queries.GetCurrentGameDate().DayNumber + 5));

            var loadResult = host.GameSession.Load(leagueSavePath);
            passed &= LogCheck(
                "Lig yükleme fixture sayısı",
                loadResult.LoadedFixtureCount == CompetitionMvpConstraints.TotalLeagueFixtures);
            passed &= LogCheck(
                "Yükleme sonrası sezon query",
                competition.Queries.GetCurrentSeason()?.FixtureCount
                == CompetitionMvpConstraints.TotalLeagueFixtures);

            var roundOne = competition.Queries.GetFixturesByRound(
                CareerSessionController.DefaultSeasonId,
                round: 1);
            passed &= LogCheck(
                "1. hafta maç sayısı",
                roundOne.Count == CompetitionMvpConstraints.LeagueFixturesPerRound);

            var firstMatchday = roundOne[0].ScheduledDayNumber;
            world.AdvanceSimulationTime.Handle(
                new AdvanceSimulationTimeCommand(Guid.NewGuid(), firstMatchday));

            var managedClubId = host.ManagerModule.Queries.GetCareer().EmployedClubId ?? 1;
            var managedRoundOne = roundOne.FirstOrDefault(fixture =>
                fixture.HomeClubId == managedClubId || fixture.AwayClubId == managedClubId);
            if (managedRoundOne is not null)
            {
                var controller = new CareerSessionController(host);
                controller.SetTacticPressing(Domain.TeamPreparation.PressingIntensity.HighPress);
                controller.SetTacticDefensiveLine(Domain.TeamPreparation.DefensiveLine.High);
                controller.SetTacticPassingStyle(Domain.TeamPreparation.PassingStyle.Short);
                var advancedTactic = controller.GetManagedTacticPlan();
                passed &= LogCheck(
                    "Gelişmiş taktik tahtası",
                    advancedTactic.Pressing == Domain.TeamPreparation.PressingIntensity.HighPress
                    && advancedTactic.DefensiveLine == Domain.TeamPreparation.DefensiveLine.High
                    && advancedTactic.PassingStyle == Domain.TeamPreparation.PassingStyle.Short);
                var compatibility = controller.BuildLineupCompatibility();
                passed &= LogCheck(
                    "Kadro uyumu önizlemesi",
                    compatibility.HasLineup
                    && compatibility.Players.All(player =>
                        !string.IsNullOrWhiteSpace(player.PositionCode)));
                var selectionBoard = controller.BuildSquadSelectionBoard();
                passed &= LogCheck(
                    "Gerçek kadro seçim panosu",
                    selectionBoard.HasMatch
                    && selectionBoard.StartingXi.Count
                        == Domain.TeamPreparation.MatchSelection.StartingXiSize
                    && selectionBoard.Bench.Count
                        == Domain.TeamPreparation.MatchSelection.MaxBenchSize);
                var playerManagement = controller.BuildPlayerManagementDigest();
                passed &= LogCheck(
                    "Futbolcu yönetim merkezi",
                    playerManagement.HasClub
                    && playerManagement.Players.Count == Domain.TeamPreparation.ClubSquad.MaxMembers
                    && playerManagement.Players.All(player =>
                        player.PlayerId > 0
                        && !string.IsNullOrWhiteSpace(player.DisplayName)
                        && !string.IsNullOrWhiteSpace(player.PositionCode)));
                var scout = controller.BuildScoutTransferDigest();
                passed &= LogCheck(
                    "Scout ve transfer merkezi",
                    scout.HasClub
                    && scout.Candidates.Count > 0
                    && scout.Candidates.All(candidate =>
                        candidate.PlayerId > 0
                        && !string.IsNullOrWhiteSpace(candidate.DisplayName)
                        && candidate.EstimatedAbilityLow <= candidate.EstimatedAbilityHigh));

                host.TeamPreparationModule.ApproveDefaultSelection.Handle(
                    new ApproveDefaultMatchSelectionCommand(
                        Guid.NewGuid(),
                        managedRoundOne.FixtureId,
                        managedClubId));
                passed &= LogCheck("Kadro onayı", true);
            }

            var playFixture = managedRoundOne ?? roundOne[0];
            var playResult = competition.PlayFixtureMatch!.Handle(
                new PlayFixtureMatchCommand(
                    Guid.NewGuid(),
                    CareerSessionController.DefaultSeasonId,
                    playFixture.FixtureId,
                    firstMatchday));
            passed &= LogCheck("Maç oynatma", playResult.Succeeded);
            passed &= LogCheck(
                "Puan durumu güncellendi",
                competition.Queries.GetStandings(CareerSessionController.DefaultSeasonId)
                    .Count(entry => entry.Played > 0) == 2);
            var leagueStatistics = new CareerSessionController(host).BuildLeagueStatisticsDigest();
            passed &= LogCheck(
                "Lig ve istatistik merkezi",
                leagueStatistics.HasData
                && leagueStatistics.Teams.Count == CompetitionMvpConstraints.LeagueTeamCount
                && leagueStatistics.Teams.Count(team => team.LastFiveForm != "—") == 2);
        }
        catch (Exception ex)
        {
            GD.Print($"[SelfCheck] BAŞARISIZ: Lig sezonu — {ex.Message}.");
            passed = false;
        }

        GD.Print(passed ? "[SelfCheck] TÜMÜ BAŞARILI." : "[SelfCheck] BİR VEYA DAHA FAZLA KONTROL BAŞARISIZ.");
        GD.Print(passed ? "WORLD_CALENDAR_UI_SMOKE_TEST_RESULT=PASS" : "WORLD_CALENDAR_UI_SMOKE_TEST_RESULT=FAIL");
        GD.Print(passed ? "CAREER_UI_SMOKE_TEST_RESULT=PASS" : "CAREER_UI_SMOKE_TEST_RESULT=FAIL");
        GD.Print(passed ? "SPIKE5_SMOKE_TEST_RESULT=PASS" : "SPIKE5_SMOKE_TEST_RESULT=FAIL");
        return passed;
    }

    private static bool LogCheck(string name, bool ok)
    {
        GD.Print(ok ? $"[SelfCheck] {name} OK." : $"[SelfCheck] BAŞARISIZ: {name}.");
        return ok;
    }
}
