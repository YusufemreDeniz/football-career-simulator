using Godot;

namespace FootballCareerSimulator.Presentation;

/// <summary>
/// Ana sahne: menü → kariyer hub → maç günü → maç sonucu. CI/export smoke ayrı host'ta çalışır.
/// </summary>
public partial class CareerAppRoot : Control
{
    private Control? _currentScreen;
    private Tween? _screenTransition;

    public override void _Ready()
    {
        AnchorRight = 1f;
        AnchorBottom = 1f;
        GrowHorizontal = GrowDirection.Both;
        GrowVertical = GrowDirection.Both;

        if (ShouldRunSmokeTest())
        {
            GD.Print("[CareerAppRoot] Smoke test modu.");
            CareerUiSmokeTest.Run();
            return;
        }

        ShowMainMenu();
    }

    public void ShowMainMenu()
    {
        var menu = new MainMenuScreen();
        menu.NewCareerRequested += OnNewCareer;
        menu.ContinueRequested += OnContinueCareer;
        ReplaceScreen(menu);
    }

    private void ShowNewCareerSetup()
    {
        var setup = new CareerSetupScreen(CareerPresentationHost.GetNewCareerClubs());
        setup.CancelRequested += ShowMainMenu;
        setup.CareerConfirmed += StartNewCareer;
        ReplaceScreen(setup);
    }

    public void ShowHub(
        CareerSessionController controller,
        string? statusMessage = null,
        Application.Competition.Queries.PostMatchOfficeDigest? officeReturn = null,
        Application.CareerHub.Queries.CareerResumeDigest? careerResume = null)
    {
        var hub = new CareerHubScreen(controller);
        hub.BackToMenuRequested += ShowMainMenu;
        hub.MatchDayRequested += () => ShowMatchDay(controller);
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
    }

    public void ShowMatchDay(CareerSessionController controller)
    {
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
            var halfTime = controller.BuildManagedHalfTimeDigest();
            if (halfTime.HasManagedMatch)
            {
                ShowHalfTime(controller, halfTime);
                return;
            }

            var results = controller.PlayDueMatches();
            if (results.Succeeded && results.MatchLines.Count > 0)
            {
                ShowMatchResults(controller, results);
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
                ShowMatchResults(controller, results);
                return;
            }

            ShowMatchDay(controller);
        };
        ReplaceScreen(panel);
    }

    public void ShowMatchResults(CareerSessionController controller, PlayMatchesUiResult results)
    {
        var panel = new MatchResultScreen(results);
        panel.ContinueRequested += () => ReturnFromMatchNight(controller, results);
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

        _screenTransition = CreateTween();
        _screenTransition
            .TweenProperty(screen, "modulate:a", 1f, 0.22f)
            .SetTrans(Tween.TransitionType.Cubic)
            .SetEase(Tween.EaseType.Out);
    }

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
