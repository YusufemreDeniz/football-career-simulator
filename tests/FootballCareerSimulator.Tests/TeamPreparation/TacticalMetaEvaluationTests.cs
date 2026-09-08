using FootballCareerSimulator.Application.TeamPreparation.Services;
using FootballCareerSimulator.Domain.TeamPreparation;

namespace FootballCareerSimulator.Tests.TeamPreparation;

public sealed class TacticalMetaEvaluationTests
{
    private const string TestLeague = "TUR_S1";
    private const int SeasonYear = 2026;

    [Fact]
    public void TacticalMetaSnapshot_NoObservations_ReturnsBalancedFallback()
    {
        var service = new TacticalEcosystemService();
        var snapshot = service.CompileSnapshot(TestLeague, SeasonYear);

        Assert.Equal(TestLeague, snapshot.CompetitionCode);
        Assert.Equal(SeasonYear, snapshot.SeasonYear);
        Assert.Equal(34, snapshot.Formation433Share);
        Assert.Equal(33, snapshot.Formation442Share);
        Assert.Equal(33, snapshot.Formation352Share);
        Assert.Equal(TacticalApproach.Balanced, snapshot.DominantApproach);
    }

    [Fact]
    public void TacticalMetaSnapshot_DominantObservations_ComputesDominantFormation()
    {
        var service = new TacticalEcosystemService();

        // Record 3 matches where 4-3-3 Attacking is heavily used
        service.RecordMatchTactics(TestLeague, SeasonYear, Formation.F433, TacticalApproach.Attacking, Formation.F433, TacticalApproach.Attacking);
        service.RecordMatchTactics(TestLeague, SeasonYear, Formation.F433, TacticalApproach.Attacking, Formation.F442, TacticalApproach.Balanced);
        service.RecordMatchTactics(TestLeague, SeasonYear, Formation.F433, TacticalApproach.Balanced, Formation.F352, TacticalApproach.Defensive);

        var snapshot = service.CompileSnapshot(TestLeague, SeasonYear);

        Assert.Equal(Formation.F433, snapshot.DominantFormation);
        Assert.Equal(TacticalApproach.Attacking, snapshot.DominantApproach);
        Assert.True(snapshot.Formation433Share >= 60);
    }

    [Fact]
    public void TacticalMetaSnapshot_CounterTactics_RecommendsCorrectAntidote()
    {
        Assert.Equal(Formation.F352, LeagueTacticalMetaSnapshot.GetCounterFormation(Formation.F433));
        Assert.Equal(Formation.F442, LeagueTacticalMetaSnapshot.GetCounterFormation(Formation.F352));
        Assert.Equal(Formation.F433, LeagueTacticalMetaSnapshot.GetCounterFormation(Formation.F442));

        Assert.Equal(TacticalApproach.Defensive, LeagueTacticalMetaSnapshot.GetCounterApproach(TacticalApproach.Attacking));
        Assert.Equal(TacticalApproach.Attacking, LeagueTacticalMetaSnapshot.GetCounterApproach(TacticalApproach.Defensive));
    }

    [Fact]
    public void TacticalIntelligenceDigest_ProducesProperBulletins()
    {
        var service = new TacticalEcosystemService();

        service.RecordMatchTactics(TestLeague, SeasonYear, Formation.F433, TacticalApproach.Attacking, Formation.F433, TacticalApproach.Attacking);

        var digest = service.BuildDigest(TestLeague, SeasonYear);

        Assert.Equal(Formation.F433, digest.DominantFormation);
        Assert.Equal(Formation.F352, digest.RecommendedCounterFormation);
        Assert.Equal(TacticalApproach.Defensive, digest.RecommendedCounterApproach);
        Assert.Equal("Akıcı Hücum & 4-3-3 Dalgası", digest.PrevailingMetaName);
        Assert.NotEmpty(digest.TacticalBulletins);
        Assert.Contains(digest.TacticalBulletins, b => b.Contains("F352"));
    }
}
