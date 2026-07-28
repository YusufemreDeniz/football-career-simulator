using Godot;

namespace FootballCareerSimulator.Presentation;

/// <summary>
/// Ana sahne: menü → kariyer hub → maç sonucu. CI/export smoke ayrı host'ta çalışır.
/// </summary>
public partial class CareerAppRoot : Control
{
    private Control? _currentScreen;

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

    public void ShowHub(
        CareerSessionController controller,
        string? statusMessage = null,
        Application.Competition.Queries.PostMatchOfficeDigest? officeReturn = null)
    {
        var hub = new CareerHubScreen(controller);
        hub.BackToMenuRequested += ShowMainMenu;
        hub.MatchResultsReady += results => ShowMatchResults(controller, results);
        ReplaceScreen(hub);
        if (officeReturn is not null)
        {
            hub.ApplyOfficeReturn(officeReturn);
        }
        else if (!string.IsNullOrWhiteSpace(statusMessage))
        {
            hub.SetStatus(statusMessage!);
        }
    }

    public void ShowMatchResults(CareerSessionController controller, PlayMatchesUiResult results)
    {
        var panel = new MatchResultScreen(results);
        panel.ContinueRequested += () =>
            ShowHub(controller, officeReturn: controller.BuildPostMatchOfficeReturn(results));
        ReplaceScreen(panel);
    }

    private void OnNewCareer()
    {
        var host = CareerPresentationHost.CreateDefault();
        var controller = new CareerSessionController(host);
        var setup = controller.EnsureLeagueReady();
        ShowHub(controller, setup.Message);
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

        ShowHub(controller, load.Message);
    }

    private void ReplaceScreen(Control screen)
    {
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
        AddChild(screen);
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
