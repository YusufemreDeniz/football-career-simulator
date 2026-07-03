namespace FootballCareerSimulator.Domain.Spike1Placeholder;

/// <summary>
/// Spike 1 (bkz. docs/18_SPIKE_EXECUTION_PLAN.md Kart 2) kapsamında dünya ölçeğini kanıtlamak için
/// kullanılan yer tutucu, güçlü tipli kulüp kimliğidir. `docs/03_DOMAIN_MODEL.md`'deki gerçek Club
/// aggregate kimliğinin yerine geçmez.
/// </summary>
public readonly record struct ClubId(int Value)
{
    public static ClubId FromIndex(int index) => new(index);

    public override string ToString() => $"Club#{Value}";
}
