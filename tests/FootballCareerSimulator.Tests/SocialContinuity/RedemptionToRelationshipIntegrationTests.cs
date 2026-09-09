using FootballCareerSimulator.Application.SocialContinuity.Services;
using FootballCareerSimulator.Domain.Competition;
using FootballCareerSimulator.Domain.Interaction;
using FootballCareerSimulator.Domain.PlayerCareer;
using FootballCareerSimulator.Domain.Shared;
using FootballCareerSimulator.Domain.SocialContinuity;
using FootballCareerSimulator.Domain.WorldCalendar;

namespace FootballCareerSimulator.Tests.SocialContinuity;

/// <summary>
/// Entegrasyon testi: Telafi edilen hafızanın ardından oyuncu güveninin toparlanma eğrisine girmesi.
/// docs/20 §12.21 — RedemptionToRelationshipIntegrationTests
/// </summary>
public sealed class RedemptionToRelationshipIntegrationTests
{
    private static readonly GameDate SözVerildi   = GameDate.FromCalendarDate(2026, 8, 1);
    private static readonly GameDate SözKırıldı   = GameDate.FromCalendarDate(2026, 8, 10);
    private static readonly GameDate TelafiGünü   = GameDate.FromCalendarDate(2026, 10, 5);
    private static readonly FixtureId KritikMaç   = new(999);
    private static readonly ActorRef  OyuncuRef   = new(ActorKind.Player, 101);

    [Fact]
    public void TelafiEdilmiş_Hafıza_AktifNüfuzuSıfırlarVeDormantKılar()
    {
        var değerlendirici = new MemoryResolutionEvaluator();

        // Sözün kırılmasıyla oluşan olumsuz hafıza
        var olumsuzHafıza = MemoryRecord.CreateSelectionOmitted(
            new MemoryId(1),
            OyuncuRef,
            new FixtureId(501),
            SözKırıldı);

        Assert.Equal(45, olumsuzHafıza.CurrentInfluence);
        Assert.Equal(MemoryResolutionStatus.Unresolved, olumsuzHafıza.ResolutionStatus);

        // Kritik maçta ilk 11'e dahil ederek telafi et
        var telafiEdilmiş = değerlendirici.EvaluateSelectionRedemption(
            olumsuzHafıza,
            isStartingAppearance: true,
            isCrucialMatch: true,
            TelafiGünü);

        // Telafi sonrası nüfuz sıfırlanmış, durum Dormant'a geçmiş olmalı
        Assert.Equal(MemoryResolutionStatus.Resolved, telafiEdilmiş.ResolutionStatus);
        Assert.Equal(0, telafiEdilmiş.CurrentInfluence);
        Assert.Equal(MemoryStatus.Dormant, telafiEdilmiş.Status);
        Assert.Contains("Kritik", telafiEdilmiş.ResolutionReasonCode);
    }

    [Fact]
    public void TelafiSonrası_DigestOzeti_ÇözülmüşOlarakRaporlar()
    {
        var değerlendirici = new MemoryResolutionEvaluator();

        var aktifHafıza = MemoryRecord.CreateSelectionOmitted(
            new MemoryId(2),
            OyuncuRef,
            new FixtureId(502),
            SözKırıldı);

        var telafiEdilmiş = değerlendirici.EvaluateSelectionRedemption(
            aktifHafıza,
            isStartingAppearance: true,
            isCrucialMatch: true,
            TelafiGünü);

        // Digest üret — telafi edilmiş hafıza "çözüldü" olarak işaretlenmeli
        var digest = değerlendirici.BuildDigest(101, new[] { telafiEdilmiş });

        Assert.Equal(0, digest.ActiveUnresolvedCount);
        Assert.Equal(1, digest.ResolvedCount);
        Assert.True(digest.Memories[0].IsRedeemed);
        // ResolutionStatus Resolved olduğunda IsRedeemed true gelmelidir
        Assert.Equal(MemoryResolutionStatus.Resolved, digest.Memories[0].ResolutionStatus);
    }

    [Fact]
    public void YenidenaçılmışHafıza_ÇiftKatNüfuz_UygulanırVeAktifOlur()
    {
        var değerlendirici = new MemoryResolutionEvaluator();

        // Olumsuz hafıza kısmen çözüldü
        var hafıza = MemoryRecord.CreateSelectionOmitted(
            new MemoryId(3),
            OyuncuRef,
            KritikMaç,
            SözKırıldı);

        var kısmenÇözülmüş = hafıza.MarkResolution(
            MemoryResolutionStatus.PartiallyResolved,
            TelafiGünü,
            "Kısmi çözüm");

        // Sonra tekrar söz kırılınca yara yeniden açılır
        var sonSöz = GameDate.FromCalendarDate(2027, 2, 1);
        var yenidenAçılmış = kısmenÇözülmüş.MarkResolution(
            MemoryResolutionStatus.Reopened,
            sonSöz,
            "Yara tekrar açıldı");

        // Yeniden açıldığında nüfuz taban değerine geri dönmeli (45)
        Assert.Equal(MemoryResolutionStatus.Reopened, yenidenAçılmış.ResolutionStatus);
        Assert.Equal(MemoryStatus.Active, yenidenAçılmış.Status);
        Assert.True(yenidenAçılmış.CurrentInfluence >= 44); // Math.Max(45, 22*2=44)
    }

    [Fact]
    public void NormalMaç_TelafiSağlamaz_SadeceParsıyel_Açılırsa()
    {
        var değerlendirici = new MemoryResolutionEvaluator();

        var hafıza = MemoryRecord.CreateSelectionOmitted(
            new MemoryId(4),
            OyuncuRef,
            new FixtureId(504),
            SözKırıldı);

        // Normal maçta forma verildi ama kritik değil → kısmi çözüm
        var sonuç = değerlendirici.EvaluateSelectionRedemption(
            hafıza,
            isStartingAppearance: true,
            isCrucialMatch: false,
            TelafiGünü);

        // Kritik olmayan maç tam çözüm vermez — PartiallyResolved veya Contradicted
        Assert.NotEqual(MemoryResolutionStatus.Resolved, sonuç.ResolutionStatus);
        Assert.True(sonuç.CurrentInfluence < hafıza.CurrentInfluence);
    }
}
