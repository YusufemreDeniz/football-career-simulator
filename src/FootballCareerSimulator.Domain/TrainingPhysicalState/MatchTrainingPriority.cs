namespace FootballCareerSimulator.Domain.TrainingPhysicalState;

/// <summary>
/// Sıradaki maça kadar uygulanabilen, tek maçlık hazırlık öncelikleri.
/// Sayısal değerler UI action kodu olarak kalıcıdır; yeniden sıralanmamalıdır.
/// </summary>
public enum MatchTrainingPriority
{
    Recovery = 1,
    MatchSharpness = 2,
    PressResistance = 3,
    DefensiveTransitions = 4,
    AttackingPatterns = 5,
}
