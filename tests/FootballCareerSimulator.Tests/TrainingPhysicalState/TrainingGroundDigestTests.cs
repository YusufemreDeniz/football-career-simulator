using FootballCareerSimulator.Application.TrainingPhysicalState.Queries;
using FootballCareerSimulator.Domain.TrainingPhysicalState;
using Xunit;

namespace FootballCareerSimulator.Tests.TrainingPhysicalState;

public class TrainingGroundDigestTests
{
    private static ClubTrainingSummaryReadModel Training(
        int fatigue,
        int fitness,
        int injured = 0,
        TrainingIntensity? intensity = TrainingIntensity.Medium,
        TrainingFocus? focus = TrainingFocus.General,
        RestApproach? rest = RestApproach.Normal,
        bool hasPlan = true,
        long? clubId = 1) =>
        new(
            ClubId: clubId,
            Focus: focus is null ? null : (int)focus,
            Intensity: intensity is null ? null : (int)intensity,
            RestApproach: rest is null ? null : (int)rest,
            FocusName: null,
            IntensityName: null,
            RestApproachName: null,
            SetAtDayNumber: hasPlan ? 1 : null,
            AverageFatigue: hasPlan ? fatigue : null,
            AverageFitness: hasPlan ? fitness : null,
            HasPlan: hasPlan,
            InjuredSlotCount: injured,
            UnavailableSlotCount: injured);

    [Fact]
    public void Unemployed_NoVoice()
    {
        var digest = TrainingGroundDigest.Compose(Training(40, 60, clubId: null));

        Assert.Null(digest);
    }

    [Fact]
    public void NoPlan_NoVoice()
    {
        var digest = TrainingGroundDigest.Compose(Training(40, 60, hasPlan: false));

        Assert.Null(digest);
    }

    [Fact]
    public void InjuryHeavy_VoiceSurfacesMedicalPressure()
    {
        var digest = TrainingGroundDigest.Compose(Training(30, 70, injured: 3));

        Assert.NotNull(digest);
        Assert.Equal(TrainingGroundDigest.Brand, digest!.BrandTitle);
        Assert.Contains("Sakat sayısı arttı", digest.VoiceLine, StringComparison.Ordinal);
    }

    [Fact]
    public void HighFatigueNearMatch_UrgesLoadCut()
    {
        var digest = TrainingGroundDigest.Compose(
            Training(72, 45),
            daysUntilNextMatch: 1);

        Assert.NotNull(digest);
        Assert.Contains("maç yakınken yükü düşür", digest!.VoiceLine, StringComparison.Ordinal);
    }

    [Fact]
    public void HighFatigue_SilenceOnThePitch()
    {
        var digest = TrainingGroundDigest.Compose(
            Training(66, 50),
            daysUntilNextMatch: 6);

        Assert.NotNull(digest);
        Assert.Contains("Yorgunluk sızıyor", digest!.VoiceLine, StringComparison.Ordinal);
    }

    [Fact]
    public void PeakFitness_PositiveVoice()
    {
        var digest = TrainingGroundDigest.Compose(Training(25, 85));

        Assert.NotNull(digest);
        Assert.Contains("Form zirvede", digest!.VoiceLine, StringComparison.Ordinal);
    }

    [Fact]
    public void HighIntensity_HardWorkVoice()
    {
        var digest = TrainingGroundDigest.Compose(Training(45, 60, intensity: TrainingIntensity.High));

        Assert.NotNull(digest);
        Assert.Contains("Sert çalıştık", digest!.VoiceLine, StringComparison.Ordinal);
    }

    [Fact]
    public void RecoveryFocus_CalmVoice()
    {
        var digest = TrainingGroundDigest.Compose(Training(35, 55, focus: TrainingFocus.Recovery));

        Assert.NotNull(digest);
        Assert.Contains("Toparlanma havası", digest!.VoiceLine, StringComparison.Ordinal);
    }

    [Fact]
    public void BalancedPlan_TrackVoice()
    {
        var digest = TrainingGroundDigest.Compose(Training(35, 65));

        Assert.NotNull(digest);
        Assert.Contains("Plan oturuyor", digest!.VoiceLine, StringComparison.Ordinal);
    }

    [Fact]
    public void InjuryPriority_OverridesFitness()
    {
        var digest = TrainingGroundDigest.Compose(Training(20, 90, injured: 4));

        Assert.NotNull(digest);
        Assert.Contains("Sakat sayısı arttı", digest!.VoiceLine, StringComparison.Ordinal);
    }
}
