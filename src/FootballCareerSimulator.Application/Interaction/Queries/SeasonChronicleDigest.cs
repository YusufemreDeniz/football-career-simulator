using FootballCareerSimulator.Domain.Interaction;

namespace FootballCareerSimulator.Application.Interaction.Queries;

/// <summary>
/// Sezon sonunda üretilen deterministik kariyer belgeseli okuma modeli (Yenilik 10: Career Chronicle).
/// </summary>
public sealed record SeasonChronicleDigest(
    int SeasonYear,
    string SeasonTitle,
    string DramaticArcNarrative,
    int FinalLeaguePosition,
    bool WonTitle,
    IReadOnlyList<NarrativeFactRecord> KeyMilestones,
    string SeasonVerdict);
