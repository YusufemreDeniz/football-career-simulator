namespace FootballCareerSimulator.Domain.ManagerCareer;

/// <summary>
/// Menajerin davranış profili (docs/20 §7).
/// Çoklu kanıt boyutlarından oluşur; 0–100 bandında normalize edilir, nötr başlangıç 50'dir.
/// </summary>
public sealed class ManagerBehavioralProfile
{
    public const int NeutralScore = 50;
    public const int MinScore = 0;
    public const int MaxScore = 100;
    public const int MaxBoundedObservations = 100;

    private readonly List<BehavioralObservation> _observations = new();

    public ManagerBehavioralProfile(
        int youthOpportunity = NeutralScore,
        int promiseReliability = NeutralScore,
        int publicSupport = NeutralScore,
        int negotiationPersistence = NeutralScore,
        int rotationConsistency = NeutralScore,
        int deadlineAggression = NeutralScore,
        IEnumerable<BehavioralObservation>? observations = null)
    {
        YouthOpportunity = Clamp(youthOpportunity);
        PromiseReliability = Clamp(promiseReliability);
        PublicSupport = Clamp(publicSupport);
        NegotiationPersistence = Clamp(negotiationPersistence);
        RotationConsistency = Clamp(rotationConsistency);
        DeadlineAggression = Clamp(deadlineAggression);

        if (observations != null)
        {
            _observations.AddRange(observations);
        }
    }

    public int YouthOpportunity { get; private set; }
    public int PromiseReliability { get; private set; }
    public int PublicSupport { get; private set; }
    public int NegotiationPersistence { get; private set; }
    public int RotationConsistency { get; private set; }
    public int DeadlineAggression { get; private set; }

    public IReadOnlyList<BehavioralObservation> Observations => _observations;

    public void AddObservation(BehavioralObservation observation)
    {
        ArgumentNullException.ThrowIfNull(observation);

        if (_observations.Count >= MaxBoundedObservations)
        {
            _observations.RemoveAt(0);
        }
        _observations.Add(observation);

        switch (observation.Category)
        {
            case BehavioralEvidenceCategory.YouthOpportunity:
                YouthOpportunity = Clamp(YouthOpportunity + observation.ScoreDelta);
                break;
            case BehavioralEvidenceCategory.PromiseReliability:
                PromiseReliability = Clamp(PromiseReliability + observation.ScoreDelta);
                break;
            case BehavioralEvidenceCategory.PublicSupport:
                PublicSupport = Clamp(PublicSupport + observation.ScoreDelta);
                break;
            case BehavioralEvidenceCategory.NegotiationPersistence:
                NegotiationPersistence = Clamp(NegotiationPersistence + observation.ScoreDelta);
                break;
            case BehavioralEvidenceCategory.RotationConsistency:
                RotationConsistency = Clamp(RotationConsistency + observation.ScoreDelta);
                break;
            case BehavioralEvidenceCategory.DeadlineAggression:
                DeadlineAggression = Clamp(DeadlineAggression + observation.ScoreDelta);
                break;
        }
    }

    public int GetScore(BehavioralEvidenceCategory category) => category switch
    {
        BehavioralEvidenceCategory.YouthOpportunity => YouthOpportunity,
        BehavioralEvidenceCategory.PromiseReliability => PromiseReliability,
        BehavioralEvidenceCategory.PublicSupport => PublicSupport,
        BehavioralEvidenceCategory.NegotiationPersistence => NegotiationPersistence,
        BehavioralEvidenceCategory.RotationConsistency => RotationConsistency,
        BehavioralEvidenceCategory.DeadlineAggression => DeadlineAggression,
        _ => NeutralScore,
    };

    private static int Clamp(int value) => Math.Clamp(value, MinScore, MaxScore);
}
