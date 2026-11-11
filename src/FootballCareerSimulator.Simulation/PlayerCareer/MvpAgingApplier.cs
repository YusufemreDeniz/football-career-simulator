using FootballCareerSimulator.Domain.WorldCalendar;

namespace FootballCareerSimulator.Simulation.PlayerCareer;

/// <summary>
/// Takvim yılı başına bir kez yaşlanma/düşüş uygular.
/// </summary>
public static class MvpAgingApplier
{
    public static IReadOnlyList<Domain.PlayerCareer.PlayerCareer> ApplyDueAging(
        IEnumerable<Domain.PlayerCareer.PlayerCareer> careers,
        GameDate day)
    {
        ArgumentNullException.ThrowIfNull(careers);
        return careers.Select(career => career.ApplyAnnualAging(day)).ToArray();
    }

    public static int ResolveBirthYear(int referenceYear, int slotIndex, int rootSeed)
    {
        // 18–32 yaş arası deterministik doğum yılı.
        var age = 18 + ((slotIndex + (rootSeed % 7)) % 15);
        return referenceYear - age;
    }
}
