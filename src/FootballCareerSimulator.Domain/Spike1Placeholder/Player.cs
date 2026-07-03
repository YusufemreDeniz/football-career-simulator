namespace FootballCareerSimulator.Domain.Spike1Placeholder;

/// <summary>
/// Spike 1 için oluşturulmuş yer tutucu futbolcu temsili. Yalnızca dünya ölçeğini ve sezon başına
/// yapılan minimal işi (yaş artışı) simüle etmek için gereken en az alanı taşır; kesin futbolcu
/// domain modeli `docs/03_DOMAIN_MODEL.md` ve `docs/11_PLAYER_CAREER.md` kapsamında ayrıca
/// tasarlanacaktır. Gerçek yaşlanma/gelişim/emeklilik formülleri burada uygulanmaz.
/// </summary>
public sealed class Player
{
    public PlayerId Id { get; }

    public ClubId ClubId { get; }

    public int Age { get; private set; }

    public Player(PlayerId id, ClubId clubId, int age)
    {
        if (age < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(age), age, "Age cannot be negative.");
        }

        Id = id;
        ClubId = clubId;
        Age = age;
    }

    public void AgeOneYear() => Age++;
}
