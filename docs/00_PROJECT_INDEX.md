# Proje Dokümantasyon Endeksi

**Durum:** Taslak / Çalışılıyor

## Amaç

Bu belge, `docs/` klasöründeki tüm dokümanların listesini, amaçlarını, mevcut durumlarını ve önerilen çalışma sırasını tek bir yerden takip edebilmek için hazırlanmıştır.

## Ana Referans Belge

Tüm kararların başlangıç noktası:

- **`01_GAME_DESIGN_DOCUMENT.md`** — Ana oyun tasarım dokümanı. Vizyon, tasarım sütunları, tüm sistemlerin genel tanımı ve değişmez proje kuralları burada yer alır. Diğer tüm belgeler bu belgeyi referans alır ve onu çelişmeden detaylandırır.

## Doküman Listesi

| Belge | Amaç | Durum |
|---|---|---|
| `00_PROJECT_INDEX.md` | Dokümantasyonun genel haritası ve çalışma sırası | Çalışılıyor |
| `01_GAME_DESIGN_DOCUMENT.md` | Ana oyun tasarım dokümanı (vizyon, tüm sistemler, kurallar) | Yaşayan belge / geliştirmeye açık |
| `02_MVP_SCOPE.md` | İlk oynanabilir sürümün kesin kapsamı | Kesinleşti |
| `03_DOMAIN_MODEL.md` | Oyunun alan (domain) modeli, aktörler ve bileşenleri | Kesinleşti |
| `04_EVENT_RULE_ENGINE.md` | Olay, bağlam ve sonuç sistemi / kural motoru | Kesinleşti |
| `05_MEMORY_AND_PROMISE_SYSTEM.md` | Hafıza ve söz sistemi | Kesinleşti |
| `06_RELATIONSHIP_SYSTEM.md` | İlişki, kişilik ve motivasyon sistemi | Kesinleşti |
| `07_DIALOGUE_SYSTEM.md` | Diyalog sistemi | Kesinleşti |
| `08_TRANSFER_SYSTEM.md` | Transfer ve sözleşme sistemi | Kesinleşti |
| `09_MATCH_SIMULATION.md` | Maç ve taktik simülasyonu | Kesinleşti |
| `10_MANAGER_CAREER.md` | Teknik direktör kariyeri ve istihdam sistemi | Kesinleşti |
| `11_PLAYER_CAREER.md` | Futbolcu kariyeri, gelişim ve emeklilik sistemi | Kesinleşti |
| `12_WORLD_SIMULATION.md` | Dünya simülasyonu ve zaman akışı sistemi | Kesinleşti |
| `13_SAVE_SYSTEM.md` | Kayıt ve dünya bütünlüğü sistemi | Kesinleşti |
| `14_TEST_STRATEGY.md` | Test stratejisi ve uzun dönem simülasyon testleri | Taslak / Henüz çalışılmadı |
| `15_DECISION_LOG.md` | Alınan tasarım/teknik kararların günlüğü | Başlangıç kayıtlarıyla oluşturuldu |
| `16_INITIAL_ANALYSIS.md` | Ana belgeye dayanan kapsamlı başlangıç analizi | Tamamlandı (ilk sürüm) |
| `17_TECHNOLOGY_AND_ARCHITECTURE_DECISION.md` | Teknoloji yığını ve yüksek seviyeli mimari kararı | Kesinleşti |

## Henüz Hazırlanmayan Belgeler

Aşağıdaki belgeler yalnızca temel iskelet (amaç, sorular, referans, durum) içerir; ayrıntılı tasarım kararları henüz işlenmemiştir:

- `14_TEST_STRATEGY.md`

## Önerilen Çalışma Sırası

1. Ana oyun tasarım belgesinin incelenmesi
2. MVP kapsamının kesinleştirilmesi
3. Domain modelinin belirlenmesi
4. Olay ve kural motorunun tasarlanması
5. Hafıza ve söz sisteminin tasarlanması
6. İlişki sisteminin tasarlanması
7. Dünya simülasyonunun tasarlanması
8. Maç ve transfer sistemlerinin tasarlanması
9. Kayıt stratejisinin belirlenmesi
10. Test stratejisinin hazırlanması
11. Teknoloji ve mimari seçimi
12. Küçük prototiplerin geliştirilmesi

Bu sıra, `01_GAME_DESIGN_DOCUMENT.md` Bölüm 36 (Geliştirme Yaklaşımı) ile tutarlıdır: önce tasarım, sonra veri modeli, sonra kod.
