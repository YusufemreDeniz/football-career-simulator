namespace FootballCareerSimulator.Simulation.EventRuleEvaluation;

public static class EventEffectIdempotencyCanonicalStateHasher
{
    public static string BuildCanonicalText(IReadOnlyList<string> processingKeys)
    {
        ArgumentNullException.ThrowIfNull(processingKeys);

        if (processingKeys.Count == 0)
        {
            return "effectKeys:0";
        }

        var ordered = processingKeys
            .OrderBy(key => key, StringComparer.Ordinal)
            .ToArray();
        return $"effectKeys:{ordered.Length}:{string.Join(';', ordered)}";
    }
}
