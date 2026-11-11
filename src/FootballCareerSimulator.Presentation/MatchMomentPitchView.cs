using FootballCareerSimulator.Application.Competition.Commands;
using FootballCareerSimulator.Application.Competition.Queries;
using FootballCareerSimulator.Domain.Match;
using Godot;

namespace FootballCareerSimulator.Presentation;

/// <summary>
/// FM27 Broadcast 2D Stadium Match Pitch.
/// Features full-stage stadium surroundings, white roof rafters/stands,
/// perimeter LED advertising boards, team dugouts (benches) with seated substitute dots,
/// alternating emerald turf mowing stripes, crisp pitch markings,
/// and full 22-player tokens with authentic directional movement arrows (Yön Oku),
/// central referee, and sideline linesmen tracking play.
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
    private Color _homeTeamColor = new(0.95f, 0.98f, 1.0f);     // Crystal Palace white/red
    private Color _homeSecondaryColor = new(0.85f, 0.12f, 0.20f);
    private Color _awayTeamColor = new(1.0f, 0.84f, 0.0f);      // Leeds United yellow/navy
    private Color _awaySecondaryColor = new(0.08f, 0.14f, 0.30f);
    private string _homeClubName = "Crystal Palace";
    private string _awayClubName = "Leeds United";
    private string _competitionName = "PREMIER LEAGUE";

    private string? _activeShoutText;
    private Color _activeShoutColor = new(1f, 0.78f, 0.18f);
    private float _shoutFlashProgress;
    private bool _keyDecisionFreezeActive;
    private float _decisionIntroProgress = 1f;
    private Action<string>? _onPitchDecisionSelected;
    private readonly List<OnPitchTargetDecisionButton> _decisionTargetButtons = [];

    // Continuous clock simulation state (00:00 to 90:00)
    private float _simulatedMatchSeconds;
    private float _clockSpeedMultiplier = 1f;
    private double _organicCirculationTimer;
    private Vector2 _currentBallPos = new(0.5f, 0.5f);
    private Vector2 _targetBallPos = new(0.5f, 0.5f);
    private float _ballHeight;
    private readonly List<Vector2> _ballTrail = [];
    private readonly float[] _homePlayerAngles = new float[11];
    private readonly float[] _awayPlayerAngles = new float[11];
    private readonly Vector2[] _homeLivePositions = new Vector2[11];
    private readonly Vector2[] _awayLivePositions = new Vector2[11];
    private Vector2 _refereePos = new(0.5f, 0.45f);
    private float _linesmanTopX = 0.5f;
    private float _linesmanBotX = 0.5f;

    // Authentic squad numbers
    private static readonly int[] HomeSquadNumbers = [1, 5, 32, 23, 6, 8, 10, 14, 7, 11, 9];
    private static readonly int[] AwaySquadNumbers = [1, 2, 5, 6, 31, 4, 8, 13, 20, 22, 9];

    // Standard base formations
    private static readonly Vector2[] BaseHomePositions =
    [
        new(0.045f, 0.50f), // GK
        new(0.180f, 0.16f), // LB
        new(0.150f, 0.38f), // CB
        new(0.150f, 0.62f), // CB
        new(0.180f, 0.84f), // RB
        new(0.300f, 0.50f), // DM
        new(0.420f, 0.34f), // CM
        new(0.420f, 0.66f), // CM
        new(0.620f, 0.18f), // LW
        new(0.620f, 0.82f), // RW
        new(0.720f, 0.50f), // CF
    ];

    public MatchMomentPitchView()
    {
        MouseFilter = MouseFilterEnum.Pass;
        ClipContents = true;
        CustomMinimumSize = new Vector2(360, 220);
        InitializePlayerEntities();
    }

    private void InitializePlayerEntities()
    {
        for (var i = 0; i < 11; i++)
        {
            _homeLivePositions[i] = BaseHomePositions[i];
            _awayLivePositions[i] = new Vector2(1f - BaseHomePositions[i].X, BaseHomePositions[i].Y);
            _homePlayerAngles[i] = 0f;
            _awayPlayerAngles[i] = Mathf.Pi;
        }
    }

    public float SecondsPerMoment
    {
        get => _secondsPerMoment;
        set => _secondsPerMoment = Math.Clamp(value, 0.25f, 10f);
    }

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

    public Color HomeSecondaryColor
    {
        get => _homeSecondaryColor;
        set
        {
            _homeSecondaryColor = value;
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

    public Color AwaySecondaryColor
    {
        get => _awaySecondaryColor;
        set
        {
            _awaySecondaryColor = value;
            QueueRedraw();
        }
    }

    public string HomeClubName
    {
        get => _homeClubName;
        set
        {
            _homeClubName = value;
            QueueRedraw();
        }
    }

    public string AwayClubName
    {
        get => _awayClubName;
        set
        {
            _awayClubName = value;
            QueueRedraw();
        }
    }

    public string CompetitionName
    {
        get => _competitionName;
        set
        {
            _competitionName = value;
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

    public float MatchClockSeconds => _simulatedMatchSeconds;
    public int CurrentMatchMinute => Mathf.FloorToInt(_simulatedMatchSeconds / 60f);
    public int CurrentMatchSecond => Mathf.FloorToInt(_simulatedMatchSeconds % 60f);

    public event Action<int, MatchMomentStoryboardFrame>? MomentChanged;
    public event Action? SequenceFinished;
    public event Action<int, int>? ClockUpdated;
    public event Action<string>? CommentaryGenerated;

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
        if (!_playing)
        {
            return;
        }

        var dt = (float)Math.Max(0, delta);
        UpdateMatchClockAndOrganicSimulation(dt);

        if (_storyboard.Frames.Count == 0)
        {
            QueueRedraw();
            return;
        }

        _elapsed += dt;
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

    private void UpdateMatchClockAndOrganicSimulation(float delta)
    {
        var targetMinute = CurrentFrame?.Minute ?? 90;
        var currentMinute = CurrentMatchMinute;

        // Clock advances towards current key moment minute, or flows continuously
        var clockRate = targetMinute > currentMinute ? 65f : 12f;
        _simulatedMatchSeconds += delta * clockRate * _clockSpeedMultiplier;
        if (_simulatedMatchSeconds > 5400f) // 90 mins max
        {
            _simulatedMatchSeconds = 5400f;
        }

        ClockUpdated?.Invoke(CurrentMatchMinute, CurrentMatchSecond);

        // Organic ball circulation and player breathing between highlights
        _organicCirculationTimer += delta * 2.2f;
        var wave = (float)Math.Sin(_organicCirculationTimer);
        var cosWave = (float)Math.Cos(_organicCirculationTimer * 0.85f);

        // Interpolate live players towards formation or active frame
        var frame = CurrentFrame;
        var p = FrameProgress;
        var eased = ReducedMotion ? 1f : EaseOutCubic(p);

        for (var i = 0; i < 11; i++)
        {
            var homeBase = BaseHomePositions[i];
            var awayBase = new Vector2(1f - BaseHomePositions[i].X, BaseHomePositions[i].Y);

            // Natural team shifting according to ball position
            var ballShiftX = (_currentBallPos.X - 0.5f) * 0.08f;
            var ballShiftY = (_currentBallPos.Y - 0.5f) * 0.06f;

            var targetHome = new Vector2(
                Mathf.Clamp(homeBase.X + ballShiftX + (wave * 0.008f), 0.02f, 0.98f),
                Mathf.Clamp(homeBase.Y + ballShiftY + (cosWave * 0.006f), 0.05f, 0.95f));

            var targetAway = new Vector2(
                Mathf.Clamp(awayBase.X + ballShiftX - (wave * 0.008f), 0.02f, 0.98f),
                Mathf.Clamp(awayBase.Y + ballShiftY - (cosWave * 0.006f), 0.05f, 0.95f));

            // If active in frame, pull towards frame actor/support
            if (frame is not null)
            {
                if (frame.IsHomeSide && frame.PrimarySlotIndex == i)
                {
                    var actorNorm = new Vector2(frame.ActorPosition.X, frame.ActorPosition.Y);
                    targetHome = targetHome.Lerp(actorNorm, 0.85f);
                }
                else if (!frame.IsHomeSide && frame.PrimarySlotIndex == i)
                {
                    var actorNorm = new Vector2(frame.ActorPosition.X, frame.ActorPosition.Y);
                    targetAway = targetAway.Lerp(actorNorm, 0.85f);
                }
            }

            // Smoothly move players
            var homeVel = (targetHome - _homeLivePositions[i]) * 4.5f * delta;
            _homeLivePositions[i] += homeVel;
            if (homeVel.LengthSquared() > 0.00001f)
            {
                _homePlayerAngles[i] = Mathf.LerpAngle(_homePlayerAngles[i], homeVel.Angle(), 8f * delta);
            }
            else
            {
                var lookTarget = _currentBallPos - _homeLivePositions[i];
                _homePlayerAngles[i] = Mathf.LerpAngle(_homePlayerAngles[i], lookTarget.Angle(), 4f * delta);
            }

            var awayVel = (targetAway - _awayLivePositions[i]) * 4.5f * delta;
            _awayLivePositions[i] += awayVel;
            if (awayVel.LengthSquared() > 0.00001f)
            {
                _awayPlayerAngles[i] = Mathf.LerpAngle(_awayPlayerAngles[i], awayVel.Angle(), 8f * delta);
            }
            else
            {
                var lookTarget = _currentBallPos - _awayLivePositions[i];
                _awayPlayerAngles[i] = Mathf.LerpAngle(_awayPlayerAngles[i], lookTarget.Angle(), 4f * delta);
            }
        }

        // Referee & Linesmen follow the play
        _refereePos = _refereePos.Lerp(new Vector2(_currentBallPos.X * 0.7f + 0.15f, _currentBallPos.Y * 0.6f + 0.2f), 3.0f * delta);
        _linesmanTopX = Mathf.Lerp(_linesmanTopX, Mathf.Clamp(_currentBallPos.X, 0.15f, 0.85f), 4.0f * delta);
        _linesmanBotX = Mathf.Lerp(_linesmanBotX, Mathf.Clamp(_currentBallPos.X, 0.15f, 0.85f), 4.0f * delta);

        // Ball movement
        if (frame is not null)
        {
            var start = new Vector2(frame.BallStart.X, frame.BallStart.Y);
            var end = new Vector2(frame.BallEnd.X, frame.BallEnd.Y);
            _targetBallPos = start.Lerp(end, eased);
            var isGoal = string.Equals(frame.Kind, "Goal", StringComparison.OrdinalIgnoreCase);
            _ballHeight = isGoal ? Mathf.Sin(p * Mathf.Pi) * 0.12f : 0f;
        }
        else
        {
            _targetBallPos = new Vector2(0.5f + (wave * 0.12f), 0.5f + (cosWave * 0.14f));
            _ballHeight = 0f;
        }

        _currentBallPos = _currentBallPos.Lerp(_targetBallPos, 7.5f * delta);

        // Maintain ball trail
        _ballTrail.Add(_currentBallPos);
        if (_ballTrail.Count > 10)
        {
            _ballTrail.RemoveAt(0);
        }
    }

    public override void _Draw()
    {
        if (Size.X < 24 || Size.Y < 24)
        {
            return;
        }

        var bounds = new Rect2(Vector2.Zero, Size);

        // 1. Stadium Ambient Background
        DrawRect(bounds, new Color(0.04f, 0.07f, 0.12f, 1f));

        // Calculate pitch rectangle with stadium margins for roof rafters and LED boards
        var sideMargin = Mathf.Clamp(Size.X * 0.075f, 26f, 64f);
        var topMargin = Mathf.Clamp(Size.Y * 0.085f, 20f, 36f);
        var botMargin = Mathf.Clamp(Size.Y * 0.055f, 14f, 24f);

        var field = new Rect2(
            new Vector2(sideMargin, topMargin),
            new Vector2(Size.X - (sideMargin * 2f), Size.Y - topMargin - botMargin));

        // 2. Draw Stadium Grandstands / Roof Rafters & Perimeter Boards
        DrawStadiumSurroundings(bounds, field);

        // 3. Draw Pitch Turf, Striping & Field Markings
        DrawPitchField(field);

        // 4. Draw Sideline Dugouts (Yedek Kulübeleri)
        DrawSidelineDugouts(field);

        // 5. Draw Match Officials (Hakem ve Yan Hakemler)
        DrawMatchOfficials(field);

        // 6. Draw 22 Player Tokens with Directional Movement Wedges
        DrawPlayers(field);

        // 7. Draw Match Ball & Trajectories
        DrawBall(field);

        // 8. Draw Active Moment Highlight Visuals
        if (CurrentFrame is { } frame)
        {
            DrawMomentOverlay(field, frame, FrameProgress);
        }

        // 9. Touchline Shout Aura Pulse
        if (_shoutFlashProgress > 0.01f)
        {
            DrawTouchlineShoutAura(field);
        }

        // 10. Direct On-Pitch Key Decision Highlight (Bezier Arrows + Target Buttons)
        if (_keyDecisionFreezeActive && CurrentFrame is { } freezeFrame)
        {
            DrawKeyDecisionOverlay(field, freezeFrame);
        }
    }

    private void DrawStadiumSurroundings(Rect2 bounds, Rect2 field)
    {
        // Left Grandstand Roof Rafters (White/Light structural steel beams)
        var rafterColor = new Color(0.85f, 0.90f, 0.96f, 0.45f);
        var steelTruss = new Color(0.18f, 0.26f, 0.38f, 0.85f);
        var standSeatColor = new Color(0.09f, 0.14f, 0.22f, 0.92f);

        // Left stands geometry
        Vector2[] leftStandPoly =
        [
            new(0f, 0f),
            new(field.Position.X - 4f, field.Position.Y - 6f),
            new(field.Position.X - 4f, field.End.Y + 6f),
            new(0f, bounds.Size.Y),
        ];
        DrawColoredPolygon(leftStandPoly, standSeatColor);

        // Right stands geometry
        Vector2[] rightStandPoly =
        [
            new(field.End.X + 4f, field.Position.Y - 6f),
            new(bounds.Size.X, 0f),
            new(bounds.Size.X, bounds.Size.Y),
            new(field.End.X + 4f, field.End.Y + 6f),
        ];
        DrawColoredPolygon(rightStandPoly, standSeatColor);

        // Roof Trusses / Rafter lines (Left)
        const int trussCount = 5;
        for (var i = 0; i <= trussCount; i++)
        {
            var t = i / (float)trussCount;
            var outerY = Mathf.Lerp(0f, bounds.Size.Y, t);
            var innerY = Mathf.Lerp(field.Position.Y - 6f, field.End.Y + 6f, t);
            DrawLine(new Vector2(0f, outerY), new Vector2(field.Position.X - 4f, innerY), rafterColor, 2.2f);
            if (i < trussCount)
            {
                var nextInnerY = Mathf.Lerp(field.Position.Y - 6f, field.End.Y + 6f, (i + 1) / (float)trussCount);
                DrawLine(new Vector2(0f, outerY), new Vector2(field.Position.X - 4f, nextInnerY), steelTruss, 1.2f);
            }
        }

        // Roof Trusses / Rafter lines (Right)
        for (var i = 0; i <= trussCount; i++)
        {
            var t = i / (float)trussCount;
            var outerY = Mathf.Lerp(0f, bounds.Size.Y, t);
            var innerY = Mathf.Lerp(field.Position.Y - 6f, field.End.Y + 6f, t);
            DrawLine(new Vector2(bounds.Size.X, outerY), new Vector2(field.End.X + 4f, innerY), rafterColor, 2.2f);
            if (i < trussCount)
            {
                var nextInnerY = Mathf.Lerp(field.Position.Y - 6f, field.End.Y + 6f, (i + 1) / (float)trussCount);
                DrawLine(new Vector2(bounds.Size.X, outerY), new Vector2(field.End.X + 4f, nextInnerY), steelTruss, 1.2f);
            }
        }

        // LED Perimeter Advertising Boards (Top and Bottom Touchlines)
        DrawLedAdBoard(new Rect2(field.Position.X - 2f, field.Position.Y - 7f, field.Size.X + 4f, 5f));
        DrawLedAdBoard(new Rect2(field.Position.X - 2f, field.End.Y + 2f, field.Size.X + 4f, 5f));
    }

    private void DrawLedAdBoard(Rect2 rect)
    {
        // Glowing LED backdrop
        DrawRect(rect, new Color(0.06f, 0.10f, 0.16f, 0.95f), filled: true);
        DrawRect(rect, new Color(0.12f, 0.45f, 0.85f, 0.40f), filled: false, width: 1.0f);

        // Pulsing neon segments along the perimeter board
        var segmentCount = 6;
        var segWidth = rect.Size.X / segmentCount;
        for (var i = 0; i < segmentCount; i++)
        {
            var x = rect.Position.X + (i * segWidth) + 4f;
            var color = i % 2 == 0 ? new Color(0f, 0.96f, 0.61f, 0.55f) : new Color(1f, 0.72f, 0.18f, 0.55f);
            DrawLine(new Vector2(x, rect.GetCenter().Y), new Vector2(x + segWidth - 8f, rect.GetCenter().Y), color, 1.6f);
        }
    }

    private void DrawSidelineDugouts(Rect2 field)
    {
        // Top sideline dugouts (Home Dugout Left, Away Dugout Right)
        var dugoutWidth = field.Size.X * 0.18f;
        var dugoutHeight = 10f;
        var dugoutY = field.Position.Y - 18f;

        // Home Dugout
        var homeDugoutRect = new Rect2(field.Position.X + (field.Size.X * 0.22f), dugoutY, dugoutWidth, dugoutHeight);
        DrawRect(homeDugoutRect, new Color(0.08f, 0.12f, 0.18f, 0.90f), filled: true);
        DrawRect(homeDugoutRect, _homeSecondaryColor, filled: false, width: 1.5f);
        // Bench seats
        for (var i = 0; i < 6; i++)
        {
            var dotX = homeDugoutRect.Position.X + 6f + (i * ((dugoutWidth - 12f) / 5f));
            DrawCircle(new Vector2(dotX, homeDugoutRect.GetCenter().Y), 2.5f, _homeTeamColor);
        }

        // Away Dugout
        var awayDugoutRect = new Rect2(field.Position.X + (field.Size.X * 0.60f), dugoutY, dugoutWidth, dugoutHeight);
        DrawRect(awayDugoutRect, new Color(0.08f, 0.12f, 0.18f, 0.90f), filled: true);
        DrawRect(awayDugoutRect, _awaySecondaryColor, filled: false, width: 1.5f);
        // Bench seats
        for (var i = 0; i < 6; i++)
        {
            var dotX = awayDugoutRect.Position.X + 6f + (i * ((dugoutWidth - 12f) / 5f));
            DrawCircle(new Vector2(dotX, awayDugoutRect.GetCenter().Y), 2.5f, _awayTeamColor);
        }
    }

    private void DrawPitchField(Rect2 field)
    {
        // Alternating horizontal mowing stripes (FM27 Emerald Grass bands)
        var grassDark = new Color(0.09f, 0.38f, 0.20f, 1f);   // #176133
        var grassStripe = new Color(0.12f, 0.44f, 0.24f, 1f); // #1E703D
        const int horizontalBands = 12;
        var bandHeight = field.Size.Y / horizontalBands;

        for (var i = 0; i < horizontalBands; i++)
        {
            var y = field.Position.Y + (i * bandHeight);
            var rect = new Rect2(field.Position.X, y, field.Size.X, bandHeight + 0.5f);
            DrawRect(rect, i % 2 == 0 ? grassDark : grassStripe, filled: true);
        }

        // Outer field border glow and crisp white boundary line
        var boundaryColor = new Color(0.96f, 0.98f, 1.0f, 0.88f);
        var softGlow = new Color(0.96f, 0.98f, 1.0f, 0.18f);
        DrawRect(field, softGlow, filled: false, width: 4.5f);
        DrawRect(field, boundaryColor, filled: false, width: 2.2f);

        // Halfway Line
        var midX = field.Position.X + (field.Size.X * 0.5f);
        DrawLine(new Vector2(midX, field.Position.Y), new Vector2(midX, field.End.Y), boundaryColor, 2.0f);

        // Center Circle & Spot
        var center = field.GetCenter();
        var centerRadius = field.Size.Y * 0.18f;
        DrawArc(center, centerRadius, 0f, Mathf.Tau, 36, boundaryColor, 2.0f);
        DrawCircle(center, 3.2f, boundaryColor);

        // Left Penalty Box (18-yard box)
        var boxWidth = field.Size.X * 0.165f;
        var boxTopY = field.Position.Y + (field.Size.Y * 0.21f);
        var boxHeight = field.Size.Y * 0.58f;
        var leftBoxRect = new Rect2(field.Position.X, boxTopY, boxWidth, boxHeight);
        DrawRect(leftBoxRect, boundaryColor, filled: false, width: 2.0f);

        // Left Six-Yard Box (Altıpas)
        var sixYardWidth = field.Size.X * 0.058f;
        var sixYardTopY = field.Position.Y + (field.Size.Y * 0.36f);
        var sixYardHeight = field.Size.Y * 0.28f;
        var leftSixYardRect = new Rect2(field.Position.X, sixYardTopY, sixYardWidth, sixYardHeight);
        DrawRect(leftSixYardRect, boundaryColor, filled: false, width: 1.8f);

        // Left Penalty Spot & Arc (D-arc)
        var leftPenSpot = new Vector2(field.Position.X + (field.Size.X * 0.11f), center.Y);
        DrawCircle(leftPenSpot, 2.8f, boundaryColor);
        DrawArc(leftPenSpot, centerRadius * 0.85f, -0.92f, 0.92f, 16, boundaryColor, 1.8f);

        // Left Goalmouth & 3D Net Mesh
        var goalTopY = field.Position.Y + (field.Size.Y * 0.42f);
        var goalHeight = field.Size.Y * 0.16f;
        var leftNetRect = new Rect2(field.Position.X - 10f, goalTopY, 10f, goalHeight);
        DrawRect(leftNetRect, new Color(0.95f, 0.98f, 1f, 0.25f), filled: true);
        DrawRect(leftNetRect, boundaryColor, filled: false, width: 2.0f);

        // Right Penalty Box (18-yard box)
        var rightBoxRect = new Rect2(field.End.X - boxWidth, boxTopY, boxWidth, boxHeight);
        DrawRect(rightBoxRect, boundaryColor, filled: false, width: 2.0f);

        // Right Six-Yard Box
        var rightSixYardRect = new Rect2(field.End.X - sixYardWidth, sixYardTopY, sixYardWidth, sixYardHeight);
        DrawRect(rightSixYardRect, boundaryColor, filled: false, width: 1.8f);

        // Right Penalty Spot & Arc
        var rightPenSpot = new Vector2(field.End.X - (field.Size.X * 0.11f), center.Y);
        DrawCircle(rightPenSpot, 2.8f, boundaryColor);
        DrawArc(rightPenSpot, centerRadius * 0.85f, Mathf.Pi - 0.92f, Mathf.Pi + 0.92f, 16, boundaryColor, 1.8f);

        // Right Goalmouth & 3D Net Mesh
        var rightNetRect = new Rect2(field.End.X, goalTopY, 10f, goalHeight);
        DrawRect(rightNetRect, new Color(0.95f, 0.98f, 1f, 0.25f), filled: true);
        DrawRect(rightNetRect, boundaryColor, filled: false, width: 2.0f);

        // Corner Arcs
        const float cornerR = 8.5f;
        DrawArc(field.Position, cornerR, 0f, Mathf.Pi * 0.5f, 8, boundaryColor, 1.8f);
        DrawArc(new Vector2(field.End.X, field.Position.Y), cornerR, Mathf.Pi * 0.5f, Mathf.Pi, 8, boundaryColor, 1.8f);
        DrawArc(new Vector2(field.Position.X, field.End.Y), cornerR, -Mathf.Pi * 0.5f, 0f, 8, boundaryColor, 1.8f);
        DrawArc(field.End, cornerR, Mathf.Pi, Mathf.Pi * 1.5f, 8, boundaryColor, 1.8f);
    }

    private void DrawMatchOfficials(Rect2 field)
    {
        // Central Referee (Green circular token)
        var refPixel = ToPixel(_refereePos, field);
        DrawCircle(refPixel + new Vector2(0f, 2f), 6.5f, new Color(0f, 0f, 0f, 0.40f));
        DrawCircle(refPixel, 6.0f, new Color(0f, 0.90f, 0.45f, 1f));
        DrawArc(refPixel, 6.0f, 0f, Mathf.Tau, 16, Colors.Black, 1.2f);

        // Assistant Referee 1 (Top touchline with 'A' badge)
        var a1Pixel = new Vector2(field.Position.X + (_linesmanTopX * field.Size.X), field.Position.Y - 4f);
        DrawCircle(a1Pixel, 5.0f, new Color(0f, 0.90f, 0.45f, 1f));
        DrawArc(a1Pixel, 5.0f, 0f, Mathf.Tau, 12, Colors.Black, 1.0f);

        // Assistant Referee 2 (Bottom touchline with 'A' badge)
        var a2Pixel = new Vector2(field.Position.X + (_linesmanBotX * field.Size.X), field.End.Y + 4f);
        DrawCircle(a2Pixel, 5.0f, new Color(0f, 0.90f, 0.45f, 1f));
        DrawArc(a2Pixel, 5.0f, 0f, Mathf.Tau, 12, Colors.Black, 1.0f);
    }

    private void DrawPlayers(Rect2 field)
    {
        var radius = Mathf.Clamp(field.Size.Y * 0.042f, 8.5f, 14.5f);
        var frame = CurrentFrame;

        // Draw 11 Home Players
        for (var i = 0; i < 11; i++)
        {
            var pos = ToPixel(_homeLivePositions[i], field);
            var isGk = i == 0;
            var fill = isGk ? new Color(0.12f, 0.85f, 0.45f) : _homeTeamColor;
            var stroke = isGk ? Colors.Black : _homeSecondaryColor;
            var num = HomeSquadNumbers[i];
            var angle = _homePlayerAngles[i];
            var active = frame is { IsHomeSide: true } && frame.PrimarySlotIndex == i;

            DrawFm27PlayerToken(pos, fill, stroke, num, angle, radius, active, isGk);
        }

        // Draw 11 Away Players
        for (var i = 0; i < 11; i++)
        {
            var pos = ToPixel(_awayLivePositions[i], field);
            var isGk = i == 0;
            var fill = isGk ? new Color(0.15f, 0.15f, 0.15f) : _awayTeamColor;
            var stroke = isGk ? Colors.White : _awaySecondaryColor;
            var num = AwaySquadNumbers[i];
            var angle = _awayPlayerAngles[i];
            var active = frame is { IsHomeSide: false } && frame.PrimarySlotIndex == i;

            DrawFm27PlayerToken(pos, fill, stroke, num, angle, radius, active, isGk);
        }
    }

    private void DrawFm27PlayerToken(
        Vector2 position,
        Color bodyFill,
        Color borderStroke,
        int squadNumber,
        float facingAngle,
        float radius,
        bool active,
        bool isGoalkeeper)
    {
        // 1. Drop shadow under token
        DrawCircle(position + new Vector2(0f, 3f), radius + 2.0f, new Color(0f, 0f, 0f, 0.48f));

        // 2. Active pulsing ring if highlighted in moment
        if (active)
        {
            DrawCircle(position, radius + 8.5f, new Color(0f, 0.96f, 0.61f, 0.22f));
            DrawArc(position, radius + 6.0f, 0f, Mathf.Tau, 24, CareerUiTheme.ActionBright, 2.4f);
        }

        // 3. DIRECTIONAL MOVEMENT WEDGE / ARROW (FM27 Yön Oku)
        // Attached directly to the rim of the circular token pointing in facingAngle
        var tipDist = radius + 5.2f;
        var baseDist = radius + 0.5f;
        var wedgeTip = position + new Vector2(Mathf.Cos(facingAngle), Mathf.Sin(facingAngle)) * tipDist;
        var wedgeLeft = position + new Vector2(Mathf.Cos(facingAngle + 0.38f), Mathf.Sin(facingAngle + 0.38f)) * baseDist;
        var wedgeRight = position + new Vector2(Mathf.Cos(facingAngle - 0.38f), Mathf.Sin(facingAngle - 0.38f)) * baseDist;

        var arrowColor = active ? CareerUiTheme.Accent : borderStroke;
        DrawColoredPolygon([wedgeTip, wedgeLeft, wedgeRight], arrowColor);

        // 4. Circular Token Body & Rim
        DrawCircle(position, radius + 1.2f, borderStroke);
        DrawCircle(position, radius, bodyFill);

        // 5. Squad Number (Forma Numarası) in center
        var font = ThemeDB.FallbackFont;
        var fontSize = Math.Max(8, Mathf.RoundToInt(radius * 1.25f));
        var text = squadNumber.ToString();
        var size = font.GetStringSize(text, HorizontalAlignment.Center, -1, fontSize);
        var textColor = isGoalkeeper ? Colors.White : (bodyFill.Luminance > 0.55f ? Colors.Black : Colors.White);
        DrawString(
            font,
            position + new Vector2(-size.X * 0.5f, size.Y * 0.34f),
            text,
            HorizontalAlignment.Center,
            -1,
            fontSize,
            textColor);
    }

    private void DrawBall(Rect2 field)
    {
        var ballPixel = ToPixel(_currentBallPos, field);
        var shadowPixel = ballPixel + new Vector2(0f, 2.5f + (_ballHeight * 60f));

        // Ball Trail
        if (_ballTrail.Count > 1)
        {
            for (var i = 1; i < _ballTrail.Count; i++)
            {
                var p0 = ToPixel(_ballTrail[i - 1], field);
                var p1 = ToPixel(_ballTrail[i], field);
                var alpha = (i / (float)_ballTrail.Count) * 0.28f;
                DrawLine(p0, p1, new Color(1f, 1f, 1f, alpha), 1.8f);
            }
        }

        // Ball Shadow
        var shadowRadius = Mathf.Clamp(4.5f - (_ballHeight * 12f), 2.0f, 5.0f);
        DrawCircle(shadowPixel, shadowRadius, new Color(0f, 0f, 0f, 0.45f));

        // Football Token
        var ballDrawPos = ballPixel - new Vector2(0f, _ballHeight * 60f);
        DrawCircle(ballDrawPos, 4.2f, Colors.White);
        DrawCircle(ballDrawPos, 1.6f, new Color(0.10f, 0.12f, 0.15f));
        DrawArc(ballDrawPos, 4.2f, 0f, Mathf.Tau, 12, new Color(0.15f, 0.18f, 0.22f), 1.0f);
    }

    private void DrawMomentOverlay(Rect2 field, MatchMomentStoryboardFrame frame, float progress)
    {
        var eased = ReducedMotion ? 1f : EaseOutCubic(progress);
        var actor = ToPixel(new Vector2(frame.ActorPosition.X, frame.ActorPosition.Y), field);

        var signalColor = MomentColor(frame.Kind);
        var pulse = ReducedMotion ? 1f : 0.85f + (0.15f * Mathf.Sin(progress * Mathf.Tau));

        DrawCircle(actor, 15f * pulse, new Color(signalColor.R, signalColor.G, signalColor.B, 0.18f));
        DrawArc(actor, 14f * pulse, 0f, Mathf.Tau, 28, signalColor, 2.5f);

        if (IsKind(frame.Kind, MatchKeyMomentKind.Goal))
        {
            var end = ToPixel(new Vector2(frame.BallEnd.X, frame.BallEnd.Y), field);
            DrawGoalBurst(end, ReducedMotion ? 1f : progress, signalColor);
        }
        else if (IsKind(frame.Kind, MatchKeyMomentKind.YellowCard) || IsKind(frame.Kind, MatchKeyMomentKind.RedCard))
        {
            var cardSize = new Vector2(10, 16);
            var cardPos = actor + new Vector2(-cardSize.X * 0.5f, -28f);
            DrawRect(new Rect2(cardPos, cardSize), signalColor);
            DrawRect(new Rect2(cardPos, cardSize), Colors.Black, filled: false, width: 1.5f);
        }
    }

    private void DrawGoalBurst(Vector2 center, float progress, Color color)
    {
        var reveal = Math.Clamp((progress - 0.55f) / 0.45f, 0f, 1f);
        if (reveal <= 0)
        {
            return;
        }

        var radius = 10f + (22f * reveal);
        for (var ray = 0; ray < 8; ray++)
        {
            var direction = Vector2.FromAngle((Mathf.Tau / 8f) * ray);
            DrawLine(
                center + (direction * (radius * 0.45f)),
                center + (direction * radius),
                new Color(color.R, color.G, color.B, 0.85f),
                2.5f);
        }
    }

    private void DrawTouchlineShoutAura(Rect2 field)
    {
        var center = field.GetCenter();
        var maxR = Mathf.Min(field.Size.X, field.Size.Y) * 0.48f;
        var curR = Mathf.Lerp(16f, maxR, 1f - _shoutFlashProgress);
        var ringColor = new Color(_activeShoutColor.R, _activeShoutColor.G, _activeShoutColor.B, _shoutFlashProgress * 0.80f);
        DrawArc(center, curR, 0f, Mathf.Tau, 48, ringColor, 3.5f);
        DrawRect(field, new Color(_activeShoutColor.R, _activeShoutColor.G, _activeShoutColor.B, _shoutFlashProgress * 0.12f), filled: true);
    }

    private void DrawKeyDecisionOverlay(Rect2 field, MatchMomentStoryboardFrame freezeFrame)
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

    private static (Vector2 Carrier, Vector2 TopCorner, Vector2 BackPost, Vector2 DribbleSpot) ResolveDecisionTargetPoints(
        MatchMomentStoryboardFrame freezeFrame,
        Rect2 field)
    {
        var carrier = ToPixel(new Vector2(freezeFrame.ActorPosition.X, freezeFrame.ActorPosition.Y), field);
        var goalNormX = freezeFrame.IsHomeSide ? 0.94f : 0.06f;
        var boxNormX = freezeFrame.IsHomeSide ? 0.84f : 0.16f;

        var topCorner = ToPixel(new Vector2(goalNormX, 0.28f), field);
        var backPost = ToPixel(new Vector2(boxNormX, 0.72f), field);
        var dribbleNorm = new Vector2(
            Mathf.Lerp(freezeFrame.ActorPosition.X, boxNormX, 0.62f),
            Mathf.Clamp(freezeFrame.ActorPosition.Y, 0.36f, 0.64f));
        var dribbleSpot = ToPixel(dribbleNorm, field);

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
        _simulatedMatchSeconds = 0;

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
        _simulatedMatchSeconds = 0;
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
        var frame = _storyboard.Frames[frameIndex];
        _simulatedMatchSeconds = Math.Max(_simulatedMatchSeconds, frame.Minute * 60f);

        RefreshTextOverlay();
        QueueRedraw();
        MomentChanged?.Invoke(frameIndex, frame);

        var commentary = GenerateCommentaryForFrame(frame);
        CommentaryGenerated?.Invoke(commentary);
    }

    private string GenerateCommentaryForFrame(MatchMomentStoryboardFrame frame)
    {
        var min = frame.Minute;
        var actor = frame.PrimaryPlayerName;
        var assist = frame.AssistPlayerName;

        return frame.Kind switch
        {
            "Goal" => assist is { Length: > 0 }
                ? $"{min}' GOOOL! {assist} harika kesti, {actor} ağları havalandırdı!"
                : $"{min}' GOOOL! {actor} ceza sahasında nefis vurdu ve topu ağlara yolladı!",
            "YellowCard" => $"{min}' SARI KART! {actor} taktik faulü sonrası hakem tarafından uyarıldı.",
            "RedCard" => $"{min}' KIRMIZI KART! {actor} doğrudan ihraç edildi! Takımı 10 kişi!",
            "Injury" => $"{min}' SAKATLIK! {actor} acı içinde yerde kaldı, sağlık ekibi sahada.",
            "Save" => $"{min}' MÜTHİŞ KURTARIŞ! Kaleci köşeye giden topu son anda çeldi!",
            _ => $"{min}' {actor} atağı sürdürüyor...",
        };
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
        overlay.OffsetTop = 8;
        overlay.OffsetRight = -12;
        overlay.OffsetBottom = 55;
        overlay.AddThemeConstantOverride("separation", 1);
        AddChild(overlay);

        _headlineLabel = new Label
        {
            HorizontalAlignment = HorizontalAlignment.Center,
            MouseFilter = MouseFilterEnum.Ignore,
        };
        CareerUiTheme.StyleHeadline(_headlineLabel);
        _headlineLabel.AddThemeFontSizeOverride("font_size", CareerUiTheme.FontSize(17));
        _headlineLabel.AddThemeColorOverride("font_outline_color", new Color(0f, 0f, 0f, 0.94f));
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
            _headlineLabel.Text = "FM27 CANLI MAÇ YAYINI";
            _detailLabel.Text = "Karşılaşma devam ediyor";
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
        var btnSize = new Vector2(130, 44);
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

    private static Vector2 ToPixel(Vector2 norm, Rect2 field)
    {
        var x = field.Position.X + (Mathf.Clamp(norm.X, 0f, 1f) * field.Size.X);
        var y = field.Position.Y + (Mathf.Clamp(norm.Y, 0f, 1f) * field.Size.Y);
        return new Vector2(x, y);
    }

    private void OnResized()
    {
        if (_keyDecisionFreezeActive && CurrentFrame is { } freezeFrame)
        {
            var field = new Rect2(
                new Vector2(Size.X * 0.075f, Size.Y * 0.085f),
                new Vector2(Size.X * 0.85f, Size.Y * 0.86f));
            var (_, topCorner, backPost, dribbleSpot) = ResolveDecisionTargetPoints(freezeFrame, field);
            LayoutOnPitchDecisionTargets(topCorner, backPost, dribbleSpot);
        }

        QueueRedraw();
    }

    private static float EaseOutCubic(float x) =>
        1f - Mathf.Pow(1f - x, 3f);

    private static string MomentTitle(string kind) =>
        kind switch
        {
            "Goal" => "GOL",
            "YellowCard" => "SARI KART",
            "RedCard" => "KIRMIZI KART",
            "Injury" => "SAKATLIK",
            "Save" => "KURTARIŞ",
            _ => "KRİTİK POZİSYON",
        };

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
            return new Color(1f, 0.38f, 0.36f);
        }

        return new Color(0.24f, 0.72f, 1f);
    }

    /// <summary>
    /// FM27 Glassmorphic target decision button placed directly on the pitch.
    /// </summary>
    public sealed partial class OnPitchTargetDecisionButton : Button
    {
        private readonly string _title;
        private readonly string _subtitle;
        private readonly Color _accent;

        public OnPitchTargetDecisionButton(string title, string subtitle, Color accent)
        {
            _title = title;
            _subtitle = subtitle;
            _accent = accent;
            CustomMinimumSize = new Vector2(130, 44);
            MouseFilter = MouseFilterEnum.Stop;
            FocusMode = FocusModeEnum.All;
        }

        public override void _Draw()
        {
            var rect = new Rect2(Vector2.Zero, Size);
            var hovered = IsHovered();

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
                ShadowColor = new Color(_accent.R, _accent.G, _accent.B, hovered ? 0.45f : 0.25f),
                ShadowSize = hovered ? 14 : 10,
                ShadowOffset = new Vector2(0, 3),
            };
            DrawStyleBox(bg, rect);

            var topHighlight = new Rect2(2, 2, Size.X - 4, 1.5f);
            DrawRect(topHighlight, new Color(1f, 1f, 1f, 0.35f));

            var font = ThemeDB.FallbackFont;
            var subSize = font.GetStringSize(_subtitle, HorizontalAlignment.Center, -1, 10);
            DrawString(
                font,
                new Vector2((Size.X - subSize.X) * 0.5f, 15f),
                _subtitle,
                HorizontalAlignment.Center,
                -1,
                10,
                new Color(_accent.R, _accent.G, _accent.B, 0.88f));

            var titleSize = font.GetStringSize(_title, HorizontalAlignment.Center, -1, 12);
            DrawString(
                font,
                new Vector2((Size.X - titleSize.X) * 0.5f, 32f),
                _title,
                HorizontalAlignment.Center,
                -1,
                12,
                Colors.White);
        }
    }
}
