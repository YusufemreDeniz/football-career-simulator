namespace FootballCareerSimulator.Infrastructure;

/// <summary>
/// Spike 3 (bkz. docs/18_SPIKE_EXECUTION_PLAN.md Kart 4) için oluşturulmuş yer tutucu SQLite kayıt
/// şema sürümleridir. Kesin ve kalıcı save şeması `docs/13_SAVE_SYSTEM.md` ve ilgili teknik spike
/// sonuçlarına göre ayrıca kesinleştirilecektir (`docs/15_DECISION_LOG.md` D-284); bu sabitler yalnızca
/// bu spike'ın round-trip/migration/corruption kanıtını üretmek için kullanılır.
/// </summary>
internal static class SqliteSaveSchema
{
    /// <summary>
    /// V1: Form alanı ve bütünlük hash'i yoktu (bu spike'ın "eski sürüm" senaryosu).
    /// V2: Form alanı ve CanonicalStateHash bütünlük alanı eklendi.
    /// </summary>
    public const int CurrentVersion = 2;

    public const int MinSupportedVersion = 1;
}
