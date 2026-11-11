namespace FootballCareerSimulator.Application.CareerHub.Queries;

/// <summary>
/// Sakin havadan maç temposuna geçiş — oyuncuya “hafta değişti” hissi.
/// </summary>
public static class WeekMoodTempoBridge
{
    public sealed record Shift(string Headline, string AdviceLine, string NextFocusCode);

    public static bool IsCalmFamily(string? moodCode) =>
        string.Equals(moodCode, WeekMoodDigest.MoodCalm, StringComparison.Ordinal)
        || string.Equals(moodCode, WeekMoodDigest.MoodCalmMatch, StringComparison.Ordinal);

    public static bool IsMatchTempo(string? moodCode) =>
        string.Equals(moodCode, WeekMoodDigest.MoodMatchDraft, StringComparison.Ordinal)
        || string.Equals(moodCode, WeekMoodDigest.MoodMatchReady, StringComparison.Ordinal)
        || string.Equals(moodCode, WeekMoodDigest.MoodPromise, StringComparison.Ordinal)
        || string.Equals(moodCode, WeekMoodDigest.MoodCalmMatch, StringComparison.Ordinal);

    public static Shift? Resolve(string? previousMoodCode, string? nextMoodCode)
    {
        if (string.IsNullOrWhiteSpace(nextMoodCode)
            || string.Equals(previousMoodCode, nextMoodCode, StringComparison.Ordinal))
        {
            return null;
        }

        // Sakin hafta → maç günü / söz / kadro kilidi
        if (IsCalmFamily(previousMoodCode))
        {
            return nextMoodCode switch
            {
                WeekMoodDigest.MoodCalmMatch => new Shift(
                    "Tempo yükseldi — maç takvimde.",
                    "Sakin tempo bozulmasın — sıradaki maça temiz bak.",
                    TodayPulseDigest.FocusMatch),
                WeekMoodDigest.MoodMatchDraft => new Shift(
                    "Tempo yükseldi — kadro kilidi bekliyor.",
                    "Sıradaki Maç — kadroyu kilitle, sonra düdük.",
                    TodayPulseDigest.FocusMatch),
                WeekMoodDigest.MoodMatchReady => new Shift(
                    "Tempo yükseldi — düdük yakın.",
                    "Bugün'de kal — Maç Gününe Git.",
                    TodayPulseDigest.FocusMatch),
                WeekMoodDigest.MoodPromise => new Shift(
                    "Tempo yükseldi — söz gerilimi.",
                    "XI↔Yedek düşünmeden düdük çalma.",
                    TodayPulseDigest.FocusMatch),
                _ => null,
            };
        }

        // Maç taslak → hazır (nadiren gün ilerleyince; onay sonrası da kullanılabilir)
        if (string.Equals(previousMoodCode, WeekMoodDigest.MoodMatchDraft, StringComparison.Ordinal)
            && string.Equals(nextMoodCode, WeekMoodDigest.MoodMatchReady, StringComparison.Ordinal))
        {
            return new Shift(
                "Tempo oturdu — kadro kilitli, düdük yakın.",
                "Bugün'de kal — Maç Gününe Git.",
                TodayPulseDigest.FocusMatch);
        }

        return null;
    }
}
