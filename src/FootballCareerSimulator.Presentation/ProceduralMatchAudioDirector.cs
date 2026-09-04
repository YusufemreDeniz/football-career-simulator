using FootballCareerSimulator.Application.CareerHub.Queries;
using FootballCareerSimulator.Application.Competition.Commands;
using FootballCareerSimulator.Domain.Match;
using Godot;

namespace FootballCareerSimulator.Presentation;

public enum MatchAudioChannel
{
    Master = 0,
    Music = 1,
    Sfx = 2,
    Crowd = 3,
}

public enum MatchAudioCue
{
    Button = 0,
    Whistle = 1,
    Goal = 2,
    YellowCard = 3,
    RedCard = 4,
    Injury = 5,
    ResultWin = 6,
    ResultDraw = 7,
    ResultLoss = 8,
}

/// <summary>
/// Local mix settings for <see cref="ProceduralMatchAudioDirector"/>. Levels use
/// the familiar linear 0..1 range and are clamped when applied.
/// </summary>
public sealed record MatchAudioSettings
{
    public static MatchAudioSettings Default { get; } = new();

    public bool MasterEnabled { get; init; } = true;

    public bool MusicEnabled { get; init; } = true;

    public bool SfxEnabled { get; init; } = true;

    public bool CrowdEnabled { get; init; } = true;

    public float MasterLevel { get; init; } = 0.82f;

    public float MusicLevel { get; init; } = 0.24f;

    public float SfxLevel { get; init; } = 0.78f;

    public float CrowdLevel { get; init; } = 0.34f;

    public MatchAudioSettings Normalized() =>
        this with
        {
            MasterLevel = Math.Clamp(MasterLevel, 0f, 1f),
            MusicLevel = Math.Clamp(MusicLevel, 0f, 1f),
            SfxLevel = Math.Clamp(SfxLevel, 0f, 1f),
            CrowdLevel = Math.Clamp(CrowdLevel, 0f, 1f),
        };

    public static MatchAudioSettings FromPreferences(GameExperiencePreferences preferences)
    {
        ArgumentNullException.ThrowIfNull(preferences);
        var prefs = preferences.Normalize();
        return Default with
        {
            MasterEnabled = prefs.SoundEnabled,
            MusicEnabled = prefs.EffectiveMusicEnabled,
            SfxEnabled = prefs.EffectiveSfxEnabled,
            CrowdEnabled = prefs.EffectiveCrowdEnabled,
        };
    }
}

/// <summary>
/// Asset-free match audio. The node generates short PCM cues and seamless stadium
/// ambience in memory, then plays them through local players on the Master bus.
/// It never mutates the project's global audio-bus layout.
/// </summary>
public sealed partial class ProceduralMatchAudioDirector : Node
{
    private const int SfxVoiceCount = 4;

    private readonly Dictionary<MatchAudioCue, AudioStreamWav> _cueStreams = [];
    private readonly List<AudioStreamPlayer> _sfxVoices = [];

    private AudioStreamPlayer? _musicPlayer;
    private AudioStreamPlayer? _crowdPlayer;
    private int _nextVoice;
    private int? _atmosphereSeed;
    private bool _playbackSuppressed;

    /// <summary>
    /// Set before adding the node to the scene tree to explicitly disable playback
    /// in smoke tests. Godot headless runs are suppressed automatically.
    /// </summary>
    public bool SuppressPlayback { get; set; }

    public MatchAudioSettings Settings { get; private set; } = MatchAudioSettings.Default;

    public bool IsPlaybackAvailable =>
        !_playbackSuppressed
        && IsInsideTree()
        && _musicPlayer is not null
        && _crowdPlayer is not null;

    public bool IsAtmospherePlaying =>
        IsPlaybackAvailable
        && ((_musicPlayer?.Playing ?? false) || (_crowdPlayer?.Playing ?? false));

    public event Action<MatchAudioSettings>? SettingsChanged;

    public event Action<MatchAudioCue>? CuePlayed;

    public override void _Ready()
    {
        _playbackSuppressed = SuppressPlayback || OS.HasFeature("headless");
        if (_playbackSuppressed)
        {
            return;
        }

        _musicPlayer = CreatePlayer("ProceduralMusic");
        _crowdPlayer = CreatePlayer("ProceduralCrowd");
        AddChild(_musicPlayer);
        AddChild(_crowdPlayer);

        for (var index = 0; index < SfxVoiceCount; index++)
        {
            var voice = CreatePlayer($"ProceduralSfx{index + 1}");
            _sfxVoices.Add(voice);
            AddChild(voice);
        }

        ApplyVolumes();
    }

    public override void _ExitTree()
    {
        StopAtmosphere();
        foreach (var voice in _sfxVoices)
        {
            voice.Stop();
        }
    }

    public void ApplySettings(MatchAudioSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);
        Settings = settings.Normalized();
        ApplyVolumes();
        SettingsChanged?.Invoke(Settings);
    }

    public void SetChannelEnabled(MatchAudioChannel channel, bool enabled)
    {
        ApplySettings(channel switch
        {
            MatchAudioChannel.Master => Settings with { MasterEnabled = enabled },
            MatchAudioChannel.Music => Settings with { MusicEnabled = enabled },
            MatchAudioChannel.Sfx => Settings with { SfxEnabled = enabled },
            MatchAudioChannel.Crowd => Settings with { CrowdEnabled = enabled },
            _ => Settings,
        });
    }

    public void SetChannelLevel(MatchAudioChannel channel, float level)
    {
        var safeLevel = Math.Clamp(level, 0f, 1f);
        ApplySettings(channel switch
        {
            MatchAudioChannel.Master => Settings with { MasterLevel = safeLevel },
            MatchAudioChannel.Music => Settings with { MusicLevel = safeLevel },
            MatchAudioChannel.Sfx => Settings with { SfxLevel = safeLevel },
            MatchAudioChannel.Crowd => Settings with { CrowdLevel = safeLevel },
            _ => Settings,
        });
    }

    /// <summary>
    /// Starts two subtle, seamless loops: a tonal stadium bed and a crowd murmur.
    /// The seed only changes procedural phase choices; it never uses shared RNG state.
    /// </summary>
    public bool StartAtmosphere(int seed = 0)
    {
        if (!IsPlaybackAvailable || _musicPlayer is null || _crowdPlayer is null)
        {
            return false;
        }

        if (_atmosphereSeed != seed)
        {
            _musicPlayer.Stream = ProceduralMatchAudioSynthesis.CreateMusicLoop(seed);
            _crowdPlayer.Stream = ProceduralMatchAudioSynthesis.CreateCrowdLoop(seed);
            _atmosphereSeed = seed;
        }

        if (!_musicPlayer.Playing)
        {
            _musicPlayer.Play();
        }

        if (!_crowdPlayer.Playing)
        {
            _crowdPlayer.Play();
        }

        return true;
    }

    public void StopAtmosphere()
    {
        _musicPlayer?.Stop();
        _crowdPlayer?.Stop();
    }

    public bool TryPlayCue(MatchAudioCue cue, int variationSeed = 0)
    {
        if (!IsPlaybackAvailable
            || !Settings.MasterEnabled
            || !Settings.SfxEnabled
            || _sfxVoices.Count == 0)
        {
            return false;
        }

        if (!_cueStreams.TryGetValue(cue, out var stream))
        {
            stream = ProceduralMatchAudioSynthesis.CreateCue(cue);
            _cueStreams.Add(cue, stream);
        }

        var voice = _sfxVoices[_nextVoice % _sfxVoices.Count];
        _nextVoice = (_nextVoice + 1) % _sfxVoices.Count;
        voice.Stream = stream;
        voice.PitchScale = ResolvePitchVariation(cue, variationSeed);
        voice.Play();
        CuePlayed?.Invoke(cue);
        return true;
    }

    public bool TryPlayMoment(MatchKeyMomentReadModel moment)
    {
        ArgumentNullException.ThrowIfNull(moment);
        return TryPlayMoment(moment.Kind, moment.Minute, moment.PrimarySlotIndex);
    }

    public bool TryPlayMoment(string? kind, int minute = 0, int slotIndex = 0)
    {
        if (!TryResolveMomentCue(kind, out var cue))
        {
            return false;
        }

        var variationSeed = unchecked((minute * 397) ^ (slotIndex * 31));
        return TryPlayCue(cue, variationSeed);
    }

    public bool TryPlayResult(int managedGoals, int opponentGoals, int variationSeed = 0)
    {
        var cue = managedGoals > opponentGoals
            ? MatchAudioCue.ResultWin
            : managedGoals < opponentGoals
                ? MatchAudioCue.ResultLoss
                : MatchAudioCue.ResultDraw;
        return TryPlayCue(cue, variationSeed);
    }

    public static bool TryResolveMomentCue(string? kind, out MatchAudioCue cue)
    {
        if (string.Equals(kind, nameof(MatchKeyMomentKind.Goal), StringComparison.OrdinalIgnoreCase))
        {
            cue = MatchAudioCue.Goal;
            return true;
        }

        if (string.Equals(kind, nameof(MatchKeyMomentKind.YellowCard), StringComparison.OrdinalIgnoreCase))
        {
            cue = MatchAudioCue.YellowCard;
            return true;
        }

        if (string.Equals(kind, nameof(MatchKeyMomentKind.RedCard), StringComparison.OrdinalIgnoreCase))
        {
            cue = MatchAudioCue.RedCard;
            return true;
        }

        if (string.Equals(kind, nameof(MatchKeyMomentKind.Injury), StringComparison.OrdinalIgnoreCase))
        {
            cue = MatchAudioCue.Injury;
            return true;
        }

        cue = default;
        return false;
    }

    private static AudioStreamPlayer CreatePlayer(string name) =>
        new()
        {
            Name = name,
            MaxPolyphony = 1,
        };

    private void ApplyVolumes()
    {
        var master = Settings.MasterEnabled ? Settings.MasterLevel : 0f;
        if (_musicPlayer is not null)
        {
            _musicPlayer.VolumeLinear = master * (Settings.MusicEnabled ? Settings.MusicLevel : 0f);
        }

        if (_crowdPlayer is not null)
        {
            _crowdPlayer.VolumeLinear = master * (Settings.CrowdEnabled ? Settings.CrowdLevel : 0f);
        }

        var sfx = master * (Settings.SfxEnabled ? Settings.SfxLevel : 0f);
        foreach (var voice in _sfxVoices)
        {
            voice.VolumeLinear = sfx;
        }
    }

    private static float ResolvePitchVariation(MatchAudioCue cue, int seed)
    {
        var hash = unchecked((uint)seed);
        hash ^= ((uint)cue + 1u) * 0x9E3779B9u;
        hash ^= hash >> 16;
        hash *= 0x7FEB352Du;
        hash ^= hash >> 15;
        var centered = ((hash & 0xFFu) / 255f) - 0.5f;
        return 1f + (centered * 0.035f);
    }
}

internal static class ProceduralMatchAudioSynthesis
{
    private const int SampleRate = 22050;
    private const float SafePeak = 0.92f;
    private static readonly double[] WinResultNotes = [392d, 494d, 659d];
    private static readonly double[] DrawResultNotes = [330d, 392d, 330d];
    private static readonly double[] LossResultNotes = [392d, 330d, 247d];

    public static AudioStreamWav CreateCue(MatchAudioCue cue)
    {
        var seconds = cue switch
        {
            MatchAudioCue.Button => 0.09,
            MatchAudioCue.Whistle => 0.62,
            MatchAudioCue.Goal => 1.15,
            MatchAudioCue.YellowCard => 0.28,
            MatchAudioCue.RedCard => 0.48,
            MatchAudioCue.Injury => 0.72,
            _ => 1.12,
        };

        return Render(seconds, (time, sample) => CueSample(cue, time, sample), loop: false);
    }

    public static AudioStreamWav CreateMusicLoop(int seed) =>
        Render(
            seconds: 6,
            (time, _) =>
            {
                const double duration = 6;
                var phase = time / duration;
                var breathe = 0.72 + (0.28 * Math.Sin(Math.Tau * phase));
                var root = Math.Sin(Math.Tau * 330 * phase);
                var fifth = Math.Sin((Math.Tau * 495 * phase) + SeedPhase(seed, 1));
                var octave = Math.Sin((Math.Tau * 660 * phase) + SeedPhase(seed, 2));
                var shimmer = Math.Sin((Math.Tau * 990 * phase) + (0.35 * Math.Sin(Math.Tau * phase)));
                return (float)((root * 0.34 + fifth * 0.24 + octave * 0.17 + shimmer * 0.08) * breathe * 0.42);
            },
            loop: true);

    public static AudioStreamWav CreateCrowdLoop(int seed) =>
        Render(
            seconds: 4,
            (time, _) =>
            {
                const double duration = 4;
                var phase = time / duration;
                var murmur = HarmonicNoise(phase, seed, bandCount: 18, firstCycle: 26, spacing: 7);
                var swell = 0.68
                    + (0.18 * Math.Sin((Math.Tau * phase) + SeedPhase(seed, 41)))
                    + (0.10 * Math.Sin((Math.Tau * 3 * phase) + SeedPhase(seed, 73)));
                var lowStand = Math.Sin((Math.Tau * 54 * phase) + SeedPhase(seed, 11)) * 0.13;
                return (float)((murmur * swell * 0.52) + lowStand);
            },
            loop: true);

    private static float CueSample(MatchAudioCue cue, double time, int sample)
    {
        return cue switch
        {
            MatchAudioCue.Button => Button(time),
            MatchAudioCue.Whistle => Whistle(time),
            MatchAudioCue.Goal => Goal(time, sample),
            MatchAudioCue.YellowCard => YellowCard(time, sample),
            MatchAudioCue.RedCard => RedCard(time, sample),
            MatchAudioCue.Injury => Injury(time),
            MatchAudioCue.ResultWin => Result(time, result: 1),
            MatchAudioCue.ResultLoss => Result(time, result: -1),
            _ => Result(time, result: 0),
        };
    }

    private static float Button(double time)
    {
        var envelope = AttackRelease(time, 0.09, 0.004, 0.065);
        var chirp = Math.Sin(Math.Tau * (720 + (time * 1450)) * time)
            + (0.34 * Math.Sin(Math.Tau * 1120 * time));
        return (float)(chirp * envelope * 0.38);
    }

    private static float Whistle(double time)
    {
        var envelope = AttackRelease(time, 0.62, 0.018, 0.18);
        var vibrato = 1 + (0.012 * Math.Sin(Math.Tau * 17 * time));
        var body = Math.Sin(Math.Tau * 1850 * vibrato * time)
            + (0.42 * Math.Sin((Math.Tau * 2220 * vibrato * time) + 0.3));
        return (float)(body * envelope * 0.34);
    }

    private static float Goal(double time, int sample)
    {
        var rise = Math.Clamp(time / 0.46, 0, 1);
        var tonal = Math.Sin(Math.Tau * (190 + (360 * rise)) * time)
            + (0.55 * Math.Sin(Math.Tau * (285 + (520 * rise)) * time));
        var cheer = SmoothNoise(sample, 701) * (0.25 + (0.75 * rise));
        var envelope = AttackRelease(time, 1.15, 0.012, 0.34);
        return (float)(((tonal * 0.30) + (cheer * 0.48)) * envelope);
    }

    private static float YellowCard(double time, int sample)
    {
        var first = GaussianPulse(time, 0.025, 0.012);
        var second = GaussianPulse(time, 0.145, 0.016);
        var click = SmoothNoise(sample, 211) * ((first * 0.55) + (second * 0.42));
        var tone = Math.Sin(Math.Tau * 840 * time) * AttackRelease(time, 0.28, 0.004, 0.15) * 0.22;
        return (float)(click + tone);
    }

    private static float RedCard(double time, int sample)
    {
        var hit = GaussianPulse(time, 0.035, 0.018);
        var warning = Math.Sin(Math.Tau * (310 - (time * 170)) * time)
            + (0.35 * Math.Sin(Math.Tau * 155 * time));
        var envelope = AttackRelease(time, 0.48, 0.006, 0.28);
        return (float)((warning * envelope * 0.42) + (SmoothNoise(sample, 419) * hit * 0.28));
    }

    private static float Injury(double time)
    {
        var fall = Math.Sin(Math.Tau * (520 - (time * 360)) * time)
            + (0.38 * Math.Sin(Math.Tau * (260 - (time * 120)) * time));
        return (float)(fall * AttackRelease(time, 0.72, 0.012, 0.38) * 0.38);
    }

    private static float Result(double time, int result)
    {
        var noteStep = Math.Min(2, (int)(time / 0.28));
        var notes = result switch
        {
            > 0 => WinResultNotes,
            < 0 => LossResultNotes,
            _ => DrawResultNotes,
        };
        var frequency = notes[noteStep];
        var localTime = time - (noteStep * 0.28);
        var noteEnvelope = Math.Exp(-Math.Max(0, localTime) * 3.3);
        var tail = AttackRelease(time, 1.12, 0.008, 0.30);
        var tone = Math.Sin(Math.Tau * frequency * time)
            + (0.25 * Math.Sin(Math.Tau * frequency * 2 * time));
        return (float)(tone * noteEnvelope * tail * 0.36);
    }

    private static AudioStreamWav Render(
        double seconds,
        Func<double, int, float> sampleFactory,
        bool loop)
    {
        var sampleCount = Math.Max(1, (int)Math.Round(seconds * SampleRate));
        var data = new byte[sampleCount * sizeof(short)];
        for (var sample = 0; sample < sampleCount; sample++)
        {
            var value = Math.Clamp(sampleFactory(sample / (double)SampleRate, sample), -SafePeak, SafePeak);
            var pcm = (short)Math.Round(value * short.MaxValue);
            data[sample * 2] = unchecked((byte)(pcm & 0xFF));
            data[(sample * 2) + 1] = unchecked((byte)((pcm >> 8) & 0xFF));
        }

        return new AudioStreamWav
        {
            Data = data,
            Format = AudioStreamWav.FormatEnum.Format16Bits,
            MixRate = SampleRate,
            Stereo = false,
            LoopMode = loop
                ? AudioStreamWav.LoopModeEnum.Forward
                : AudioStreamWav.LoopModeEnum.Disabled,
            LoopBegin = 0,
            LoopEnd = sampleCount,
        };
    }

    private static double AttackRelease(
        double time,
        double duration,
        double attack,
        double release)
    {
        var attackGain = Math.Clamp(time / Math.Max(0.001, attack), 0, 1);
        var remaining = Math.Max(0, duration - time);
        var releaseGain = Math.Clamp(remaining / Math.Max(0.001, release), 0, 1);
        return attackGain * releaseGain;
    }

    private static double GaussianPulse(double time, double center, double width)
    {
        var distance = (time - center) / Math.Max(0.001, width);
        return Math.Exp(-(distance * distance));
    }

    private static double HarmonicNoise(
        double loopPhase,
        int seed,
        int bandCount,
        int firstCycle,
        int spacing)
    {
        var value = 0d;
        var weight = 0d;
        for (var band = 0; band < bandCount; band++)
        {
            var cycle = firstCycle + (band * spacing) + (int)(Hash(seed, band) % 5u);
            var amplitude = 1d / Math.Sqrt(1 + band);
            value += Math.Sin((Math.Tau * cycle * loopPhase) + SeedPhase(seed, band + 101)) * amplitude;
            weight += amplitude;
        }

        return weight <= 0 ? 0 : value / weight;
    }

    private static double SeedPhase(int seed, int salt) =>
        (Hash(seed, salt) / (double)uint.MaxValue) * Math.Tau;

    private static float SmoothNoise(int sample, int seed) =>
        (Noise(sample, seed) + (Noise(sample - 1, seed) * 0.72f) + (Noise(sample - 2, seed) * 0.38f)) / 2.1f;

    private static float Noise(int sample, int seed)
    {
        var hash = Hash(seed, sample);
        return ((hash & 0xFFFFu) / 32767.5f) - 1f;
    }

    private static uint Hash(int seed, int salt)
    {
        var value = unchecked((uint)seed) ^ (unchecked((uint)salt) * 0x9E3779B9u);
        value ^= value >> 16;
        value *= 0x7FEB352Du;
        value ^= value >> 15;
        value *= 0x846CA68Bu;
        value ^= value >> 16;
        return value;
    }
}
