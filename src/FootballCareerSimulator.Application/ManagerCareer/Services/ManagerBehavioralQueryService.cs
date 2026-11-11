using FootballCareerSimulator.Application.ManagerCareer.Queries;
using FootballCareerSimulator.Domain.ManagerCareer;

namespace FootballCareerSimulator.Application.ManagerCareer.Services;

/// <summary>
/// Menajerin davranış profili sorgu servisi (docs/20 §7).
/// Ham puanlardan oyuncu yüzüne yönelik stil etiketlerini deterministik olarak türetir.
/// </summary>
public sealed class ManagerBehavioralQueryService
{
    public const int DistinctHighThreshold = 65;
    public const int DistinctLowThreshold = 35;

    public ManagerBehavioralProfileReadModel GetProfile(ManagerBehavioralProfile profile)
    {
        ArgumentNullException.ThrowIfNull(profile);

        var badges = new List<string>();

        if (profile.YouthOpportunity >= DistinctHighThreshold)
        {
            badges.Add("Genç Yetenek Hamisi");
        }
        else if (profile.YouthOpportunity <= DistinctLowThreshold)
        {
            badges.Add("Tecrübe Odaklı");
        }

        if (profile.PromiseReliability >= DistinctHighThreshold)
        {
            badges.Add("Sözünün Eri");
        }
        else if (profile.PromiseReliability <= DistinctLowThreshold)
        {
            badges.Add("Vaatleri Riskli");
        }

        if (profile.PublicSupport >= DistinctHighThreshold)
        {
            badges.Add("Basın Kalkanı");
        }
        else if (profile.PublicSupport <= DistinctLowThreshold)
        {
            badges.Add("Açık Sözlü Eleştirmen");
        }

        if (profile.NegotiationPersistence >= DistinctHighThreshold)
        {
            badges.Add("Pazarlıkta İnatçı");
        }

        if (profile.DeadlineAggression >= DistinctHighThreshold)
        {
            badges.Add("Son Gün Avcısı");
        }

        if (profile.RotationConsistency >= DistinctHighThreshold)
        {
            badges.Add("Rotasyon Ustası");
        }

        return new ManagerBehavioralProfileReadModel(
            profile.YouthOpportunity,
            profile.PromiseReliability,
            profile.PublicSupport,
            profile.NegotiationPersistence,
            profile.RotationConsistency,
            profile.DeadlineAggression,
            badges,
            profile.Observations.Count);
    }
}
