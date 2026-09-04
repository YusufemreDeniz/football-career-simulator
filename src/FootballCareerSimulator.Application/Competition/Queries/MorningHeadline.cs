namespace FootballCareerSimulator.Application.Competition.Queries;

/// <summary>
/// Maç gecesinin ertesi sabahı — deterministik "sabah manşeti" (ofis dönüşünde dış ses).
/// </summary>
public static class MorningHeadline
{
    public static string? Compose(
        int? managedGoalMargin,
        IReadOnlyList<string>? afterWhistleLines)
    {
        if (managedGoalMargin is null)
        {
            return null;
        }

        var whistle = afterWhistleLines ?? Array.Empty<string>();
        if (whistle.Any(line => line.Contains("işten çıkardı", StringComparison.OrdinalIgnoreCase)))
        {
            return "Sabah manşeti: \"Koltuk gitti — yönetim sabırsızdı.\"";
        }

        if (whistle.Any(line => line.Contains("basın", StringComparison.OrdinalIgnoreCase)))
        {
            return "Sabah manşeti: \"Basın kapıda — sorular sert olacak.\"";
        }

        return managedGoalMargin.Value switch
        {
            >= 3 => "Sabah manşeti: \"Rakibe nefes aldırmadılar — şehir zevk uyandı.\"",
            > 0 => "Sabah manşeti: \"Üç puan — şehir mutlu uyandı.\"",
            0 => "Sabah manşeti: \"Puanlar paylaşıldı — kazanan çıkmadı.\"",
            <= -3 => "Sabah manşeti: \"Sahada dağıldılar — tepki dinmeyecek.\"",
            _ => "Sabah manşeti: \"Sürpriz kayıp — taraftar soracak.\"",
        };
    }
}
