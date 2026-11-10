using FootballCareerSimulator.Application.TrainingPhysicalState.Queries;
using FootballCareerSimulator.Domain.Shared;
using FootballCareerSimulator.Domain.TrainingPhysicalState;
using FootballCareerSimulator.Domain.WorldCalendar;

namespace FootballCareerSimulator.Tests.TrainingPhysicalState;

public sealed class FixtureCongestionForecastDigestTests
{
    [Fact]
    public void Compose_CalmScheduleAndRestedSquad_ReturnsNormalRisk()
    {
        var clubId = new ClubId(1);
        var states = Enumerable.Range(0, 18)
            .Select(slot => PlayerPhysicalState.CreateRested(clubId, slot))
            .ToArray();

        var forecast = FixtureCongestionForecastDigest.Compose(
            currentDayNumber: 100,
            upcomingScheduledMatchDays: [106, 113, 120, 127],
            squadStates: states);

        Assert.True(forecast.IsAvailable);
        Assert.Equal(CongestionRiskLevel.Normal, forecast.RiskLevel);
        Assert.Equal(1, forecast.MatchesNext7Days);
        Assert.Equal(2, forecast.MatchesNext14Days);
        Assert.Equal(4, forecast.MatchesNext30Days);
        Assert.Equal(7, forecast.MinRestGapDays);
        Assert.Contains("Takvim Dengeli", forecast.RiskBadgeText, StringComparison.Ordinal);
    }

    [Fact]
    public void Compose_TwoMatchesInSevenDays_ReturnsHeavyRisk()
    {
        var clubId = new ClubId(1);
        var states = Enumerable.Range(0, 18)
            .Select(slot => PlayerPhysicalState.CreateRested(clubId, slot))
            .ToArray();

        var forecast = FixtureCongestionForecastDigest.Compose(
            currentDayNumber: 100,
            upcomingScheduledMatchDays: [102, 106, 113, 120],
            squadStates: states);

        Assert.Equal(CongestionRiskLevel.Heavy, forecast.RiskLevel);
        Assert.Equal(2, forecast.MatchesNext7Days);
        Assert.Equal(4, forecast.MinRestGapDays);
        Assert.Contains("Yoğun Takvim", forecast.RiskBadgeText, StringComparison.Ordinal);
    }

    [Fact]
    public void Compose_ThreeMatchesInSevenDaysOrExhaustedSquad_ReturnsCriticalRisk()
    {
        var clubId = new ClubId(1);
        var day = GameDate.FromDayNumber(100);
        var states = Enumerable.Range(0, 18)
            .Select(slot => PlayerPhysicalState.CreateRested(clubId, slot)
                .WithLevels(fatigue: slot < 5 ? 72 : 35, fitness: slot < 5 ? 58 : 78))
            .ToArray();

        var forecast = FixtureCongestionForecastDigest.Compose(
            currentDayNumber: day.DayNumber,
            upcomingScheduledMatchDays: [101, 104, 107, 114],
            squadStates: states);

        Assert.Equal(CongestionRiskLevel.Critical, forecast.RiskLevel);
        Assert.Equal(3, forecast.MatchesNext7Days);
        Assert.Equal(3, forecast.MinRestGapDays);
        Assert.Equal(5, forecast.HighRiskStarterCount);
        Assert.Contains("Kritik Fikstür Yükü", forecast.RiskBadgeText, StringComparison.Ordinal);
        Assert.Contains("Ağır Dinlenme ve rotasyon 11'i önerilir", forecast.RecommendationLine, StringComparison.Ordinal);
    }
}
