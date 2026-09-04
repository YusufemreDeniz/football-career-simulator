using FootballCareerSimulator.Application.Competition.Queries;

namespace FootballCareerSimulator.Tests.Competition;

public sealed class LeagueWorldBriefingTests
{
    [Fact]
    public void NoSeason_WhenEmpty()
    {
        var briefing = LeagueWorldBriefing.Compose(
            seasonStatus: "Active",
            acceptedFixtureCount: 0,
            totalFixtureCount: 0,
            clubCount: 0,
            managedRank: null,
            managedPoints: null,
            managedPlayed: null,
            managedGoalDifference: null,
            managedClubName: null,
            leaderClubName: null,
            leaderPoints: null,
            nextMatchLine: null);

        Assert.False(briefing.HasSeason);
        Assert.Contains("kurulmadı", briefing.Headline, StringComparison.Ordinal);
    }

    [Fact]
    public void Leader_KeepsSummitHeadline()
    {
        var briefing = LeagueWorldBriefing.Compose(
            "Active",
            acceptedFixtureCount: 10,
            totalFixtureCount: 30,
            clubCount: 8,
            managedRank: 1,
            managedPoints: 22,
            managedPlayed: 10,
            managedGoalDifference: 8,
            managedClubName: "Home FC",
            leaderClubName: "Home FC",
            leaderPoints: 22,
            nextMatchLine: "Ev vs Rival · yarın");

        Assert.Equal("Zirvedesin — hedefi koru.", briefing.Headline);
        Assert.True(briefing.DemandsAttention);
        Assert.Equal(LeagueNextStep.Summit, briefing.NextStep!.ReasonCode);
        Assert.Equal(LeagueNextStep.TargetPrep, briefing.NextStep.TargetPageCode);
        Assert.Contains(briefing.BeatLines, b => b.Contains("1. Home FC", StringComparison.Ordinal));
        Assert.Contains(briefing.BeatLines, b => b.Contains("Sıradaki senin maçın", StringComparison.Ordinal));
        Assert.Contains("Liderliği koru", briefing.AdviceLine, StringComparison.Ordinal);
        Assert.Contains("Öneri:", briefing.ToDisplayText(), StringComparison.Ordinal);
    }

    [Fact]
    public void BottomTable_WarnsPressure()
    {
        var briefing = LeagueWorldBriefing.Compose(
            "Active",
            12,
            30,
            clubCount: 10,
            managedRank: 10,
            managedPoints: 5,
            managedPlayed: 12,
            managedGoalDifference: -14,
            managedClubName: "Struggle United",
            leaderClubName: "Giants",
            leaderPoints: 28,
            nextMatchLine: null);

        Assert.Contains("Alt sıralar", briefing.Headline, StringComparison.Ordinal);
        Assert.Contains("Küme hattı", briefing.AdviceLine, StringComparison.Ordinal);
        Assert.True(briefing.DemandsAttention);
        Assert.Equal(LeagueNextStep.Survival, briefing.NextStep!.ReasonCode);
        Assert.Equal(LeagueNextStep.TargetToday, briefing.NextStep.TargetPageCode);
    }

    [Fact]
    public void TitleRace_RoutesToNextMatch()
    {
        var briefing = LeagueWorldBriefing.Compose(
            "Active",
            18,
            30,
            clubCount: 8,
            managedRank: 2,
            managedPoints: 40,
            managedPlayed: 18,
            managedGoalDifference: 6,
            managedClubName: "Chasers",
            leaderClubName: "Leaders",
            leaderPoints: 42,
            nextMatchLine: "Ev vs Rival · yarın");

        Assert.True(briefing.DemandsAttention);
        Assert.Equal(LeagueNextStep.TitleRace, briefing.NextStep!.ReasonCode);
        Assert.Contains("Sıradaki Maç", briefing.NextStep.ButtonLabel, StringComparison.Ordinal);
    }

    [Fact]
    public void FreshLeague_WaitsFirstWhistle()
    {
        var briefing = LeagueWorldBriefing.Compose(
            "Active",
            0,
            56,
            8,
            managedRank: null,
            managedPoints: null,
            managedPlayed: null,
            managedGoalDifference: null,
            managedClubName: "Home FC",
            leaderClubName: null,
            leaderPoints: null,
            nextMatchLine: null);

        Assert.Equal("Lig kuruldu — ilk düdükleri bekliyor.", briefing.Headline);
        Assert.Contains("günü ilerlet", briefing.AdviceLine, StringComparison.OrdinalIgnoreCase);
        Assert.True(briefing.DemandsAttention);
        Assert.Equal(LeagueNextStep.Kickstart, briefing.NextStep!.ReasonCode);
        Assert.Equal(LeagueNextStep.ActionAdvanceDay, briefing.NextStep.ActionCode);
    }

    [Fact]
    public void MidTable_DoesNotDemandAttention()
    {
        var briefing = LeagueWorldBriefing.Compose(
            "Active",
            10,
            30,
            clubCount: 8,
            managedRank: 4,
            managedPoints: 14,
            managedPlayed: 10,
            managedGoalDifference: 1,
            managedClubName: "Mid FC",
            leaderClubName: "Leaders",
            leaderPoints: 22,
            nextMatchLine: null);

        Assert.False(briefing.DemandsAttention);
        Assert.Null(briefing.NextStep);
    }
}
