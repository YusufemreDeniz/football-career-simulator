using FootballCareerSimulator.Application.ClubGovernance.Services;
using FootballCareerSimulator.Domain.ClubGovernance;
using FootballCareerSimulator.Domain.Shared;

namespace FootballCareerSimulator.Tests.ClubGovernance;

public sealed class ClubDnaCalculationTests
{
    private static readonly ClubId TestClubId = new(1);

    [Fact]
    public void ClubDnaProfile_InitialState_IsNeutral()
    {
        var profile = ClubDnaProfile.CreateDefault(TestClubId);
        var service = new ClubDnaService();

        Assert.Equal(ClubDnaProfile.DefaultNeutralScore, profile.YouthDevelopmentCulture);
        Assert.Equal(ClubDnaProfile.DefaultNeutralScore, profile.RecruitmentAggression);
        Assert.Equal(ClubDnaProfile.DefaultNeutralScore, profile.SellingClubTendency);
        Assert.Equal(ClubDnaProfile.DefaultNeutralScore, profile.ManagerialPatience);
        Assert.Equal(ClubDnaProfile.DefaultNeutralScore, profile.WageDiscipline);
        Assert.Equal(ClubDnaProfile.DefaultNeutralScore, profile.HighIntensityTradition);
        Assert.Equal(0, profile.SeasonsTracked);

        var digest = service.BuildDigest(TestClubId);
        Assert.Equal("Dengeli Kurumsal Yapı", digest.PhilosophySummary);
        Assert.Empty(digest.CultureBadges);
    }

    [Fact]
    public void ClubDnaProfile_YouthHeavySeasons_RaisesYouthCultureAndAwardsBadge()
    {
        var profile = ClubDnaProfile.CreateDefault(TestClubId);
        var service = new ClubDnaService();
        service.RegisterProfile(profile);

        // 2 consecutive seasons with high youth minutes (60% and 55%)
        profile.RecordSeasonFacts(
            youthMinutesRatioPercent: 65,
            netTransferSpendMillion: 0,
            managersDismissedCount: 0,
            wageUtilizationPercent: 85);

        profile.RecordSeasonFacts(
            youthMinutesRatioPercent: 65,
            netTransferSpendMillion: 0,
            managersDismissedCount: 0,
            wageUtilizationPercent: 85);

        Assert.True(profile.YouthDevelopmentCulture >= 65);
        Assert.Equal(2, profile.SeasonsTracked);

        var digest = service.BuildDigest(TestClubId);
        Assert.Contains("Akademi Geleneği", digest.CultureBadges);
    }

    [Fact]
    public void ClubDnaProfile_MultipleDismissals_LowersManagerialPatienceAndAwardsImpatientBadge()
    {
        var profile = ClubDnaProfile.CreateDefault(TestClubId);
        var service = new ClubDnaService();
        service.RegisterProfile(profile);

        // Season with 2 manager sackings
        profile.RecordSeasonFacts(
            youthMinutesRatioPercent: 25,
            netTransferSpendMillion: 10,
            managersDismissedCount: 2,
            wageUtilizationPercent: 90);

        Assert.Equal(26, profile.ManagerialPatience); // 50 - 24 = 26

        var digest = service.BuildDigest(TestClubId);
        Assert.Contains("Sabırsız Yönetim", digest.CultureBadges);
    }

    [Fact]
    public void ClubDnaProfile_NetTransferProfit_RaisesSellingTendency()
    {
        var profile = ClubDnaProfile.CreateDefault(TestClubId);
        var service = new ClubDnaService();
        service.RegisterProfile(profile);

        // Net transfer spend = -60M (60M net profit from sales)
        profile.RecordSeasonFacts(
            youthMinutesRatioPercent: 25,
            netTransferSpendMillion: -60,
            managersDismissedCount: 0,
            wageUtilizationPercent: 80);

        Assert.Equal(65, profile.SellingClubTendency); // 50 + 15 = 65

        var digest = service.BuildDigest(TestClubId);
        Assert.Contains("Yetiştirici / Satıcı Kulüp", digest.CultureBadges);
    }

    [Fact]
    public void ClubDnaProfile_ScoreClamping_DoesNotExceedBounds()
    {
        var profile = new ClubDnaProfile(
            TestClubId,
            youthDevelopmentCulture: 95,
            recruitmentAggression: 5,
            sellingClubTendency: 50,
            managerialPatience: 50,
            wageDiscipline: 50,
            highIntensityTradition: 50);

        profile.RecordSeasonFacts(
            youthMinutesRatioPercent: 100,
            netTransferSpendMillion: -100,
            managersDismissedCount: 0,
            wageUtilizationPercent: 50);

        Assert.Equal(ClubDnaProfile.MaxScore, profile.YouthDevelopmentCulture);
        Assert.Equal(ClubDnaProfile.MinScore, profile.RecruitmentAggression);
    }
}
