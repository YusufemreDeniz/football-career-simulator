namespace FootballCareerSimulator.Domain.ManagerCareer;

public enum EmploymentRiskBand
{
    Secure = 1,
    Stable = 2,
    UnderReview = 3,
    Critical = 4,
}

public static class EmploymentRisk
{
    public static EmploymentRiskBand FromConfidence(int boardConfidence) =>
        boardConfidence switch
        {
            >= 70 => EmploymentRiskBand.Secure,
            >= 50 => EmploymentRiskBand.Stable,
            >= 30 => EmploymentRiskBand.UnderReview,
            _ => EmploymentRiskBand.Critical,
        };
}
