using FootballCareerSimulator.Application.ManagerCareer.Services;
using FootballCareerSimulator.Domain.ManagerCareer;
using FootballCareerSimulator.Domain.TeamPreparation;
using FootballCareerSimulator.Domain.WorldCalendar;

namespace FootballCareerSimulator.Tests.ManagerCareerLineage;

public sealed class PlayerToManagerTransitionTests
{
    private static readonly ManagerId UserManagerId = new(1);
    private static readonly ManagerId NewManagerId = new(201);
    private static readonly GameDate TransitionDay = GameDate.FromCalendarDate(2028, 6, 15);

    [Fact]
    public void TransitionPlayerToManager_CreatesValidPedigree()
    {
        var service = new CoachingLineageService();

        var pedigree = service.TransitionPlayerToManager(
            newManagerId: NewManagerId,
            originPlayerId: 1001,
            fullName: "Emre Kaptan",
            mentorManagerId: UserManagerId,
            mentorManagerName: "Yusuf Deniz",
            favoredFormation: Formation.F433,
            leadershipScore: 85,
            day: TransitionDay);

        Assert.Equal(NewManagerId, pedigree.ManagerId);
        Assert.Equal(1001, pedigree.OriginPlayerId);
        Assert.Equal("Emre Kaptan", pedigree.FullName);
        Assert.Equal(UserManagerId, pedigree.MentoredByManagerId);
        Assert.Equal(Formation.F433, pedigree.FavoredFormation);
        Assert.Equal(85, pedigree.LeadershipReputation);
        Assert.True(pedigree.HasMentor);
    }

    [Fact]
    public void LineageTreeDigest_WhenFormerPupilOfUser_FlagsBadgeAndSummary()
    {
        var service = new CoachingLineageService();

        service.TransitionPlayerToManager(
            newManagerId: NewManagerId,
            originPlayerId: 1001,
            fullName: "Emre Kaptan",
            mentorManagerId: UserManagerId,
            mentorManagerName: "Yusuf Deniz",
            favoredFormation: Formation.F433,
            leadershipScore: 85,
            day: TransitionDay);

        var digest = service.BuildDigest(NewManagerId, UserManagerId);

        Assert.True(digest.IsFormerPupilOfUser);
        Assert.Equal("Eski Öğrenciniz", digest.PedigreeBadge);
        Assert.Contains("sizin tedrisatınızdan geçmiş", digest.NarrativeSummary);
    }

    [Fact]
    public void LineageTreeDigest_WhenOtherMentor_ReflectsLineageSchool()
    {
        var service = new CoachingLineageService();
        var rivalMentorId = new ManagerId(99);

        service.TransitionPlayerToManager(
            newManagerId: NewManagerId,
            originPlayerId: 1002,
            fullName: "Mehmet Lider",
            mentorManagerId: rivalMentorId,
            mentorManagerName: "Fatih Terim",
            favoredFormation: Formation.F352,
            leadershipScore: 90,
            day: TransitionDay);

        var digest = service.BuildDigest(NewManagerId, UserManagerId);

        Assert.False(digest.IsFormerPupilOfUser);
        Assert.Equal("Fatih Terim Ekolü", digest.PedigreeBadge);
        Assert.Contains("Fatih Terim'ın taktiksel mirasını", digest.NarrativeSummary);
    }

    [Fact]
    public void CoachingPedigree_InvariantValidation_ThrowsOnInvalidId()
    {
        Assert.Throws<ManagerCareerInvariantViolationException>(() =>
            new CoachingPedigree(
                NewManagerId,
                originPlayerId: 0,
                fullName: "Test",
                mentoredByManagerId: null,
                mentoredByManagerName: null,
                Formation.F442,
                leadershipReputation: 50,
                TransitionDay));
    }
}
