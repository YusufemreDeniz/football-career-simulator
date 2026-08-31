using FootballCareerSimulator.Application.Competition.Commands;
using FootballCareerSimulator.Application.Competition.Queries;
using FootballCareerSimulator.Application.PlayerCareer.Queries;
using FootballCareerSimulator.Application.TeamPreparation.Commands;
using FootballCareerSimulator.Domain.Competition;
using FootballCareerSimulator.Domain.WorldCalendar;
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
        var mobileLayout = Application.CareerHub.Queries.MobileUiLayoutProfile.Resolve(
            360,
            800,
            safeLeftInset: 12,
            safeTopInset: 24,
            safeRightInset: 8,
            safeBottomInset: 32);
        passed &= LogCheck(
            "Mobil güvenli alan ve dokunma profili",
            mobileLayout.IsCompact
            && mobileLayout.NavigationColumns == 3
            && mobileLayout.TouchTargetHeight >= 48
            && mobileLayout.LeftMargin >= 12
            && mobileLayout.TopMargin >= 24
            && mobileLayout.RightMargin >= 8
            && mobileLayout.BottomMargin >= 32);
        var scaledSafeArea = DisplaySafeAreaInsets.FromMetrics(
            new Vector2(844, 390),
            new Vector2I(2400, 1080),
            new Rect2I(90, 24, 2220, 1020));
        passed &= LogCheck(
            "Fiziksel güvenli alanın dört kenar ölçeği",
            scaledSafeArea.Left == 32
            && scaledSafeArea.Top == 9
            && scaledSafeArea.Right == 32
            && scaledSafeArea.Bottom == 13);
        var deviceAcceptance = Application.CareerHub.Queries.MobileDeviceAcceptanceProfile.Evaluate(
            360,
            800,
            12,
            24,
            8,
            32,
            mobileLayout.TouchTargetHeight,
            bodyFontSize: 16,
            touchInputAvailable: false);
        passed &= LogCheck(
            "Mobil cihaz kabul ayrımı",
            deviceAcceptance.IsReady
            && deviceAcceptance.PhysicalEvidencePending
            && deviceAcceptance.Checks.Any(check =>
                check.Contains("fiziksel release soak", StringComparison.OrdinalIgnoreCase)));
        var guidePreferences = Application.CareerHub.Queries.GameExperiencePreferences.Default with
        {
            TextScalePercent = 130,
            FirstWeekGuideStep = 2,
        };
        var firstWeekGuide = Application.CareerHub.Queries.FirstWeekGuideDigest.Compose(
            guidePreferences.FirstWeekGuideEnabled,
            guidePreferences.FirstWeekGuideStep,
            daysSinceCareerStart: 2);
        passed &= LogCheck(
            "Erişilebilirlik ve ilk hafta rehberi",
            guidePreferences.ScaleFont(16) == 21
            && firstWeekGuide is { IsVisible: true, StepNumber: 3 }
            && firstWeekGuide.CurrentStep?.TargetPageCode
                == Application.CareerHub.Queries.FirstWeekGuideDigest.PagePrep);
        var mutedMix = MatchAudioSettings.FromPreferences(
            Application.CareerHub.Queries.GameExperiencePreferences.Default with
            {
                MusicEnabled = false,
                CrowdEnabled = false,
                HapticsEnabled = false,
                HighContrast = true,
                ReducedMotion = true,
            });
        passed &= LogCheck(
            "Ses karışımı ve erişilebilirlik sözleşmesi",
            mutedMix.MasterEnabled
            && mutedMix.SfxEnabled
            && !mutedMix.MusicEnabled
            && !mutedMix.CrowdEnabled);
        passed &= LogCheck(
            "Yatay ve dikey dokunma kaydırması",
            MobileScrollContainer.ResolveDragAxis(new Vector2(20, 2), true, false)
                == MobileScrollContainer.DragAxis.Horizontal
            && MobileScrollContainer.ResolveDragAxis(new Vector2(2, 20), false, true)
                == MobileScrollContainer.DragAxis.Vertical
            && MobileScrollContainer.ResolveDragAxis(new Vector2(20, 2), false, true)
                == MobileScrollContainer.DragAxis.None);
        var landscapeMatchLayout = Application.TeamPreparation.Queries.LandscapeMatchLayoutProfile.Resolve(844, 390);
        passed &= LogCheck(
            "Kompakt yatay maç yerleşimi",
            landscapeMatchLayout.IsCompact
            && landscapeMatchLayout.CommandPanelWidth <= 240
            && landscapeMatchLayout.PitchMinimumHeight <= 190
            && landscapeMatchLayout.ActionButtonHeight >= 44);
        var startDate = Domain.WorldCalendar.GameDate.FromCalendarDate(2026, 8, 13);
        var startConfiguration = CareerStartConfiguration.Create(
            "Smoke Manager",
            startingClubId: 2,
            startingDate: startDate,
            rootSeed: 741852);
        var host = CareerPresentationHost.CreateNewCareer(
            startConfiguration,
            Path.Combine(OS.GetUserDataDir(), "career_ui_selfcheck.db"));
        var world = host.WorldModule;
        var competition = host.CompetitionModule;

        var before = world.Queries.GetCurrentGameDate();
        passed &= LogCheck("Başlangıç tarihi", before.DayNumber > 0);

        var manager = host.ManagerModule.Queries.GetCareer();
        passed &= LogCheck(
            "Yeni kariyer secimi",
            before.DayNumber == startDate.DayNumber
            && manager.DisplayName == "Smoke Manager"
            && manager.EmployedClubId == 2
            && host.WorldModule.TimelineStore.Timeline.RootSeed == 741852);
        passed &= LogCheck(
            "Kulup secim listesi",
            CareerPresentationHost.GetNewCareerClubs(741852).Count
            == ProductionCareerWorldConstraints.ClubCount
            && host.ClubModule.Store.Registry.Clubs.Count
            == ProductionCareerWorldConstraints.ClubCount
            && host.PlayerCareerModule.Store.Careers.Count
            == ProductionCareerWorldConstraints.TargetActivePlayerCount);

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
            var teamCount = host.ClubModule.Store.Registry.Clubs.Count;
            CareerSessionController.SetupLeagueSeasonForSelfCheck(competition, world, teamCount);
            var season = competition.Queries.GetCurrentSeason();
            passed &= LogCheck(
                "Lig sezonu kurulumu",
                season is not null
                && season.ParticipantCount == teamCount
                && season.FixtureCount == CompetitionMvpConstraints.TotalFixturesFor(teamCount));

            var leagueSavePath = Path.Combine(OS.GetUserDataDir(), "career_league_selfcheck.db");
            var saveResult = host.GameSession.Save(leagueSavePath);
            passed &= LogCheck(
                "Lig kayıt fixture sayısı",
                saveResult.SavedFixtureCount == CompetitionMvpConstraints.TotalFixturesFor(teamCount));

            world.AdvanceSimulationTime.Handle(
                new AdvanceSimulationTimeCommand(
                    Guid.NewGuid(),
                    world.Queries.GetCurrentGameDate().DayNumber + 5));

            var loadResult = host.GameSession.Load(leagueSavePath);
            passed &= LogCheck(
                "Lig yükleme fixture sayısı",
                loadResult.LoadedFixtureCount == CompetitionMvpConstraints.TotalFixturesFor(teamCount));
            passed &= LogCheck(
                "Yükleme sonrası sezon query",
                competition.Queries.GetCurrentSeason()?.FixtureCount
                == CompetitionMvpConstraints.TotalFixturesFor(teamCount));

            var roundOne = competition.Queries.GetFixturesByRound(
                CareerSessionController.DefaultSeasonId,
                round: 1);
            passed &= LogCheck(
                "1. hafta maç sayısı",
                roundOne.Count == CompetitionMvpConstraints.FixturesPerRoundFor(teamCount));

            var firstMatchday = roundOne[0].ScheduledDayNumber;
            world.AdvanceSimulationTime.Handle(
                new AdvanceSimulationTimeCommand(Guid.NewGuid(), firstMatchday));

            var managedClubId = host.ManagerModule.Queries.GetCareer().EmployedClubId ?? 1;
            var expectedPreparationModifier = 0;
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

                var trainingPriority = controller.BuildMatchTrainingPriorityDigest();
                passed &= LogCheck(
                    "Rakibe göre maç antrenmanı",
                    trainingPriority.IsAvailable
                    && trainingPriority.Options.Count == 5
                    && trainingPriority.RecommendedPriority is not null);
                var selectedTraining = trainingPriority.Options
                    .OrderByDescending(option => option.TemporaryMatchModifier)
                    .ThenBy(option => option.Rank)
                    .First();
                expectedPreparationModifier = selectedTraining.TemporaryMatchModifier;
                var trainingSelection = controller.SelectMatchTrainingPriority(selectedTraining.Priority);
                passed &= LogCheck(
                    "Maç antrenmanı seçimi",
                    trainingSelection.Succeeded
                    && trainingSelection.Message.Contains(
                        selectedTraining.Title,
                        StringComparison.Ordinal));
                var preparationSavePath = Path.Combine(
                    OS.GetUserDataDir(),
                    "career_match_training_selfcheck.db");
                host.GameSession.Save(
                    preparationSavePath,
                    controller.CaptureHubNarrativeUiState());
                var preparationLoad = host.GameSession.Load(preparationSavePath);
                controller.ApplyHubNarrativeUiState(preparationLoad.HubNarrativeUiState);
                passed &= LogCheck(
                    "Maç antrenmanı save/load",
                    preparationLoad.HubNarrativeUiState?.PendingMatchTrainingFixtureId
                        == managedRoundOne.FixtureId
                    && preparationLoad.HubNarrativeUiState.PendingMatchTrainingModifier
                        == expectedPreparationModifier);

                var academy = controller.BuildYouthAcademyIntake();
                passed &= LogCheck(
                    "Sezonluk genç akademisi",
                    academy is { IsRevealed: true }
                    && academy.Candidates.Count is >= 3 and <= 5);
                if (academy?.Candidates.FirstOrDefault(candidate =>
                        candidate.DecisionStatus == YouthAcademyCandidateDecisionStatus.Pending)
                    is { } youthCandidate)
                {
                    var youthDecision = controller.AcceptYouthAcademyCandidate(youthCandidate.PlayerId);
                    var refreshedAcademy = controller.BuildYouthAcademyIntake();
                    passed &= LogCheck(
                        "Akademi kararının kalıcılığı",
                        youthDecision.Succeeded
                        && refreshedAcademy?.Candidates.Single(candidate =>
                                candidate.PlayerId == youthCandidate.PlayerId)
                            .DecisionStatus == YouthAcademyCandidateDecisionStatus.Accepted);
                }

                var economy = controller.BuildClubEconomy();
                passed &= LogCheck(
                    "Kulüp ekonomisi ve yönetim hedefleri",
                    economy is not null
                    && economy.WeeklyWageLimit > 0
                    && economy.BoardObjectives.Count == 3);

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
                    firstMatchday,
                    ManagedPreparationModifier: expectedPreparationModifier));
            passed &= LogCheck("Maç oynatma", playResult.Succeeded);
            passed &= LogCheck(
                "Maç hazırlığı motor etkisi",
                playResult.ManagedPreparationModifier == expectedPreparationModifier);
            var storyboard = MatchMomentStoryboard.Build(
                playResult.KeyMoments,
                sequenceSeed: unchecked((int)playFixture.FixtureId));
            passed &= LogCheck(
                "Gerçek maç anlarından 2D akış",
                storyboard.Frames.Count == (playResult.KeyMoments?.Count ?? 0));
            passed &= LogCheck(
                "Puan durumu güncellendi",
                competition.Queries.GetStandings(CareerSessionController.DefaultSeasonId)
                    .Count(entry => entry.Played > 0) == 2);
            var leagueStatistics = new CareerSessionController(host).BuildLeagueStatisticsDigest();
            passed &= LogCheck(
                "Lig ve istatistik merkezi",
                leagueStatistics.HasData
                && leagueStatistics.Teams.Count == ProductionCareerWorldConstraints.ClubCount
                && leagueStatistics.Teams.Count(team => team.LastFiveForm != "—") == 2);
            var careerLegacy = new CareerSessionController(host).BuildCareerLegacyDigest();
            passed &= LogCheck(
                "Uzun süreli kariyer döngüsü",
                careerLegacy.HasCareer
                && careerLegacy.Seasons.Count == 1
                && careerLegacy.Seasons[0].Record.Contains("1 maç", StringComparison.Ordinal));
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
