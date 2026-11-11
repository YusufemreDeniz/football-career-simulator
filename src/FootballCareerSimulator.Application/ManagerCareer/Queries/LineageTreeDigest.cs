using FootballCareerSimulator.Domain.ManagerCareer;
using FootballCareerSimulator.Domain.TeamPreparation;

namespace FootballCareerSimulator.Application.ManagerCareer.Queries;

/// <summary>
/// Menajer profilinde veya maç öncesi rakip analizinde soy ağacı ve hoca-öğrenci ilişkisini gösteren okuma modeli (Yenilik 8).
/// </summary>
public sealed record LineageTreeDigest(
    ManagerId ManagerId,
    string FullName,
    long OriginPlayerId,
    bool IsFormerPupilOfUser,
    string? MentorName,
    Formation FavoredFormation,
    string PedigreeBadge,
    string NarrativeSummary);
