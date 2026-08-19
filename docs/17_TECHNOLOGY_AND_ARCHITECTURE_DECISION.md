# Teknoloji ve Yüksek Seviyeli Mimari Kararı

**Belge:** `docs/17_TECHNOLOGY_AND_ARCHITECTURE_DECISION.md`
**Durum:** Kesinleşti
**Karar tarihi:** 2026-07-02
**Ana referans:** `docs/01_GAME_DESIGN_DOCUMENT.md`
**MVP sınırı:** `docs/02_MVP_SCOPE.md`
**İlgili karar günlüğü:** `docs/15_DECISION_LOG.md`

---

## 1. Belgenin Amacı

Bu belge, Football Career Simulator projesinin hedef platformunu, oyun motorunu, programlama dilini, kullanıcı arayüzü yaklaşımını, simülasyon çekirdeğinin çalışma biçimini, yüksek seviyeli katman sınırlarını, veri ve kayıt yönünü, test altyapısını, operasyonel araçlarını ve ilk teknik doğrulama spike'larını kesinleştirir.

Bu belge:

* tam domain modeli oluşturmaz,
* entity veya aggregate sınıfları tanımlamaz,
* kesin veritabanı tablo şeması üretmez,
* olay tiplerini ayrıntılandırmaz,
* maç matematiğini tasarlamaz,
* GDD veya MVP kapsamını değiştirmez,
* `docs/13_SAVE_SYSTEM.md` ve `docs/14_TEST_STRATEGY.md` belgelerinin ayrıntılı tasarım sorumluluğunu devralmaz.

Bu belgenin görevi, sonraki tasarım ve geliştirme çalışmalarının üzerinde ilerleyeceği teknik sınırları belirlemektir.

---

## 2. Karar Bağlamı

Football Career Simulator:

* uzun vadeli,
* sistemik,
* olay tabanlı,
* en az 10 sezonluk simülasyonu destekleyen,
* menü, metin, tablo, karar ve veri ekranları ağırlıklı,
* teknik direktör kariyerine odaklanan

bir futbol kariyeri ve yaşam simülasyonudur.

MVP:

* 1 kurgusal ülke,
* 1 profesyonel lig,
* 20 kulüp,
* yaklaşık 500 aktif futbolcu,
* en fazla 10 tamamlanmış sezon,
* haftalık kontrol merkezi,
* kadro, taktik, antrenman ve maç simülasyonu,
* transfer ve sözleşmeler,
* ilişki, hafıza ve söz sistemleri,
* olay ve kural motoru,
* işten çıkarılma ve sınırlı kulüp değiştirme,
* kayıt ve yükleme,
* olay zaman çizelgesi tabanlı maç sunumu

içerir.

Fiziksel 2D ve 3D maç gösterimi MVP dışındadır. Bununla birlikte uzun vadede gelişmiş 2D maç sunumu olasıdır. 3D tamamen dışlanmamış ancak teknoloji seçiminin ana belirleyicisi değildir.

Tek geliştiricinin güçlü olduğu ana teknoloji C#/.NET'tir. Godot deneyimi orta, Unity deneyimi temel düzeydedir. İlk hedef yalnızca Windows masaüstüdür.

---

## 3. Bağlayıcı Gereksinimler

Teknoloji ve mimari kararı aşağıdaki gereksinimlere tabidir:

1. Domain ve simülasyon kuralları kullanıcı arayüzünde bulunamaz.
2. UI domain state'i doğrudan değiştiremez.
3. Simülasyon çekirdeği mümkün olduğunca oyun motorundan bağımsız test edilebilmelidir.
4. Maç simülasyonu görsel sunum olmadan çalışabilmelidir.
5. Dünya simülasyonu UI açılmadan otomatik çalıştırılabilmelidir.
6. Rastlantısallık tohumlanabilir, sürümlenebilir ve tekrar üretilebilir olmalıdır.
7. En az 10 sezonluk otomatik simülasyon testleri desteklenmelidir.
8. Kayıt sürümü, migration ve geriye dönük uyumluluk desteklenmelidir.
9. Kayıt bütünlüğü kısa vadeli geliştirme hızına feda edilemez.
10. İnternet ve harici üretken AI temel oynanış için zorunlu bağımlılık olamaz.
11. Teknoloji yığını tek geliştirici tarafından sürdürülebilir olmalıdır.
12. MVP'de fiziksel 2D veya 3D maç gösterimi zorunlu değildir.
13. Gelecekteki görsel maç katmanı, simülasyon çekirdeğinin yeniden yazılmasını gerektirmemelidir.
14. Runtime state, authored content ve presentation state birbirinden ayrılmalıdır.
15. Testler yalnızca oyun editörü açıldığında çalışabilir durumda olamaz.
16. Simülasyon frame rate'e bağlanamaz.
17. Save formatı Godot scene veya resource formatına bağlanamaz.
18. İçerik verisi kaynak kod içine gömülemez.
19. Domain modelleri doğrudan motor node veya component sınıflarına dönüştürülemez.
20. Olay motoru kontrolsüz global mesajlaşma sistemine dönüştürülemez.

---

## 4. Değerlendirilen Seçenekler

### 4.1. Godot 4 .NET + C#

Godot'un oyun sunumu, 2D/3D, input, audio ve animasyon yeteneklerini; C#/.NET'in statik tip, refactoring, test ve domain modelleme avantajlarıyla birleştirir.

Bu seçenek, simülasyon çekirdeğinin Godot bağımsız saf .NET bileşenleri olarak tutulması koşuluyla projenin ihtiyaçlarına güçlü biçimde uyar.

### 4.2. Godot 4 + GDScript

Godot entegrasyonu ve kısa vadeli prototipleme hızı yüksektir. Buna karşılık geniş domain modeli, save migration, uzun dönem refactoring ve motor bağımsız testler açısından C#/.NET'e göre daha yüksek bakım riski taşır.

### 4.3. Unity + C#

Güçlü 2D/3D, profiling, C# ve test altyapısı sunar. Ancak MVP'nin görsel kapsamına göre daha ağırdır; geliştiricinin Unity deneyimi daha düşüktür ve ticari lisans/vendor riski Godot'a göre daha yüksektir.

### 4.4. Motor Bağımsız .NET + Avalonia

Domain geliştirme, test, yoğun veri tabloları ve masaüstü UI için çok güçlüdür. Gelecekte oyun hissi, gelişmiş 2D sunum veya 3D görünüm gerektiğinde ek renderer ya da ayrı motor entegrasyonu gerektirir.

### 4.5. React/TypeScript + Tauri + .NET

Veri ekranlarında yüksek UI geliştirme hızı sunar. Fakat React/TypeScript, Tauri/Rust ve C#/.NET arasında çoklu teknoloji ve IPC sınırı oluşturarak tek geliştirici bakım maliyetini artırır. Web veya mobil hedef bulunmadığı için bu karmaşıklık yeterli karşılık üretmez.

---

## 5. Karar Matrisi

Puanlama:

* 5: Çok güçlü uyum
* 4: Güçlü uyum
* 3: Kabul edilebilir, belirgin bedelli
* 2: Önemli dezavantaj
* 1: Zayıf uyum

Ağırlıkların toplamı 100'dür.

| Kriter | Ağırlık | Godot + C# | Godot + GDScript | Unity + C# | .NET + Avalonia | React/Tauri + .NET |
| --- | ---: | ---: | ---: | ---: | ---: | ---: |
| Domain ve simülasyon test edilebilirliği | 10 | 5 | 3 | 4 | 5 | 4 |
| Deterministik/headless simülasyon | 10 | 5 | 4 | 4 | 5 | 4 |
| UI ağırlıklı oyun geliştirme verimliliği | 10 | 4 | 5 | 4 | 4.5 | 5 |
| Tek geliştirici öğrenme ve bakım maliyeti | 9 | 5 | 4 | 3 | 5 | 3 |
| Uzun dönem refactoring güvenliği | 7 | 5 | 3 | 5 | 5 | 4 |
| Kayıt ve veri sürümleme kolaylığı | 7 | 5 | 3 | 4 | 5 | 4 |
| Masaüstü dağıtımı | 5 | 5 | 5 | 5 | 5 | 4 |
| Performans | 5 | 4 | 4 | 5 | 4 | 4 |
| Debugging ve profiling | 5 | 4 | 4 | 5 | 5 | 4 |
| Otomatik test desteği | 7 | 5 | 3 | 5 | 5 | 4 |
| AI kodlama araçlarıyla çalışma kolaylığı | 4 | 5 | 3 | 5 | 5 | 4 |
| Lisans ve ticari risk | 6 | 5 | 5 | 3 | 5 | 5 |
| Gelecekte 2D/3D ekleyebilme | 7 | 5 | 5 | 5 | 2 | 2 |
| Motor bağımlılığını kontrol altında tutma | 4 | 4 | 2 | 3 | 5 | 4 |
| Topluluk ve resmî dokümantasyon | 2 | 4 | 4 | 5 | 4 | 4 |
| Uzun vadeli sürdürülebilirlik | 2 | 4 | 4 | 4 | 4 | 3 |

Ağırlıklı sonuç:

| Sıra | Seçenek | Sonuç |
| ---: | --- | ---: |
| 1 | Godot 4 .NET + C# | 94.4 / 100 |
| 2 | .NET + Avalonia | 93.0 / 100 |
| 3 | Unity + C# | 84.6 / 100 |
| 4 | React/TypeScript + Tauri + .NET | 78.2 / 100 |
| 5 | Godot + GDScript | 77.0 / 100 |

Puanlar mutlak teknik gerçek olarak değerlendirilmemelidir. UI geliştirme hızı, öğrenme maliyeti ve bakım puanları geliştiricinin mevcut C#/.NET, Godot, Unity ve frontend deneyimine göre verilmiştir.

Godot + C# ile Avalonia arasındaki fark küçüktür. Godot'un seçilmesinin temel nedeni projenin yalnızca masaüstü veri uygulaması olmaması ve gelecekte gelişmiş 2D maç sunumu olasılığının korunmak istenmesidir.

---

## 6. Seçilen Teknoloji Yığını

### 6.1. Hedef platform

Birincil MVP hedefi:

`Windows 10/11 x64 masaüstü`

Linux ve macOS:

* MVP kabul kapsamına dahil değildir,
* MVP sırasında zorunlu build veya test hedefi değildir,
* mimari olarak gereksiz yere engellenmeyecektir.

Mobil ve web hedeflenmemektedir.

### 6.2. Oyun motoru

Seçilen motor ailesi:

`Godot 4 .NET`

İlk teknik spike, karar tarihindeki güncel kararlı Godot 4 .NET sürümüyle yapılacaktır.

Kesin patch sürümü:

* teknik doğrulama spike'ları tamamlandıktan sonra sabitlenecek,
* repository içinde açıkça pinlenecek,
* kontrolsüz motor yükseltmeleri yapılmayacaktır.

Preview, beta veya release candidate sürümleri üretim tabanı olarak kullanılmayacaktır.

### 6.3. Programlama dili

Ana ve varsayılan dil:

`C#`

C# şu alanlarda kullanılacaktır:

* Domain
* Simulation
* Application / Use Cases
* Infrastructure adaptörleri
* Godot presentation davranışları
* Testler
* Tooling
* Simulation runner

GDScript, MVP üretim kodunda varsayılan olarak kullanılmayacaktır.

GDScript:

* domain,
* simulation,
* persistence,
* migration,
* save validation,
* iş kuralları

içinde kullanılamaz.

İleride yalnızca küçük ve izole bir Godot editor aracı için somut fayda oluşursa ayrı karar kaydıyla istisna değerlendirilebilir.

### 6.4. .NET sürüm yönü

Teknik spike ve Android/Godot export doğrulamaları sonucunda ortak target framework `net9.0`, exact SDK `9.0.317` ve roll-forward politikası `latestPatch` olarak pinlenmiştir (D-384). Godot Presentation `Godot.NET.Sdk/4.7.0` kullanır.

Domain, Simulation, Application, Infrastructure, Tooling ve Test projeleri ortak uyumlu target framework tabanını kullanmalıdır.

---

## 7. Hedef Platform ve Dağıtım

İlk dağıtım Windows x64 olacaktır.

İlk paketleme yönü:

* Godot Windows export,
* portable build,
* gerekli runtime ve third-party license bildirimleri,
* ayrı save/log klasörleri,
* temiz Windows ortamında açılış doğrulaması.

Installer ve code signing MVP dağıtım aşamasında ayrıca değerlendirilecektir.

Linux ve macOS portları ancak MVP doğrulandıktan sonra ayrı teknik ve ticari değerlendirme ile ele alınacaktır.

---

## 8. Yüksek Seviyeli Mimari Yön

Mimari katmanlar:

1. Domain
2. Simulation
3. Application / Use Cases
4. Presentation
5. Persistence / Infrastructure
6. Content / Data
7. Tooling
8. Automated Tests

### 8.1. Domain

Domain:

* temel iş kavramlarını,
* invariant'ları,
* domain davranışlarını,
* değer nesnelerini,
* domain sonuçlarını

taşır.

Domain:

* Godot'a,
* UI'a,
* SQLite'a,
* dosya sistemine,
* log provider'a,
* gerçek saate,
* network'e

bağımlı olamaz.

### 8.2. Simulation

Simulation:

* dünya ilerletme,
* maç simülasyonu,
* kontrollü rastlantısallık,
* takvim adımları,
* uzun dönem davranış

gibi simülasyon koordinasyonundan sorumludur.

Simulation, Godot frame loop'una bağlı olamaz.

### 8.3. Application / Use Cases

Application:

* command ve query use-case'lerini,
* transaction sınırlarını,
* orchestration'ı,
* save/load çağrılarını,
* zaman ilerletme taleplerini,
* read model üretim akışını

koordine eder.

Application, UI framework ayrıntılarını bilmez.

### 8.4. Presentation

Presentation:

* Godot scene ve `Control` bileşenlerini,
* input işlemlerini,
* ekran navigasyonunu,
* read model sunumunu,
* animasyon, ses ve görsel geri bildirimi

yönetir.

Presentation domain state'i doğrudan değiştiremez.

### 8.5. Persistence / Infrastructure

Infrastructure:

* SQLite save adapter'ını,
* file system erişimini,
* content loading'i,
* structured logging provider'ını,
* backup ve migration uygulamalarını,
* sistem saati ve benzeri dış servis adaptörlerini

barındırır.

Infrastructure, Application tarafından tanımlanan portları uygular.

### 8.6. Content / Data

Content/Data:

* authored JSON dosyalarını,
* schema tanımlarını,
* stable ID kataloglarını,
* content version bilgisini

taşır.

### 8.7. Tooling

Tooling:

* headless simulation runner,
* content validator,
* save inspector,
* migration verifier,
* balance report generator

gibi üretim dışı araçları barındırabilir.

### 8.8. Automated Tests

Test projeleri katman bazında ayrılmalıdır.

Testlerin büyük bölümü Godot editörü veya GPU gerektirmeden çalışmalıdır.

### 8.9. Bağımlılık yönü

```text
Presentation.Godot
        |
        v
Application
   |          |
   v          v
Domain <--- Simulation

Infrastructure
        |
        v
Application ports / Domain mappings

Tooling
        |
        v
Application / Simulation / Infrastructure composition
```

Domain içeri doğru bağımlılık merkezidir.

Presentation ve Infrastructure birbirine doğrudan bağlanmaz. Somut bağımlılıklar composition root tarafından bir araya getirilir.

---

## 9. Simülasyon Çekirdeği

Simülasyon çekirdeği motor bağımsız olacaktır.

Domain, Simulation ve Application katmanları:

* Godot node'larına,
* scene tree'ye,
* Godot resource formatına,
* Godot matematik tiplerine,
* Godot RNG'ye

bağımlı olmayacaktır.

Ana headless çalışma yöntemi saf .NET simulation runner'dır.

Godot headless çalışma:

* presentation smoke testleri,
* import doğrulaması,
* Windows export,
* engine integration

için kullanılacaktır.

### 9.1. Deterministik rastlantısallık

Simülasyon kendine ait bir RNG abstraction'ı kullanacaktır.

Kayıt dosyasında en az:

* root seed,
* RNG algorithm/version,
* gerekiyorsa RNG state veya deterministik stream bilgisi

korunacaktır.

`System.Random` veya Godot RNG domain'e dağınık biçimde çağrılamaz.

Aynı:

* başlangıç state'i,
* content version,
* simulation version,
* input sequence,
* seed

aynı canonical sonucu üretmelidir.

Kesin PRNG algoritması teknik spike ile doğrulandıktan sonra sürümlenecektir.

### 9.2. Zaman ilerletme

Simülasyon zamanı ayrık adımlarla ilerler.

UI yalnızca zaman ilerletme use-case'ini çağırır.

Frame delta:

* gün ilerletmez,
* maç sonucu üretmez,
* olay zinciri çalıştırmaz,
* domain state'in sahibi değildir.

Kesin takvim kuralları ilgili alt sistem belgesinde tasarlanacaktır.

### 9.3. Maç sunumu sınırı

Maç çekirdeği presentation-neutral çıktı üretir:

* timeline,
* events,
* statistics,
* snapshots,
* result,
* explanation metadata.

Text timeline, gelişmiş 2D ve olası 3D sunum aynı temel çıktıları tüketmelidir.

---

## 10. UI ve Sunum Katmanı

Ana UI yaklaşımı Godot `Control` tabanlı scene/component yapısıdır.

Örnek ekran alanları:

* Weekly Control Center
* Squad
* Tactics
* Training
* Transfers
* Relationships
* Match Timeline
* Inbox / Decisions
* Career History

UI akışı:

```text
User Interaction
        |
        v
Application Command / Query
        |
        v
Domain / Simulation
        |
        v
Result / Read Model
        |
        v
Godot Presentation
```

UI:

* SQLite'a doğrudan erişemez,
* domain collection'larını doğrudan mutasyona uğratamaz,
* moral, ilişki, yorgunluk, söz veya transfer state'i değiştiremez.

### 10.1. Büyük listeler

500 futbolculuk liste ve benzeri veri ekranlarında:

* tüm satırlar için pahalı scene oluşturulması varsayılmayacak,
* row recycling, virtualization veya paging değerlendirilecek,
* sorting ve filtering read model/query katmanında ele alınacak,
* filtreler domain state'i değiştirmeyecek,
* selection state ile domain state ayrılacak,
* performans spike ile ölçülecektir.

Godot'un hazır UI bileşenlerinin bütün tablo gereksinimlerini doğrudan karşılayacağı varsayılmayacaktır.

---

## 11. Veri ve İçerik Yaklaşımı

Runtime state ve authored content birbirinden ayrılır.

### 11.1. Runtime state

Runtime state:

* aktif kariyer,
* dünya durumu,
* kulüp ve futbolcu state'i,
* ilişkiler,
* sözler,
* hafızalar,
* takvim,
* simülasyon state'i

gibi değişken verileri içerir.

Aktif runtime state domain modelinde memory içinde bulunur.

### 11.2. Authored content

Authored content:

* başlangıç kulüpleri,
* başlangıç futbolcuları,
* olay şablonları,
* diyalog şablonları,
* isim havuzları,
* kültür ve kural parametreleri

gibi tasarım verilerini içerir.

Ana format UTF-8 JSON'dır.

İçerik:

* stable ID kullanmalı,
* version bilgisi taşımalı,
* schema validation'dan geçmeli,
* semantic validation'dan geçmeli,
* kaynak koddan ayrı tutulmalıdır.

### 11.3. İçerik araçları

MVP başında özel editör zorunlu değildir.

İlk yön:

* elle düzenlenebilir JSON,
* otomatik validator,
* gerekirse spreadsheet-to-JSON dönüşüm aracı.

Runtime doğrudan spreadsheet dosyasına bağlı olmayacaktır.

---

## 12. Kayıt ve Sürüm Geçişi Yönü

Kayıt formatı yönü:

`Versioned SQLite tabanlı tek dosyalı save container`

SQLite:

* runtime domain state'in yerine geçmez,
* persistence mekanizmasıdır,
* UI tarafından doğrudan kullanılmaz.

### 12.1. Save schema version

Her kayıt zorunlu schema version taşımalıdır.

Ek olarak gerektiğinde:

* game version,
* simulation version,
* content version,
* RNG version,
* creation/update metadata

saklanmalıdır.

### 12.2. Migration

Migration'lar:

* sıralı,
* tekrarlanabilir,
* otomatik testli,
* tek yönlü,
* loglanabilir

olmalıdır.

Migration başlamadan önce save yedeği alınır.

Migration başarısız olursa:

* orijinal save korunur,
* yarım migration sonucu geçerli save olarak kabul edilmez,
* hata açık biçimde raporlanır.

### 12.3. Doğrulama

Save load öncesinde:

* format,
* schema version,
* required records,
* referential integrity,
* temel domain invariant'ları

doğrulanır.

Bozuk kayıt sessizce yüklenmez.

Kurtarılabilir durumda son sağlıklı backup önerilebilir.

### 12.4. Snapshot ve event log

Snapshot ana state kaynağıdır.

Event log:

* tam event sourcing mekanizması değildir,
* her state değişikliğini sonsuza kadar saklamaz,
* önemli tarihçe,
* açıklanabilirlik,
* debug,
* sınırlı audit

amaçlarına hizmet eder.

Event retention, compaction ve summary politikaları `docs/13_SAVE_SYSTEM.md` içinde ayrıntılandırılacaktır.

Kesin tablo şeması bu belgede tanımlanmaz.

---

## 13. Test Stratejisi Yönü

Ana test altyapısı:

* saf .NET test projeleri,
* `dotnet test`,
* sabitlenmiş güncel xUnit.net sürümü,
* ayrı headless simulation runner.

### 13.1. Unit tests

Saf domain kuralları ve küçük simulation bileşenleri izole test edilir.

### 13.2. Integration tests

Application, infrastructure adapter'ları, geçici SQLite save ve content loading birlikte test edilir.

### 13.3. Simulation tests

Belirli dünya ve seed ile birden fazla gün, hafta, sezon veya kariyer adımı çalıştırılır.

### 13.4. Property ve invariant tests

Örnek invariant alanları:

* negatif olmayan zorunlu değerler,
* geçerli kulüp/futbolcu referansları,
* aynı olay sonucunun iki kez uygulanmaması,
* takvim sırasının bozulmaması,
* emekli veya silinmiş aktör referanslarının kontrolü,
* söz state geçişlerinin geçerli olması.

Kesin invariant listesi ilgili sistem belgelerinde tanımlanacaktır.

### 13.5. Save/load round-trip

Save öncesi ve load sonrası canonical state eşdeğerliği doğrulanır.

### 13.6. Determinism tests

Aynı seed ve input sequence ile canonical sonuç hash'i eşit olmalıdır.

Farklı seed senaryolarında invariant'lar korunurken en az bazı anlamlı sonuçların değişmesi beklenir.

### 13.7. Ten-season soak tests

UI açılmadan 10 sezonluk simülasyon çalıştırılır.

Test:

* exception,
* invalid state,
* reference corruption,
* uncontrolled memory growth,
* runaway event growth,
* save/load failure

aramalıdır.

### 13.8. Thousands-of-matches tests

Binlerce maç çalıştırılarak:

* skor dağılımı,
* güç farkı ile sonuç ilişkisi,
* aşırı uç sonuçlar,
* beraberlik oranı,
* home/away etkisi,
* determinism,
* invariant ihlalleri

raporlanır.

Kesin denge eşikleri maç sistemi belgesinde belirlenecektir.

### 13.9. Test failure diagnostics

Simulation test hataları en az:

* seed,
* scenario ID,
* simulation version,
* content version,
* simulation date,
* canonical state hash veya checkpoint

bilgisini raporlamalıdır.

---

## 14. Loglama, Profiling ve Debug

Loglama abstraction'ı:

`Microsoft.Extensions.Logging`

Yapılandırılmış yerel dosya logları için Serilog uyumlu provider yönü kullanılacaktır.

Loglar en az şu bağlamları taşıyabilmelidir:

* timestamp,
* severity,
* subsystem,
* career/save ID,
* simulation date,
* seed,
* correlation/context ID,
* application version.

Domain doğrudan dosya logger'ına bağımlı olmayacaktır.

Yerel loglar:

* rolling,
* retention,
* boyut sınırı

uygulamalıdır.

MVP'de zorunlu çevrim içi telemetry bulunmaz.

Debug seed, simulation report ve son güvenli checkpoint hata teşhisinde kullanılmalıdır.

Profiling yönü:

* Godot profiler: presentation, scene, rendering ve UI
* .NET diagnostics: CPU, allocation ve memory
* BenchmarkDotNet: izole performans benchmark'ları için gerektiğinde

---

## 15. Build ve CI Yönü

CI sağlayıcısı:

`GitHub Actions`

Birincil runner:

`Windows`

Per-commit veya pull request kontrolleri, ilgili altyapı oluşturulduğunda en az şunları kapsamalıdır:

1. Restore
2. Build
3. Unit tests
4. Integration smoke tests
5. Determinism smoke test
6. Save/load smoke test
7. Content validation
8. Documentation validation

Godot entegrasyonu oluşturulduğunda:

* headless import,
* headless Windows export,
* exported build smoke test

ayrı job olarak çalıştırılmalıdır.

Uzun soak ve büyük denge testleri ayrı:

* scheduled,
* nightly,
* manual

workflow olarak çalıştırılabilir.

İlk dağıtım Windows x64 portable build yönündedir.

Installer ve code signing ayrı dağıtım kararıdır.

---

## 16. Teknik Doğrulama Spike'ları

En fazla altı teknik spike uygulanacaktır.

### Spike 1 — Motor bağımsız 10 sezonluk headless simulation

**Doğruladığı risk**

Saf .NET çekirdeğinin Godot olmadan çalışması, 20 kulüp ve yaklaşık 500 futbolculuk dünya ölçeğinin uzun dönem yürütülebilmesi.

**Başarı kriterleri**

* UI ve Godot açılmadan çalışır.
* 10 sezonluk minimal dünya senaryosu tamamlanır.
* Ardışık en az 10 çalıştırmada exception veya invariant ihlali oluşmaz.
* CI için tek çalıştırma süresi beş dakikalık üst bütçeyi aşmaz.
* Retained memory sezonlar boyunca sürekli ve açıklanamayan biçimde büyümez.
* Seed ve performans raporu üretilir.

**Başarısız olursa etkilenecek karar**

Simulation katmanı sınırları, dünya ayrıntı seviyesi ve performans bütçesi yeniden değerlendirilir. Godot seçimi tek başına değişmez.

### Spike 2 — Deterministik sonuç ve seed doğrulaması

**Doğruladığı risk**

Rastlantısallığın tekrar üretilebilirliği ve save/load sonrasında aynı akışın devam etmesi.

**Başarı kriterleri**

* Aynı başlangıç state'i ve seed ile en az 20 tekrar aynı canonical final hash'i üretir.
* Simülasyon ortasında save/load yapıldığında kesintisiz koşuyla aynı final hash elde edilir.
* Farklı seed, invariant'ları bozmadan en az bir anlamlı sonuç farkı üretir.
* RNG version bilgisi raporlanır.

**Başarısız olursa etkilenecek karar**

RNG abstraction, stream stratejisi, serialization ve simulation ordering yaklaşımı yeniden değerlendirilir.

### Spike 3 — SQLite save/load, migration ve corruption davranışı

**Doğruladığı risk**

Kayıt bütünlüğü, transaction, schema version ve migration güvenliği.

**Başarı kriterleri**

* Save/load round-trip canonical state eşdeğerliği sağlar.
* Örnek eski sürüm save yeni sürüme migrate edilir.
* Migration öncesi backup oluşturulur.
* Migration hatasında orijinal save değişmez.
* Bilinçli bozulmuş save algılanır ve geçerli state olarak yüklenmez.
* Geçici dosya veya yarım işlem geçerli save olarak kalmaz.

**Başarısız olursa etkilenecek karar**

SQLite provider, save container yapısı veya migration transaction yaklaşımı yeniden değerlendirilir. Snapshot-first kararı korunur.

### Spike 4 — 500 futbolculuk Godot UI listesi

**Doğruladığı risk**

Godot `Control` UI'nin yoğun veri ekranlarında performans ve bakım yeterliliği.

**Başarı kriterleri**

* 500 kayıt görüntülenebilir.
* Sorting, filtering ve selection doğru çalışır.
* Filtre sonucu normal geliştirme bilgisayarında 100 ms hedefinin altında güncellenir.
* Scroll sırasında belirgin input kilitlenmesi oluşmaz.
* Hedef 60 FPS'tir; p95 frame süresi 33 ms'yi aşmamalıdır.
* Row recycling, virtualization veya paging yaklaşımından en az biri doğrulanır.
* UI domain state'i doğrudan değiştirmez.

**Başarısız olursa etkilenecek karar**

Godot UI component yaklaşımı yeniden tasarlanır. Sorun çözülemezse presentation katmanı için Avalonia geri dönüş seçeneği yeniden açılır; Domain, Simulation ve Application katmanları değişmez.

### Spike 5 — Windows x64 export ve temiz ortam çalıştırma

**Doğruladığı risk**

Paketleme, runtime bağımlılıkları ve kullanıcı makinesinde açılış.

**Başarı kriterleri**

* Windows x64 export alınır.
* Godot editörü ve .NET SDK bulunmayan temiz Windows ortamında uygulama açılır.
* Temel UI açılış smoke testi geçer.
* Save ve log klasörlerine yazılabilir.
* Third-party license bildirimleri pakette bulunur.
* Crash veya missing runtime hatası oluşmaz.

**Başarısız olursa etkilenecek karar**

Deployment modeli, runtime packaging veya Godot/.NET sürüm kombinasyonu yeniden değerlendirilir.

### Spike 6 — CI üzerinde saf .NET testleri ve Godot headless doğrulaması

**Doğruladığı risk**

Yerel ortam bağımlılığı ve otomasyon zinciri.

**Başarı kriterleri**

* Windows GitHub Actions runner üzerinde saf .NET build ve testler çalışır.
* Domain ve Simulation testleri Godot editörü gerektirmez.
* Godot headless import ve export job'ı çalışır.
* Test ve build artefact'ları saklanır.
* Her hata non-zero exit code üretir.
* Aynı seed ile determinism smoke test sonucu CI ve yerel ortamda eşleşir.

**Başarısız olursa etkilenecek karar**

SDK pinleme, CI image, Godot export automation ve build scripts yönü yeniden değerlendirilir.

---

## 17. Reddedilen Alternatifler

### 17.1. Godot + GDScript

Ana dil olarak reddedildi.

Gerekçe:

* geliştiricinin ileri C#/.NET deneyimini kullanmaması,
* büyük domain modelinde refactoring güvenliğinin daha düşük olması,
* standart .NET test ve migration araçlarından uzaklaşması,
* motor bağımsız çekirdek sınırını korumanın zorlaşması.

### 17.2. Unity + C#

Birincil motor olarak reddedildi.

Gerekçe:

* MVP için gereğinden ağır editör ve motor yüzeyi,
* geliştiricinin Unity deneyiminin daha düşük olması,
* olası 3D ihtiyacının kesin ürün hedefi olmaması,
* Godot'a göre daha yüksek lisans ve vendor riski.

### 17.3. .NET + Avalonia

Birincil presentation framework olarak reddedildi ancak geri dönüş seçeneği olarak korundu.

Gerekçe:

* veri ekranlarında çok güçlü olmasına rağmen oyun sunumu ve gelişmiş 2D için ek renderer maliyeti,
* gelecekte ayrı oyun motoru entegrasyonu ihtimali,
* Godot'un mevcut proje için daha dengeli oyun/UI kombinasyonu sunması.

Godot UI spike'ı başarısız olursa presentation katmanı için ilk yeniden değerlendirme adayı Avalonia'dır.

### 17.4. React/TypeScript + Tauri + .NET

Reddedildi.

Gerekçe:

* React/TypeScript, Rust/Tauri ve C#/.NET arasında çoklu teknoloji yüzeyi,
* IPC ve process lifecycle karmaşıklığı,
* web veya mobil hedef bulunmaması,
* tek geliştirici bakım maliyetinin artması.

### 17.5. MonoGame benzeri düşük seviyeli framework

Birincil seçenek olarak reddedildi.

Gerekçe:

* UI, scene, tooling ve asset altyapısının önemli bölümünü özel geliştirme gerektirmesi,
* MVP'nin doğrulanmasını geciktirmesi,
* Godot'un aynı görsel esnekliği daha düşük altyapı maliyetiyle sunması.

---

## 18. Riskler ve Azaltma Planları

| Risk | Etki | Azaltma |
| --- | --- | --- |
| Godot UI'nin büyük tablolarda yetersiz kalması | Ana veri ekranlarının yavaş veya pahalı geliştirilmesi | 500 kayıt UI spike'ı; recycling, virtualization veya paging; başarısızlıkta Avalonia değerlendirmesi |
| Godot tiplerinin domain'e sızması | Motor bağımlılığı ve test zorluğu | Saf .NET proje sınırları, dependency rules ve architecture tests |
| Save şemasının hızlı değişmesi | Eski kayıtların bozulması | Schema version, migration testleri, backup, round-trip ve corruption testleri |
| Event log büyümesi | Save boyutu ve performans sorunu | Snapshot-first, bounded audit log, compaction ve summary politikası |
| Determinizmin call-order değişiklikleriyle bozulması | Hataların tekrar üretilememesi | Project-owned RNG abstraction, versioned seed/state, canonical hash testleri |
| En yeni motor sürümünde regresyon | Build veya runtime kararsızlığı | Stable release kullanımı, spike, exact patch pinleme, kontrollü upgrade |
| Tek geliştirici bakım yükü | Geliştirmenin yavaşlaması | Tek ana dil, sınırlı package yüzeyi, katmanlı yapı, otomatik test ve tooling |
| Çok erken özel içerik aracı geliştirme | MVP kapsam kayması | JSON + validator ile başlama; editör ihtiyacını ölçümle doğrulama |
| Aşırı genel event bus | Gizli coupling ve sıralama hataları | Typed event contracts, açık handler sahipliği ve application orchestration |
| Uzun soak testlerinin CI'ı yavaşlatması | Geri bildirim süresinin artması | Hızlı test ve scheduled soak workflow ayrımı |

---

## 19. Açık Kalan Teknik Sorular

Aşağıdaki konular kararın özünü değiştirmez ancak spike veya ilgili alt tasarım belgesinde kesinleştirilmelidir:

1. Versioned deterministic PRNG algoritması ve stream stratejisinin nihai genişleme modeli.
2. Godot büyük liste component'inin recycling, virtualization veya paging uygulaması.
3. İlk portable build sonrasında installer biçimi.
4. Code signing zamanı ve sertifika yaklaşımı.
5. Event/audit retention ve compaction eşikleri.
6. Save backup sayısı ve saklama politikası.
7. Scheduled soak testlerinin kesin süre ve performans eşikleri.

Bu sorular sessiz varsayımla kapatılmamalıdır.

Kapanan teknik pinler: Godot 4.7-stable mono/.NET, `net9.0`, SDK `9.0.317/latestPatch` ve `Microsoft.Data.Sqlite 10.0.9` (D-339, D-384).

---

## 20. Sonraki Adım

Altı teknik spike tamamlanmış, üretim implementasyonu başlamış ve araç zinciri D-384 ile pinlenmiştir. Güncel uygulama sırası `docs/19_PRODUCTION_IMPLEMENTATION_PLAN.md` üstündeki uygulama kontrol noktasında izlenir; sıradaki teknik hedefler tam test kapısını korumak, çok sezon kariyer bütünlüğünü tamamlamak ve tam 10 sezonluk MVP kabul koşusunu kanıtlamaktır.

---

## Karar Özeti

Seçilen yön:

* Platform: Windows 10/11 x64
* Motor: Godot 4 .NET
* Dil: C#
* UI: Godot `Control`
* Simülasyon: Godot'tan bağımsız saf .NET
* Headless çalışma: saf .NET simulation runner
* Persistence: versioned SQLite tek dosyalı save
* İçerik: doğrulanan UTF-8 JSON
* Test: xUnit.net, `dotnet test`, simulation ve soak runner
* Loglama: `Microsoft.Extensions.Logging` abstraction ve structured local logging
* CI: GitHub Actions, Windows runner
* Gelecek görsel sunum: presentation-neutral match timeline/snapshot çıktıları

Ana trade-off:

Godot'un veri tablosu yetenekleri Avalonia veya web UI kadar hazır değildir. Buna karşılık Godot; oyun sunumu, animasyon, ses ve gelecekteki gelişmiş 2D görünüm için daha dengeli bir temel sağlar.
