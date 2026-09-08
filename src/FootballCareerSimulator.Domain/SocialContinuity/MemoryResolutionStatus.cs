namespace FootballCareerSimulator.Domain.SocialContinuity;

/// <summary>
/// Hafızanın telafi, pekişme veya çözümlenme yaşam döngüsü durumu (Post-MVP Yenilik 7: Redemption Memory).
/// </summary>
public enum MemoryResolutionStatus
{
    /// <summary>
    /// Varsayılan durum: Hafıza aktif veya uyku durumunda, henüz çözümlenmemiş veya sorgulanmamış.
    /// </summary>
    Unresolved = 0,

    /// <summary>
    /// Aynı yöndeki yeni tecrübelerle pekiştirilmiş.
    /// </summary>
    Reinforced = 1,

    /// <summary>
    /// Zıt yönde olumlu/olumsuz yeni bir davranışla sorgulanmış, etkisi sarsılmış.
    /// </summary>
    Contradicted = 2,

    /// <summary>
    /// Kısmen telafi edilmiş, etkisi önemli ölçüde azaltılmış.
    /// </summary>
    PartiallyResolved = 3,

    /// <summary>
    /// Tamamen çözümlenmiş ve barışılmış; aktif ilişkide etki üretmez (CurrentInfluence = 0).
    /// </summary>
    Resolved = 4,

    /// <summary>
    /// Çözümlenmiş eski bir yaranın yeni bir krizle tekrar açılması; çift kat etki üretir.
    /// </summary>
    Reopened = 5,
}
