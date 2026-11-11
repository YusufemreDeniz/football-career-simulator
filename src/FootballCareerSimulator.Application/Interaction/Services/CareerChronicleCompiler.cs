using FootballCareerSimulator.Application.Interaction.Queries;
using FootballCareerSimulator.Domain.Interaction;

namespace FootballCareerSimulator.Application.Interaction.Services;

/// <summary>
/// Committed domain fact'lerinden deterministik ve dramatik kariyer belgeseli derleyen servis (Yenilik 10).
/// </summary>
public sealed class CareerChronicleCompiler
{
    private readonly Dictionary<int, List<NarrativeFactRecord>> _seasonFacts = new();

    public void RecordFact(int seasonYear, NarrativeFactRecord fact)
    {
        ArgumentNullException.ThrowIfNull(fact);

        if (!_seasonFacts.TryGetValue(seasonYear, out var list))
        {
            list = new List<NarrativeFactRecord>();
            _seasonFacts[seasonYear] = list;
        }

        list.Add(fact);
    }

    public SeasonChronicleDigest CompileSeasonChronicle(
        int seasonYear,
        string clubName,
        int finalLeaguePosition,
        bool wonTitle)
    {
        var facts = _seasonFacts.TryGetValue(seasonYear, out var list)
            ? list.OrderByDescending(f => f.ImportanceScore).ThenBy(f => f.OccurredOn.DayNumber).ToList()
            : new List<NarrativeFactRecord>();

        var title = wonTitle
            ? $"{seasonYear}-{seasonYear + 1}: {clubName} ile Zirve Yürüyüşü ve Şampiyonluk Masalı"
            : (finalLeaguePosition <= 4
                ? $"{seasonYear}-{seasonYear + 1}: {clubName} Zirve Yarışı ve Avrupa Bileti"
                : $"{seasonYear}-{seasonYear + 1}: {clubName} Yeniden Yapılanma ve Mücadele Sezonu");

        var verdict = wonTitle
            ? "Tarihe geçen unutulmaz bir zaferle taçlanan efsanevi sezon."
            : (finalLeaguePosition <= 4
                ? "Gelecek sezon için umut veren, hedeflere ulaşılan başarılı bir dönem."
                : "Zorluklarla dolu, tecrübe kazandıran bir geçiş sezonu.");

        var arcBuilder = new System.Text.StringBuilder();
        arcBuilder.AppendLine($"Sezon başlangıcında {clubName} camiası yeni hedeflerle yola çıktı.");

        if (facts.Count > 0)
        {
            var topMilestones = facts.Take(3).ToList();
            foreach (var m in topMilestones)
            {
                arcBuilder.AppendLine($"- {m.NarrativeSnippet}");
            }
        }
        else
        {
            arcBuilder.AppendLine("Sezon boyunca büyük dalgalanmalar yaşanmadan dengeli bir grafik çizildi.");
        }

        arcBuilder.AppendLine($"Sezon sonunda lig {finalLeaguePosition}. sırada tamamlandı. {verdict}");

        return new SeasonChronicleDigest(
            seasonYear,
            title,
            arcBuilder.ToString().Trim(),
            finalLeaguePosition,
            wonTitle,
            facts,
            verdict);
    }
}
