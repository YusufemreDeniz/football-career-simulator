namespace FootballCareerSimulator.Domain.Spike1Placeholder;

/// <summary>
/// Spike 1 için oluşturulmuş yer tutucu kulüp temsili. Yalnızca dünya ölçeğini (bkz. <see cref="ClubId"/>)
/// simüle etmek için gereken en az alanı taşır; kesin kulüp domain modeli `docs/03_DOMAIN_MODEL.md`
/// kapsamında ayrıca tasarlanacaktır.
/// </summary>
public sealed class Club
{
    public ClubId Id { get; }

    public string Name { get; }

    public Club(ClubId id, string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Club name cannot be empty.", nameof(name));
        }

        Id = id;
        Name = name;
    }
}
