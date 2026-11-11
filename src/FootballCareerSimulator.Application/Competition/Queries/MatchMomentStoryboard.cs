namespace FootballCareerSimulator.Application.Competition.Queries;

using FootballCareerSimulator.Application.Competition.Commands;
using FootballCareerSimulator.Domain.Match;

/// <summary>
/// Normalized pitch coordinate used by the presentation layer. Values are always
/// clamped to the visible 0..1 pitch area.
/// </summary>
public readonly record struct MatchMomentPitchPoint(float X, float Y)
{
    public MatchMomentPitchPoint Clamped() =>
        new(Math.Clamp(X, 0f, 1f), Math.Clamp(Y, 0f, 1f));
}

/// <summary>
/// A renderer-neutral, deterministic frame for one critical match moment.
/// </summary>
public sealed record MatchMomentStoryboardFrame(
    int SequenceIndex,
    string Kind,
    int Minute,
    bool IsHomeSide,
    int PrimarySlotIndex,
    int? AssistSlotIndex,
    string PrimaryPlayerName,
    string? AssistPlayerName,
    MatchMomentPitchPoint ActorPosition,
    MatchMomentPitchPoint? SupportPosition,
    MatchMomentPitchPoint BallStart,
    MatchMomentPitchPoint BallEnd);

/// <summary>
/// Converts match key-moment read models into a stable 2D storyboard. It uses no
/// engine state or process-wide random source, so the same moments and seed always
/// produce the same frame order and coordinates.
/// </summary>
public sealed record MatchMomentStoryboard(IReadOnlyList<MatchMomentStoryboardFrame> Frames)
{
    private static readonly MatchMomentPitchPoint[] HomeFormation =
    [
        new(0.08f, 0.50f),
        new(0.22f, 0.14f),
        new(0.19f, 0.38f),
        new(0.19f, 0.62f),
        new(0.22f, 0.86f),
        new(0.40f, 0.20f),
        new(0.36f, 0.42f),
        new(0.36f, 0.64f),
        new(0.42f, 0.80f),
        new(0.64f, 0.36f),
        new(0.66f, 0.64f),
    ];

    public static MatchMomentStoryboard Empty { get; } = new(Array.Empty<MatchMomentStoryboardFrame>());

    public static MatchMomentStoryboard Build(
        IReadOnlyList<MatchKeyMomentReadModel>? moments,
        int sequenceSeed = 0)
    {
        if (moments is null || moments.Count == 0)
        {
            return Empty;
        }

        var ordered = moments
            .Select((moment, sourceIndex) => (Moment: moment, SourceIndex: sourceIndex))
            .OrderBy(item => item.Moment.Minute)
            .ThenBy(item => item.SourceIndex)
            .ToArray();

        var frames = new MatchMomentStoryboardFrame[ordered.Length];
        for (var sequenceIndex = 0; sequenceIndex < ordered.Length; sequenceIndex++)
        {
            var moment = ordered[sequenceIndex].Moment;
            var actor = ResolvePlayerPosition(moment.PrimarySlotIndex, moment.IsHomeSide);
            var support = moment.AssistSlotIndex is int assistSlot
                ? ResolvePlayerPosition(assistSlot, moment.IsHomeSide)
                : (MatchMomentPitchPoint?)null;
            var variation = ResolveVariation(sequenceSeed, moment, ordered[sequenceIndex].SourceIndex);
            var isGoal = string.Equals(
                moment.Kind,
                nameof(MatchKeyMomentKind.Goal),
                StringComparison.OrdinalIgnoreCase);

            var ballStart = support ?? actor;
            var ballEnd = isGoal
                ? GoalMouth(moment.IsHomeSide, variation)
                : NearbyTarget(actor, moment.IsHomeSide, variation);

            frames[sequenceIndex] = new MatchMomentStoryboardFrame(
                sequenceIndex,
                NormalizeKind(moment.Kind),
                Math.Max(0, moment.Minute),
                moment.IsHomeSide,
                moment.PrimarySlotIndex,
                moment.AssistSlotIndex,
                PlayerName(moment.PrimaryPlayerName, moment.PrimarySlotIndex),
                moment.AssistSlotIndex is int supportSlot
                    ? PlayerName(moment.AssistPlayerName, supportSlot)
                    : null,
                actor,
                support,
                ballStart,
                ballEnd);
        }

        return new MatchMomentStoryboard(frames);
    }

    /// <summary>
    /// Returns the stable formation coordinate used for either side. Invalid slot
    /// values safely fall back to the nearest valid first-team slot.
    /// </summary>
    public static MatchMomentPitchPoint ResolvePlayerPosition(int slotIndex, bool isHomeSide)
    {
        var safeSlot = Math.Clamp(slotIndex, 0, HomeFormation.Length - 1);
        var home = HomeFormation[safeSlot];
        return isHomeSide
            ? home
            : new MatchMomentPitchPoint(1f - home.X, home.Y);
    }

    private static MatchMomentPitchPoint GoalMouth(bool homeAttacks, float variation)
    {
        var targetY = 0.5f + ((variation - 0.5f) * 0.30f);
        return new MatchMomentPitchPoint(homeAttacks ? 0.985f : 0.015f, targetY).Clamped();
    }

    private static MatchMomentPitchPoint NearbyTarget(
        MatchMomentPitchPoint actor,
        bool homeAttacks,
        float variation)
    {
        var direction = homeAttacks ? 1f : -1f;
        var x = actor.X + (direction * (0.055f + (variation * 0.035f)));
        var y = actor.Y + ((variation - 0.5f) * 0.20f);
        return new MatchMomentPitchPoint(
            Math.Clamp(x, 0.035f, 0.965f),
            Math.Clamp(y, 0.055f, 0.945f));
    }

    private static string NormalizeKind(string? kind)
    {
        if (string.Equals(kind, nameof(MatchKeyMomentKind.Goal), StringComparison.OrdinalIgnoreCase))
        {
            return nameof(MatchKeyMomentKind.Goal);
        }

        if (string.Equals(kind, nameof(MatchKeyMomentKind.YellowCard), StringComparison.OrdinalIgnoreCase))
        {
            return nameof(MatchKeyMomentKind.YellowCard);
        }

        if (string.Equals(kind, nameof(MatchKeyMomentKind.RedCard), StringComparison.OrdinalIgnoreCase))
        {
            return nameof(MatchKeyMomentKind.RedCard);
        }

        if (string.Equals(kind, nameof(MatchKeyMomentKind.Injury), StringComparison.OrdinalIgnoreCase))
        {
            return nameof(MatchKeyMomentKind.Injury);
        }

        return string.IsNullOrWhiteSpace(kind) ? "Moment" : kind.Trim();
    }

    private static string PlayerName(string? name, int slotIndex) =>
        string.IsNullOrWhiteSpace(name)
            ? $"Oyuncu #{Math.Max(0, slotIndex) + 1}"
            : name.Trim();

    private static float ResolveVariation(
        int sequenceSeed,
        MatchKeyMomentReadModel moment,
        int sourceIndex)
    {
        var hash = 2166136261u;
        hash = Mix(hash, sequenceSeed);
        hash = Mix(hash, moment.Minute);
        hash = Mix(hash, moment.PrimarySlotIndex);
        hash = Mix(hash, moment.AssistSlotIndex ?? -1);
        hash = Mix(hash, moment.IsHomeSide ? 1 : 0);
        hash = Mix(hash, sourceIndex);
        foreach (var character in moment.Kind ?? string.Empty)
        {
            hash ^= character;
            hash *= 16777619u;
        }

        hash ^= hash >> 16;
        hash *= 0x7FEB352Du;
        hash ^= hash >> 15;
        return (hash & 0x00FFFFFFu) / 16777215f;
    }

    private static uint Mix(uint hash, int value)
    {
        hash ^= unchecked((uint)value);
        hash *= 16777619u;
        return hash;
    }
}
