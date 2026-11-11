namespace FootballCareerSimulator.Application.CareerHub.Queries;

/// <summary>
/// Sakin haftada Ofiste kısa “Not:” satırı — gün numarasına göre deterministik varyasyon.
/// </summary>
public static class OfficeCalmNote
{
    private static readonly string[] CalmNotes =
    [
        "Staff sessiz — plan kendi yürüyor.",
        "Takvim dolu değil, baskı da yok; günü temiz ilerlet.",
        "Soyunma odası sakin — yarın aynı ritim.",
        "Masada dosya yok; nabız sakin tut.",
        "İdman planı oturmuş — gereksiz müdahale etme.",
        "Ofis boş, tempo senin — bir gün daha tut.",
    ];

    private static readonly string[] CalmMatchNotes =
    [
        "Rakip kapıda ama tempo senin kontrolünde.",
        "XI hazır sayılır — sakin tempoyu bozma.",
        "Maç yakın; panik yok, ritim var.",
        "Hazırlık tutuyor — düdüğe acele etme.",
        "Sakin tempo — sıradaki maça temiz git.",
    ];

    public static string? Resolve(string? moodCode, int dayNumber)
    {
        if (string.IsNullOrWhiteSpace(moodCode))
        {
            return null;
        }

        var pool = moodCode switch
        {
            WeekMoodDigest.MoodCalm => CalmNotes,
            WeekMoodDigest.MoodCalmMatch => CalmMatchNotes,
            _ => null,
        };
        if (pool is null || pool.Length == 0)
        {
            return null;
        }

        var idx = Math.Abs(dayNumber) % pool.Length;
        return pool[idx];
    }

    public static string? ToBeatLine(string? moodCode, int dayNumber)
    {
        var note = Resolve(moodCode, dayNumber);
        return note is null ? null : $"Not: {note}";
    }

    /// <summary>
    /// Gün ilerledikten sonra status satırı — not değiştiyse oyuncu fark etsin.
    /// </summary>
    public static string? ToAdvanceConfirmation(string? previousNote, string? nextNote)
    {
        if (string.IsNullOrWhiteSpace(nextNote))
        {
            return null;
        }

        if (string.IsNullOrWhiteSpace(previousNote))
        {
            return $"Ofis notu: {nextNote}";
        }

        if (string.Equals(previousNote, nextNote, StringComparison.Ordinal))
        {
            return "Yeni gün — sakin tempo sürüyor.";
        }

        return $"Not yenilendi: {nextNote}";
    }
}
