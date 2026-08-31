using FootballCareerSimulator.Application.Competition.Queries;
using Godot;

namespace FootballCareerSimulator.Presentation;

/// <summary>
/// Presents the deterministic match simulation as a spoiler-free, controllable
/// 2D live timeline before the report and final score pages are revealed.
/// </summary>
public sealed partial class LiveMatchTimelineScreen : Control
{
    private readonly PlayMatchesUiResult _results;
    private readonly ProceduralMatchAudioDirector? _audio;
    private readonly List<string> _eventLines = [];

    private MatchMomentPitchView _pitch = null!;
    private Label _clockLabel = null!;
    private Label _stateLabel = null!;
    private Label _eventFeed = null!;
    private Button _playPauseButton = null!;
    private Button _previousButton = null!;
    private Button _nextButton = null!;
    private Button _resultsButton = null!;
    private float _baseSecondsPerMoment;
    private float _speed = 1f;

    public event Action? ResultsRequested;

    public LiveMatchTimelineScreen(
        PlayMatchesUiResult results,
        ProceduralMatchAudioDirector? audio = null)
    {
        _results = results ?? throw new ArgumentNullException(nameof(results));
        _audio = audio;
    }

    public override void _Ready()
    {
        CareerUiTheme.EnsureLoaded();
        var margin = MatchScreenUi.CreateStageRoot(
            this,
            new Color(CareerUiTheme.Accent.R, CareerUiTheme.Accent.G, CareerUiTheme.Accent.B, 0.04f));
        var shell = MatchScreenUi.VerticalStack(10);
        margin.AddChild(shell);

        shell.AddChild(MatchScreenUi.StageMarker(
            "CANLI 2D  •  MAÇ MERKEZİ",
            "Skoru görmeden maçı yaşa",
            CareerUiTheme.Accent));

        var statusCard = MatchScreenUi.Card(emphasized: true);
        shell.AddChild(statusCard);
        var statusStack = MatchScreenUi.VerticalStack(5);
        statusCard.AddChild(statusStack);
        _clockLabel = MatchScreenUi.BodyLine("0' · Başlama vuruşu", alignment: HorizontalAlignment.Center);
        CareerUiTheme.StyleHeadline(_clockLabel);
        _clockLabel.AddThemeFontSizeOverride("font_size", CareerUiTheme.FontSize(24));
        statusStack.AddChild(_clockLabel);
        _stateLabel = MatchScreenUi.BodyLine("CANLI · 1.0x", muted: true, alignment: HorizontalAlignment.Center);
        statusStack.AddChild(_stateLabel);

        var pitchCard = MatchScreenUi.Card(emphasized: true);
        pitchCard.SizeFlagsVertical = SizeFlags.ExpandFill;
        shell.AddChild(pitchCard);
        _baseSecondsPerMoment = GameExperienceSettingsStore.Current.ReducedMotion ? 2.25f : 1.65f;
        _pitch = new MatchMomentPitchView
        {
            ReducedMotion = GameExperienceSettingsStore.Current.ReducedMotion,
            SecondsPerMoment = _baseSecondsPerMoment,
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            SizeFlagsVertical = SizeFlags.ExpandFill,
            CustomMinimumSize = new Vector2(320, 210),
        };
        _pitch.MomentChanged += OnMomentChanged;
        _pitch.SequenceFinished += OnSequenceFinished;
        pitchCard.AddChild(_pitch);

        var controls = new HFlowContainer
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
        };
        controls.AddThemeConstantOverride("h_separation", 7);
        controls.AddThemeConstantOverride("v_separation", 7);
        shell.AddChild(controls);
        _previousButton = AddControlButton(controls, "◀ Önceki", () =>
        {
            _pitch.Pause();
            _pitch.Retreat();
            RefreshControls();
        });
        _playPauseButton = AddControlButton(controls, "Duraklat", TogglePlayback);
        _nextButton = AddControlButton(controls, "Sonraki ▶", () =>
        {
            _pitch.Pause();
            if (!_pitch.Advance()
                || _pitch.CurrentFrameIndex + 1 >= _pitch.Storyboard.Frames.Count)
            {
                OnSequenceFinished();
            }
            RefreshControls();
        });
        AddControlButton(controls, "0.5x", () => SetSpeed(0.5f));
        AddControlButton(controls, "1x", () => SetSpeed(1f));
        AddControlButton(controls, "2x", () => SetSpeed(2f));

        var feedCard = MatchScreenUi.Card();
        shell.AddChild(feedCard);
        _eventFeed = MatchScreenUi.BodyLine("Maç anlatımı başlıyor…", muted: true);
        _eventFeed.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        feedCard.AddChild(_eventFeed);

        _resultsButton = new Button
        {
            Text = "Maçı Bitir ve Raporu Aç",
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            Disabled = true,
        };
        CareerUiTheme.StylePrimaryButton(_resultsButton);
        _resultsButton.Pressed += () => ResultsRequested?.Invoke();
        shell.AddChild(_resultsButton);

        _pitch.SetMoments(_results.KeyMoments, _results.MatchSequenceSeed, autoplay: true);
        RefreshControls();
    }

    private Button AddControlButton(Control parent, string text, Action action)
    {
        var button = new Button { Text = text, SizeFlagsHorizontal = SizeFlags.ExpandFill };
        CareerUiTheme.StyleSecondaryButton(button);
        button.Pressed += action;
        parent.AddChild(button);
        return button;
    }

    private void TogglePlayback()
    {
        if (_pitch.IsPlaying)
        {
            _pitch.Pause();
        }
        else if (_pitch.CurrentFrameIndex + 1 >= _pitch.Storyboard.Frames.Count)
        {
            _pitch.Restart(autoplay: true);
            _eventLines.Clear();
        }
        else
        {
            _pitch.Play();
        }

        RefreshControls();
    }

    private void SetSpeed(float speed)
    {
        _speed = speed;
        _pitch.SecondsPerMoment = _baseSecondsPerMoment / speed;
        RefreshControls();
    }

    private void OnMomentChanged(int index, MatchMomentStoryboardFrame frame)
    {
        _clockLabel.Text = $"{frame.Minute}' · {FormatMoment(frame.Kind)}";
        var line = frame.AssistPlayerName is { Length: > 0 } assist
            ? $"{frame.Minute}'  {frame.PrimaryPlayerName} · asist {assist}"
            : $"{frame.Minute}'  {frame.PrimaryPlayerName} · {FormatMoment(frame.Kind)}";
        if (!_eventLines.Contains(line, StringComparer.Ordinal))
        {
            _eventLines.Add(line);
        }

        _eventFeed.Text = string.Join('\n', _eventLines.TakeLast(4));
        _audio?.TryPlayMoment(frame.Kind, frame.Minute, frame.PrimarySlotIndex);
        RefreshControls();
    }

    private void OnSequenceFinished()
    {
        _pitch.Pause();
        _clockLabel.Text = $"MAÇ SONU · {_results.ManagedGoals}-{_results.OpponentGoals}";
        _stateLabel.Text = "TAMAMLANDI · rapor hazır";
        _resultsButton.Disabled = false;
        _resultsButton.GrabFocus();
        RefreshControls();
    }

    private void RefreshControls()
    {
        if (_pitch is null || _pitch.Storyboard.Frames.Count == 0)
        {
            return;
        }

        _playPauseButton.Text = _pitch.IsPlaying ? "Duraklat" : "Devam Et";
        _previousButton.Disabled = _pitch.CurrentFrameIndex <= 0;
        _nextButton.Disabled = _pitch.CurrentFrameIndex + 1 >= _pitch.Storyboard.Frames.Count;
        if (!_resultsButton.Disabled)
        {
            _stateLabel.Text = "TAMAMLANDI · rapor hazır";
        }
        else
        {
            _stateLabel.Text = $"{(_pitch.IsPlaying ? "CANLI" : "DURAKLATILDI")} · {_speed:0.0}x";
        }
    }

    private static string FormatMoment(string kind) => kind switch
    {
        "Goal" => "Gol",
        "YellowCard" => "Sarı kart",
        "RedCard" => "Kırmızı kart",
        "Injury" => "Sakatlık",
        _ => "Kritik an",
    };
}
