using FootballCareerSimulator.Domain.WorldCalendar;

namespace FootballCareerSimulator.Tests.WorldCalendar;

public class SimulationStepIdTests
{
    [Fact]
    public void Next_IncrementsValueByOne()
    {
        var step = SimulationStepId.Zero.Next();

        Assert.Equal(1, step.Value);
    }

    [Fact]
    public void Constructor_RejectsNegativeValue()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new SimulationStepId(-1));
    }

    [Fact]
    public void Comparison_IsMonotonicByValue()
    {
        var earlier = new SimulationStepId(10);
        var later = new SimulationStepId(11);

        Assert.True(earlier < later);
        Assert.True(later > earlier);
    }
}
