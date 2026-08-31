using FootballCareerSimulator.Application.Competition.Commands;
using FootballCareerSimulator.Application.Competition.Queries;
using FootballCareerSimulator.Domain.Match;
using Godot;

namespace FootballCareerSimulator.Presentation;

/// <summary>
/// Lightweight, code-drawn 2D stage for critical match moments. It consumes a
/// deterministic <see cref="MatchMomentStoryboard"/>, needs no textures or scene
/// resources, and can be driven automatically or one frame at a time.
/// </summary>
public sealed partial class MatchMomentPitchView : Control
{
    private MatchMomentStoryboard _storyboard = MatchMomentStoryboard.Empty;
    private Label? _headlineLabel;
    private Label? _detailLabel;
    private int _frameIndex = -1;
    private double _elapsed;
    private float _secondsPerMoment = 1.65f;
    private bool _playing;
    private bool _reducedMotion;
    private Color _homeTeamColor = new(0.24f, 0.78f, 0.48f);
    private Color _awayTeamColor = new(0.32f, 0.68f, 0.94f);

    public MatchMomentPitchView()
    {
        MouseFilter = MouseFilterEnum.Ignore;
        ClipContents = true;
        CustomMinimumSize = new Vector2(300, 180);
    }

    public float SecondsPerMoment
    {
        get => _secondsPerMoment;
        set => _secondsPerMoment = Math.Clamp(value, 0.25f, 10f);
    }

    /// <summary>
    /// Keeps sequence timing and readable state changes, but removes ball travel,
    /// pulsing and burst animation.
    /// </summary>
    public bool ReducedMotion
    {
        get => _reducedMotion;
        set
        {
            if (_reducedMotion == value)
            {
                return;
            }

            _reducedMotion = value;
            QueueRedraw();
        }
    }

    public Color HomeTeamColor
    {
        get => _homeTeamColor;
        set
        {
            _homeTeamColor = value;
            QueueRedraw();
        }
    }

    public Color AwayTeamColor
    {
        get => _awayTeamColor;
        set
        {
            _awayTeamColor = value;
            QueueRedraw();
        }
    }

    public MatchMomentStoryboard Storyboard => _storyboard;

    public int CurrentFrameIndex => _frameIndex;

    public MatchMomentStoryboardFrame? CurrentFrame =>
        _frameIndex >= 0 && _frameIndex < _storyboard.Frames.Count
            ? _storyboard.Frames[_frameIndex]
            : null;

    public bool IsPlaying => _playing;

    public float FrameProgress =>
        ReducedMotion
            ? 1f
            : Math.Clamp((float)(_elapsed / SecondsPerMoment), 0f, 1f);

    public event Action<int, MatchMomentStoryboardFrame>? MomentChanged;

    public event Action? SequenceFinished;

    public override void _Ready()
    {
        BuildTextOverlay();
        Resized += OnResized;
        SetProcess(_playing);
        RefreshTextOverlay();
        QueueRedraw();
    }

    public override void _ExitTree()
    {
        Resized -= OnResized;
    }

    public override void _Process(double delta)
    {
        if (!_playing || _storyboard.Frames.Count == 0)
        {
            return;
        }

        _elapsed += Math.Max(0, delta);
        while (_elapsed >= SecondsPerMoment && _playing)
        {
            _elapsed -= SecondsPerMoment;
            if (_frameIndex + 1 < _storyboard.Frames.Count)
            {
                PresentFrame(_frameIndex + 1);
                continue;
            }

            _elapsed = SecondsPerMoment;
            _playing = false;
            SetProcess(false);
            SequenceFinished?.Invoke();
        }

        QueueRedraw();
    }

    public override void _Draw()
    {
        if (Size.X < 24 || Size.Y < 24)
        {
            return;
        }

        var field = new Rect2(
            new Vector2(4, 4),
            new Vector2(Mathf.Max(1, Size.X - 8), Mathf.Max(1, Size.Y - 8)));
        DrawPitch(field);
        DrawTeams(field);

        if (CurrentFrame is { } frame)
        {
            DrawMoment(field, frame, FrameProgress);
        }
    }

    public void SetMoments(
        IReadOnlyList<MatchKeyMomentReadModel>? moments,
        int sequenceSeed = 0,
        bool autoplay = true) =>
        SetStoryboard(MatchMomentStoryboard.Build(moments, sequenceSeed), autoplay);

    public void SetStoryboard(MatchMomentStoryboard storyboard, bool autoplay = true)
    {
        ArgumentNullException.ThrowIfNull(storyboard);
        _storyboard = storyboard;
        _elapsed = 0;

        if (_storyboard.Frames.Count == 0)
        {
            _frameIndex = -1;
            Pause();
            RefreshTextOverlay();
            QueueRedraw();
            return;
        }

        PresentFrame(0);
        if (autoplay)
        {
            Play();
        }
        else
        {
            Pause();
        }
    }

    public bool Play()
    {
        if (_storyboard.Frames.Count == 0)
        {
            return false;
        }

        if (_frameIndex < 0)
        {
            PresentFrame(0);
        }

        _playing = true;
        if (IsInsideTree())
        {
            SetProcess(true);
        }

        return true;
    }

    public void Pause()
    {
        _playing = false;
        if (IsInsideTree())
        {
            SetProcess(false);
        }
    }

    public bool Restart(bool autoplay = true)
    {
        if (_storyboard.Frames.Count == 0)
        {
            return false;
        }

        _elapsed = 0;
        PresentFrame(0);
        if (autoplay)
        {
            Play();
        }
        else
        {
            Pause();
        }

        return true;
    }

    public bool ShowFrame(int frameIndex)
    {
        if (frameIndex < 0 || frameIndex >= _storyboard.Frames.Count)
        {
            return false;
        }

        _elapsed = 0;
        PresentFrame(frameIndex);
        return true;
    }

    public bool Advance()
    {
        if (_frameIndex + 1 >= _storyboard.Frames.Count)
        {
            return false;
        }

        _elapsed = 0;
        PresentFrame(_frameIndex + 1);
        return true;
    }

    private void PresentFrame(int frameIndex)
    {
        _frameIndex = frameIndex;
        RefreshTextOverlay();
        QueueRedraw();
        MomentChanged?.Invoke(frameIndex, _storyboard.Frames[frameIndex]);
    }

    private void BuildTextOverlay()
    {
        var overlay = new VBoxContainer
        {
            Name = "MomentText",
            MouseFilter = MouseFilterEnum.Ignore,
        };
        overlay.SetAnchor(Side.Left, 0f);
        overlay.SetAnchor(Side.Top, 0f);
        overlay.SetAnchor(Side.Right, 1f);
        overlay.SetAnchor(Side.Bottom, 0f);
        overlay.OffsetLeft = 12;
        overlay.OffsetTop = 10;
        overlay.OffsetRight = -12;
        overlay.OffsetBottom = 60;
        overlay.AddThemeConstantOverride("separation", 1);
        AddChild(overlay);

        _headlineLabel = new Label
        {
            HorizontalAlignment = HorizontalAlignment.Center,
            MouseFilter = MouseFilterEnum.Ignore,
        };
        CareerUiTheme.StyleHeadline(_headlineLabel);
        _headlineLabel.AddThemeFontSizeOverride("font_size", CareerUiTheme.FontSize(18));
        _headlineLabel.AddThemeColorOverride("font_outline_color", new Color(0f, 0f, 0f, 0.92f));
        _headlineLabel.AddThemeConstantOverride("outline_size", 6);
        overlay.AddChild(_headlineLabel);

        _detailLabel = new Label
        {
            HorizontalAlignment = HorizontalAlignment.Center,
            AutowrapMode = TextServer.AutowrapMode.WordSmart,
            MouseFilter = MouseFilterEnum.Ignore,
        };
        CareerUiTheme.StyleEyebrow(_detailLabel, CareerUiTheme.Ink);
        _detailLabel.AddThemeColorOverride("font_outline_color", new Color(0f, 0f, 0f, 0.90f));
        _detailLabel.AddThemeConstantOverride("outline_size", 5);
        overlay.AddChild(_detailLabel);
    }

    private void RefreshTextOverlay()
    {
        if (_headlineLabel is null || _detailLabel is null)
        {
            return;
        }

        if (CurrentFrame is not { } frame)
        {
            _headlineLabel.Text = "KRİTİK AN";
            _detailLabel.Text = "Maç olayı bekleniyor";
            return;
        }

        _headlineLabel.Text = $"{frame.Minute}'  ·  {MomentTitle(frame.Kind)}";
        _detailLabel.Text = frame.AssistPlayerName is { Length: > 0 } assist
            ? $"{frame.PrimaryPlayerName}  ·  Asist: {assist}"
            : frame.PrimaryPlayerName;
    }

    private void DrawPitch(Rect2 field)
    {
        DrawRect(field, new Color(0.035f, 0.26f, 0.13f));
        var stripeWidth = field.Size.X / 8f;
        for (var stripe = 0; stripe < 8; stripe += 2)
        {
            DrawRect(
                new Rect2(
                    field.Position.X + (stripe * stripeWidth),
                    field.Position.Y,
                    stripeWidth,
                    field.Size.Y),
                new Color(0.05f, 0.34f, 0.17f));
        }

        var line = new Color(0.91f, 0.97f, 0.92f, 0.78f);
        var inset = Mathf.Max(8f, Mathf.Min(field.Size.X, field.Size.Y) * 0.035f);
        var playable = new Rect2(
            field.Position + new Vector2(inset, inset),
            field.Size - new Vector2(inset * 2, inset * 2));
        DrawRect(playable, line, filled: false, width: 2);

        var center = playable.GetCenter();
        DrawLine(
            new Vector2(center.X, playable.Position.Y),
            new Vector2(center.X, playable.End.Y),
            line,
            2);
        DrawArc(
            center,
            Mathf.Min(playable.Size.X, playable.Size.Y) * 0.14f,
            0,
            Mathf.Tau,
            36,
            line,
            2);
        DrawCircle(center, 2.5f, line);

        var boxWidth = playable.Size.X * 0.16f;
        var boxHeight = playable.Size.Y * 0.48f;
        DrawRect(
            new Rect2(
                playable.Position.X,
                center.Y - (boxHeight * 0.5f),
                boxWidth,
                boxHeight),
            line,
            filled: false,
            width: 2);
        DrawRect(
            new Rect2(
                playable.End.X - boxWidth,
                center.Y - (boxHeight * 0.5f),
                boxWidth,
                boxHeight),
            line,
            filled: false,
            width: 2);
    }

    private void DrawTeams(Rect2 field)
    {
        var radius = Mathf.Clamp(Mathf.Min(field.Size.X, field.Size.Y) * 0.019f, 3.2f, 7f);
        for (var slot = 0; slot < 11; slot++)
        {
            DrawPlayer(
                ToPixel(MatchMomentStoryboard.ResolvePlayerPosition(slot, isHomeSide: true), field),
                HomeTeamColor,
                radius);
            DrawPlayer(
                ToPixel(MatchMomentStoryboard.ResolvePlayerPosition(slot, isHomeSide: false), field),
                AwayTeamColor,
                radius);
        }
    }

    private void DrawMoment(
        Rect2 field,
        MatchMomentStoryboardFrame frame,
        float progress)
    {
        var eased = ReducedMotion ? 1f : EaseOutCubic(progress);
        var actor = ToPixel(frame.ActorPosition, field);
        var support = frame.SupportPosition is { } supportPoint
            ? ToPixel(supportPoint, field)
            : (Vector2?)null;
        var start = ToPixel(frame.BallStart, field);
        var end = ToPixel(frame.BallEnd, field);
        var ball = start.Lerp(end, eased);

        if (support is { } supportPixel)
        {
            DrawArc(
                supportPixel,
                9,
                0,
                Mathf.Tau,
                24,
                new Color(1f, 1f, 1f, 0.54f),
                1.5f);
        }

        var pulse = ReducedMotion ? 1f : 0.84f + (0.16f * Mathf.Sin(progress * Mathf.Tau));
        var signalColor = MomentColor(frame.Kind);
        DrawCircle(actor, 13f * pulse, new Color(signalColor.R, signalColor.G, signalColor.B, 0.16f));
        DrawArc(actor, 12f * pulse, 0, Mathf.Tau, 28, signalColor, 2.5f);

        if (IsKind(frame.Kind, MatchKeyMomentKind.Goal))
        {
            DrawLine(start, ball, new Color(1f, 1f, 1f, 0.38f), 2);
            DrawCircle(ball, 4.2f, Colors.White);
            DrawCircle(ball, 1.5f, new Color(0.08f, 0.10f, 0.09f));
            DrawGoalBurst(end, ReducedMotion ? 1f : progress, signalColor);
            return;
        }

        if (IsKind(frame.Kind, MatchKeyMomentKind.YellowCard)
            || IsKind(frame.Kind, MatchKeyMomentKind.RedCard))
        {
            var cardSize = new Vector2(9, 14);
            var cardPosition = actor + new Vector2(-cardSize.X * 0.5f, -25);
            DrawRect(new Rect2(cardPosition, cardSize), signalColor);
            DrawRect(
                new Rect2(cardPosition, cardSize),
                new Color(0f, 0f, 0f, 0.74f),
                filled: false,
                width: 1.5f);
            return;
        }

        if (IsKind(frame.Kind, MatchKeyMomentKind.Injury))
        {
            const float arm = 6f;
            var cross = actor + new Vector2(0, -20);
            DrawLine(cross - new Vector2(arm, 0), cross + new Vector2(arm, 0), signalColor, 4);
            DrawLine(cross - new Vector2(0, arm), cross + new Vector2(0, arm), signalColor, 4);
            return;
        }

        DrawCircle(ball, 4f, signalColor);
    }

    private void DrawGoalBurst(Vector2 center, float progress, Color color)
    {
        var reveal = Math.Clamp((progress - 0.58f) / 0.42f, 0f, 1f);
        if (reveal <= 0)
        {
            return;
        }

        var radius = 8f + (16f * reveal);
        for (var ray = 0; ray < 8; ray++)
        {
            var direction = Vector2.FromAngle((Mathf.Tau / 8f) * ray);
            DrawLine(
                center + (direction * (radius * 0.45f)),
                center + (direction * radius),
                new Color(color.R, color.G, color.B, 0.72f),
                2);
        }
    }

    private void DrawPlayer(Vector2 position, Color shirt, float radius)
    {
        DrawCircle(position + new Vector2(0, 1.5f), radius + 2f, new Color(0f, 0f, 0f, 0.45f));
        DrawCircle(position, radius + 1f, new Color(0.91f, 0.97f, 0.93f, 0.88f));
        DrawCircle(position, radius, shirt);
    }

    private static Vector2 ToPixel(MatchMomentPitchPoint point, Rect2 field) =>
        field.Position + new Vector2(point.X * field.Size.X, point.Y * field.Size.Y);

    private static bool IsKind(string kind, MatchKeyMomentKind expected) =>
        string.Equals(kind, expected.ToString(), StringComparison.OrdinalIgnoreCase);

    private static Color MomentColor(string kind)
    {
        if (IsKind(kind, MatchKeyMomentKind.Goal))
        {
            return new Color(0.38f, 0.95f, 0.58f);
        }

        if (IsKind(kind, MatchKeyMomentKind.YellowCard))
        {
            return new Color(1f, 0.82f, 0.20f);
        }

        if (IsKind(kind, MatchKeyMomentKind.RedCard))
        {
            return new Color(1f, 0.26f, 0.24f);
        }

        if (IsKind(kind, MatchKeyMomentKind.Injury))
        {
            return new Color(1f, 0.46f, 0.42f);
        }

        return new Color(0.38f, 0.78f, 0.96f);
    }

    private static string MomentTitle(string kind)
    {
        if (IsKind(kind, MatchKeyMomentKind.Goal))
        {
            return "GOL";
        }

        if (IsKind(kind, MatchKeyMomentKind.YellowCard))
        {
            return "SARI KART";
        }

        if (IsKind(kind, MatchKeyMomentKind.RedCard))
        {
            return "KIRMIZI KART";
        }

        if (IsKind(kind, MatchKeyMomentKind.Injury))
        {
            return "SAKATLIK";
        }

        return "KRİTİK AN";
    }

    private static float EaseOutCubic(float value)
    {
        var inverse = 1f - Math.Clamp(value, 0f, 1f);
        return 1f - (inverse * inverse * inverse);
    }

    private void OnResized() => QueueRedraw();
}
