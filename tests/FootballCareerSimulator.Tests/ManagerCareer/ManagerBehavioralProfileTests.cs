using FootballCareerSimulator.Application.ManagerCareer.Services;
using FootballCareerSimulator.Domain.ManagerCareer;

namespace FootballCareerSimulator.Tests.ManagerBehavioral;

public sealed class ManagerBehavioralProfileTests
{
    [Fact]
    public void ManagerBehavioralProfile_InitialState_IsNeutral()
    {
        var profile = new ManagerBehavioralProfile();
        var service = new ManagerBehavioralQueryService();

        Assert.Equal(ManagerBehavioralProfile.NeutralScore, profile.YouthOpportunity);
        Assert.Equal(ManagerBehavioralProfile.NeutralScore, profile.PromiseReliability);
        Assert.Equal(ManagerBehavioralProfile.NeutralScore, profile.PublicSupport);
        Assert.Equal(ManagerBehavioralProfile.NeutralScore, profile.NegotiationPersistence);
        Assert.Equal(ManagerBehavioralProfile.NeutralScore, profile.RotationConsistency);
        Assert.Equal(ManagerBehavioralProfile.NeutralScore, profile.DeadlineAggression);
        Assert.Empty(profile.Observations);

        var readModel = service.GetProfile(profile);
        Assert.Empty(readModel.StyleBadges);
        Assert.Equal(0, readModel.TotalObservationsCount);
    }

    [Fact]
    public void ManagerBehavioralProfile_YouthOpportunityEvidence_RaisesScoreAndAwardsBadge()
    {
        var profile = new ManagerBehavioralProfile();
        var service = new ManagerBehavioralQueryService();

        // Scenario A slice: Manager gives starting appearances to young players
        profile.AddObservation(new BehavioralObservation(
            "YouthStart_1",
            BehavioralEvidenceCategory.YouthOpportunity,
            +6,
            10,
            "Genç futbolcu ilk 11 başladı"));

        profile.AddObservation(new BehavioralObservation(
            "YouthStart_2",
            BehavioralEvidenceCategory.YouthOpportunity,
            +6,
            14,
            "Genç futbolcu ilk 11 başladı"));

        profile.AddObservation(new BehavioralObservation(
            "YouthStart_3",
            BehavioralEvidenceCategory.YouthOpportunity,
            +6,
            21,
            "Genç futbolcu ilk 11 başladı"));

        Assert.Equal(68, profile.YouthOpportunity);
        Assert.Equal(3, profile.Observations.Count);

        var readModel = service.GetProfile(profile);
        Assert.Contains("Genç Yetenek Hamisi", readModel.StyleBadges);
    }

    [Fact]
    public void ManagerBehavioralProfile_PromiseBroken_LowersScoreAndAwardsRiskBadge()
    {
        var profile = new ManagerBehavioralProfile();
        var service = new ManagerBehavioralQueryService();

        profile.AddObservation(new BehavioralObservation(
            "PromiseBroken_1",
            BehavioralEvidenceCategory.PromiseReliability,
            -10,
            30,
            "Forma sözü tutulmadı"));

        profile.AddObservation(new BehavioralObservation(
            "PromiseBroken_2",
            BehavioralEvidenceCategory.PromiseReliability,
            -8,
            45,
            "Forma sözü tutulmadı"));

        Assert.Equal(32, profile.PromiseReliability);

        var readModel = service.GetProfile(profile);
        Assert.Contains("Vaatleri Riskli", readModel.StyleBadges);
    }

    [Fact]
    public void ManagerBehavioralProfile_ScoreClamping_DoesNotExceedBounds()
    {
        var profile = new ManagerBehavioralProfile();

        profile.AddObservation(new BehavioralObservation(
            "MegaBoost",
            BehavioralEvidenceCategory.PublicSupport,
            +200,
            1,
            "Aşırı destek"));

        Assert.Equal(ManagerBehavioralProfile.MaxScore, profile.PublicSupport);

        profile.AddObservation(new BehavioralObservation(
            "MegaPenalty",
            BehavioralEvidenceCategory.PublicSupport,
            -300,
            2,
            "Aşırı ceza"));

        Assert.Equal(ManagerBehavioralProfile.MinScore, profile.PublicSupport);
    }

    [Fact]
    public void ManagerBehavioralProfile_ObservationLimit_EnforcesCapacity()
    {
        var profile = new ManagerBehavioralProfile();

        for (var i = 0; i < ManagerBehavioralProfile.MaxBoundedObservations + 10; i++)
        {
            profile.AddObservation(new BehavioralObservation(
                $"Obs_{i}",
                BehavioralEvidenceCategory.RotationConsistency,
                +1,
                i + 1,
                "Rotasyon"));
        }

        Assert.Equal(ManagerBehavioralProfile.MaxBoundedObservations, profile.Observations.Count);
        Assert.Equal("Obs_10", profile.Observations[0].ObservationCode);
    }
}
