namespace FootballCareerSimulator.Application.TrainingPhysicalState.Queries;

/// <summary>
/// Antrenman Sahası — hafta içi çalışmanın sesi: yük, form ve sakatlığa göre
/// deterministik saha raporu. Hazırlık kararı sonucu görür.
/// </summary>
public sealed record TrainingGroundDigest(
    string BrandTitle,
    string VoiceLine)
{
    public const string Brand = "Antrenman Sahası";

    /// <summary>
    /// İşsiz veya plan yoksa null döner — sadece gerçek hazırlık günleri ses verir.
    /// Öncelik: sakatlık → yorgunluk → form → yoğunluk/odak tonu → dengeli rapor.
    /// </summary>
    public static TrainingGroundDigest? Compose(
        ClubTrainingSummaryReadModel training,
        int? daysUntilNextMatch = null)
    {
        ArgumentNullException.ThrowIfNull(training);

        if (training.ClubId is null || !training.HasPlan)
        {
            return null;
        }

        var fatigue = training.AverageFatigue ?? 0;
        var fitness = training.AverageFitness ?? 0;
        var injured = training.InjuredSlotCount;

        var line = ResolveVoiceLine(fatigue, fitness, injured, training, daysUntilNextMatch);
        return new TrainingGroundDigest(Brand, line);
    }

    private static string ResolveVoiceLine(
        int fatigue,
        int fitness,
        int injured,
        ClubTrainingSummaryReadModel training,
        int? daysUntilNextMatch)
    {
        if (injured >= 3)
        {
            return "Sakat sayısı arttı — antrenman kısıtlı, doktor kapıda.";
        }

        if (fatigue >= 60 && daysUntilNextMatch is <= 2)
        {
            return "Bacaklar ağır — maç yakınken yükü düşür.";
        }

        if (fatigue >= 60)
        {
            return "Yorgunluk sızıyor — antrenman sonu sessiz.";
        }

        if (fitness >= 80)
        {
            return "Form zirvede — top ayakta, neşe yerinde.";
        }

        if (training.Intensity == (int)Domain.TrainingPhysicalState.TrainingIntensity.High)
        {
            return "Sert çalıştık — ter sahadan damladı.";
        }

        if (training.Focus == (int)Domain.TrainingPhysicalState.TrainingFocus.Recovery)
        {
            return "Toparlanma havası — bacaklar dinleniyor.";
        }

        return "Plan oturuyor — çalışma ritmi yerinde.";
    }
}
