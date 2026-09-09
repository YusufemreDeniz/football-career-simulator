using FootballCareerSimulator.Application.EventRuleEvaluation.Infrastructure;
using FootballCareerSimulator.Application.EventRuleEvaluation.Services;
using FootballCareerSimulator.Application.Interaction.Infrastructure;
using FootballCareerSimulator.Application.Interaction.Services;
using FootballCareerSimulator.Application.SocialContinuity.Infrastructure;
using FootballCareerSimulator.Application.SocialContinuity.Services;
using FootballCareerSimulator.Domain.EventRuleEvaluation;
using FootballCareerSimulator.Domain.Interaction;
using FootballCareerSimulator.Domain.ManagerCareer;
using FootballCareerSimulator.Domain.PlayerCareer;
using FootballCareerSimulator.Domain.Shared;
using FootballCareerSimulator.Domain.SocialContinuity;
using FootballCareerSimulator.Domain.WorldCalendar;

namespace FootballCareerSimulator.Tests.SocialContinuity;

/// <summary>
/// Entegrasyon testi: Kırılan söz → güven kaybı → doğru "Neden?" nedensellik kaydının oluşması.
/// docs/20 §6.21 — RelationshipCausalityIntegrationTests
/// </summary>
public sealed class RelationshipCausalityIntegrationTests
{
    private static readonly GameDate SözVerildi = GameDate.FromCalendarDate(2026, 8, 1);
    private static readonly GameDate SözKırıldı = GameDate.FromCalendarDate(2026, 8, 20);
    private static readonly GameDate SorguGünü   = GameDate.FromCalendarDate(2026, 8, 21);

    [Fact]
    public void KırılanSöz_GüvenKaybıVeNedensellikKaydı_TutarlıÜretilir()
    {
        // Altyapı bileşenlerini başlat
        var causalityStore     = new InMemoryCausalityStore();
        var causalityJournal   = new CausalityJournalQueryService(causalityStore);
        var requestStore       = new InMemoryDecisionRequestStore();
        var relationshipStore  = new InMemoryRelationshipStore();
        var promiseStore       = new InMemoryPromiseStore();
        var memoryStore        = new InMemoryMemoryStore();

        var queryService = new DecisionRequestQueryService(
            requestStore,
            relationshipStore,
            promiseStore,
            memoryStore,
            null,
            causalityJournal);

        var oyuncuId  = new PlayerId(101);
        var menajerId = new ManagerId(1);
        var kulübId   = new ClubId(1);

        // Kırık forma sözü oluştur
        var kırıkSöz = Promise.Rehydrate(
            new PromiseId(10),
            PromiseKind.StartingOpportunity,
            new ActorRef(ActorKind.Manager, menajerId.Value),
            new ActorRef(ActorKind.Player,  oyuncuId.Value),
            kulübId,
            targetStarts: 5,
            startsGiven: 0,
            deadlineOn: SözKırıldı,
            createdOn:  SözVerildi,
            status:     PromiseStatus.Broken,
            terminalOn: SözKırıldı,
            countedFixtureIds: Array.Empty<long>());
        promiseStore.Upsert(kırıkSöz);

        // Düşük güven ilişkisi oluştur (söz kırıldı → güven düştü)
        var ilişki = RelationshipRecord.Rehydrate(
            new RelationshipId(1),
            new ActorRef(ActorKind.Player,  oyuncuId.Value),
            new ActorRef(ActorKind.Manager, menajerId.Value),
            trust:  18,   // Düşük güven bandı (≤ 33)
            respect: 45,
            professionalCompatibility: 50,
            status:  RelationshipStatus.Active,
            createdOn:       SözVerildi,
            lastChangedOn:   SözKırıldı,
            lastChangeReasonCode: "PromiseBrokenTrust",
            processedEffectKeys: Array.Empty<string>());
        relationshipStore.Upsert(ilişki);

        // Transfer talebi açık — nedensellik burada sorgulanacak
        var karar = DecisionRequest.OpenTransferRequest(
            new DecisionRequestId(55),
            menajerId,
            oyuncuId,
            kulübId,
            SorguGünü,
            SorguGünü.AddDays(3),
            isHardBlocker: true);
        requestStore.Upsert(karar);

        // Act: Nedensellik açıklaması üret
        var açıklama = queryService.ExplainStructuredCausality(karar.DecisionRequestId);

        // Assert: Açıklama üretilmiş ve birincil neden sözün tutulmamasına işaret ediyor
        Assert.NotNull(açıklama);
        Assert.Equal(CausalityDimension.TransferResult, açıklama.Dimension);
        Assert.Contains("tutulmaması", açıklama.PrimaryCause);
        Assert.NotEmpty(açıklama.SupportingCauses);
        Assert.Contains("güvenin düşük", açıklama.SupportingCauses[0]);
        Assert.Equal("Yüksek", açıklama.Confidence);

        // Assert: Aynı kayıt nedensellik deposuna da yazılmış olmalı (idempotency gateı bypass etmeden)
        var kayıt = causalityStore.FindByTargetEventId(açıklama.TargetEventId);
        Assert.NotNull(kayıt);
        Assert.Equal(CausalityDimension.TransferResult, kayıt.Dimension);
    }

    [Fact]
    public void KırılanSöz_İkiSorgu_AynıDimension_AynıPrimaryCause_DöndürülenAçıklama_Tutarlı()
    {
        // İkinci sorgulama aynı semantik sonucu döndürmeli (deterministik sonuç)
        var causalityStore    = new InMemoryCausalityStore();
        var causalityJournal  = new CausalityJournalQueryService(causalityStore);
        var requestStore      = new InMemoryDecisionRequestStore();
        var relationshipStore = new InMemoryRelationshipStore();
        var promiseStore      = new InMemoryPromiseStore();
        var memoryStore       = new InMemoryMemoryStore();

        var queryService = new DecisionRequestQueryService(
            requestStore, relationshipStore, promiseStore, memoryStore, null, causalityJournal);

        var oyuncuId  = new PlayerId(202);
        var menajerId = new ManagerId(1);
        var kulübId   = new ClubId(1);

        var kırıkSöz = Promise.Rehydrate(
            new PromiseId(20),
            PromiseKind.StartingOpportunity,
            new ActorRef(ActorKind.Manager, menajerId.Value),
            new ActorRef(ActorKind.Player,  oyuncuId.Value),
            kulübId,
            targetStarts: 3,
            startsGiven: 0,
            deadlineOn: SözKırıldı,
            createdOn:  SözVerildi,
            status:     PromiseStatus.Broken,
            terminalOn: SözKırıldı,
            countedFixtureIds: Array.Empty<long>());
        promiseStore.Upsert(kırıkSöz);

        var ilişki = RelationshipRecord.Rehydrate(
            new RelationshipId(2),
            new ActorRef(ActorKind.Player,  oyuncuId.Value),
            new ActorRef(ActorKind.Manager, menajerId.Value),
            trust: 20, respect: 50, professionalCompatibility: 50,
            status: RelationshipStatus.Active,
            createdOn: SözVerildi, lastChangedOn: SözKırıldı,
            lastChangeReasonCode: "PromiseBrokenTrust",
            processedEffectKeys: Array.Empty<string>());
        relationshipStore.Upsert(ilişki);

        var karar = DecisionRequest.OpenTransferRequest(
            new DecisionRequestId(66),
            menajerId, oyuncuId, kulübId,
            SorguGünü, SorguGünü.AddDays(3),
            isHardBlocker: true);
        requestStore.Upsert(karar);

        // İlk ve ikinci sorgu aynı semantik sonucu üretmeli
        var ilkAçıklama    = queryService.ExplainStructuredCausality(karar.DecisionRequestId);
        var ikinciAçıklama = queryService.ExplainStructuredCausality(karar.DecisionRequestId);

        Assert.NotNull(ilkAçıklama);
        Assert.NotNull(ikinciAçıklama);

        // Her iki çağrı da aynı Dimension ve aynı birincil neden metnini döndürmeli (deterministik)
        Assert.Equal(ilkAçıklama!.Dimension,     ikinciAçıklama!.Dimension);
        Assert.Equal(ilkAçıklama.PrimaryCause,   ikinciAçıklama.PrimaryCause);
        Assert.Equal(ilkAçıklama.Confidence,     ikinciAçıklama.Confidence);
    }
}
