namespace FootballCareerSimulator.Domain.Spike1Placeholder;

/// <summary>
/// Spike 1 (bkz. docs/18_SPIKE_EXECUTION_PLAN.md Kart 2) için oluşturulmuş, yaklaşık 20 kulüp / 500
/// futbolcu ölçeğinde uzun dönem çalışabilirliği kanıtlamaya yönelik yer tutucu bir dünya durumudur.
/// `docs/03_DOMAIN_MODEL.md`'deki 14 bounded context'in gerçek domain modelini TEMSİL ETMEZ; gerçek
/// domain modeli ayrı bir çalışmayla implemente edilecek ve bu tip büyük olasılıkla tamamen
/// değiştirilecektir.
/// </summary>
public sealed class World
{
    public int CurrentSeason { get; private set; }

    public IReadOnlyList<Club> Clubs { get; }

    public IReadOnlyList<Player> Players { get; }

    public World(int currentSeason, IReadOnlyList<Club> clubs, IReadOnlyList<Player> players)
    {
        if (currentSeason < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(currentSeason), currentSeason, "Season cannot be negative.");
        }

        CurrentSeason = currentSeason;
        Clubs = clubs ?? throw new ArgumentNullException(nameof(clubs));
        Players = players ?? throw new ArgumentNullException(nameof(players));
    }

    public void AdvanceSeasonCounter() => CurrentSeason++;
}
