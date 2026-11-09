using FootballCareerSimulator.Domain.ClubGovernance;
using FootballCareerSimulator.Domain.PlayerCareer;
using FootballCareerSimulator.Domain.Shared;
using FootballCareerSimulator.Domain.TrainingPhysicalState;
using FootballCareerSimulator.Domain.WorldCalendar;
using FootballCareerSimulator.Simulation.PlayerCareer;

namespace FootballCareerSimulator.Tests.ClubGovernance;

public sealed class ClubFacilitySystemTests
{
    private static readonly GameDate Day = GameDate.FromCalendarDate(2026, 7, 1);

    [Fact]
    public void DefaultFacilityLevel_ScalesWithClubStrength()
    {
        var elite = Club.Create(new ClubId(1), "Elite FC", new ClubCode("ELI"), 92);
        var midTable = Club.Create(new ClubId(2), "Mid FC", new ClubCode("MID"), 72);
        var lower = Club.Create(new ClubId(3), "Lower FC", new ClubCode("LOW"), 55);
        var amateur = Club.Create(new ClubId(4), "Grassroots FC", new ClubCode("GRA"), 40);

        Assert.Equal(4, elite.FacilityLevel);
        Assert.Equal(3, midTable.FacilityLevel);
        Assert.Equal(2, lower.FacilityLevel);
        Assert.Equal(1, amateur.FacilityLevel);
    }

    [Fact]
    public void UpgradeFacilities_UpgradesLevelAndDeductsCostFromTransferBudget()
    {
        var club = Club.Create(new ClubId(10), "Upgrade FC", new ClubCode("UPG"), 60, initialFacilityLevel: 1);
        Assert.Equal(1, club.FacilityLevel);
        var initialAvailable = club.AvailableTransferFunds;
        var cost = Club.FacilityUpgradeCost(1);

        var upgraded = club.UpgradeFacilities();

        Assert.Equal(2, upgraded.FacilityLevel);
        Assert.Equal(cost, upgraded.SpentTransferFunds);
        Assert.Equal(initialAvailable - cost, upgraded.AvailableTransferFunds);
        Assert.True(upgraded.FacilityTrainingMultiplier > club.FacilityTrainingMultiplier);
    }

    [Fact]
    public void UpgradeFacilities_ThrowsWhenAtMaxLevel()
    {
        var maxClub = Club.Rehydrate(
            new ClubId(1),
            "Max Facility FC",
            new ClubCode("MAX"),
            sportiveStrength: 95,
            transferBudgetLimit: 100_000_000,
            reservedTransferFunds: 0,
            spentTransferFunds: 0,
            wageBudgetLimit: 500_000,
            reservedWeeklyWage: 0,
            facilityLevel: 5);

        Assert.Throws<ClubGovernanceInvariantViolationException>(() => maxClub.UpgradeFacilities());
    }

    [Fact]
    public void UpgradeFacilities_ThrowsWhenInsufficientFunds()
    {
        var poorClub = Club.Rehydrate(
            new ClubId(1),
            "Broke FC",
            new ClubCode("BRO"),
            sportiveStrength: 50,
            transferBudgetLimit: 1_000_000, // Upgrade costs 2.5M
            reservedTransferFunds: 0,
            spentTransferFunds: 0,
            wageBudgetLimit: 200_000,
            reservedWeeklyWage: 0,
            facilityLevel: 1);

        Assert.Throws<ClubGovernanceInvariantViolationException>(() => poorClub.UpgradeFacilities());
    }

    [Fact]
    public void TrainingApplier_FacilityMultiplier_IncreasesDevelopmentGains()
    {
        var day = GameDate.FromCalendarDate(2026, 7, 5);
        var plan = WeeklyTrainingPlan.Set(
            new ClubId(1), TrainingFocus.General, TrainingIntensity.High, RestApproach.Normal, day);

        var playerBase = Domain.PlayerCareer.PlayerCareer.CreateForSlot(
            new ClubId(1), 5, currentAbility: 60, potentialAbility: 80, birthYear: 2005);
        var playerWithEliteFacility = Domain.PlayerCareer.PlayerCareer.CreateForSlot(
            new ClubId(1), 5, currentAbility: 60, potentialAbility: 80, birthYear: 2005);

        // Seviye 5 tesis çarpanı 1,4x'tir (1,0 + 4 × 0,1).
        const double levelFiveMultiplier = 1.4;
        var trainedStandard = MvpPlayerDevelopmentApplier.ApplyWeeklyTraining(
            playerBase, plan, null, day, facilityMultiplier: 1.0);
        var trainedElite = MvpPlayerDevelopmentApplier.ApplyWeeklyTraining(
            playerWithEliteFacility, plan, null, day, facilityMultiplier: levelFiveMultiplier);

        // SinWave eğrisi altında aynı oyuncu, daha iyi tesiste kesin olarak daha fazla gelişim puanı toplar.
        var standardProgress = (trainedStandard.CurrentAbility * 10) + trainedStandard.DevelopmentPoints;
        var eliteProgress = (trainedElite.CurrentAbility * 10) + trainedElite.DevelopmentPoints;
        Assert.True(eliteProgress > standardProgress, $"elite={eliteProgress} standard={standardProgress}");
    }

    [Fact]
    public void DevelopmentService_UsesClubFacilityLevel_ForWeeklyTraining()
    {
        // Üretim yolu: tesis seviyesi kulüp kaydından okunur ve haftalık antrenmana uygulanır.
        var day = GameDate.FromCalendarDate(2026, 7, 5);
        var plan = WeeklyTrainingPlan.Set(
            new ClubId(1), TrainingFocus.General, TrainingIntensity.High, RestApproach.Normal, day);

        var standard = RunWeeklyTraining(facilityLevel: 1, plan, day);
        var elite = RunWeeklyTraining(facilityLevel: 5, plan, day);

        Assert.True(elite > standard, $"elite={elite} standard={standard}");
    }

    private static int RunWeeklyTraining(int facilityLevel, WeeklyTrainingPlan plan, GameDate day)
    {
        var registry = LeagueClubRegistry.CreateMvpLeague();
        var club = registry.GetClubOrThrow(new ClubId(1));
        var rehydrated = Club.Rehydrate(
            club.Id,
            club.DisplayName,
            club.Code,
            club.SportiveStrength,
            club.TransferBudgetLimit,
            club.ReservedTransferFunds,
            club.SpentTransferFunds,
            club.WageBudgetLimit,
            club.ReservedWeeklyWage,
            facilityLevel);
        var clubStore = new FootballCareerSimulator.Application.ClubGovernance.Infrastructure.InMemoryClubRegistryStore(
            registry.WithClub(rehydrated));
        var playerStore = new FootballCareerSimulator.Application.PlayerCareer.Infrastructure.InMemoryPlayerCareerStore();
        var service = new FootballCareerSimulator.Application.PlayerCareer.Services.PlayerCareerDevelopmentService(
            playerStore,
            clubRegistry: clubStore);

        service.EnsureClub(new ClubId(1), rootSeed: 42, day);
        service.EnsureAndApplyWeeklyTraining(new ClubId(1), plan, rootSeed: 42, day);
        return playerStore.Careers
            .Where(career => career.OriginClubId == new ClubId(1))
            .Sum(career => (career.CurrentAbility * 10) + career.DevelopmentPoints);
    }
}
