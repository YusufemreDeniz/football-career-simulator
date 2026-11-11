using FootballCareerSimulator.Domain.ClubGovernance;
using FootballCareerSimulator.Domain.Competition;
using FootballCareerSimulator.Domain.ManagerCareer;
using FootballCareerSimulator.Domain.Shared;
using FootballCareerSimulator.Domain.WorldCalendar;

namespace FootballCareerSimulator.Tests.ManagerCareerBoard;

public sealed class ManagerProgressionAndDnaTests
{
    private static readonly GameDate Day = GameDate.FromCalendarDate(2026, 8, 15);

    [Fact]
    public void StartingBackground_SetsDistinctSkillArchetypes()
    {
        var tacticalManager = Domain.ManagerCareer.ManagerCareer.CreateAwaitingInitialEmployment(
            new ManagerId(1), "Tactic Master", StartingBackground.TacticalSpecialist);
        var youthCoach = Domain.ManagerCareer.ManagerCareer.CreateAwaitingInitialEmployment(
            new ManagerId(2), "Youth Mentor", StartingBackground.YouthAcademyCoach);
        var exPlayer = Domain.ManagerCareer.ManagerCareer.CreateAwaitingInitialEmployment(
            new ManagerId(3), "Legend Leader", StartingBackground.RecentlyRetiredPlayer);

        Assert.Equal(75, tacticalManager.Progression.Skills.TacticalAcumen);
        Assert.Equal(75, youthCoach.Progression.Skills.YouthDevelopment);
        Assert.Equal(75, exPlayer.Progression.Skills.ManManagement);
    }

    [Fact]
    public void AddExperience_LevelsUpAndImprovesSkillAttributes()
    {
        var manager = Domain.ManagerCareer.ManagerCareer.StartNewCareer(
            new ManagerId(10), "Rising Star", new ClubId(1), Day, SeasonExpectationTier.MidTable);

        Assert.Equal(1, manager.Progression.Level);
        var initialTactical = manager.Progression.Skills.TacticalAcumen;

        // Level 2 requires 500 XP
        var leveledUp = manager.AddExperience(550);

        Assert.Equal(2, leveledUp.Progression.Level);
        Assert.Equal(550, leveledUp.Progression.ExperiencePoints);
        Assert.Equal(initialTactical + 1, leveledUp.Progression.Skills.TacticalAcumen);
    }

    [Fact]
    public void ApplyMatchBoardAssessment_AwardsMatchXpToProgression()
    {
        var manager = Domain.ManagerCareer.ManagerCareer.StartNewCareer(
            new ManagerId(11), "Match Manager", new ClubId(1), Day, SeasonExpectationTier.MidTable);

        var result = manager.ApplyMatchBoardAssessment(
            new FixtureId(101),
            MatchOutcomeForManagedClub.Win,
            leaguePosition: 5,
            leagueSize: 18);

        Assert.True(result.WasApplied);
        Assert.Equal(ManagerProgression.WinXp, result.Career.Progression.ExperiencePoints);
    }

    [Fact]
    public void ImpatientBoardDna_DeductsExtraConfidenceOnLoss()
    {
        var manager = Domain.ManagerCareer.ManagerCareer.StartNewCareer(
            new ManagerId(20), "Pressured Boss", new ClubId(1), Day, SeasonExpectationTier.MidTable);

        // Sabırsız yönetim: ManagerialPatience = 20
        var impatientDna = new ClubDnaProfile(new ClubId(1), managerialPatience: 20);

        var standardAssessment = manager.ApplyMatchBoardAssessment(
            new FixtureId(201),
            MatchOutcomeForManagedClub.Loss,
            leaguePosition: 15,
            leagueSize: 18);

        var dnaAssessment = manager.ApplyMatchBoardAssessment(
            new FixtureId(202),
            MatchOutcomeForManagedClub.Loss,
            leaguePosition: 15,
            leagueSize: 18,
            impatientDna);

        // Standart mağlubiyet (beklenti altı) = -6
        // Sabırsız yönetim mağlubiyeti = -8 (-6 - 2)
        Assert.Equal(-6, standardAssessment.ConfidenceDelta);
        Assert.Equal(-8, dnaAssessment.ConfidenceDelta);
        Assert.Contains("ImpatientBoard", dnaAssessment.ReasonCode);
    }

    [Fact]
    public void PatientBoardDna_SoftensLossConfidencePenalty()
    {
        var manager = Domain.ManagerCareer.ManagerCareer.StartNewCareer(
            new ManagerId(21), "Trusted Boss", new ClubId(1), Day, SeasonExpectationTier.MidTable);

        // Sabırlı yönetim: ManagerialPatience = 85
        var patientDna = new ClubDnaProfile(new ClubId(1), managerialPatience: 85);

        var assessment = manager.ApplyMatchBoardAssessment(
            new FixtureId(203),
            MatchOutcomeForManagedClub.Loss,
            leaguePosition: 5, // Beklenti dahilinde
            leagueSize: 18,
            patientDna);

        // Standart kayıp (on track) = -3
        // Sabırlı yönetim = -1 (-3 + 2)
        Assert.Equal(-1, assessment.ConfidenceDelta);
        Assert.Contains("PatientBoard", assessment.ReasonCode);
    }
}
