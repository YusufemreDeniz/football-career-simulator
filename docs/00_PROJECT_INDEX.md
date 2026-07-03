# Proje Dokümantasyon Endeksi

**Durum:** Ana sistem dokümantasyonu kesinleşti; uygulama öncesi teknik doğrulama aşamasına geçildi

## Amaç

Bu belge, `docs/` klasöründeki tüm dokümanların listesini, amaçlarını, mevcut durumlarını ve önerilen çalışma sırasını tek bir yerden takip edebilmek için hazırlanmıştır.

## Ana Referans Belge

Tüm kararların başlangıç noktası:

- **`01_GAME_DESIGN_DOCUMENT.md`** — Ana oyun tasarım dokümanı. Vizyon, tasarım sütunları, tüm sistemlerin genel tanımı ve değişmez proje kuralları burada yer alır. Diğer tüm belgeler bu belgeyi referans alır ve onu çelişmeden detaylandırır.

## Doküman Listesi

| Belge | Amaç | Durum |
|---|---|---|
| `00_PROJECT_INDEX.md` | Dokümantasyonun genel haritası ve çalışma sırası | Güncel |
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
| `14_TEST_STRATEGY.md` | Test stratejisi ve uzun dönem simülasyon testleri | Kesinleşti |
| `15_DECISION_LOG.md` | Alınan tasarım/teknik kararların günlüğü | Başlangıç kayıtlarıyla oluşturuldu |
| `16_INITIAL_ANALYSIS.md` | Ana belgeye dayanan kapsamlı başlangıç analizi | Tamamlandı (ilk sürüm) |
| `17_TECHNOLOGY_AND_ARCHITECTURE_DECISION.md` | Teknoloji yığını ve yüksek seviyeli mimari kararı | Kesinleşti |
| `18_SPIKE_EXECUTION_PLAN.md` | Altı teknik doğrulama spike'ının uygulama sırası ve çalışma kartları | Kesinleşti |

## Henüz Hazırlanmayan Belgeler

Ayrıntılı tasarımı henüz hazırlanmamış belge bulunmamaktadır.

## Önerilen Çalışma Sırası

1. Ana oyun tasarım belgesinin incelenmesi — Tamamlandı
2. MVP kapsamının kesinleştirilmesi — Tamamlandı
3. Domain modelinin belirlenmesi — Tamamlandı
4. Olay ve kural motorunun tasarlanması — Tamamlandı
5. Hafıza ve söz sisteminin tasarlanması — Tamamlandı
6. İlişki sisteminin tasarlanması — Tamamlandı
7. Dünya simülasyonunun tasarlanması — Tamamlandı
8. Maç ve transfer sistemlerinin tasarlanması — Tamamlandı
9. Kayıt stratejisinin belirlenmesi — Tamamlandı
10. Test stratejisinin hazırlanması — Tamamlandı
11. Teknoloji ve mimari seçimi — Tamamlandı
12. Küçük prototiplerin (teknik doğrulama spike'larının) geliştirilmesi — Sırası ve çalışma kartları planlandı (`18_SPIKE_EXECUTION_PLAN.md`); Kart 0 (minimum repository iskeleti), Kart 1 (CI-lite), Kart 2 (Spike 1 — headless 10 sezon), Kart 3 (Spike 2 — determinizm), Kart 4 (Spike 3 — SQLite save/load/migration) ve Kart 5 (minimum Godot proje kabuğu) tamamlandı, Kart 6 (Spike 4 — 500 futbolculuk Godot UI listesi) sıradaki adım

Bu sıra, `01_GAME_DESIGN_DOCUMENT.md` Bölüm 36 (Geliştirme Yaklaşımı) ile tutarlıdır: önce tasarım, sonra veri modeli, sonra kod.

Madde 1–11 kapsamındaki bütün ana sistem belgeleri (`02_MVP_SCOPE.md`–`14_TEST_STRATEGY.md` ve `17_TECHNOLOGY_AND_ARCHITECTURE_DECISION.md`) kesinleşmiştir (bkz. `15_DECISION_LOG.md`, özellikle D-328). Proje şu an madde 12'ye, yani `17_TECHNOLOGY_AND_ARCHITECTURE_DECISION.md` Bölüm 16'da tanımlanan ve `18_SPIKE_EXECUTION_PLAN.md` içinde sıraya konan altı teknik doğrulama spike'ının yürütülmesi aşamasına geçmiştir; bu aşamadan önce üretim kodu, test projesi veya proje iskeleti oluşturulmamıştır.
