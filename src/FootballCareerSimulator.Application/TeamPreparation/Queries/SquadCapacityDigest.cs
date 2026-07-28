namespace FootballCareerSimulator.Application.TeamPreparation.Queries;

/// <summary>
/// Aktif sözleşme vs maç günü kadrosu — taşma oyuncuya görünür olsun.
/// </summary>
public sealed record SquadCapacityDigest(
    bool IsEmployed,
    bool IsOverCapacity,
    bool IsFull,
    string BrandTitle,
    string Headline,
    string AdviceLine,
    int ActiveContractCount,
    int SquadMemberCount,
    int MaxMembers,
    IReadOnlyList<long> OverflowPlayerIds)
{
    public const string Brand = "Kadro Kapasitesi";

    public static SquadCapacityDigest Unemployed() =>
        new(
            IsEmployed: false,
            IsOverCapacity: false,
            IsFull: false,
            Brand,
            "Kulüp yok — kadro kapasitesi yok.",
            string.Empty,
            0,
            0,
            Domain.TeamPreparation.ClubSquad.MaxMembers,
            Array.Empty<long>());

    public static SquadCapacityDigest Compose(
        int activeContractCount,
        int squadMemberCount,
        int maxMembers,
        IReadOnlyList<long> overflowPlayerIds)
    {
        ArgumentNullException.ThrowIfNull(overflowPlayerIds);
        if (maxMembers < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(maxMembers));
        }

        var over = overflowPlayerIds.Count > 0 || activeContractCount > maxMembers;
        var full = activeContractCount >= maxMembers;
        string headline;
        string advice;

        if (over)
        {
            headline =
                $"{overflowPlayerIds.Count} sözleşmeli oyuncu maç kadrosuna sığmıyor.";
            advice =
                "Yer açmak için sözleşme bitmesini bekle veya transferle çıkış planla — taşanlar XI'ye giremez.";
        }
        else if (full)
        {
            headline = $"Kadro dolu ({squadMemberCount}/{maxMembers}).";
            advice = "Yeni imza veya gelen transfer için önce yer gerekir.";
        }
        else if (squadMemberCount == 0)
        {
            headline = "Kadro henüz oluşmadı.";
            advice = "Lig kur / gün ilerle — sözleşmeler kadroya işler.";
        }
        else
        {
            headline = $"Kadro açık ({squadMemberCount}/{maxMembers}).";
            advice = $"{maxMembers - activeContractCount} slot boş — serbest imza veya transfer mümkün.";
        }

        return new SquadCapacityDigest(
            IsEmployed: true,
            IsOverCapacity: over,
            IsFull: full,
            Brand,
            headline,
            advice,
            activeContractCount,
            squadMemberCount,
            maxMembers,
            overflowPlayerIds.Take(5).ToArray());
    }

    public string ToDisplayText()
    {
        if (!IsEmployed)
        {
            return $"{BrandTitle}\n{Headline}";
        }

        var overflow = OverflowPlayerIds.Count == 0
            ? string.Empty
            : "\n· Taşan: "
              + string.Join(", ", OverflowPlayerIds.Select(id => $"#{id}"))
              + (ActiveContractCount - SquadMemberCount > OverflowPlayerIds.Count
                  ? "…"
                  : string.Empty);
        var advice = string.IsNullOrWhiteSpace(AdviceLine)
            ? string.Empty
            : $"\nÖneri: {AdviceLine}";
        return $"{BrandTitle}\n{Headline}\n· Sözleşme {ActiveContractCount} · kadro {SquadMemberCount}/{MaxMembers}{overflow}{advice}";
    }
}
