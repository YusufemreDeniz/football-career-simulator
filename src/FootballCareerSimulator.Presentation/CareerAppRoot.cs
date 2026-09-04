using Godot;

namespace FootballCareerSimulator.Presentation;

/// <summary>
/// Ana sahne: menü → kariyer hub → maç günü → maç sonucu. CI/export smoke ayrı host'ta çalışır.
/// </summary>
public partial class CareerAppRoot : Control
{
    private Control? _currentScreen;
    private Tween? _screenTransition;
    private ProceduralMatchAudioDirector _audioDirector = null!;
    private MobileRuntimeTelemetryMonitor _runtimeTelemetry = null!;

    public override void _Ready()
    {
        CareerUiTheme.Configure(GameExperienceSettingsStore.Current);
        _runtimeTelemetry = new MobileRuntimeTelemetryMonitor();
        AddChild(_runtimeTelemetry);
        _audioDirector = new ProceduralMatchAudioDirector();
        _audioDirector.ApplySettings(ToAudioSettings(GameExperienceSettingsStore.Current));
        AddChild(_audioDirector);
        GameExperienceSettingsStore.Changed += OnExperienceSettingsChanged;

        if (OS.HasFeature("android"))
        {
            DisplayServer.ScreenSetOrientation(DisplayServer.ScreenOrientation.Landscape);
        }

        AnchorRight = 1f;
        AnchorBottom = 1f;
        GrowHorizontal = GrowDirection.Both;
        GrowVertical = GrowDirection.Both;

        if (TryGetSnapshotPath(out var snapshotPath))
        {
            CaptureCareerSnapshot(snapshotPath);
            return;
        }

        if (ShouldRunSmokeTest())
        {
            GD.Print("[CareerAppRoot] Smoke test modu.");
            CareerUiSmokeTest.Run();
            return;
        }

        ShowMainMenu();
    }

    public override void _ExitTree()
    {
        GameExperienceSettingsStore.Changed -= OnExperienceSettingsChanged;
    }

    private async void CaptureCareerSnapshot(string snapshotPath)
    {
        try
        {
            var startConfiguration = CareerStartConfiguration.Create(
                "UI Preview",
                startingClubId: 2,
                startingDate: Domain.WorldCalendar.GameDate.FromCalendarDate(2026, 8, 13),
                rootSeed: 741852);
            var host = CareerPresentationHost.CreateNewCareer(
                startConfiguration,
                Path.Combine(OS.GetUserDataDir(), "career_ui_snapshot.db"));
            var controller = new CareerSessionController(host);
            controller.EnsureLeagueReady();
            ShowHub(controller);

            // Snapshot QA needs the final readable state, not a partially faded
            // transition frame. Normal interactive transitions remain unchanged.
            _screenTransition?.Kill();
            if (_currentScreen is not null)
            {
                _currentScreen.Modulate = Colors.White;
            }

            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);

            var absolutePath = Path.GetFullPath(snapshotPath);
            DirAccess.MakeDirRecursiveAbsolute(Path.GetDirectoryName(absolutePath)!);
            var result = GetViewport().GetTexture().GetImage().SavePng(absolutePath);
            GD.Print($"CAREER_UI_SNAPSHOT_RESULT={result};PATH={absolutePath}");
            GetTree().Quit(result == Error.Ok ? 0 : 1);
        }
        catch (Exception ex)
        {
            GD.PushError($"[CareerAppRoot] UI snapshot alınamadı: {ex}");
            GetTree().Quit(1);
        }
    }

    private static bool TryGetSnapshotPath(out string path)
    {
        const string prefix = "--career-snapshot=";
        var argument = OS.GetCmdlineUserArgs()
            .Concat(OS.GetCmdlineArgs())
            .FirstOrDefault(arg => arg.StartsWith(prefix, StringComparison.Ordinal));
        path = argument is null ? string.Empty : argument[prefix.Length..];
        return !string.IsNullOrWhiteSpace(path);
    }

    public void ShowMainMenu()
    {
        _audioDirector.StopAtmosphere();
        var menu = new MainMenuScreen();
        menu.NewCareerRequested += OnNewCareer;
        menu.ContinueRequested += OnContinueCareer;
        ReplaceScreen(menu);
    }

    private void ShowNewCareerSetup()
    {
        var setup = new CareerSetupScreen();
        setup.CancelRequested += ShowMainMenu;
        setup.CareerConfirmed += StartNewCareer;
        ReplaceScreen(setup);
    }

    public void ShowHub(
        CareerSessionController controller,
        string? statusMessage = null,
        Application.Competition.Queries.PostMatchOfficeDigest? officeReturn = null,
        Application.CareerHub.Queries.CareerResumeDigest? careerResume = null,
        bool showExperienceSettings = false)
    {
        _audioDirector.StopAtmosphere();
        var hub = new CareerHubScreen(controller);
        hub.BackToMenuRequested += ShowMainMenu;
        hub.MatchDayRequested += () => ShowMatchDay(controller);
        hub.ExperienceSettingsChanged += () =>
            Callable.From(() => ShowHub(
                controller,
                "Oyun deneyimi ayarları uygulandı.",
                showExperienceSettings: true)).CallDeferred();
        ReplaceScreen(hub);
        if (officeReturn is not null)
        {
            hub.ApplyOfficeReturn(officeReturn);
        }
        else if (careerResume is not null)
        {
            hub.ApplyCareerResume(careerResume);
        }
        else if (!string.IsNullOrWhiteSpace(statusMessage))
        {
            hub.SetStatus(statusMessage!);
        }

        if (showExperienceSettings)
        {
            hub.ShowExperienceSettings();
        }
    }

    public void ShowMatchDay(CareerSessionController controller)
    {
        var timeline = controller.Host.WorldModule.TimelineStore.Timeline;
        _audioDirector.StartAtmosphere(
            unchecked(timeline.RootSeed ^ timeline.CurrentDate.DayNumber));
        var panel = new LandscapeMatchDayScreen(controller);
        panel.BackRequested += () => ShowHub(controller);
        panel.KickoffRequested += () => ShowKickoffMoment(controller);
        ReplaceScreen(panel);
    }

    /// <summary>
    /// Düdük anı — maç nabzı: düdükten sonra sahaya giriş satırları, sonra HT/sonuç.
    /// </summary>
    public void ShowKickoffMoment(CareerSessionController controller)
    {
        var panel = new MatchKickoffScreen(controller.BuildMatchKickoffMoment());
        panel.BackRequested += () => ShowMatchDay(controller);
        panel.ProceedRequested += () => ProceedAfterKickoff(controller);
        ReplaceScreen(panel);
    }

    private void ProceedAfterKickoff(CareerSessionController controller)
    {
        try
        {
            _audioDirector.TryPlayCue(MatchAudioCue.Whistle);
            var halfTime = controller.BuildManagedHalfTimeDigest();
            if (halfTime.HasManagedMatch)
            {
                ShowHalfTime(controller, halfTime);
                return;
            }

            var results = controller.PlayDueMatches();
            if (results.Succeeded && results.MatchLines.Count > 0)
            {
                ShowLiveMatch(controller, results);
                return;
            }

            ShowMatchDay(controller);
        }
        catch (Exception ex)
        {
            GD.PushError($"[CareerAppRoot] Düdük sonrası ilerleme başarısız: {ex}");
            ShowMatchDay(controller);
        }
    }

    public void ShowHalfTime(CareerSessionController controller, Application.Competition.Queries.MatchHalfTimeDigest digest)
    {
        var panel = new LandscapeHalfTimeScreen(controller, digest);
        panel.BackRequested += () => ShowMatchDay(controller);
        panel.SecondHalfRequested += (delta, substitutionBridge) =>
        {
            var decisionLabel = delta switch
            {
                Application.Competition.Queries.MatchHalfTimeDigest.DecisionAttack =>
                    "Devre arasında hücuma geçtin.",
                Application.Competition.Queries.MatchHalfTimeDigest.DecisionDefend =>
                    "Devre arasında savunmaya çektin.",
                _ => "Devre arasında aynı planla devam ettin.",
            };
            var results = controller.PlayDueMatches(
                managedSecondHalfDelta: delta,
                halfTime: digest,
                halfTimeDecisionLabel: decisionLabel,
                halfTimeSubstitutionLabel: substitutionBridge);
            if (results.Succeeded && results.MatchLines.Count > 0)
            {
                ShowLiveMatch(controller, results);
                return;
            }

            ShowMatchDay(controller);
        };
        ReplaceScreen(panel);
    }

    public void ShowMatchResults(CareerSessionController controller, PlayMatchesUiResult results)
    {
        _audioDirector.TryPlayResult(
            results.ManagedGoals,
            results.OpponentGoals,
            results.MatchSequenceSeed);
        var panel = new MatchResultScreen(results, _audioDirector);
        panel.ContinueRequested += () => ReturnFromMatchNight(controller, results);
        ReplaceScreen(panel);
    }

    private void ShowLiveMatch(CareerSessionController controller, PlayMatchesUiResult results)
    {
        if (results.KeyMoments is not { Count: > 0 })
        {
            ShowMatchResults(controller, results);
            return;
        }

        var panel = new LiveMatchTimelineScreen(results, _audioDirector);
        panel.ResultsRequested += () => ShowMatchResults(controller, results);
        ReplaceScreen(panel);
    }

    private void ReturnFromMatchNight(CareerSessionController controller, PlayMatchesUiResult results)
    {
        try
        {
            var office = controller.BuildPostMatchOfficeReturn(results);
            ShowHub(controller, officeReturn: office);
        }
        catch (Exception ex)
        {
            GD.PushError($"[CareerAppRoot] Maç sonrası ofis dönüşü başarısız: {ex}");
            ShowHub(controller, statusMessage: $"Maçlar tamam. (Ofis özeti atlandı: {ex.Message})");
        }
    }

    private void OnNewCareer()
    {
        ShowNewCareerSetup();
    }

    private void StartNewCareer(CareerStartConfiguration configuration)
    {
        try
        {
            ClearPreviousCareerSave();
            GameExperienceSettingsStore.Update(current => current with { FirstWeekGuideStep = 0 });
        }
        catch (Exception ex)
        {
            GD.PushError($"[CareerAppRoot] Onceki kayit temizlenemedi: {ex}");
            ShowMainMenu();
            return;
        }

        var host = CareerPresentationHost.CreateNewCareer(configuration);
        var controller = new CareerSessionController(host);
        var setup = controller.EnsureLeagueReady();
        ShowHub(controller, setup.Message);
    }

    private static void ClearPreviousCareerSave()
    {
        var savePath = Path.Combine(OS.GetUserDataDir(), "career_save.db");
        foreach (var path in new[] { savePath, savePath + ".bak", savePath + ".migrating.tmp" })
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
    }

    private void OnContinueCareer()
    {
        var host = CareerPresentationHost.CreateDefault();
        var controller = new CareerSessionController(host);
        var load = controller.LoadGame();
        if (!load.Succeeded)
        {
            if (_currentScreen is MainMenuScreen menu)
            {
                menu.SetStatus(load.Message);
            }

            return;
        }

        ShowHub(controller, careerResume: controller.LastCareerResume);
    }

    private void ReplaceScreen(Control screen)
    {
        _screenTransition?.Kill();
        if (_currentScreen is not null)
        {
            _audioDirector.TryPlayCue(MatchAudioCue.Button);
            RemoveChild(_currentScreen);
            _currentScreen.QueueFree();
        }

        _currentScreen = screen;
        screen.AnchorRight = 1f;
        screen.AnchorBottom = 1f;
        screen.GrowHorizontal = GrowDirection.Both;
        screen.GrowVertical = GrowDirection.Both;
        screen.Modulate = new Color(1f, 1f, 1f, 0f);
        AddChild(screen);
        Callable.From(() => UiFocusCoordinator.Prepare(
            screen,
            grabInitialFocus: GameExperienceSettingsStore.Current.GamepadNavigationHintsEnabled))
            .CallDeferred();

        if (CareerUiTheme.ReducedMotion)
        {
            screen.Modulate = Colors.White;
            return;
        }

        _screenTransition = CreateTween();
        _screenTransition
            .TweenProperty(screen, "modulate:a", 1f, 0.22f)
            .SetTrans(Tween.TransitionType.Cubic)
            .SetEase(Tween.EaseType.Out);
    }

    private void OnExperienceSettingsChanged(
        Application.CareerHub.Queries.GameExperiencePreferences preferences)
    {
        CareerUiTheme.Configure(preferences);
        _audioDirector.ApplySettings(ToAudioSettings(preferences));
    }

    private static MatchAudioSettings ToAudioSettings(
        Application.CareerHub.Queries.GameExperiencePreferences preferences) =>
        MatchAudioSettings.FromPreferences(preferences);

    internal static bool ShouldRunSmokeTest()
    {
        if (DisplayServer.GetName() == "headless")
        {
            return true;
        }

        foreach (var arg in OS.GetCmdlineArgs())
        {
            if (arg is "--career-smoke" or "--smoke-test")
            {
                return true;
            }

            if (arg.StartsWith("--quit-after", StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }
}
