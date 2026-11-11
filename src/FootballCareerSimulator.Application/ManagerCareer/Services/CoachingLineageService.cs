using FootballCareerSimulator.Application.ManagerCareer.Queries;
using FootballCareerSimulator.Domain.ManagerCareer;
using FootballCareerSimulator.Domain.TeamPreparation;
using FootballCareerSimulator.Domain.WorldCalendar;

namespace FootballCareerSimulator.Application.ManagerCareer.Services;

/// <summary>
/// Emekli futbolcuların teknik direktörlük kariyerine geçişini ve soy ağacı ilişkilerini yöneten servis (Yenilik 8).
/// </summary>
public sealed class CoachingLineageService
{
    private readonly Dictionary<ManagerId, CoachingPedigree> _pedigrees = new();

    public CoachingPedigree TransitionPlayerToManager(
        ManagerId newManagerId,
        long originPlayerId,
        string fullName,
        ManagerId? mentorManagerId,
        string? mentorManagerName,
        Formation favoredFormation,
        int leadershipScore,
        GameDate day)
    {
        var pedigree = new CoachingPedigree(
            newManagerId,
            originPlayerId,
            fullName,
            mentorManagerId,
            mentorManagerName,
            favoredFormation,
            leadershipScore,
            day);

        _pedigrees[newManagerId] = pedigree;
        return pedigree;
    }

    public CoachingPedigree? GetPedigree(ManagerId managerId)
    {
        _pedigrees.TryGetValue(managerId, out var pedigree);
        return pedigree;
    }

    public LineageTreeDigest BuildDigest(ManagerId targetManagerId, ManagerId userManagerId)
    {
        if (!_pedigrees.TryGetValue(targetManagerId, out var pedigree))
        {
            return new LineageTreeDigest(
                targetManagerId,
                FullName: "Bilinmeyen Menajer",
                OriginPlayerId: 0,
                IsFormerPupilOfUser: false,
                MentorName: null,
                FavoredFormation: Formation.F442,
                PedigreeBadge: "Geleneksel Menajer",
                NarrativeSummary: "Ligin geleneksel menajer havuzundan gelmektedir.");
        }

        var isFormerPupil = pedigree.MentoredByManagerId == userManagerId;
        var badge = isFormerPupil
            ? "Eski Öğrenciniz"
            : (pedigree.HasMentor ? $"{pedigree.MentoredByManagerName} Ekolü" : "Eski Profesyonel Futbolcu");

        var summary = isFormerPupil
            ? $"Geçmişte sizin tedrisatınızdan geçmiş eski futbolcunuz ({pedigree.FullName})."
            : (pedigree.HasMentor
                ? $"Eski hocası {pedigree.MentoredByManagerName}'ın taktiksel mirasını ({pedigree.FavoredFormation}) benimsemiştir."
                : $"Uzun yıllar profesyonel futbol oynadıktan sonra menajerliğe adım atmıştır.");

        return new LineageTreeDigest(
            targetManagerId,
            pedigree.FullName,
            pedigree.OriginPlayerId,
            isFormerPupil,
            pedigree.MentoredByManagerName,
            pedigree.FavoredFormation,
            badge,
            summary);
    }
}
