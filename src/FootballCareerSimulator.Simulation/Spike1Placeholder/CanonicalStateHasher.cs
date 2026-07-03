using System.Security.Cryptography;
using System.Text;
using FootballCareerSimulator.Domain.Spike1Placeholder;

namespace FootballCareerSimulator.Simulation.Spike1Placeholder;

/// <summary>
/// Spike 2'nin (bkz. docs/18_SPIKE_EXECUTION_PLAN.md Kart 3) "aynı seed ile 20 tekrar aynı canonical
/// final hash'i üretir" başarı kriterini karşılamak için kullanılan, semantic state üzerinden çalışan
/// hash hesaplayıcısıdır (`docs/15_DECISION_LOG.md` D-276, D-294 ile uyumlu: fiziksel byte sırasına
/// veya koleksiyon iterasyon sırasına değil, açıkça sıralanmış semantic içeriğe dayanır).
/// </summary>
public static class CanonicalStateHasher
{
    public static string ComputeHash(World world)
    {
        var canonicalText = BuildCanonicalText(world);
        var hashBytes = SHA256.HashData(Encoding.UTF8.GetBytes(canonicalText));
        return Convert.ToHexString(hashBytes);
    }

    public static string BuildCanonicalText(World world)
    {
        ArgumentNullException.ThrowIfNull(world);

        var builder = new StringBuilder();
        builder.Append("Season=").Append(world.CurrentSeason).Append(';');

        builder.Append("Clubs=[");
        foreach (var club in world.Clubs.OrderBy(club => club.Id.Value))
        {
            builder.Append(club.Id.Value).Append(':').Append(club.Name).Append('|');
        }

        builder.Append("];Players=[");
        foreach (var player in world.Players.OrderBy(player => player.Id.Value))
        {
            builder.Append(player.Id.Value).Append(':')
                .Append(player.ClubId.Value).Append(':')
                .Append(player.Age).Append(':')
                .Append(player.Form).Append('|');
        }

        builder.Append(']');

        return builder.ToString();
    }
}
