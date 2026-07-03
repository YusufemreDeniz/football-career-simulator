# Test Stratejisi ve Uzun Dönem Simülasyon Testleri

**Belge:** `docs/14_TEST_STRATEGY.md`
**Durum:** Kesinleşti
**Ana referans:** `docs/01_GAME_DESIGN_DOCUMENT.md`
**Kesin MVP sınırı:** `docs/02_MVP_SCOPE.md`
**Domain sınırları:** `docs/03_DOMAIN_MODEL.md`
**Olay ve kural sözleşmeleri:** `docs/04_EVENT_RULE_ENGINE.md`
**Hafıza ve söz sözleşmeleri:** `docs/05_MEMORY_AND_PROMISE_SYSTEM.md`
**İlişki sözleşmeleri:** `docs/06_RELATIONSHIP_SYSTEM.md`
**Diyalog ve karar sözleşmeleri:** `docs/07_DIALOGUE_SYSTEM.md`
**Transfer ve sözleşme sözleşmeleri:** `docs/08_TRANSFER_SYSTEM.md`
**Maç sözleşmeleri:** `docs/09_MATCH_SIMULATION.md`
**Teknik direktör kariyeri sözleşmeleri:** `docs/10_MANAGER_CAREER.md`
**Futbolcu kariyeri sözleşmeleri:** `docs/11_PLAYER_CAREER.md`
**Dünya simülasyonu ve zaman akışı sözleşmeleri:** `docs/12_WORLD_SIMULATION.md`
**Kayıt ve dünya bütünlüğü sözleşmeleri:** `docs/13_SAVE_SYSTEM.md`
**Mimari yön:** `docs/17_TECHNOLOGY_AND_ARCHITECTURE_DECISION.md`
**İlgili karar günlüğü:** `docs/15_DECISION_LOG.md`

---

## 1. Belgenin Amacı

Bu belge, Football Career Simulator projesinin ana oyun tasarım belgesindeki Kural 4 ve Kural 9'u (`docs/01_GAME_DESIGN_DOCUMENT.md` Bölüm 30) ve `docs/02_MVP_SCOPE.md` Bölüm 22'deki kesin MVP kabul kriterlerini nasıl otomatik ve ölçülebilir biçimde doğrulanacağını tanımlayan çapraz kalite sözleşmesidir.

Belgenin amacı en az şunları kapsar:

* Domain invariant'larının otomatik doğrulanmasını sağlamak.
* Sistemlerin birbirlerinin authoritative state'ini ihlal etmediğini doğrulamak.
* Aynı domain etkisinin ikinci kez uygulanmasını engelleyen idempotency sözleşmelerini doğrulamak.
* Aynı snapshot, input dizisi, content/rule sürümü ve seed ile aynı semantic sonucun üretildiğini doğrulamak.
* Farklı seed'lerin farklı fakat geçerli kariyerler ürettiğini doğrulamak.
* Save/load round-trip sonrasında semantic state eşdeğerliğini doğrulamak.
* Migration ve recovery süreçlerinin kullanıcı kaydını bozmadığını doğrulamak.
* Dünya simülasyonunun en az on sezon tamamlanabildiğini kanıtlamak.
* Yaklaşık 500 futbolculuk ve 20 kulüplük dünyanın uzun dönemde bütünlüğünü koruduğunu doğrulamak.
* Binlerce Match çalışmasında invalid state oluşmadığını doğrulamak.
* Maç sonuçlarının güç farkıyla ilişkili fakat deterministik olarak tek sonuca kilitlenmemiş olduğunu ölçmek.
* Transfer, Promise, Relationship, Memory, career ve world event sistemlerinin gerçek çapraz sistem sonuçları ürettiğini doğrulamak.
* Event, history, process, memory ve save verisinin kontrolsüz büyümediğini ölçmek.
* Performans gerilemelerini görünür hâle getirmek.
* Hataların seed, scenario ve checkpoint bilgileriyle tekrar üretilebilmesini sağlamak.
* Regression'ların aynı hata tekrar oluşmadan önce testle yakalanmasını sağlamak.
* UI açılmadan domain ve simulation doğrulaması yapabilmek.
* MVP kabul kriterlerini otomatik veya ölçülebilir doğrulama kapılarına dönüştürmek.

Bu belge:

* test kodu üretmez,
* test project dosyası, `.csproj` veya solution yapısı üretmez,
* package reference veya package sürümü belirlemez,
* CI workflow veya YAML üretmez,
* benchmark kodu üretmez,
* fixture dosyası (JSON, SQLite, save artefact) üretmez,
* kesin coverage yüzdesi belirlemez,
* kesin çalışma süresi eşiği belirlemez,
* kesin memory limiti belirlemez,
* kesin istatistiksel dağılım aralığı belirlemez,
* GDD'yi veya `docs/02_MVP_SCOPE.md` kapsamını değiştirmez,
* önceki kesinleşmiş belgelerin domain sözleşmelerini değiştirmez.

---

## 2. Referanslar ve Kapsam

Kaynak önceliği:

1. `docs/01_GAME_DESIGN_DOCUMENT.md`
2. `docs/02_MVP_SCOPE.md`
3. `docs/03_DOMAIN_MODEL.md`
4. `docs/04_EVENT_RULE_ENGINE.md`
5. `docs/05_MEMORY_AND_PROMISE_SYSTEM.md`
6. `docs/06_RELATIONSHIP_SYSTEM.md`
7. `docs/07_DIALOGUE_SYSTEM.md`
8. `docs/08_TRANSFER_SYSTEM.md`
9. `docs/09_MATCH_SIMULATION.md`
10. `docs/10_MANAGER_CAREER.md`
11. `docs/11_PLAYER_CAREER.md`
12. `docs/12_WORLD_SIMULATION.md`
13. `docs/13_SAVE_SYSTEM.md`
14. `docs/17_TECHNOLOGY_AND_ARCHITECTURE_DECISION.md`
15. `docs/15_DECISION_LOG.md`

Bu belge, `docs/03_DOMAIN_MODEL.md` Bölüm 5'te kesinleşen 14 bounded context yapısını değiştirmez ve yeni bir bounded context oluşturmaz. `docs/12_WORLD_SIMULATION.md` Bölüm 35 ve `docs/13_SAVE_SYSTEM.md` Bölüm 41, ayrıntılı genel test stratejisi sorumluluğunu açıkça bu belgeye bırakmıştır; bu belge o sorumluluğu üstlenir ve ilgili alt sistem belgelerindeki test aileleriyle tutarlı, konsolide bir sözleşme üretir.

Bu belge her alt sistem belgesinde zaten var olan "Test Matrisi" veya "Test Gereksinimleri" bölümlerini geçersiz kılmaz; onlarla tutarlı, onları tamamlayan ve on sezonluk/çapraz sistem boyutunu ekleyen üst düzey bir sözleşme sağlar. Bir çelişki tespit edilirse, ilgili alt sistem belgesi bu belgeye göre önceliklidir (Bölüm 2'deki kaynak önceliği listesi ile uyumlu).

---

## 3. Kritik Mimari Ayrım

### 3.1. Test Strategy bir domain sistemi değildir

Test Strategy:

* yeni bir bounded context,
* runtime domain state sahibi,
* oyun içi aggregate,
* event producer,
* simulation sonucu değiştiren sistem

değildir.

Test stratejisi; domain sözleşmelerinin, architectural boundary'lerin, deterministic simulation davranışının, save/load bütünlüğünün, uzun dönem dünya davranışının, performans ve veri büyümesinin nasıl doğrulanacağını tanımlayan çapraz kalite sözleşmesidir.

Test araçları production domain state'ini yönetemez.

### 3.2. Test kodu ile üretim kodu sınırı

Testler:

* public ve tanımlı Application/Domain sözleşmelerini kullanmalıdır,
* gerekli test seam'leri üzerinden clock, RNG, persistence ve content bağımlılıklarını kontrol etmelidir,
* yalnız testi kolaylaştırmak için production invariant'larını devre dışı bırakmamalıdır,
* doğrudan database satırı değiştirerek geçerli domain sonucu taklit etmemelidir,
* UI üzerinden çalışmak zorunda olmamalıdır.

Test yardımcıları authoritative business rule sahibi olamaz.

### 3.3. Test sonucu ile oyun sonucu ayrımı

Test sonucu; pass, fail, metric, diagnostic veya baseline comparison gibi kalite bilgisidir.

Test sonucu hiçbir runtime oyun state'inin authoritative girdisi değildir.

### 3.4. Test ortamı ile production ortamı ayrımı

Testlerde kullanılan sabit clock, deterministic RNG, in-memory adapter, geçici SQLite, fake file system, controlled content catalog ve scenario fixture; production domain kurallarını değiştiremez.

Adapter değişebilir; semantic domain sözleşmesi değişemez.

---

## 4. Kesin Teknoloji ve Test Altyapısı Yönü

Aşağıdaki kesinleşmiş teknoloji kararları (`docs/17_TECHNOLOGY_AND_ARCHITECTURE_DECISION.md` Bölüm 6, 9, 13, 15) bu belgede aynen korunur:

* Ana üretim ve test dili C#'tır.
* Ana test altyapısı saf .NET test projeleridir.
* Ana test çalıştırma yolu `dotnet test`tir.
* Test framework yönü, repository'de sabitlenecek güncel kararlı xUnit.net sürümüdür.
* Ayrı bir saf .NET headless simulation runner bulunacaktır.
* Testlerin büyük bölümü Godot editörü, scene tree, renderer veya GPU gerektirmeden çalışmalıdır.
* Domain, Simulation ve Application katmanları Godot bağımsız test edilebilmelidir.
* Persistence entegrasyon testlerinde geçici SQLite save artefact'ları kullanılabilir.
* Godot headless testleri yalnız presentation/import/export/engine integration gibi ayrı alanlarda kullanılmalıdır.
* İnternet veya harici üretken yapay zekâ servisi testlerin çalışması için zorunlu olamaz.
* CI sağlayıcısı yönü GitHub Actions'tır.
* Birincil CI runner yönü Windows'tur.
* Kesin package sürümleri, workflow YAML'ları ve proje yapısı bu belgede üretilmeyecektir.

---

## 5. Bağlayıcı Test İlkeleri

1. Test edilebilirlik domain kurallarını zayıflatma gerekçesi değildir.
2. Her ana sistem için unit, invariant, integration ve long-running test aileleri bulunmalıdır.
3. Testler yalnız happy path'i doğrulayamaz.
4. Her kritik lifecycle terminal ve invalid transition testlerine sahip olmalıdır.
5. Bir sistemin başarı kriteri yalnız "exception oluşmadı" olamaz.
6. Deterministik testler gerçek duvar saatine, thread scheduling'e veya collection iteration sırasına bağımlı olamaz.
7. Testlerde global veya gizli RNG kullanılamaz.
8. Test scenario'su seed, version ve input dizisini açıkça bilmelidir.
9. Aynı scenario ve seed başarısızlık sonrasında tekrar çalıştırılabilmelidir.
10. İstatistiksel testler tek maç veya tek kariyer sonucuna dayanamaz.
11. Balance testi ile invariant testi birbirinden ayrılmalıdır.
12. Performans testi ile correctness testi birbirinin yerine geçemez.
13. Snapshot veya golden test, yanlış domain davranışını yalnız "mevcut çıktı" olduğu için onaylayamaz.
14. UI metni veya localization çıktısı domain test oracle'ı olamaz.
15. Persistence testi yalnız dosyanın açılabildiğini doğrulamakla yetinemez.
16. Save/load testi semantic state eşdeğerliğini doğrulamalıdır.
17. Retry, duplicate delivery ve partial failure test edilmelidir.
18. Testler foreign state'i doğrudan mutation ile kurmamalıdır.
19. Test fixture'ları geçerli domain command veya açık rehydration/test factory sözleşmeleriyle oluşturulmalıdır.
20. Testlerin sırası birbirinden bağımsız olmalıdır.
21. Bir test başka testin bıraktığı database, dosya, RNG veya static state'e bağımlı olamaz.
22. Başarısız test diagnostic bilgi üretmelidir.
23. Flaky test normal kabul edilemez.
24. Failing test'i otomatik retry ile yeşile çevirmek varsayılan çözüm olamaz.
25. Exact coverage, performance ve distribution eşikleri ölçüm yapılmadan sessizce belirlenemez.

---

## 6. Test Sınıflandırması

### 6.1. Unit Test

Tek bir saf kural, value object, policy veya küçük domain davranışını dış bağımlılık olmadan doğrular.

### 6.2. Aggregate Test

Aggregate lifecycle, command validation, invariant ve emitted Domain Event davranışını doğrular.

### 6.3. Invariant Test

Belirli bir örnek çıktının ötesinde her geçerli state'te korunması gereken değişmezleri doğrular.

### 6.4. Contract Test

Bounded context veya katmanlar arasındaki command, event, snapshot, query ve adapter sözleşmelerini doğrular.

### 6.5. Application / Use Case Test

Transaction, idempotency, orchestration ve çok context'li süreç akışını doğrular.

### 6.6. Integration Test

Birden fazla gerçek bileşeni birlikte çalıştırır. Örnekler: Application + Domain + Simulation; Application + SQLite adapter; save/load + migration; content loading + semantic validation.

### 6.7. Component Test

Bir bounded context veya teknik bileşeni dış sınırları kontrollü adapter'larla bütün olarak doğrular.

### 6.8. Headless End-to-End Test

Godot veya UI açmadan gerçek Application, Simulation, Domain ve gerekli Infrastructure adapter'ları üzerinden oyuncu veya scheduler akışını doğrular.

### 6.9. Property-Based Test

Geniş ve geçerli input uzaylarında invariant'ların korunmasını doğrular. Kesin property-based testing kütüphanesi bu belgede seçilmez.

### 6.10. Metamorphic Test

Doğrudan kesin sonuç oracle'ının zor olduğu alanlarda, kontrollü input değişikliklerinin beklenen yön veya değişmezlik ilişkisini doğrular. Örnekler: aynı snapshot + aynı seed → aynı sonuç; yalnız presentation değişimi → aynı domain sonucu; takım gücü kontrollü artırıldığında uzun örneklemde başarı ihtimalinin anlamsız biçimde düşmemesi.

### 6.11. Determinism Test

Aynı canonical başlangıç state'i ve input dizisinin aynı semantic committed sonucu üretmesini doğrular.

### 6.12. Statistical / Distribution Test

Çok sayıda simülasyon sonucunun makul dağılım ve ilişki özelliklerini ölçer. Tekil sonucu doğrulamaz.

### 6.13. Regression Test

Daha önce tespit edilmiş bir hatanın yeniden oluşmasını engeller.

### 6.14. Golden Fixture Test

Sürüm kontrollü scenario, save veya canonical output fixture'ının beklenen semantic sözleşmeyle uyumunu doğrular. Golden fixture fiziksel byte düzenine gereksiz yere bağlanmamalıdır.

### 6.15. Failure Injection Test

Belirli işlem aşamalarında kontrollü hata oluşturarak rollback, retry, backup, idempotency ve recovery davranışını doğrular.

### 6.16. Soak Test

Uzun süre veya çok sayıda simülasyon adımı çalıştırarak memory, event, process, history ve save büyümesini gözlemler.

### 6.17. Performance Test

Runtime, allocation, memory, save/load süresi ve work-item hacmi gibi teknik ölçümleri toplar.

### 6.18. Content Validation Test

Authored content'in schema, stable ID, reference ve semantic kurallarını doğrular.

### 6.19. Documentation Validation

Belge bağlantıları, karar ID sırası, metadata ve index tutarlılığını doğrular.

### 6.20. Presentation Smoke Test

Godot tarafında scene açılışı, import, temel navigation ve Application bağlantısını doğrular; domain doğrulamasının yerine geçmez.

---

## 7. Test Piramidi ve Çalışma Maliyeti

Katı test oranları belirlenmez; ancak aşağıdaki yön bağlayıcıdır:

* En geniş ve en sık çalışan katman saf unit, aggregate ve invariant testleridir.
* Application ve contract testleri orta sıklıkta çalışır.
* SQLite, migration ve geniş integration testleri daha kontrollü çalışır.
* Headless season ve multi-season testleri scheduled veya özel suite'lerde çalışabilir.
* Godot presentation testleri domain testlerinden ayrı tutulur.
* Binlerce Match ve çok seed'li on sezon testleri her küçük local değişiklikte zorunlu olmayabilir.
* Uzun testlerin seyrek çalışması, hiç çalışmaması anlamına gelemez.
* Her test ailesi açık execution category veya trait taşımalıdır.

Kavramsal kategoriler: `Fast`, `Unit`, `Invariant`, `Contract`, `Integration`, `Determinism`, `SaveLoad`, `Simulation`, `Statistical`, `Performance`, `Soak`, `Migration`, `GodotIntegration`. Bunlar kesin enum veya attribute olarak üretilmez; yalnız kavramsal sınıflandırmadır.

---

## 8. Test Scenario Modeli

Her tekrar üretilebilir simulation scenario'su en az şu kavramsal bilgileri taşımalıdır:

* `ScenarioId`
* açıklama
* test amacı
* başlangıç world fixture veya builder reference
* root seed
* RNG version
* simulation version
* content version
* rule/model version bilgileri
* ordered input veya command sequence
* hedef GameDate veya season sayısı
* fidelity profile
* expected hard invariant'lar
* expected metric set'i
* timeout veya work-budget yönü
* diagnostic verbosity
* fixture schema version

Kesin class veya JSON formatı bu belgede üretilmez.

---

## 9. Standart Test Dünyaları

### 9.1. Minimal Domain World

Az sayıda entity ile tek lifecycle veya tek context entegrasyonunu hızlı doğrular.

### 9.2. Vertical Slice World

`docs/02_MVP_SCOPE.md` Bölüm 6.1 ve Bölüm 20'deki ilk dikey kesit ile uyumlu olarak; bir oyuncu kulübü, sınırlı rakipler, gerçek kadro, training, Match, Promise, Relationship, Memory ve save/load akışını doğrular.

### 9.3. Full MVP World

* 1 kurgusal ülke
* 1 profesyonel lig
* 20 kulüp
* yaklaşık 460 contracted Player
* yaklaşık 40 free agent
* yaklaşık 500 active Player
* her kulüp için active Manager
* çift devreli 38 maçlık sezon

ölçeğini kullanır (`docs/02_MVP_SCOPE.md` Bölüm 17 ile uyumlu).

### 9.4. Ten-Season World

Full MVP World'ü en fazla on tamamlanmış sezon boyunca ilerletir (`docs/02_MVP_SCOPE.md` Bölüm 5.2 ile uyumlu).

### 9.5. Match Distribution World

Kontrollü takım profilleriyle binlerce Match çalıştırır.

### 9.6. Failure World

Eksik referans, duplicate delivery, interrupted save, failed process veya corrupted artefact gibi kontrollü hata koşullarını içerir.

### 9.7. Migration Corpus

Desteklenen eski save/schema/content sürümlerini temsil eden versioned fixture kümesidir. Kesin fixture sayısı bu belgede belirlenmez.

---

## 10. Test Oracle Hiyerarşisi

Doğrulama önceliği aşağıdaki yöndedir:

1. Hard domain invariant
2. Lifecycle ve state-transition doğruluğu
3. Authoritative ownership sınırı
4. Idempotency ve effect uniqueness
5. Referential integrity
6. Deterministic semantic equality
7. Application/process completion sözleşmesi
8. Açıklanabilir event ve result metadata'sı
9. Statistical veya balance expectation
10. Performance ve growth baseline
11. Presentation output

Bir UI metninin değişmesi domain regression anlamına gelmez. Bir golden snapshot'ın değişmemesi de tek başına doğru domain davranışı kanıtı değildir.

---

## 11. Canonical State Karşılaştırması

Determinism ve round-trip testlerinde fiziksel object graph veya SQLite byte dizisi yerine semantic canonical state kullanılmalıdır (`docs/13_SAVE_SYSTEM.md` Bölüm 27, D-276 ile uyumlu).

Canonical comparison en az şu ilkeleri desteklemelidir:

* collection sırası semantic değilse stable ID ile normalize edilir,
* technical timestamp'ler domain eşitliğinin dışında tutulabilir,
* generated identity gerçekten domain state ise korunur,
* transient cache ve projection karşılaştırılmaz,
* committed authoritative state karşılaştırılır,
* pending process ve idempotency state'i gerekli kapsamda karşılaştırılır,
* RNG state ve simulation cursor karşılaştırılır,
* farklı serialization düzeni aynı semantic state olabilir.

Canonical comparison'ın kesin serialization ve hash algoritması bu belgede belirlenmez.

---

## 12. Determinizm Stratejisi

Bağlayıcı sözleşme (`docs/12_WORLD_SIMULATION.md` Bölüm 33 ve `docs/04_EVENT_RULE_ENGINE.md` Bölüm 10 ile uyumlu):

> Aynı doğrulanmış başlangıç snapshot'ı, aynı ordered command/input dizisi, aynı simulation/content/rule/RNG sürümleri ve aynı seed ile çalıştırıldığında aynı semantic committed domain sonuçları üretilmelidir.

Testler en az şunları doğrulamalıdır:

* aynı run'ın tekrarında aynı sonuç,
* save/load sınırı eklenince aynı sonuç,
* farklı collection insertion order ile aynı sonuç,
* farklı UI frame rate ile aynı sonuç,
* headless runner ve Godot presentation üzerinden aynı domain sonuçları,
* farklı thread scheduling'in sonucu değiştirmemesi,
* aynı Match snapshot ve seed ile aynı Match Result,
* aynı World checkpoint ve seed ile aynı due-work sırası,
* RNG stream ayrımının ilgisiz sistemleri gereksiz yere değiştirmemesi.

Farklı seed testleri; aynı sonucu üretmek zorunda değildir, invariant'ları korumalıdır ve en az bazı anlamlı sonuçlarda çeşitlilik üretmelidir. Kesin çeşitlilik yüzdesi bu belgede belirlenmez.

---

## 13. İstatistiksel ve Denge Testleri

İstatistiksel testler hard correctness testlerinden ayrılır.

### 13.1. Match ölçümleri

Gol ortalaması, beraberlik oranı, home/away etkisi, güç farkı ile kazanma ilişkisi, aşırı skor oranı, kart ve sakatlık dağılımı, oyuncu performans dağılımı, güçlü takımın uzun örneklem başarısı, güçlü takımın bütün maçları kazanmaması.

### 13.2. Transfer ölçümleri

Transfer sayısı, free-agent hareketi, squad need ile transfer ilişkisi, maaşın tek belirleyici olmaması, farklı profile ve motivation etkileri, transfer window yoğunluğu, başarısız veya cancelled süreç oranları.

### 13.3. Career ölçümleri

Manager dismissal ve employment değişimi, unemployment süreleri, Job Offer çeşitliliği, Player development, decline, retirement, generated Player sayısı, active population devamlılığı.

### 13.4. Social ölçümleri

Promise fulfillment/breach dağılımı, Memory üretimi ve retention, Relationship değişim nedenleri, aynı event'in aşırı tekrar oranı, pending Decision Request birikimi.

### 13.5. World ölçümleri

Club strength çeşitlenmesi, manager turnover, transfer hareketliliği, standings çeşitliliği, event diversity, event repetition, farklı seed'lerde kariyer ayrışması.

Bağlayıcı kurallar:

* Tek bir seed balance kararı için yeterli değildir.
* Tek bir run kesin olasılık testi değildir.
* Exact kabul aralıkları ölçüm ve dengeleme çalışması olmadan belirlenmez.
* İstatistiksel failure diagnostic olarak seed set'i ve dağılım raporu üretmelidir.
* İstatistiksel testler küçük örneklem nedeniyle sık ve rastgele fail olacak biçimde tasarlanmamalıdır.
* Geniş toleranslı smoke test ile dar balance regression testi ayrılmalıdır.

---

## 14. Property-Based Test Yönü

Property-based testler en az şu alanlarda düşünülmelidir:

* GameDate monotonluğu
* identity uniqueness ve preservation
* active Contract tekilliği
* active Manager tekilliği
* Fixture ve Match lifecycle geçerliliği
* Match Result'ın tek kabulü
* Promise terminal state tekilliği
* Transfer completion atomikliği
* retired Player'ın active Registration taşımaması
* completed Simulation Step'in tekrar uygulanmaması
* duplicate effect identity'nin ikinci kez uygulanmaması
* save/load round-trip semantic equality
* migration sonrası invariant korunması
* bütün geçerli squad'ların Match hazırlığına dönüştürülebilmesi
* invalid input'ların sessizce kabul edilmemesi

Generated input shrink edildiğinde failure'ın tekrar üretilebilir seed veya örneği raporlanmalıdır. Kesin property testing framework'ü bu belgede seçilmez.

---

## 15. Metamorphic Test Yönü

En az şu metamorphic ilişkiler tanımlanır:

* presentation ayrıntısı değişimi domain sonucunu değiştirmemeli,
* save/load eklenmesi sonraki deterministic sonucu değiştirmemeli,
* collection insertion order değişimi sonucu değiştirmemeli,
* read model rebuild authoritative state'i değiştirmemeli,
* duplicate event delivery ikinci business effect üretmemeli,
* aynı completed command'ın retry'ı ikinci state transition üretmemeli,
* background fidelity değişimi invariant ve resmî sonucu geçersiz hâle getirmemeli,
* daha güçlü takım profili geniş örneklemde anlamsız biçimde daha kötü sonuç üretmemeli,
* daha yüksek fatigue geniş örneklemde performansı sistematik olarak iyileştirmemeli,
* fulfilled Promise aynı anda breached olamamalı,
* migration semantic state'i desteklenen dönüşüm dışında değiştirmemeli.

---

## 16. Event ve Rule Engine Test Matrisi

`docs/04_EVENT_RULE_ENGINE.md` Bölüm 31 ile uyumlu olarak en az şu test aileleri zorunludur: Command validation, Domain Event commit sırası, Integration Event mapping, duplicate delivery, duplicate consumer effect, causation ve correlation lineage, delayed evaluation, scheduled evaluation, retry, rejected consequence, process manager coordination, causation cycle detection, maximum chain/work budget davranışı, save/load sonrası redelivery, event retention ve compaction, notification'ın domain state olmaması, handler registration sırasından bağımsızlık.

Determinism karşılaştırması yalnız final skor veya state toplamını değil, semantic event chain, event type sırası, owner transition özeti, process completion identity, canonical state hash ve Random Context kullanımını da kapsamalıdır.

10 sezonluk test en az şunları aramalıdır: exception, invalid lifecycle, duplicate result, duplicate effect, overlapping contract, overlapping employment, orphan reference, runaway event chain, event queue leak, completed Process Manager leak, uncontrolled audit growth, determinism mismatch, save/load failure, unsupported version, missed deadline.

---

## 17. Memory, Promise ve Relationship Test Matrisi

### 17.1. Memory

`docs/05_MEMORY_AND_PROMISE_SYSTEM.md` Bölüm 45 ile uyumlu olarak en az şunlar kapsanır: creation eligibility, actor references, importance, source event lineage, retention/compaction, archive, duplicate prevention, career/club değişiminde devamlılık.

### 17.2. Promise

En az şunlar kapsanır: proposal, acceptance, activation, progress, fulfillment, breach, expiry, cancellation, terminal state tekilliği, deadline, duplicate resolution, save/load continuation.

### 17.3. Relationship

`docs/06_RELATIONSHIP_SYSTEM.md` Bölüm 44 ile uyumlu olarak en az şunlar kapsanır: boyut bazlı değişim, açıklanabilir source event, yönlü ilişki modelinde yön doğruluğu, duplicate consequence, actor identity preservation, transfer ve club change sonrası continuity, UI projection ile authoritative state ayrımı.

Exact numeric delta veya threshold bu belgede belirlenmez.

---

## 18. Dialogue ve Decision Test Matrisi

`docs/07_DIALOGUE_SYSTEM.md` Bölüm 43 ile uyumlu olarak en az şunlar kapsanır: dialogue eligibility, option visibility, hidden information sınırı, selected option validation, Decision Request lifecycle, deadline, blocking policy, duplicate response, expired request, timeout/default resolution, owner-specific Command üretimi, Promise veya Memory oluşumu, Relationship sonucu, public narrative sonucu, save/load sırasında pending decision, terminal decision'ın yeniden açılmaması, localization değişiminin domain sonucunu değiştirmemesi.

---

## 19. Transfer, Contract ve Registration Test Matrisi

`docs/08_TRANSFER_SYSTEM.md` Bölüm 45 ile uyumlu olarak en az şunlar kapsanır: Transfer Process lifecycle, transfer window eligibility, squad need, target selection, offer, negotiation, approval, rejection, cancellation, accepted ile completed ayrımı, financial boundary, Contract creation, Registration, active club transition, Squad update, atomic finalization, retry, duplicate completion, failed completion rollback/recovery, player motivation, manager/club reputation etkisi, free-agent transferi, save/load sırasında active process, deadline günü ordering.

---

## 20. Match Test Matrisi

`docs/09_MATCH_SIMULATION.md` Bölüm 35 ve 36 ile uyumlu olarak en az şunlar kapsanır: valid squad ve selection, invalid squad rejection, Match Snapshot immutability, Match Simulation Context, deterministic RNG, starting eleven, substitutes, tactic plan, fatigue ve fitness, injury, card, goal, substitution, intervention eligibility, duplicate intervention, timeline ordering, Match Result immutability, performance summary, explanation metadata, Fixture ve Match identity ayrımı, Competition Result Acceptance, duplicate result acceptance, active Match save checkpoint, save/load continuation, background fidelity, thousands-of-matches distribution, extreme-score diagnostics.

---

## 21. Manager Career Test Matrisi

`docs/10_MANAGER_CAREER.md` Bölüm 37 ile uyumlu olarak en az şunlar kapsanır: career identity, starting background, initial reputation/profile, active Employment tekilliği, Club active Manager tekilliği, Job Offer lifecycle, offer expiry, offer acceptance, employment activation, Board Confidence, Season Expectation, Board Assessment, dismissal, unemployment, club change, career history, Memory/Relationship continuity, save/load, duplicate employment transition, 10-season manager turnover.

---

## 22. Player Career Test Matrisi

`docs/11_PLAYER_CAREER.md` Bölüm 34 ile uyumlu olarak en az şunlar kapsanır: stable PlayerId, BirthDate ve age derivation, Sporting Profile ownership, evidence collection, deterministic development evaluation, Potential belirsizliği, Career Phase, decline, injury history etkisi, retirement evaluation, retirement finalization, retired Player invariant'ları, annual generation, generation provenance, population target/tolerance, duplicate generated Player, Contract/Squad ownership ayrımı, save/load, 10-season population continuity.

---

## 23. World Simulation Test Matrisi

`docs/12_WORLD_SIMULATION.md` Bölüm 35 ile uyumlu olarak en az şunlar kapsanır: GameDate monotonicity, Planning Period lifecycle, Simulation Horizon, Simulation Step identity, same-day phase ordering, due-work ordering, stable tie-breaking, Hard Blocker, Player Decision Interruption, Non-blocking Development, Technical Interruption, event queue stabilization, safety limits, background actor Command üretimi, background Match akışı, transfer window open/close, season completion, Season Transition Process, new-season activation invariant'ları, retry ve failure recovery, checkpoint, save/load continuation, headless execution, 10-season completion.

---

## 24. Save Integrity Test Matrisi

`docs/13_SAVE_SYSTEM.md` Bölüm 41 ile uyumlu olarak en az şunlar kapsanır: Save Manifest, version compatibility, safe checkpoint eligibility, full snapshot, candidate save, atomic replacement, backup, overwrite failure, round-trip semantic equality, stable identity, referential integrity, RNG state preservation, pending process preservation, active Match save, season transition save, candidate world load, rehydration, no event republish, migration, migration retry, source artefact preservation, corrupted save, healthy backup recovery, failed recovery, canonical comparison, compaction, concurrent operation serialization, 10-season save growth.

---

## 25. Çapraz Sistem Senaryoları

### 25.1. Forma Sözü Zinciri

Player forma süresi ister → Manager Promise verir → Match Selection oluşur → Promise progress değerlendirilir → Promise fulfilled veya breached olur → Memory oluşur → Relationship değişir → Transfer isteği veya Dialogue tetiklenebilir → save/load araya girdiğinde duplicate sonuç oluşmaz.

### 25.2. Transfer Finalization

Transfer accepted → Club budget doğrulanır → Contract oluşturulur → Registration değişir → active club değişir → Squad membership güncellenir → Transfer completed olur → herhangi bir aşamadaki failure kısmi geçerli state bırakmaz.

### 25.3. Manager Dismissal ve Yeni İş

Board Assessment → dismissal → Employment kapanışı → unemployment → Job Offer → Decision Request → offer acceptance → yeni Employment → eski Memory ve Relationship devamlılığı.

### 25.4. Player Retirement

Retirement kararı → Contract kapanışı → Registration kaldırılması → Squad kaldırılması → Transfer süreçlerinin kapanması → Social Continuity korunması → duplicate finalization olmaması.

### 25.5. Season Transition

Bütün Fixture'ların tamamlanması → final Standings → manager assessments → contracts → Player development/decline → retirement → new Player generation → club planning → yeni Season → save/load ve retry.

### 25.6. Maç Sonrası Dünya Etkisi

Match Result → Competition acceptance → Standings → Board Confidence → Player development evidence → Promise evaluation → Memory → Relationship → public narrative → world summary.

---

## 26. On Sezonluk Ana Doğrulama Senaryosu

Zorunlu ana scenario (`docs/02_MVP_SCOPE.md` Bölüm 17 ve 22 ile uyumlu): 1 kurgusal ülke, 1 profesyonel lig, 20 kulüp, yaklaşık 500 active Player başlangıcı, her kulüp için active Manager, çift devreli lig, kulüp başına sezon başına 38 maç, en fazla 10 tamamlanmış sezon, yaz ve kış transfer dönemleri, Player aging/development/decline/retirement, annual generated Player, Manager dismissal/unemployment/employment, transfer ve Contract süreçleri, Relationship, Memory ve Promise, save/load checkpoint'leri, farklı seed'ler.

Hard başarı koşulları en az şunlardır:

1. On Season tamamlanmalıdır.
2. Exception veya unhandled failure bulunmamalıdır.
3. Hiçbir Fixture veya Match iki kez işlenmemelidir.
4. Hiçbir Match Result iki kez kabul edilmemelidir.
5. Hiçbir Player aynı anda birden fazla active Contract taşımamalıdır.
6. Hiçbir Club aynı anda birden fazla active Manager taşımamalıdır.
7. Retired Player active Registration veya Squad taşımamalıdır.
8. Active Player population kontrolsüz biçimde çökmemeli veya patlamamalıdır.
9. Pending critical process'ler açıklamasız biçimde kaybolmamalıdır.
10. Event chain sonsuz döngüye girmemelidir.
11. GameDate geriye gitmemelidir.
12. Save artefact onuncu sezon sonunda yüklenebilir olmalıdır.
13. Save/load sonrası canonical semantic state doğrulanmalıdır.
14. Duplicate business effect bulunmamalıdır.
15. Dangling required reference bulunmamalıdır.
16. Negatif veya impossible zorunlu değer bulunmamalıdır.
17. Event, history, process ve save büyümesi raporlanmalıdır.
18. Aynı seed aynı semantic sonucu üretmelidir.
19. Farklı seed'ler invariant'ları korurken anlamlı çeşitlilik üretmelidir.

---

## 27. Uzun Dönem Metrikleri

Ten-season ve soak raporları en az şu metrikleri toplayabilmelidir: tamamlanan Season sayısı, tamamlanan Fixture ve Match sayısı, accepted Match Result sayısı, active/retired/generated Player sayıları, contracted/free-agent Player sayıları, active Manager ve employment değişimi, dismissal ve Job Offer sayısı, Transfer Process sonuçları, Contract ve Registration sayıları, Promise durum dağılımı, Memory sayısı ve retention, Relationship değişim sayısı, Decision Request sayısı ve unresolved count, event count, maximum event chain depth, Simulation Step sayısı, work item sayısı, retry ve duplicate rejection sayısı, failed/recovered process sayısı, save artefact boyutu, backup ve migration sayısı, runtime, allocation veya memory ölçümü, canonical state hash/checkpoint, event template çeşitliliği ve repetition göstergeleri, Match dağılım metrikleri.

Exact kabul eşikleri bu belgede belirlenmez; hard invariant'lar metrik threshold'larıyla karıştırılamaz.

---

## 28. Tekrar ve İçerik Tüketimi Doğrulaması

`docs/01_GAME_DESIGN_DOCUMENT.md` Bölüm 35'teki "oyuncu birkaç sezon içinde bütün olay kalıplarını tüketmemeli" hedefi bu belgeyle test edilebilir hâle getirilir.

Ölçülebilecek göstergeler: unique event rule/template sayısı, aynı event family'nin tekrar aralıkları, cooldown ihlalleri, aynı aktör üzerinde tekrar oranı, season bazında yeni event family görünümü, Decision Request çeşitliliği, narrative repetition, aynı seed ve farklı seed karşılaştırması, bütün event'lerin ilk sezonlarda tüketilip tüketilmediği.

Bağlayıcı kurallar:

* İçerik çeşitliliği yalnız unique metin sayısıyla ölçülemez.
* Aynı semantic event farklı metinle yeni event sayılmaz.
* Exact repetition threshold içerik hacmi ve gerçek simülasyon verisi olmadan belirlenmez.
* Rapor, dengeleme ve içerik üretim kararlarına veri sağlamalıdır.

---

## 29. Failure Injection Stratejisi

Aşağıdaki aşamalarda kontrollü failure senaryoları zorunlu tutulur: event consumer, Application transaction, Transfer finalization, retirement finalization, employment transition, season transition, Match checkpoint, save snapshot capture, candidate save write, manifest finalization, backup creation, atomic replacement, migration step, candidate world rehydration, post-load validation, content loading, projection rebuild.

Her failure testinde doğrulanması gerekenler: authoritative state kısmi bozulmuş mu, retry güvenli mi, completed effect ikinci kez uygulanıyor mu, failure açık state bırakıyor mu, diagnostic bilgi yeterli mi, eski save veya active world korunuyor mu, recovery mümkün mü.

---

## 30. Regression Stratejisi

1. Her doğrulanmış production bug'ı mümkün olduğunda önce failing regression test ile temsil edilmelidir.
2. Regression test hata nedenini hedeflemelidir; yalnız büyük end-to-end senaryoya gömülmemelidir.
3. Seed kaynaklı failure'ın seed'i kalıcı regression corpus'a eklenebilir.
4. Corrupted veya migrated save örneği uygun fixture corpus'a eklenebilir.
5. Regression test'leri gerekçesiz silinemez.
6. Davranış bilinçli değiştiğinde baseline değişikliği karar veya değişiklik notuyla açıklanmalıdır.
7. Golden fixture update'i otomatik ve incelemesiz yapılamaz.
8. Eski bug'a ait test yalnız implementasyon değişti diye kaldırılmamalıdır; semantic risk ortadan kalkmışsa gerekçe yazılmalıdır.

---

## 31. Flaky Test Politikası

Flaky test; aynı commit, aynı scenario, aynı seed, aynı environment altında kontrolsüz biçimde farklı sonuç üreten testtir.

Bağlayıcı politika: flaky test normal kabul edilmez; deterministic domain testinde flakiness kritik hatadır; otomatik retry kök nedeni gizlemek için kullanılamaz; infrastructure kaynaklı geçici retry ayrı raporlanmalıdır; quarantine geçici ve görünür olmalıdır; quarantined test ana suite'in başarı oranına sessizce dahil edilemez; quarantine owner, reason ve çözüm kaydı taşımalıdır; kesin quarantine süresi bu belgede belirlenmez; `Thread.Sleep`, wall-clock bekleme veya belirsiz polling varsayılan test yaklaşımı olamaz.

---

## 32. Diagnostic ve Failure Report Standardı

Simulation veya integration failure raporu en az şunları içermelidir: ScenarioId, seed, RNG version, simulation version, content version, rule/model version, GameDate, SeasonId, current Planning Period, last safe checkpoint, current Simulation Step, failing test category, ilgili aggregate/entity ID'leri, command identity, EventId, correlation ve causation bilgisi, Process Manager stage'i, canonical state hash veya semantic diff reference, son anlamlı committed olaylar, invariant adı, beklenen ve gerçekleşen durum, save fixture veya reproduction reference.

Log hacmi sınırsız olamaz; failure'a yakın açıklanabilir pencere ve gerekli artefact korunmalıdır.

---

## 33. CI ve Execution Katmanları

### 33.1. Pull request / per-commit kontrolleri

Altyapı oluşturulduğunda en az şunları kapsamalıdır: Restore, Build, Fast unit tests, Aggregate ve invariant tests, Contract tests, Integration smoke tests, Determinism smoke test, Save/load round-trip smoke test, Content validation, Documentation validation.

### 33.2. Scheduled veya nightly kontroller

En az şunları kapsayabilir: geniş integration suite, multi-seed simulation, binlerce Match, one-season full world, selected ten-season scenario, migration corpus, corruption/recovery, performance trend, memory/event/save growth.

### 33.3. Manual veya release-candidate kontrolleri

En az şunları kapsamalıdır: full ten-season suite, geniş seed matrisi, bütün desteklenen migration zincirleri, recovery corpus, full distribution report, performance comparison, Godot import/export smoke, Windows exported build smoke, documentation/index/decision validation.

Kesin GitHub Actions YAML'ı, job sayısı, trigger veya timeout bu belgede belirlenmez.

---

## 34. Quality Gate'ler

### 34.1. Sistem tasarım gate'i

Bir sistem için kod başlamadan önce amacı, kullandığı verileri, authoritative owner'ı, etkilediği sistemleri, etkilendiği sistemleri, ürettiği olayları, tepki verdiği olayları, sınır durumlarını ve test senaryolarını tanımlanmalıdır.

### 34.2. Pull request gate'i

İlgili değişikliğin unit/invariant testleri, gerekli integration testleri, regression testleri ve documentation değişiklikleri bulunmalıdır. Exact branch protection ayarı bu belgede belirlenmez.

### 34.3. Dikey kesit gate'i

İlk dikey kesit; gerçek Match sonucu, gerçek time advance, gerçek Promise/Memory/Relationship etkisi, event/idempotency, safe checkpoint, save/load round-trip ve determinism smoke testlerini geçmelidir.

### 34.4. Sistem tamamlanma gate'i

Bir ana sistem yalnız kendi unit testleri geçtiği için tamamlanmış sayılmaz. En az invariant, lifecycle, integration, failure, save/load, determinism ve long-running etkisi doğrulanmalıdır.

### 34.5. MVP release gate'i

MVP, `docs/02_MVP_SCOPE.md` Bölüm 22'deki kabul kriterlerinin tamamı doğrulanmadan tamamlanmış sayılamaz.

---

## 35. MVP Kabul Kriterlerinin Doğrulama Matrisi

`docs/02_MVP_SCOPE.md` Bölüm 22'deki 20 kabul kriteri aşağıdaki test/metric türleriyle eşleştirilir.

| # | MVP kabul kriteri (özet) | Test seviyesi | Scenario | Oracle | Metric | Execution category |
|---|---|---|---|---|---|---|
| 1 | 10 sezon hata vermeden ilerleme | Long-Running, Soak | Ten-Season World | Hard invariant (exception yok) | exception/failure sayısı | Simulation, Soak |
| 2 | Fixture ve Match'in iki kez işlenmemesi | Invariant, Property | Ten-Season World, Match Distribution World | Idempotency/effect uniqueness | duplicate işlem sayısı | Invariant, Simulation |
| 3 | Onuncu sezon save'inin yüklenebilmesi | Save/Load, Long-Running | Ten-Season World | Round-trip semantic equality | load başarı/başarısızlık | SaveLoad, Simulation |
| 4 | Dismissal'ın doğrudan game over olmaması | Integration, Lifecycle | Manager Dismissal ve Yeni İş | Lifecycle/state-transition | unemployment→yeni employment geçişi | Integration |
| 5 | Manager club change sırasında career history korunması | Integration, Property | Manager Dismissal ve Yeni İş | Referential integrity | history süreklilik kontrolü | Integration |
| 6 | Player aging/development/decline/retirement | Unit, Integration, Long-Running | Player Retirement, Ten-Season World | Lifecycle doğruluğu | development/decline/retirement sayıları | Simulation |
| 7 | Active Player population devamlılığı | Property, Long-Running | Ten-Season World | Population invariant | active population trend | Simulation, Statistical |
| 8 | Transferlerin yalnız maaş/overall'a bağlı olmaması | Statistical | Match/Transfer Distribution World | Statistical/balance expectation | Player Decision girdi çeşitliliği | Statistical |
| 9 | Squad/taktik/fatigue/fitness'ın Match olasılıklarına etkisi | Statistical, Metamorphic | Match Distribution World | Metamorphic ilişki | faktör-sonuç korelasyonu | Statistical |
| 10 | Güçlü takımın uzun örneklemde daha başarılı, her maçı kazanmaması | Statistical | Match Distribution World | Statistical/balance expectation | güç farkı–sonuç ilişkisi | Statistical |
| 11 | Verilen sözlerin gerçek sonuç üretmesi | Integration, Lifecycle | Forma Sözü Zinciri | Lifecycle/effect doğruluğu | fulfillment/breach sonrası event zinciri | Integration |
| 12 | Önemli hafızaların gelecekteki kararları etkilemesi | Integration | Forma Sözü Zinciri, Dialogue senaryoları | Explainable event metadata | Memory→Dialogue/Decision girdi izi | Integration |
| 13 | Haftalık akışın düşük önemli işlerle sürekli kesilmemesi | Integration, Property | Vertical Slice World | Interruption policy invariant | blocker sıklığı | Integration |
| 14 | Kritik kararların nedenini açıklaması | Contract, Integration | Decision Request senaryoları | Açıklanabilir event/result metadata | explanation alanı doluluğu | Contract |
| 15 | Aynı olayın sonucunun iki kez uygulanmaması | Invariant, Idempotency | Failure World | Idempotency/effect uniqueness | duplicate effect sayısı | Invariant, Integration |
| 16 | Binlerce maçta geçersiz kadro/skor oluşmaması | Property, Statistical | Match Distribution World | Hard invariant | invalid state sayısı | Statistical, Simulation |
| 17 | Relationship değişikliklerinin açıklanabilir olaylara dayanması | Unit, Integration | Relationship test matrisi senaryoları | Açıklanabilir source event | kaynak event izlenebilirliği | Integration |
| 18 | Aynı başlangıcın farklı seed'lerle farklı fakat geçerli kariyerler üretmesi | Determinism, Statistical | Ten-Season World (çok seed) | Deterministic semantic equality + çeşitlilik | seed'ler arası sonuç farkı ve invariant korunumu | Determinism, Statistical |
| 19 | Aynı save/RNG state'in kontrolsüz farklı sonuç üretmemesi | Determinism | Ten-Season World, Match Distribution World | Deterministic semantic equality | aynı seed tekrar sonucu | Determinism |
| 20 | MVP dışı sistemlerin temel oynanış için zorunlu olmaması | Component, Documentation Validation | Vertical Slice World, Full MVP World | Contract/scope doğrulaması | MVP dışı bağımlılık taraması | Contract |

---

## 36. Coverage Yaklaşımı

Coverage yalnız line veya branch yüzdesine indirgenmez.

Coverage boyutları: domain invariant coverage, lifecycle transition coverage, invalid transition coverage, event/command contract coverage, cross-context integration coverage, failure-stage coverage, save/load state coverage, migration path coverage, seed/scenario coverage, long-running system coverage, content rule coverage, platform adapter coverage.

Bağlayıcı kurallar:

* Yüksek line coverage doğru domain davranışı kanıtı değildir.
* Düşük riskli getter'lar ile kritik finalization aynı ağırlıkta değerlendirilmemelidir.
* Risk bazlı coverage matrisi tutulmalıdır.
* Exact coverage yüzdesi ölçüm ve uygulama yapısı oluşmadan belirlenmez.
* Coverage düşüşü görünür olmalıdır.
* Generated veya trivial code'un coverage politikasındaki yeri implementasyon aşamasında belirlenebilir.

---

## 37. Performans ve Büyüme Testleri

En az şu teknik ölçümler tanımlanır: headless ten-season runtime, Season başına runtime, Match başına runtime dağılımı, Simulation Step başına work item, maximum event chain depth, allocation ve peak memory, entity sayısı büyümesi, event processing ledger büyümesi, history büyümesi, Memory ve Relationship büyümesi, save artefact boyutu, save süresi, load/rehydration süresi, migration süresi, canonical comparison süresi, Godot presentation dışı simulation throughput.

Bağlayıcı kurallar:

* Performans testi kontrollü ve tanımlı environment bilgisi taşımalıdır.
* Debug ve release sonuçları karıştırılmamalıdır.
* Hardware bilgisi raporlanmalıdır.
* Correctness başarısızken performance sonucu başarılı kabul edilemez.
* Tek ölçüm regression kararı için yeterli olmayabilir.
* Trend ve baseline kaydı tutulmalıdır.
* Exact süre, memory ve save boyutu eşikleri teknik spike ve ilk ölçümler olmadan belirlenmez.

---

## 38. Domain Değişmezleriyle Uyumluluk

Bu belge, `docs/03_DOMAIN_MODEL.md` Bölüm 13 ve 20; `docs/04_EVENT_RULE_ENGINE.md` Bölüm 32; ve ilgili alt sistem belgelerindeki (`docs/05`–`docs/13`) "Domain Değişmezleri" ve "Test Matrisi" bölümleriyle çelişmez. Alt sistem belgelerinde tanımlanan invariant testleri bu belgedeki test aileleri (Bölüm 6) ve oracle hiyerarşisi (Bölüm 10) altında konsolide edilir; hiçbir alt sistem invariant'ı bu belgeyle zayıflatılmaz veya değiştirilmez.

---

## 39. İlk Dikey Kesit Test Kapsamı

`docs/02_MVP_SCOPE.md` Bölüm 20 ve ilgili alt sistem belgelerinin "İlk Dikey Kesit Kapsamı" bölümleriyle uyumlu olarak, ilk dikey kesit en az şu test ailelerini gerçek domain kurallarıyla içermelidir: unit, invariant, contract, dar kapsamlı integration, determinism smoke, idempotency, save/load round-trip smoke.

İlk dikey kesitte zorunlu değildir: binlerce Match distribution suite, çok seed'li ten-season soak, migration corpus, geniş performans trend raporlaması. Bu genişletilmiş suite'ler Kilometre Taşı 2-4 kapsamında (`docs/02_MVP_SCOPE.md` Bölüm 6) devreye girer.

---

## 40. Sınır Durumları

1. Bir test scenario'sunun kullandığı content/rule version desteklenmeyen bir sürüme yükseltilirse: scenario açık migration veya explicit re-baseline gerektirir; sessiz yeniden yorumlama yapılamaz.
2. Bir soak test sırasında ortamsal (infrastructure) hata oluşursa: bu hata deterministic domain flakiness'ten ayrı raporlanır ve otomatik retry yalnız bu kategoriye uygulanabilir.
3. Bir property-based test shrink sırasında birden fazla minimal counterexample bulursa: her biri ayrı seed/örnek olarak raporlanır; yalnız ilki ile yetinilmez.
4. Bir istatistiksel test sınır değere çok yakın sonuç üretirse: tek çalıştırma ile karar verilmez; ek seed çalıştırması veya geniş toleranslı smoke/dar balance regression ayrımı (Bölüm 13) uygulanır.
5. Bir golden fixture ile gerçek çıktı arasında fark bulunursa: fark önce semantic domain regression olup olmadığı açısından değerlendirilir; yalnız fiziksel fark olduğu için fixture güncellenmez (Bölüm 30).
6. Bir quarantined test uzun süre çözülmeden kalırsa: quarantine kaydı owner ve durumla birlikte görünür kalmaya devam eder; sessizce silinemez veya unutulamaz (Bölüm 31).

---

## 41. Açık Kalan Kararlar

Aşağıdaki konular kesinleştirilmemiştir; ilgili teknik spike, test stratejisi uygulaması veya dengeleme çalışması olmadan sessizce kapatılamaz:

* exact coverage yüzdesi,
* exact branch/mutation coverage hedefi,
* exact statistical confidence yöntemi,
* exact Match dağılım aralıkları,
* exact seed matrix boyutu,
* exact nightly scenario sayısı,
* exact performans eşikleri,
* exact memory limiti,
* exact save boyutu limiti,
* exact CI workflow/job yapısı,
* exact test timeout değerleri,
* exact property-based testing library,
* exact benchmark environment,
* exact quarantine süresi,
* exact regression fixture retention,
* exact release quality gate threshold'ları.

Bu kararlar ilk ölçümler, teknik spike'lar ve repository test altyapısı oluşturulmadan sessizce kesinleştirilmeyecektir.

---

## 42. Riskler ve Azaltma Yönleri

| Risk | Azaltma yönü |
|---|---|
| Test stratejisinin gizli bir ikinci domain sistemi hâline gelmesi | Katı ayrım (Bölüm 3); test araçlarının authoritative business rule sahibi olamaması. |
| İstatistiksel testlerin flaky/anlamsız fail üretmesi | Geniş toleranslı smoke ile dar balance regression ayrımı; çoklu seed zorunluluğu (Bölüm 13). |
| Determinizmin call-order veya collection sırasına bağlı sessizce bozulması | Canonical state comparison ve explicit determinism test ailesi (Bölüm 11, 12). |
| Uzun soak/ten-season testlerin CI'ı yavaşlatması | Per-commit/nightly/manual execution katmanı ayrımı (Bölüm 33). |
| Golden/snapshot testlerin yanlış davranışı onaylaması | Oracle hiyerarşisinde snapshot'ın en düşük öncelikte tutulması (Bölüm 10, 6.14). |
| Flaky testlerin otomatik retry ile gizlenmesi | Bağlayıcı flaky test politikası ve quarantine şeffaflığı (Bölüm 31). |
| Coverage yüzdesinin tek kalite göstergesi sayılması | Çok boyutlu, risk bazlı coverage yaklaşımı (Bölüm 36). |
| Regression testlerinin gerekçesiz silinmesi | Bağlayıcı regression stratejisi ve retention ilkeleri (Bölüm 30). |
| Exact sayısal eşiklerin erken ve sessizce kilitlenmesi | Açık kararlar listesinin korunması ve ilgili belgelere yönlendirme (Bölüm 41). |
| Test fixture'larının foreign state'i doğrudan mutate ederek geçersiz senaryo üretmesi | Bağlayıcı test ilkeleri (Bölüm 5, madde 18-19). |

---

## 43. Sonraki Adım

Bu belge kesinleştikten sonra önerilen en küçük sıradaki çalışma:

> İlk teknik doğrulama spike'larının ve repository solution/test project iskeletinin, kesinleşmiş belgelerdeki sınırlar korunarak küçük ve geri alınabilir adımlarla planlanması.

Bu adımdan önce: üretim kodu yazılmamalı, test kodu veya test projesi oluşturulmamalı, exact coverage/performans/istatistiksel eşikler belirlenmemeli, GDD veya MVP kapsamı değiştirilmemeli, bu belgede açık bırakılan kararlar sessizce kapatılmamalıdır.
