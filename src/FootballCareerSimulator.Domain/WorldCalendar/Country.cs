namespace FootballCareerSimulator.Domain.WorldCalendar;

/// <summary>
/// Kurgusal ülke kimliği. Authoritative owner World &amp; Calendar içerik bootstrap'idir;
/// yeni bir bounded context oluşturmaz.
/// </summary>
public sealed class Country
{
    private Country(CountryId id, string displayName, string code)
    {
        Id = id;
        DisplayName = displayName;
        Code = code;
    }

    public CountryId Id { get; }

    public string DisplayName { get; }

    public string Code { get; }

    public static Country Create(CountryId id, string displayName, string code)
    {
        if (string.IsNullOrWhiteSpace(displayName))
        {
            throw new WorldCalendarInvariantViolationException("Country display name cannot be empty.");
        }

        if (string.IsNullOrWhiteSpace(code))
        {
            throw new WorldCalendarInvariantViolationException("Country code cannot be empty.");
        }

        var normalizedCode = code.Trim().ToUpperInvariant();
        if (normalizedCode.Length is < 2 or > 3)
        {
            throw new WorldCalendarInvariantViolationException("Country code must be 2 or 3 characters.");
        }

        return new Country(id, displayName.Trim(), normalizedCode);
    }
}
