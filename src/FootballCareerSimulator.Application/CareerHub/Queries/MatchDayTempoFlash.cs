namespace FootballCareerSimulator.Application.CareerHub.Queries;

/// <summary>
/// Maç Günü ekranına varış — ofis tempo flash'ının karşılığı.
/// "Kadro kilitli, düdük yakın" hissini ofisten düdük anına kadar taşır.
/// </summary>
public static class MatchDayTempoFlash
{
    public sealed record Flash(string BeatLine, string AdviceLine);

    /// <summary>
    /// Maç gününe varma anı — nabız havasından düdük beklentisi satırı.
    /// Vadesi gelmiş maç yoksa veya hava net değilse null.
    /// </summary>
    public static Flash? ResolveArrival(
        WeekMoodDigest? mood,
        bool hasDueMatch,
        bool hasInjuryPressure = false)
    {
        if (mood is not { IsActive: true } || !hasDueMatch)
        {
            return null;
        }

        if (hasInjuryPressure)
        {
            return new Flash(
                "Sakatlık baskısı — XI'yi kontrol et.",
                "Düdük için önce sakatsız kadro.");
        }

        return mood.MoodCode switch
        {
            WeekMoodDigest.MoodMatchReady => new Flash(
                "Tempo oturdu — kadro kilitli, düdük yakın.",
                "Düdüğü çalabilirsin."),
            WeekMoodDigest.MoodMatchDraft => new Flash(
                "Tempo yükseldi — kadro kilidi bekliyor.",
                "Düdük kapalı — önce Kadro Onayla."),
            WeekMoodDigest.MoodPromise => new Flash(
                "Tempo yükseldi — söz gerilimi.",
                "XI↔Yedek düşünmeden düdük çalma."),
            WeekMoodDigest.MoodCalmMatch => new Flash(
                "Maç takvimde — tempo henüz oturmadı.",
                "Düdük için kadro kilidi gerek."),
            _ => null,
        };
    }
}
