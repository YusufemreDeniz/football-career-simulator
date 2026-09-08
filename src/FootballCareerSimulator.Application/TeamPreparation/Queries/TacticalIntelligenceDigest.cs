using FootballCareerSimulator.Domain.TeamPreparation;

namespace FootballCareerSimulator.Application.TeamPreparation.Queries;

/// <summary>
/// Maç öncesi hazırlık ekranında sunulan lig taktiksel trendi ve antitez istihbaratı (Yenilik 4: Dinamik Taktik Ekosistemi).
/// </summary>
public sealed record TacticalIntelligenceDigest(
    string CompetitionCode,
    int SeasonYear,
    string PrevailingMetaName,
    Formation DominantFormation,
    Formation RecommendedCounterFormation,
    TacticalApproach RecommendedCounterApproach,
    IReadOnlyList<string> TacticalBulletins);
