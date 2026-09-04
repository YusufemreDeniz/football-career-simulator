using FootballCareerSimulator.Application.CareerHub.Queries;
using FootballCareerSimulator.Application.TeamPreparation.Queries;

namespace FootballCareerSimulator.Tests.CareerHub;

public sealed class SquadOverviewDigestTests
{
    [Fact]
    public void Compose_SurfacesInjuredAndFatigueSignals()
    {
        var capacity = SquadCapacityDigest.Compose(22, 22, 25, Array.Empty<long>());
        var players = new[]
        {
            Line(1, fatigue: 20, isInjured: false, hasFatigueRisk: false),
            Line(2, fatigue: 72, isInjured: false, hasFatigueRisk: true),
            Line(3, fatigue: 30, isInjured: true, hasFatigueRisk: false),
        };

        var digest = SquadOverviewDigest.Compose(
            capacity,
            players,
            scoutNeedLine: "Sol bek derinliği zayıf",
            hasDepthGap: true,
            hasMatchBoard: true,
            matchBoardApproved: false,
            promiseRiskCount: 1);

        Assert.Contains(digest.Signals, signal => signal.Code == SquadOverviewDigest.SignalInjured);
        Assert.Contains(digest.Signals, signal => signal.Code == SquadOverviewDigest.SignalFatigue);
        Assert.Contains(digest.Signals, signal => signal.Code == SquadOverviewDigest.SignalPromise);
        Assert.Contains(digest.Signals, signal => signal.Code == SquadOverviewDigest.SignalDepth);
        Assert.True(digest.CanCreateTransferNeed);
        Assert.Contains("taslak", digest.PitchCaption, StringComparison.OrdinalIgnoreCase);
    }

    private static PlayerManagementLine Line(
        long id,
        int fatigue,
        bool isInjured,
        bool hasFatigueRisk) =>
        new(
            id,
            SlotIndex: (int)id,
            SquadNumber: (int)id,
            DisplayName: $"Oyuncu {id}",
            PositionCode: "CM",
            PositionName: "Orta saha",
            Rating: 70,
            Age: 24,
            CurrentAbility: 70,
            PotentialAbility: 80,
            CareerPhase: "Gelişim",
            Fitness: 75,
            Fatigue: fatigue,
            Availability: isInjured ? "Sakat" : "Hazır",
            IsInjured: isInjured,
            FatigueRiskBand: fatigue >= 65 ? "Yüksek Risk" : "Normal",
            HasFatigueRisk: hasFatigueRisk,
            HasPromiseRisk: false,
            MatchMinutesLast7Days: 0,
            DaysSinceLastMatch: 5,
            InjuryReasonCode: null,
            WorkloadHint: null,
            WeeklyWage: 1000,
            ContractEnd: "2028-06-30",
            Trust: 50,
            Respect: 50,
            Compatibility: 50,
            RelationshipState: "Dengeli",
            PromiseSummary: "Aktif söz yok",
            CausalitySummary: "—");
}
