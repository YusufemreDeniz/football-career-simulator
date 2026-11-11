using FootballCareerSimulator.Domain.Shared;

namespace FootballCareerSimulator.Application.ClubGovernance.Queries;

/// <summary>
/// Kulüp ekranında ve yönetim kurulu iletişiminde sunulan kurumsal kimlik özeti (Yenilik 5: Club DNA).
/// </summary>
public sealed record ClubCultureDigest(
    ClubId ClubId,
    IReadOnlyList<string> CultureBadges,
    string PhilosophySummary,
    int YouthCultureScore,
    int ManagerialPatienceScore,
    int WageDisciplineScore);
