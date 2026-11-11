using FootballCareerSimulator.Application.Competition.Queries;

namespace FootballCareerSimulator.Tests.Competition;

public sealed class MorningHeadlineTests
{
    [Fact]
    public void Compose_DominantWin_Praises()
    {
        Assert.Equal(
            "Sabah manşeti: \"Rakibe nefes aldırmadılar — şehir zevk uyandı.\"",
            MorningHeadline.Compose(managedGoalMargin: 3, afterWhistleLines: null));
    }

    [Fact]
    public void Compose_NarrowWin_ThreePointsTone()
    {
        Assert.Equal(
            "Sabah manşeti: \"Üç puan — şehir mutlu uyandı.\"",
            MorningHeadline.Compose(managedGoalMargin: 1, afterWhistleLines: null));
    }

    [Fact]
    public void Compose_Draw_NoWinnerTone()
    {
        Assert.Equal(
            "Sabah manşeti: \"Puanlar paylaşıldı — kazanan çıkmadı.\"",
            MorningHeadline.Compose(managedGoalMargin: 0, afterWhistleLines: null));
    }

    [Fact]
    public void Compose_HeavyLoss_ReactionTone()
    {
        Assert.Equal(
            "Sabah manşeti: \"Sahada dağıldılar — tepki dinmeyecek.\"",
            MorningHeadline.Compose(managedGoalMargin: -4, afterWhistleLines: null));
    }

    [Fact]
    public void Compose_NarrowLoss_SurpriseTone()
    {
        Assert.Equal(
            "Sabah manşeti: \"Sürpriz kayıp — taraftar soracak.\"",
            MorningHeadline.Compose(managedGoalMargin: -1, afterWhistleLines: null));
    }

    [Fact]
    public void Compose_Dismissed_OverridesResult()
    {
        Assert.Equal(
            "Sabah manşeti: \"Koltuk gitti — yönetim sabırsızdı.\"",
            MorningHeadline.Compose(
                managedGoalMargin: 2,
                afterWhistleLines: ["Yönetim seni işten çıkardı."]));
    }

    [Fact]
    public void Compose_PressQuestion_OverridesResult()
    {
        Assert.Equal(
            "Sabah manşeti: \"Basın kapıda — sorular sert olacak.\"",
            MorningHeadline.Compose(
                managedGoalMargin: -1,
                afterWhistleLines: ["Basın sorusu açıldı."]));
    }

    [Fact]
    public void Compose_NoManagedMargin_ReturnsNull()
    {
        Assert.Null(MorningHeadline.Compose(managedGoalMargin: null, afterWhistleLines: null));
    }
}
