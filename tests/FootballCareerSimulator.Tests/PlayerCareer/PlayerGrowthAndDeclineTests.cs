using FootballCareerSimulator.Domain.PlayerCareer;
using FootballCareerSimulator.Domain.Shared;
using FootballCareerSimulator.Domain.WorldCalendar;

namespace FootballCareerSimulator.Tests.PlayerCareer;

public sealed class PlayerGrowthAndDeclineTests
{
    private static readonly GameDate Day = GameDate.FromCalendarDate(2026, 7, 1);

    [Fact]
    public void SinWave_YoungPlayerWithHighHeadroom_GetsStrongGain()
    {
        // 18 yaşında, CA: 60, PA: 85 (geniş tavan, gelişim çağında)
        var points = SinWaveGrowthCalculator.CalculateEffectivePoints(
            rawPoints: 10,
            currentAbility: 60,
            potentialAbility: 85,
            age: 18,
            peakAge: 24);

        // SinWave ve headroom çarpanıyla güçlü puan almalı
        Assert.True(points >= 5);
    }

    [Fact]
    public void SinWave_PlayerNearCeiling_GetsDiminishingReturns()
    {
        // 23 yaşında, CA: 84, PA: 85 (tavana 1 puan kalmış, doyum noktası)
        var nearCeilingPoints = SinWaveGrowthCalculator.CalculateEffectivePoints(
            rawPoints: 10,
            currentAbility: 84,
            potentialAbility: 85,
            age: 23,
            peakAge: 24);

        // 19 yaşında, CA: 60, PA: 85 (tavana 25 puan var)
        var youngPoints = SinWaveGrowthCalculator.CalculateEffectivePoints(
            rawPoints: 10,
            currentAbility: 60,
            potentialAbility: 85,
            age: 19,
            peakAge: 24);

        Assert.True(youngPoints > nearCeilingPoints);
    }

    [Fact]
    public void PositionDecline_Goalkeeper_DeclinesLaterThanWinger()
    {
        var gkSlot = 0;
        var wingerSlot = 1;

        var gkProfile = PositionDeclineProfile.ForSlot(gkSlot);
        var wingerProfile = PositionDeclineProfile.ForSlot(wingerSlot);

        Assert.Equal(PositionCategory.Goalkeeper, gkProfile.Category);
        Assert.Equal(PositionCategory.FullbackOrWinger, wingerProfile.Category);

        // 30 yaşında kaleci zirvededir (Peak), kanat ise düşüştedir (Declining)
        Assert.Equal(CareerPhase.Peak, gkProfile.GetPhase(30));
        Assert.Equal(CareerPhase.Declining, wingerProfile.GetPhase(30));

        // 30 yaşında kaleci puan kaybetmez, kanat puan kaybeder
        var (gkDrop, _) = gkProfile.CalculateAnnualDrop(30);
        var (wingerDrop, _) = wingerProfile.CalculateAnnualDrop(30);

        Assert.Equal(0, gkDrop);
        Assert.True(wingerDrop >= 2);
    }

    [Fact]
    public void ApplySinWaveDevelopmentGain_IntegratesWithPlayerCareer()
    {
        var youngTalent = Domain.PlayerCareer.PlayerCareer.CreateForSlot(
            new ClubId(1),
            slotIndex: 9, // Forward
            currentAbility: 60,
            potentialAbility: 75,
            birthYear: 2008); // 18 years old

        var developed = youngTalent.ApplySinWaveDevelopmentGain(20, Day);

        Assert.True(developed.CurrentAbility > youngTalent.CurrentAbility || developed.DevelopmentPoints > 0);
        Assert.Equal(Day, developed.LastDevelopedOn);
    }
}
