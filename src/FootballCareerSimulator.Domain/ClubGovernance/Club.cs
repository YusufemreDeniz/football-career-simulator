namespace FootballCareerSimulator.Domain.ClubGovernance;

using FootballCareerSimulator.Domain.Shared;

/// <summary>
/// Kulüp kimliği aggregate (docs/03_DOMAIN_MODEL.md Bölüm 7.3).
/// </summary>
public sealed class Club
{
    public const int MinSportiveStrength = 1;
    public const int MaxSportiveStrength = 100;

    private Club(ClubId id, string displayName, ClubCode code, int sportiveStrength)
    {
        Id = id;
        DisplayName = displayName;
        Code = code;
        SportiveStrength = sportiveStrength;
    }

    public ClubId Id { get; }

    public string DisplayName { get; }

    public ClubCode Code { get; }

    public int SportiveStrength { get; }

    public static Club Create(ClubId id, string displayName, ClubCode code, int sportiveStrength)
    {
        if (string.IsNullOrWhiteSpace(displayName))
        {
            throw new ClubGovernanceInvariantViolationException("Club display name cannot be empty.");
        }

        if (sportiveStrength is < MinSportiveStrength or > MaxSportiveStrength)
        {
            throw new ClubGovernanceInvariantViolationException(
                $"Sportive strength must be between {MinSportiveStrength} and {MaxSportiveStrength}.");
        }

        return new Club(id, displayName.Trim(), code, sportiveStrength);
    }

    public static Club Rehydrate(ClubId id, string displayName, ClubCode code, int sportiveStrength) =>
        Create(id, displayName, code, sportiveStrength);
}
