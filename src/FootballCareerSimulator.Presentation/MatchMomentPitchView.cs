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
    private string? _activeShoutText;
    private Color _activeShoutColor = new(1f, 0.78f, 0.18f);
    private float _shoutFlashProgress;
    private bool _keyDecisionFreezeActive;
    private float _decisionIntroProgress = 1f;
    private Action<string>? _onPitchDecisionSelected;
    private readonly List<OnPitchTargetDecisionButton> _decisionTargetButtons = [];

    public MatchMomentPitchView()
    {
        MouseFilter = MouseFilterEnum.Pass;
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

    /// <summary>
    /// Optional checkpoint predicate (e.g. for half-time intermission). If it returns true,
    /// playback pauses before the specified frame is presented.
    /// </summary>
    public Func<int, MatchMomentStoryboardFrame, bool>? ShouldPauseBeforeMoment { get; set; }

    public override void _Ready()
    {
        BuildTextOverlay();
        BuildOnPitchDecisionTargets();
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
                var nextFrame = _storyboard.Frames[_frameIndex + 1];
                if (ShouldPauseBeforeMoment?.Invoke(_frameIndex + 1, nextFrame) == true)
                {
                    Pause();
                    break;
                }

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
        DrawPhaseChannels(field, CurrentFrame, FrameProgress);
        DrawTeams(field, CurrentFrame, FrameProgress);

        if (CurrentFrame is { } frame)
        {
            DrawMoment(field, frame, FrameProgress);
        }

        if (_shoutFlashProgress > 0.01f)
        {
            var center = field.GetCenter();
            var maxR = Mathf.Min(field.Size.X, field.Size.Y) * 0.45f;
            var curR = Mathf.Lerp(16f, maxR, 1f - _shoutFlashProgress);
            var ringColor = new Color(_activeShoutColor.R, _activeShoutColor.G, _activeShoutColor.B, _shoutFlashProgress * 0.78f);
            DrawArc(center, curR, 0f, Mathf.Tau, 48, ringColor, 3.5f);
            DrawRect(field, new Color(_activeShoutColor.R, _activeShoutColor.G, _activeShoutColor.B, _shoutFlashProgress * 0.14f), filled: true);
        }

        if (_keyDecisionFreezeActive && CurrentFrame is { } freezeFrame)
        {
            var intro = Mathf.Clamp(_decisionIntroProgress, 0.05f, 1f);

            // %60 karartılmış vignette overlay (Broadcast Stadium Noir)
            DrawRect(new Rect2(Vector2.Zero, Size), new Color(0.02f, 0.04f, 0.08f, 0.60f * intro), filled: true);
            DrawRect(new Rect2(Vector2.Zero, Size), new Color(0.01f, 0.02f, 0.05f, 0.78f * intro), filled: false, width: 16f);

            var (carrier, topCorner, backPost, dribbleSpot) = ResolveDecisionTargetPoints(freezeFrame, field);

            // Topa sahip oyuncunun etrafında parlayan yayın halkası
            DrawCircle(carrier, 24f * intro, new Color(1f, 0.72f, 0f, 0.24f * intro));
            DrawArc(carrier, 20f * intro, 0f, Mathf.Tau, 36, CareerUiTheme.Accent, 2.8f);
            DrawCircle(carrier, 9f, CareerUiTheme.ActionBright);

            // Topa sahip oyuncudan çıkan 3 adet parlayan kavisli ok (Bezier curve)
            var controlTop = ((carrier + topCorner) * 0.5f) + new Vector2(0f, -field.Size.Y * 0.16f);
            var controlBack = ((carrier + backPost) * 0.5f) + new Vector2(0f, field.Size.Y * 0.18f);
            var controlDribble = ((carrier + dribbleSpot) * 0.5f) + new Vector2(0f, -field.Size.Y * 0.08f);

            DrawBezierCurveArrow(carrier, controlTop, topCorner, CareerUiTheme.Accent, intro);
            DrawBezierCurveArrow(carrier, controlBack, backPost, CareerUiTheme.ActionBright, intro);
            DrawBezierCurveArrow(carrier, controlDribble, dribbleSpot, CareerUiTheme.Data, intro);

            LayoutOnPitchDecisionTargets(topCorner, backPost, dribbleSpot);
        }
    }

    private static (Vector2 Carrier, Vector2 TopCorner, Vector2 BackPost, Vector2 DribbleSpot) ResolveDecisionTargetPoints(
        MatchMomentStoryboardFrame freezeFrame,
        Rect2 field)
    {
        var carrier = ToPixel(freezeFrame.ActorPosition, field);
        var goalNormX = freezeFrame.IsHomeSide ? 0.94f : 0.06f;
        var boxNormX = freezeFrame.IsHomeSide ? 0.84f : 0.16f;

        var topCorner = ProjectFieldPoint(new Vector2(goalNormX, 0.28f), field);
        var backPost = ProjectFieldPoint(new Vector2(boxNormX, 0.72f), field);
        var dribbleNorm = new Vector2(
            Mathf.Lerp(freezeFrame.ActorPosition.X, boxNormX, 0.62f),
            Mathf.Clamp(freezeFrame.ActorPosition.Y, 0.36f, 0.64f));
        var dribbleSpot = ProjectFieldPoint(dribbleNorm, field);

        return (carrier, topCorner, backPost, dribbleSpot);
    }

    private void DrawBezierCurveArrow(Vector2 p0, Vector2 p1, Vector2 p2, Color color, float progress = 1f)
    {
        const int segments = 24;
        var activeSegments = Math.Max(2, Mathf.RoundToInt(segments * Mathf.Clamp(progress, 0.08f, 1f)));
        var prev = p0;
        var glowColor = new Color(color.R, color.G, color.B, 0.28f * progress);
        var coreColor = new Color(color.R, color.G, color.B, 0.95f * progress);

        Vector2 tip = p0;
        Vector2 prevBeforeTip = p0;
        for (var i = 1; i <= activeSegments; i++)
        {
            var t = i / (float)segments;
            var u = 1f - t;
            var pt = (u * u * p0) + (2f * u * t * p1) + (t * t * p2);
            DrawLine(prev, pt, glowColor, 7.0f);
            DrawLine(prev, pt, coreColor, 2.6f);
            prevBeforeTip = prev;
            prev = pt;
            tip = pt;
        }

        var tangent = (tip - prevBeforeTip).Normalized();
        if (tangent.LengthSquared() < 0.01f)
        {
            tangent = (p2 - p0).Normalized();
        }
        var perp = new Vector2(-tangent.Y, tangent.X);
        var headBase = tip - (tangent * 12f);
        DrawColoredPolygon(
            [tip, headBase + (perp * 6f), headBase - (perp * 6f)],
            coreColor);
    }

    public void ActivateOnPitchKeyDecision(
        MatchMomentStoryboardFrame frame,
        Action<string> onDecisionSelected,
        string? promptText = null)
    {
        _onPitchDecisionSelected = onDecisionSelected;
        _decisionIntroProgress = ReducedMotion ? 1f : 0f;
        SetKeyDecisionHighlight(
            true,
            promptText ?? $"{frame.Minute}' KRİTİK POZİSYON — SAHA ÜSTÜNDE HEDEF SEÇ");
        UpdateOnPitchDecisionTargetsVisibility(true);

        if (!ReducedMotion && IsInsideTree())
        {
            var tween = CreateTween();
            tween.SetTrans(Tween.TransitionType.Cubic).SetEase(Tween.EaseType.Out);
            tween.TweenMethod(Callable.From<float>(v =>
            {
                _decisionIntroProgress = v;
                QueueRedraw();
            }), 0f, 1f, 0.32d);

            for (var i = 0; i < _decisionTargetButtons.Count; i++)
            {
                var btn = _decisionTargetButtons[i];
                btn.PivotOffset = btn.CustomMinimumSize * 0.5f;
                btn.Scale = new Vector2(0.75f, 0.75f);
                var btnTween = btn.CreateTween();
                btnTween.SetTrans(Tween.TransitionType.Back).SetEase(Tween.EaseType.Out);
                btnTween.TweenInterval(0.06d * i);
                btnTween.TweenProperty(btn, "scale", Vector2.One, 0.24d);
            }
        }
        else
        {
            QueueRedraw();
        }
    }

    public void TriggerTouchlineShoutEffect(string shoutTitle, Color auraColor)
    {
        _activeShoutText = shoutTitle;
        _activeShoutColor = auraColor;
        _shoutFlashProgress = 1f;
        if (_headlineLabel is not null)
        {
            _headlineLabel.Text = $"KENAR TALİMATI: {shoutTitle.ToUpperInvariant()}";
        }
        var tween = CreateTween();
        tween.TweenMethod(Callable.From<float>(v =>
        {
            _shoutFlashProgress = v;
            QueueRedraw();
        }), 1f, 0f, 0.75d);
    }

    public void SetKeyDecisionHighlight(bool active, string? promptText = null)
    {
        _keyDecisionFreezeActive = active;
        UpdateOnPitchDecisionTargetsVisibility(active);
        if (active && _headlineLabel is not null && !string.IsNullOrWhiteSpace(promptText))
        {
            _headlineLabel.Text = promptText;
        }
        else if (!active)
        {
            RefreshTextOverlay();
        }
        QueueRedraw();
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

    public bool Retreat()
    {
        if (_frameIndex <= 0)
        {
            return false;
        }

        _elapsed = 0;
        PresentFrame(_frameIndex - 1);
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

    private void BuildOnPitchDecisionTargets()
    {
        _decisionTargetButtons.Clear();
        var configs = new (string Label, string SubLabel, Color Color, string Outcome)[]
        {
            ("KÖŞEYE PLASE", "KALE KÖŞESİ", CareerUiTheme.Accent, "Uzak köşeye sert plase şut denemesi! Tribünler ayakta!"),
            ("KESME PAS", "ARKA DİREK", CareerUiTheme.ActionBright, "Savunma arkasına jilet gibi kesme pas! Tehlike büyüdü!"),
            ("ÇALIMLA DAL", "CEZA SAHASI İÇİ", CareerUiTheme.Data, "Bire birde şık çalımla ceza sahasına dalış! Savunma dengesini kaybetti!"),
        };

        foreach (var cfg in configs)
        {
            var btn = new OnPitchTargetDecisionButton(cfg.Label, cfg.SubLabel, cfg.Color)
            {
                Visible = false,
            };
            var outcome = cfg.Outcome;
            btn.Pressed += () =>
            {
                var cb = _onPitchDecisionSelected;
                SetKeyDecisionHighlight(false);
                cb?.Invoke(outcome);
            };
            AddChild(btn);
            _decisionTargetButtons.Add(btn);
        }
    }

    private void UpdateOnPitchDecisionTargetsVisibility(bool visible)
    {
        foreach (var btn in _decisionTargetButtons)
        {
            btn.Visible = visible;
        }
    }

    private void LayoutOnPitchDecisionTargets(Vector2 topCorner, Vector2 backPost, Vector2 dribbleSpot)
    {
        if (_decisionTargetButtons.Count < 3)
        {
            return;
        }

        Vector2[] targets = [topCorner, backPost, dribbleSpot];
        var btnSize = new Vector2(128, 44);
        for (var i = 0; i < 3; i++)
        {
            var btn = _decisionTargetButtons[i];
            btn.Size = btnSize;
            var pos = targets[i] - (btnSize * 0.5f);
            pos.X = Mathf.Clamp(pos.X, 6f, Mathf.Max(6f, Size.X - btnSize.X - 6f));
            pos.Y = Mathf.Clamp(pos.Y, 6f, Mathf.Max(6f, Size.Y - btnSize.Y - 6f));
            btn.Position = pos;
        }
    }

    private static Vector2 ProjectFieldPoint(Vector2 norm, Rect2 field)
    {
        var ny = Mathf.Clamp(norm.Y, 0f, 1f);
        var nx = Mathf.Clamp(norm.X, 0f, 1f);
        var insetRatio = Mathf.Lerp(0.085f, 0.015f, ny);
        var rowLeft = field.Position.X + (field.Size.X * insetRatio);
        var rowWidth = field.Size.X * (1f - (insetRatio * 2f));
        var py = field.Position.Y + (Mathf.Pow(ny, 0.94f) * field.Size.Y);
        var px = rowLeft + (nx * rowWidth);
        return new Vector2(px, py);
    }

    private void DrawPitch(Rect2 field)
    {
        // Stadyum gece ambiyansı tabanı
        DrawRect(new Rect2(Vector2.Zero, Size), new Color(0.027f, 0.043f, 0.071f, 1f));

        // Koyu zümrüt yeşili (#0A2E23 / #0D382B) perspektif çim şeritleri (Trapezoid)
        var emeraldDark = new Color(0.039f, 0.180f, 0.137f, 1f);   // #0A2E23
        var emeraldStripe = new Color(0.051f, 0.220f, 0.169f, 1f); // #0D382B
        const int stripeCount = 10;
        for (var i = 0; i < stripeCount; i++)
        {
            var x0 = i / (float)stripeCount;
            var x1 = (i + 1) / (float)stripeCount;
            Vector2[] poly =
            [
                ProjectFieldPoint(new Vector2(x0, 0f), field),
                ProjectFieldPoint(new Vector2(x1, 0f), field),
                ProjectFieldPoint(new Vector2(x1, 1f), field),
                ProjectFieldPoint(new Vector2(x0, 1f), field),
            ];
            DrawColoredPolygon(poly, i % 2 == 0 ? emeraldDark : emeraldStripe);
        }

        var line = new Color(0.86f, 0.96f, 0.91f, 0.56f);
        var glowLine = new Color(0f, 0.96f, 0.61f, 0.16f);

        // Dış saha sınırları (Trapezoid)
        var tl = ProjectFieldPoint(new Vector2(0f, 0f), field);
        var tr = ProjectFieldPoint(new Vector2(1f, 0f), field);
        var br = ProjectFieldPoint(new Vector2(1f, 1f), field);
        var bl = ProjectFieldPoint(new Vector2(0f, 1f), field);
        Vector2[] border = [tl, tr, br, bl, tl];
        DrawPolyline(border, glowLine, 5f);
        DrawPolyline(border, line, 2f);

        // Orta çizgi & perspektif elips orta yuvarlak
        var midTop = ProjectFieldPoint(new Vector2(0.5f, 0f), field);
        var midBot = ProjectFieldPoint(new Vector2(0.5f, 1f), field);
        DrawLine(midTop, midBot, line, 1.8f);

        var center = ProjectFieldPoint(new Vector2(0.5f, 0.5f), field);
        var centerPts = new Vector2[33];
        for (var i = 0; i <= 32; i++)
        {
            var a = (i / 32f) * Mathf.Tau;
            var pt = new Vector2(0.5f + (Mathf.Cos(a) * 0.085f), 0.5f + (Mathf.Sin(a) * 0.145f));
            centerPts[i] = ProjectFieldPoint(pt, field);
        }
        DrawPolyline(centerPts, line, 1.8f);
        DrawCircle(center, 3f, line);

        // Sol ve Sağ Ceza Sahaları (Perspektif Trapezoid)
        Vector2[] leftBox =
        [
            ProjectFieldPoint(new Vector2(0f, 0.24f), field),
            ProjectFieldPoint(new Vector2(0.16f, 0.24f), field),
            ProjectFieldPoint(new Vector2(0.16f, 0.76f), field),
            ProjectFieldPoint(new Vector2(0f, 0.76f), field),
        ];
        DrawPolyline(leftBox, line, 1.8f);

        Vector2[] rightBox =
        [
            ProjectFieldPoint(new Vector2(1f, 0.24f), field),
            ProjectFieldPoint(new Vector2(0.84f, 0.24f), field),
            ProjectFieldPoint(new Vector2(0.84f, 0.76f), field),
            ProjectFieldPoint(new Vector2(1f, 0.76f), field),
        ];
        DrawPolyline(rightBox, line, 1.8f);
    }

    private void DrawPhaseChannels(
        Rect2 field,
        MatchMomentStoryboardFrame? frame,
        float progress)
    {
        if (frame is null || ReducedMotion)
        {
            return;
        }

        var direction = frame.IsHomeSide ? 1f : -1f;
        var eased = EaseOutCubic(progress);
        var alpha = 0.05f + (0.05f * Mathf.Sin(progress * Mathf.Pi));
        var channelWidth = field.Size.X * 0.22f;
        var channelX = direction > 0
            ? Mathf.Lerp(field.Position.X + (field.Size.X * 0.42f), field.End.X - channelWidth, eased)
            : Mathf.Lerp(field.End.X - (field.Size.X * 0.42f) - channelWidth, field.Position.X, eased);
        DrawRect(
            new Rect2(channelX, field.Position.Y, channelWidth, field.Size.Y),
            new Color(0.34f, 1f, 0.60f, alpha));

        var pressLineX = direction > 0
            ? field.Position.X + (field.Size.X * (0.53f + (0.16f * eased)))
            : field.Position.X + (field.Size.X * (0.47f - (0.16f * eased)));
        DrawLine(
            new Vector2(pressLineX, field.Position.Y + 8),
            new Vector2(pressLineX, field.End.Y - 8),
            new Color(0.66f, 1f, 0.78f, 0.22f),
            1.5f);
    }

    private void DrawTeams(
        Rect2 field,
        MatchMomentStoryboardFrame? frame,
        float progress)
    {
        var radius = Mathf.Clamp(Mathf.Min(field.Size.X, field.Size.Y) * 0.025f, 5.5f, 9.5f);
        var nearestDefenderSlot = frame is null
            ? -1
            : FindNearestDefenderSlot(frame.ActorPosition, defendingHomeSide: !frame.IsHomeSide);
        for (var slot = 0; slot < 11; slot++)
        {
            var homePosition = ResolveAnimatedPlayerPosition(
                slot,
                isHomeSide: true,
                frame,
                progress,
                nearestDefenderSlot);
            var awayPosition = ResolveAnimatedPlayerPosition(
                slot,
                isHomeSide: false,
                frame,
                progress,
                nearestDefenderSlot);

            DrawPlayer(
                ToPixel(homePosition, field),
                HomeTeamColor,
                radius,
                slot + 1,
                frame is { IsHomeSide: true } && frame.PrimarySlotIndex == slot);
            DrawPlayer(
                ToPixel(awayPosition, field),
                AwayTeamColor,
                radius,
                slot + 1,
                frame is { IsHomeSide: false } && frame.PrimarySlotIndex == slot);
        }
    }

    private MatchMomentPitchPoint ResolveAnimatedPlayerPosition(
        int slot,
        bool isHomeSide,
        MatchMomentStoryboardFrame? frame,
        float progress,
        int nearestDefenderSlot)
    {
        var origin = MatchMomentStoryboard.ResolvePlayerPosition(slot, isHomeSide);
        if (frame is null || ReducedMotion)
        {
            return origin;
        }

        var eased = EaseOutCubic(progress);
        var attacking = isHomeSide == frame.IsHomeSide;
        var direction = frame.IsHomeSide ? 1f : -1f;
        var phaseShift = attacking ? 0.075f : 0.045f;
        var lateralBreath = Mathf.Sin((slot * 0.91f) + (progress * Mathf.Pi)) * 0.012f;
        var target = new MatchMomentPitchPoint(
            origin.X + (direction * phaseShift * eased),
            origin.Y + lateralBreath);

        if (attacking && slot == frame.PrimarySlotIndex)
        {
            target = LerpPoint(target, frame.BallEnd, 0.22f + (0.32f * eased));
        }
        else if (attacking && frame.AssistSlotIndex == slot)
        {
            target = LerpPoint(target, frame.ActorPosition, 0.16f + (0.18f * eased));
        }
        else if (!attacking && slot == nearestDefenderSlot)
        {
            target = LerpPoint(target, frame.ActorPosition, 0.18f + (0.20f * eased));
        }

        return target.Clamped();
    }

    private static int FindNearestDefenderSlot(
        MatchMomentPitchPoint actor,
        bool defendingHomeSide)
    {
        var nearestSlot = 0;
        var nearestDistance = float.MaxValue;
        for (var slot = 0; slot < 11; slot++)
        {
            var candidate = MatchMomentStoryboard.ResolvePlayerPosition(slot, defendingHomeSide);
            var dx = candidate.X - actor.X;
            var dy = candidate.Y - actor.Y;
            var distance = (dx * dx) + (dy * dy);
            if (distance < nearestDistance)
            {
                nearestDistance = distance;
                nearestSlot = slot;
            }
        }

        return nearestSlot;
    }

    private static MatchMomentPitchPoint LerpPoint(
        MatchMomentPitchPoint from,
        MatchMomentPitchPoint to,
        float weight) =>
        new(
            Mathf.Lerp(from.X, to.X, weight),
            Mathf.Lerp(from.Y, to.Y, weight));

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

    private void DrawPlayer(
        Vector2 position,
        Color shirt,
        float radius,
        int squadNumber,
        bool active)
    {
        if (active)
        {
            DrawCircle(position, radius + 9f, new Color(0f, 0.96f, 0.61f, 0.20f));
            DrawArc(position, radius + 6.5f, 0, Mathf.Tau, 28, CareerUiTheme.ActionBright, 2.2f);
        }

        // 1. Alt zemin gölgesi (Drop Shadow)
        DrawCircle(position + new Vector2(0f, 3f), radius + 2.5f, new Color(0f, 0f, 0f, 0.56f));

        // 2. Dairesel kondisyon halkası (Circular Condition Arc)
        var ringRadius = radius + 2.6f;
        DrawArc(position, ringRadius, 0f, Mathf.Tau, 24, new Color(0.05f, 0.08f, 0.14f, 0.85f), 2.2f);
        var condRatio = Mathf.Clamp(0.92f - ((squadNumber % 5) * 0.04f), 0.72f, 0.98f);
        var condEndAngle = -Mathf.Pi * 0.5f + (Mathf.Tau * condRatio);
        DrawArc(
            position,
            ringRadius,
            -Mathf.Pi * 0.5f,
            condEndAngle,
            20,
            active ? CareerUiTheme.Accent : CareerUiTheme.ActionBright,
            2.2f);

        // 3. Katmanlı forma jetonu gövdesi + yaka detayı
        DrawCircle(position, radius + 0.8f, new Color(0.94f, 0.98f, 1f, 0.90f));
        DrawCircle(position, radius, shirt);
        DrawArc(
            position + new Vector2(0f, -radius * 0.5f),
            radius * 0.42f,
            0.2f,
            Mathf.Pi - 0.2f,
            10,
            new Color(1f, 1f, 1f, 0.62f),
            1.4f);

        // 4. Forma numarası (Squad Number)
        var font = ThemeDB.FallbackFont;
        var fontSize = Math.Max(8, Mathf.RoundToInt(radius * 1.25f));
        var text = squadNumber.ToString();
        var size = font.GetStringSize(text, HorizontalAlignment.Center, -1, fontSize);
        DrawString(
            font,
            position + new Vector2(-size.X * 0.5f, size.Y * 0.34f),
            text,
            HorizontalAlignment.Center,
            -1,
            fontSize,
            Colors.White);
    }

    private static Vector2 ToPixel(MatchMomentPitchPoint point, Rect2 field) =>
        ProjectFieldPoint(new Vector2(point.X, point.Y), field);

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

    /// <summary>
    /// Saha üzerindeki hedef koordinatlara (Kale köşesi, Arka direk, Ceza sahası içi)
    /// doğrudan yerleşen parlayan interaktif hedef ikonu / düğmesi (Rule 5).
    /// </summary>
    private sealed partial class OnPitchTargetDecisionButton : Button
    {
        private readonly string _title;
        private readonly string _subtitle;
        private readonly Color _accent;

        public OnPitchTargetDecisionButton(string title, string subtitle, Color accent)
        {
            _title = title;
            _subtitle = subtitle;
            _accent = accent;
            Flat = true;
            CustomMinimumSize = new Vector2(128, 44);
            MouseDefaultCursorShape = CursorShape.PointingHand;

            var empty = new StyleBoxEmpty();
            AddThemeStyleboxOverride("normal", empty);
            AddThemeStyleboxOverride("hover", empty);
            AddThemeStyleboxOverride("pressed", empty);
            AddThemeStyleboxOverride("focus", empty);

            MouseEntered += QueueRedraw;
            MouseExited += QueueRedraw;
        }

        public override void _Draw()
        {
            var rect = new Rect2(Vector2.Zero, Size);
            var hovered = IsHovered();

            // 1. Cam hap arka plan + üst speküler vurgu
            var bg = new StyleBoxFlat
            {
                BgColor = new Color(0.05f, 0.09f, 0.16f, hovered ? 0.96f : 0.90f),
                BorderColor = _accent,
                BorderWidthTop = 2,
                BorderWidthBottom = 1,
                BorderWidthLeft = 1,
                BorderWidthRight = 1,
                CornerRadiusTopLeft = 14,
                CornerRadiusTopRight = 14,
                CornerRadiusBottomLeft = 14,
                CornerRadiusBottomRight = 14,
                ShadowColor = new Color(_accent.R, _accent.G, _accent.B, hovered ? 0.42f : 0.24f),
                ShadowSize = hovered ? 14 : 10,
                ShadowOffset = new Vector2(0, 3),
            };
            DrawStyleBox(bg, rect);

            // 2. Sol tarafta parlayan vektörel hedef retikülü (Target Reticle)
            var reticleCenter = new Vector2(22f, Size.Y * 0.5f);
            DrawCircle(reticleCenter, 13f, new Color(_accent.R, _accent.G, _accent.B, 0.18f));
            DrawArc(reticleCenter, 10f, 0f, Mathf.Tau, 24, _accent, 2f);
            DrawCircle(reticleCenter, 3.2f, _accent);
            DrawLine(reticleCenter + new Vector2(-13f, 0f), reticleCenter + new Vector2(-6f, 0f), _accent, 1.6f);
            DrawLine(reticleCenter + new Vector2(6f, 0f), reticleCenter + new Vector2(13f, 0f), _accent, 1.6f);
            DrawLine(reticleCenter + new Vector2(0f, -13f), reticleCenter + new Vector2(0f, -6f), _accent, 1.6f);
            DrawLine(reticleCenter + new Vector2(0f, 6f), reticleCenter + new Vector2(0f, 13f), _accent, 1.6f);

            // 3. Sağ tarafta aksiyon metni + hedef bölge etiketi
            var font = ThemeDB.FallbackFont;
            DrawString(
                font,
                new Vector2(40f, 18f),
                _subtitle,
                HorizontalAlignment.Left,
                Size.X - 44f,
                9,
                _accent);
            DrawString(
                font,
                new Vector2(40f, 33f),
                _title,
                HorizontalAlignment.Left,
                Size.X - 44f,
                11,
                Colors.White);
        }
    }
}
