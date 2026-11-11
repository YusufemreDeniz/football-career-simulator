namespace FootballCareerSimulator.Tests.PlayerCareer;

using FootballCareerSimulator.Domain.Competition;
using FootballCareerSimulator.Domain.Shared;
using FootballCareerSimulator.Simulation.PlayerCareer;
using Xunit;

public sealed class FacilityYouthIntakeTests
{
    [Fact]
    public void HighFacilityLevelAndYouthSkill_ProduceHigherPotentialCandidates()
    {
        var clubId = new ClubId(1);
        var seasonId = new SeasonId(1);
        const int rootSeed = 12345;
        const int sportiveStrength = 70;

        var standardCandidates = MvpYouthAcademyIntakeGenerator.Generate(
            clubId,
            seasonId,
            rootSeed,
            sportiveStrength,
            MvpYouthAcademyIntakeGenerator.GeneratorVersion1,
            facilityLevel: 1,
            youthDevelopmentSkill: 50);

        var eliteFacilityCandidates = MvpYouthAcademyIntakeGenerator.Generate(
            clubId,
            seasonId,
            rootSeed,
            sportiveStrength,
            MvpYouthAcademyIntakeGenerator.GeneratorVersion1,
            facilityLevel: 5,
            youthDevelopmentSkill: 100);

        var avgStdPotential = standardCandidates.Average(c => c.PotentialAbility);
        var avgElitePotential = eliteFacilityCandidates.Average(c => c.PotentialAbility);

        Assert.True(avgElitePotential > avgStdPotential,
            $"Elite facility average potential ({avgElitePotential}) should exceed standard ({avgStdPotential})");

        Assert.Contains(eliteFacilityCandidates, c => c.PotentialAbility >= 80);
    }
}
