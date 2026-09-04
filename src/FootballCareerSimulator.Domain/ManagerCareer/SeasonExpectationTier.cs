namespace FootballCareerSimulator.Domain.ManagerCareer;

/// <summary>
/// Sezon başı yönetim beklentisi (MVP basit bant).
/// </summary>
public enum SeasonExpectationTier
{
    TitleChallenge = 1,
    UpperHalf = 2,
    MidTable = 3,
    LowerHalf = 4,
    Survival = 5,
}

public static class SeasonExpectation
{
    public static SeasonExpectationTier FromSportiveStrength(int sportiveStrength) =>
        sportiveStrength switch
        {
            >= 80 => SeasonExpectationTier.TitleChallenge,
            >= 65 => SeasonExpectationTier.UpperHalf,
            >= 50 => SeasonExpectationTier.MidTable,
            >= 35 => SeasonExpectationTier.LowerHalf,
            _ => SeasonExpectationTier.Survival,
        };

    public static bool MeetsExpectation(SeasonExpectationTier expectation, int leaguePosition, int leagueSize)
    {
        if (leaguePosition < 1 || leagueSize < 1 || leaguePosition > leagueSize)
        {
            return false;
        }

        var half = (leagueSize + 1) / 2;
        return expectation switch
        {
            SeasonExpectationTier.TitleChallenge => leaguePosition <= 3,
            SeasonExpectationTier.UpperHalf => leaguePosition <= half,
            SeasonExpectationTier.MidTable => leaguePosition <= leagueSize - 4,
            SeasonExpectationTier.LowerHalf => leaguePosition <= leagueSize - 2,
            SeasonExpectationTier.Survival => leaguePosition < leagueSize,
            _ => false,
        };
    }
}
