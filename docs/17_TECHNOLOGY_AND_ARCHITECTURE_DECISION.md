# Teknoloji ve Yüksek Seviyeli Mimari Kararı

**Belge:** `docs/17_TECHNOLOGY_AND_ARCHITECTURE_DECISION.md`
**Durum:** Kesinleşti
**Ana referans:** `docs/01_GAME_DESIGN_DOCUMENT.md`
**Kesin MVP sınırı:** `docs/02_MVP_SCOPE.md`
**İlgili karar günlüğü:** `docs/15_DECISION_LOG.md`

---

# 1. Belgenin Amacı

Bu belge, Football Career Simulator projesinin ilk oynanabilir sürümü (MVP) için onaylanmış teknoloji yığınını ve yüksek seviyeli mimari yönü kalıcı olarak kayıt altına alır.

Bu belge:

* MVP kapsamını değiştirmez veya genişletmez,
* `docs/02_MVP_SCOPE.md` içinde tanımlanan sınırları esas alır,
* domain, olay, kural, hafıza, söz, ilişki, transfer veya maç sistemlerinin ayrıntılı tasarımını yapmaz,
* kesin proje scaffold'u, kaynak kodu veya proje dosyası üretmez,
* bu belgede açıkça verilen teknoloji kararlarını yeniden tartışmaz.

Bu belgedeki kararlar bağlayıcıdır ve alt sistem tasarım belgeleri bu kararlarla çelişmeyecek şekilde hazırlanmalıdır.

---

# 2. Karar Bağlamı

Ana oyun tasarım belgesi (`docs/01_GAME_DESIGN_DOCUMENT.md`) Kural 6 ve Kural 7 gereği motor/veritabanı/arayüz teknolojisi değişse bile domain modelinin korunmasını ve ana simülasyonun harici servislere zorunlu bağımlı olmamasını şart koşar. `docs/02_MVP_SCOPE.md` ise MVP'nin kesin sayısal ve işlevsel sınırlarını tanımlamış, ancak teknoloji seçimini bilinçli olarak açık bırakmıştır (Bölüm 1).

Bu belge, o boşluğu doldurur: MVP'nin kesinleşmiş kapsamı temel alınarak motor, dil, UI yaklaşımı, simülasyon çekirdeği, kayıt yaklaşımı, test stratejisi, loglama ve CI yönü kesinleştirilir.

Karar süreci `docs/16_INITIAL_ANALYSIS.md` içinde tanımlanan teknik risklerle (performans, veri hacmi büyümesi, determinizm/yeniden üretilebilirlik, kayıt sürüm geçişi, modülerlik, dış AI bağımsızlığı) uyumlu şekilde yürütülmüştür.

---

# 3. Bağlayıcı Gereksinimler

Aşağıdaki gereksinimler bu kararın çıkış noktasıdır:

* Domain ve simülasyon çekirdeği motor teknolojisinden bağımsız olmalıdır (GDD Kural 6).
* Ana simülasyon internete veya harici üretken yapay zekâya zorunlu bağımlı olmamalıdır (GDD Kural 7).
* Kullanıcı arayüzü iş kurallarının sahibi olmamalıdır (GDD Kural 5; MVP Kapsamı Bölüm 2.2).
* Kayıt bütünlüğü, veri doğrulama, olayların tekil uygulanması ve rastlantısallığın tekrar üretilebilirliği kısa vadeli hız uğruna feda edilemez (MVP Kapsamı Bölüm 2.3).
* Sistem en az 10 sezon boyunca hatasız simüle edilebilmelidir (MVP Kapsamı Bölüm 22).
* MVP'de yaklaşık 500 aktif futbolcu, 20 kulüp ve 38 maçlık sezon ölçeği desteklenmelidir (MVP Kapsamı Bölüm 17).
* Maç sonucunu hesaplayan simülasyon çekirdeği ile maçın sunum katmanı ayrılmalıdır (MVP Kapsamı Bölüm 19).
* Fiziksel 2D veya 3D maç sunumu MVP kapsamı dışındadır ancak mimari bu olasılığı gelecekte kapatmamalıdır (MVP Kapsamı Bölüm 19, Bölüm 23).

---

# 4. Kullanıcı ve Proje Varsayımları

* Proje tek geliştirici tarafından yürütülmektedir; bakım ve öğrenme maliyeti düşük tutulmalıdır.
* Geliştiricinin C#/.NET deneyimi ileri seviyededir; Unity deneyimi temel düzeydedir.
* Birincil hedef platform Windows masaüstüdür; web, mobil veya çevrim içi çok oyunculu hedef yoktur.
* Nihai vizyonda gelecekte 2D veya olası 3D maç sunumu değerlendirilebilir; bu olasılık mimari tarafından erken kapatılmamalıdır.
* AI destekli kodlama araçlarıyla uyumlu, iyi tip güvenliğine sahip ve test edilebilir bir teknoloji ailesi tercih edilir.

---

# 5. Değerlendirilen Seçenekler

Aşağıdaki teknoloji aileleri değerlendirilmiştir:

1. **Godot 4.7 .NET + C#** — seçilen yığın.
2. **.NET + Avalonia** — güçlü ikinci aday; UI spike'ı başarısız olursa geri dönüş adayı.
3. **Unity + C#** — teknik olarak yeterli ancak reddedildi.
4. **Godot + GDScript** — reddedildi.
5. **React/TypeScript + Tauri (+ .NET)** — reddedildi.
6. **MonoGame veya benzeri düşük seviyeli yaklaşım** — reddedildi.

Değerlendirme kriterleri ve gerekçeler Bölüm 6 (Karar Matrisi) ve Bölüm 19 (Reddedilen Alternatifler) içinde detaylandırılmıştır.

---

# 6. Karar Matrisi

Puan ölçeği: 1 = zayıf, 2 = belirgin riskli, 3 = kabul edilebilir, 4 = güçlü, 5 = çok güçlü.

| Kriter | Ağırlık | Godot + C# | Avalonia + C# | Unity + C# | Godot + GDScript | React/Tauri + .NET |
| --- | ---: | ---: | ---: | ---: | ---: | ---: |
| Domain ve simulation test edilebilirliği | 10 | 5 | 5 | 4 | 3 | 4 |
| Deterministik/headless simulation | 10 | 5 | 5 | 4 | 4 | 4 |
| UI ağırlıklı oyun geliştirme verimliliği | 10 | 4 | 5 | 4 | 5 | 5 |
| Tek geliştirici bakım maliyeti | 9 | 5 | 5 | 3 | 4 | 3 |
| Refactoring ve statik tip güvenliği | 8 | 5 | 5 | 5 | 3 | 4 |
| Kayıt ve migration kolaylığı | 7 | 5 | 5 | 4 | 3 | 4 |
| Otomatik test tooling'i | 7 | 5 | 5 | 5 | 3 | 4 |
| Windows dağıtımı | 5 | 5 | 5 | 5 | 5 | 4 |
| Performans ve profiling | 5 | 4 | 4 | 5 | 4 | 4 |
| Lisans ve ticari risk | 6 | 5 | 5 | 3 | 5 | 5 |
| Gelecekte 2D/3D sunum | 8 | 5 | 2 | 5 | 5 | 2 |
| Motor bağımlılığını kontrol etme | 5 | 4 | 5 | 3 | 2 | 4 |
| AI kodlama araçlarıyla uyum | 5 | 5 | 5 | 5 | 3 | 4 |
| Dokümantasyon ve ekosistem | 2 | 4 | 4 | 5 | 4 | 4 |
| Uzun vadeli sürdürülebilirlik | 3 | 5 | 4 | 4 | 3 | 3 |

Ağırlıklı sonuçlar yaklaşık ve yönlendiricidir:

1. Godot + C#: yaklaşık 95/100
2. Avalonia + C#: yaklaşık 92/100
3. Unity + C#: yaklaşık 84/100
4. React/Tauri + .NET: yaklaşık 78/100
5. Godot + GDScript: yaklaşık 77/100

Puanların kullanıcı deneyimi, tek geliştirici kapasitesi ve gelecekteki 2D olasılığı varsayımlarına bağlı olduğu açıkça belirtilir; bu sayılar kesin bilimsel ölçüm değil, yönlendirici karar destek verisidir.

---

# 7. Seçilen Teknoloji Yığını

## 7.1. Hedef platform

* Windows 10/11, x64 masaüstü, klavye ve fare odaklı kullanım.
* MVP sırasında Linux, macOS, mobil ve web hedeflenmez ve kabul kriterine alınmaz.
* Domain, simulation, application, content ve persistence katmanları işletim sistemine özel bağımlılık taşımaz; Linux ve macOS yalnızca MVP doğrulandıktan sonra değerlendirilebilir.

## 7.2. Motor

* Seçilen motor: **Godot 4.7 .NET**.
* Godot 4.7 kararlı sürüm hattı esas alınır; kesin patch sürümü proje scaffold'u öncesindeki teknik spike sırasında seçilip sabitlenir.
* Motor sürümü kontrolsüz biçimde yükseltilmez; her yükseltme ayrı değerlendirme, migration kontrolü ve smoke test gerektirir.

## 7.3. Ana programlama dili

* Ana ve varsayılan dil: **C#**.
* C# şu alanlarda kullanılır: Domain, Simulation, Application/Use Cases, Persistence, Content validation, Tooling, Automated tests, Godot presentation davranışları, editor dışı teknik araçlar.
* GDScript MVP üretim kodunda varsayılan olarak kullanılmaz. GDScript ancak ileride küçük, domain dışı, yalnızca Godot editörünü destekleyen ve C#'a göre açık/ölçülebilir avantaj sağlayan bir araç için ayrı bir karar kaydıyla istisna olarak değerlendirilebilir.
* GDScript hiçbir durumda domain kuralları, simülasyon, kayıt migrasyonları, persistence, deterministik rastlantısallık, maç hesaplama veya olay/kural motoru için kullanılamaz.

## 7.4. .NET sürüm yönü

* Motor bağımsız .NET projeleri için tercih edilen taban: **.NET 10 LTS**.
* Ortak target framework, seçilen Godot 4.7 .NET patch sürümünün resmî desteklediği target framework doğrulanarak kesinleştirilir.
* Godot presentation projesi ve paylaşılan core projeleri arasında uyumsuz target framework kullanılmaz; gerekirse core projeleri ortak desteklenen LTS framework'ü hedefler.
* Kesin target framework teknik spike sonrasında pinlenir. Preview veya destek dışı .NET sürümü kullanılmaz.
* Bu konu teknoloji ailesini değiştiren açık soru değildir; yalnızca sürüm pinleme doğrulamasıdır.

---

# 8. Hedef Platform ve Dağıtım

* İlk build hedefi: Windows x64 portable release package. Installer daha sonra ayrı bir karar olarak değerlendirilebilir.
* Release build Godot editörü gerektirmeden çalışmalı, gerekli runtime bağımlılıkları belgelenmeli, lisans bildirimlerini içermeli, save ve log klasörlerini Windows kullanıcı dizinlerinde güvenli konumlarda tutmalıdır.
* Sürüm pinleme yönü: Godot patch sürümü pinlenir, .NET SDK sürümü pinlenir, NuGet paket sürümleri merkezi ve kontrollü tutulur, CI ve yerel geliştirme aynı ana sürümleri kullanır, otomatik major upgrade yapılmaz.

---

# 9. Yüksek Seviyeli Mimari Yön

Aşağıdaki katmanlar bağlayıcı yüksek seviyeli sınırlardır. Kesin proje scaffold'u bu belgede oluşturulmaz.

## 9.1. Domain

Sorumlulukları: temel iş kavramları, domain invariants, value object'ler, aggregate sınırları, domain davranışları, domain event sözleşmeleri.

Bağımlılıkları: başka oyun katmanına, Godot'a, dosya sistemine, logging implementasyonuna veya persistence implementasyonuna bağımlı olmaz.

## 9.2. Simulation

Sorumlulukları: dünya ilerletme, takvim adımları, maç ve diğer simülasyon süreçlerinin koordinasyonu, deterministik rastlantısallık kullanımı, uzun dönem simülasyon akışı, olay ve kural değerlendirme orkestrasyonu.

Bağımlılıkları: Domain'e bağımlı olabilir; Presentation, Godot veya Infrastructure implementasyonlarına bağımlı olamaz.

## 9.3. Application / Use Cases

Sorumlulukları: kullanıcı ve sistem use case'leri, command/query koordinasyonu, işlem sınırları, domain ve simulation çağrıları, persistence portları, read model üretimi, save/load orkestrasyonu.

Bağımlılıkları: Domain ve Simulation'a bağımlı olabilir; Infrastructure implementasyonlarına doğrudan veya Presentation'a bağımlı olmaz.

## 9.4. Contracts / Read Models

Sorumlulukları: presentation-neutral command/query sözleşmeleri, read model'ler, application sonuçları, UI'ye taşınacak salt okunur veri şekilleri. Bu modeller domain entity'lerinin kendisi değildir.

## 9.5. Persistence / Infrastructure

Sorumlulukları: save dosyası okuma/yazma, migration, checksum, atomik dosya işlemleri, backup, logging adaptörleri, işletim sistemi ve dosya sistemi entegrasyonları. Application veya core katmanlarının tanımladığı interface'leri uygular; domain kurallarının sahibi değildir.

## 9.6. Content / Data

Sorumlulukları: kulüp ve oyuncu başlangıç verileri, olay ve diyalog şablonları, doğrulanabilir tasarım verileri, content version, import ve validation. Runtime mutable state ile karıştırılmaz.

## 9.7. Presentation.Godot

Sorumlulukları: Godot scene'leri, `Control` tabanlı UI, input, ses, animasyon, navigation, Godot-specific adapter ve composition root. Domain davranışlarının sahibi değildir.

## 9.8. Tooling

Sorumlulukları: simulation runner, content validator, save inspector, migration doğrulama aracı, benchmark runner, simulation raporu üretimi. İlk geliştirme aşamasında yalnızca gerekli araçlar oluşturulur.

## 9.9. Automated Tests

Test projeleri üretim katmanlarından ayrı tutulur. Beklenen yüksek seviyeli test ayrımı: Unit Tests, Integration Tests, Simulation Tests, Persistence/Migration Tests, Presentation Smoke Tests, Benchmarks.

## 9.10. Bağımlılık yönü

* `Presentation -> Application -> Domain / Simulation`
* `Infrastructure -> Application ports / Domain contracts`
* `Tooling -> Application / Simulation / Infrastructure composition`
* `Tests -> test edilen katmanlar`

Domain ve Simulation hiçbir zaman Presentation veya Godot'a geri bağımlı olmaz.

---

# 10. Simülasyon Çekirdeği

Domain ve simülasyon çekirdeği Godot'tan bağımsız saf .NET projeleri olarak tasarlanır.

Core katmanlarında şu tipler kullanılamaz: `Godot.Node`, `Godot.Resource`, `Godot.Vector2`, `Godot.Vector3`, `Godot.RandomNumberGenerator`, `Godot.Signal`, Godot sahne veya asset referansları, presentation tipleri.

Simülasyon: kullanıcı arayüzü açılmadan çalışabilmeli, Godot motoru başlatılmadan test edilebilmeli, ayrı console simulation runner üzerinden çalıştırılabilmeli, aynı seed ve aynı input ile tekrar üretilebilir olmalı, frame rate'ten bağımsız olmalı, açık takvim adımları ve simulation checkpoint'leri üzerinden ilerlemelidir.

Godot `--headless` yalnızca presentation smoke testleri, motor entegrasyon testleri ve CI export işlemleri için kullanılır. Ana uzun dönem simülasyon testleri Godot süreci gerektirmez.

## 10.1. Deterministik rastlantısallık

`System.Random` doğrudan domain veya simulation katmanında kullanılmaz.

Deterministik rastlantısallık için proje tarafından sahip olunan, algoritması açıkça seçilmiş, sürümü kaydedilen, state'i save dosyasında saklanabilen bir RNG abstraction kullanılır.

İlk tercih: **PCG32 veya eşdeğer küçük, kararlı ve sürümlenebilir deterministik algoritma**. Kesin implementasyon teknik spike ve simulation tasarımı sırasında doğrulanır.

RNG yaklaşımı: root career seed bulunur; alt sistemler için isimlendirilmiş RNG stream'leri türetilir; maç, transfer, olay ve gelişim gibi sistemlerin rastgele sayı tüketimi mümkün olduğunca birbirinden ayrılır; seed yanında RNG algoritma sürümü de kaydedilir; debug raporunda seed ve simulation run ID bulunur.

Aynı seed için tekrar üretilebilirlik garantisi aynı oyun sürümü, aynı content sürümü, aynı save schema sürümü, aynı RNG algoritma sürümü ve aynı input sırası kapsamında verilir. Farklı oyun sürümleri arasında bit düzeyinde aynı simülasyon sonucu zorunlu değildir; eski kayıtların yüklenebilmesi ve doğru migration uygulanması zorunludur.

## 10.2. Zaman ilerletme

Simulation zamanı gerçek zamanlı render loop'tan ayrılır. Zaman; takvim günü, planlama dönemi, anlamlı checkpoint, maç veya karar noktası gibi açık simulation adımlarıyla ilerler.

Godot `_Process` veya `_PhysicsProcess`, dünya takviminin veya domain zamanının sahibi olamaz.

---

# 11. Olay ve Kural Entegrasyon Sınırı

Olay tabanlı yapı korunur ancak kontrolsüz genel mesajlaşma sistemi kurulmaz.

Temel ilkeler:

* Domain event'leri typed ve açık sözleşmeli olacaktır.
* Event dispatch sırası deterministik olmalıdır.
* Bir event'in hangi sistemler tarafından işlendiği izlenebilir olmalıdır.
* Aynı sonucun iki kez uygulanması önlenmelidir.
* Application veya Simulation işlem sınırı event'lerin uygulanmasını koordine etmelidir.
* UI, domain event bus'a doğrudan abone olarak iş kuralı çalıştırmamalıdır.
* Godot autoload singleton, domain event bus yerine kullanılamaz.
* Event log sınırsız büyüyen ana veri kaynağı olmayacaktır.
* Tam event sourcing kullanılmayacaktır.

Kesin event tipleri ve handler tasarımları bu belgede oluşturulmaz; ilgili olay/kural motoru tasarım belgesinde ele alınacaktır.

---

# 12. UI ve Sunum Katmanı

Ana UI yaklaşımı: **Godot Control tabanlı scene/component presentation katmanı**.

UI'nin sorumlulukları: Application katmanından read model almak, bilgileri göstermek, kullanıcı kararlarını toplamak, application command veya use case çağrısı yapmak, doğrulama ve işlem sonuçlarını göstermek, ses/animasyon/geçiş/görsel geri bildirim sağlamak.

UI'nin yapamayacağı işlemler: domain entity alanlarını doğrudan değiştirmek; moral, kondisyon, ilişki, hafıza, söz, transfer veya yönetim güvenini doğrudan güncellemek; maç sonucu üretmek; rastlantısallık oluşturmak; takvim zamanını frame rate üzerinden ilerletmek; persistence dosyasına doğrudan yazmak; veritabanına veya save dosyasına doğrudan erişmek; domain event'lerini kontrolsüz global signal ağına dönüştürmek.

Ana iletişim yönü: `Godot UI -> Application Command / Query -> Domain ve Simulation -> Result / Read Model -> Godot UI`.

Godot signal sistemi yalnızca presentation katmanı içindeki görsel ve input koordinasyonu için kullanılabilir. Domain event sistemi Godot signal sistemine bağımlı olmayacaktır.

## 12.1. Büyük liste ve tablolar

Kadro, transfer ve oyuncu listelerinde yaklaşık 500 aktif futbolcunun bulunacağı dikkate alınır. Godot'un standart UI kontrollerinin yeterli olacağı varsayılmaz.

Benimsenen yaklaşım: liste verisi application read model üzerinden sunulur; filtreleme ve sıralama domain state'ini değiştirmez; görünür olmayan her satır için karmaşık node ağacı oluşturulmaz; satır pooling, virtualization veya sayfalama değerlendirilir; UI performansı teknik spike ile ölçülür; UI spike başarısız olursa ilk geri dönüş adayı `.NET + Avalonia` olur; bu geri dönüşün mümkün olması için core katmanları Godot'tan bağımsız tutulur.

## 12.2. Gelecekte 2D veya 3D sunum

Maç simülasyonu görsel sunumdan tamamen bağımsız olacaktır. Maç çekirdeğinin sunuma verdiği sonuç ailesi yüksek seviyede maç sonucu, olay zaman çizelgesi, temel istatistikler, önemli anlar, presentation-neutral durum görüntüleri, açıklama ve sebep verilerini kapsayabilir. Kesin DTO veya sınıf şeması bu belgede oluşturulmaz.

Metin tabanlı timeline, gelecekteki 2D gösterim ve olası 3D gösterim aynı simülasyon sonucunu farklı biçimlerde sunabilmelidir.

Görsel katman: maç sonucunu yeniden hesaplayamaz, kendi rastlantısallığını kullanamaz, simülasyon state'ini değiştiremez, frame rate'e bağlı oyun kuralı çalıştıramaz.

---

# 13. Veri ve İçerik Yaklaşımı

## 13.1. Runtime state ve content ayrımı

İki veri ailesi kesin olarak ayrılır.

**Runtime state:** dünya state'i, takvim, kulüp ve oyuncu durumları, ilişkiler, hafıza, sözler, aktif olay zincirleri, RNG state'leri, kariyer geçmişi.

**Content data:** kulüp şablonları, futbolcu başlangıç verileri, olay şablonları, diyalog şablonları, isim havuzları, ayar ve denge tabloları.

Content verisi runtime save state'in içine kontrolsüz biçimde kopyalanmaz. Save dosyası kullanılan content sürümünü referanslar.

## 13.2. Content üretim kararı

Authoring formatları: JSON (hiyerarşik ve şablon tabanlı içerik), CSV (toplu ve tablosal başlangıç verileri), lokalizasyon anahtarları (doğrudan metin yerine referanslanabilir yapı).

Godot `Resource` dosyaları core content'in tek ve bağlayıcı kaynağı olmayacaktır.

Content pipeline: authoring dosyalarını oku; şema ve alan doğrulaması yap; referans bütünlüğünü doğrula; identifier benzersizliğini doğrula; aralık ve invariant kontrollerini çalıştır; runtime immutable content catalog üret; content version ve hash üret; CI üzerinde validation çalıştır.

İlk aşamada özel görsel content editor oluşturulmaz. Özel editor ancak manuel hata oranı yükselirse, içerik hacmi doğrulanmış biçimde büyürse veya JSON/CSV authoring geliştirmenin ana darboğazı hâline gelirse ayrı özellik olarak değerlendirilir.

---

# 14. Kayıt ve Sürüm Geçişi Yönü

## 14.1. MVP'de veritabanı kararı

MVP save sistemi için SQL Server, SQLite veya başka bir ilişkisel veritabanı kullanılmaz.

Gerekçe: dünya ölçeği yaklaşık 500 aktif futbolcu ve 20 kulüple sınırlıdır; offline ve taşınabilir tek save dosyası tercih edilmektedir; domain object graph'ının ilişkisel şemaya erken bağlanması migration ve bakım maliyetini artırır; kayıt bütünlüğü explicit persistence DTO ve migration pipeline ile daha kontrollü ele alınabilir.

SQLite gelecekte yalnızca telemetry, yerel analytics, çok büyük history sorguları, içerik arama veya editör tooling'i için somut performans ihtiyacı kanıtlanırsa ayrı karar olarak değerlendirilebilir.

## 14.2. Save biçimi

Save dosyası yönü: **sürümlendirilmiş, sıkıştırılmış JSON snapshot container**. Önerilen uzantı: `.fcsave`.

Container yüksek seviyede şunları içermelidir: `manifest.json`, `state.json`, isteğe bağlı `summary.json`, checksum bilgileri, bounded diagnostic veya timeline kayıtları.

Kesin dosya şeması `docs/13_SAVE_SYSTEM.md` içinde tasarlanacaktır.

JSON serileştirme yönü: `System.Text.Json`; explicit persistence DTO'ları; domain entity'lerini doğrudan serialize etmeme; enum ve identifier davranışlarını açıkça kontrol etme; canonical serialization veya canonical state hash yaklaşımı; invariant culture kullanımı.

## 14.3. Save source of truth

Ana kayıt kaynağı: **state snapshot**.

Event log tam event sourcing kaynağı değildir; oyuncuya gösterilen geçmiş, açıklama, audit, debug ve sınırlı yeniden üretim desteği için kullanılır; sınırsız büyümez; retention ve özetleme politikası ilgili sistem belgelerinde belirlenir.

## 14.4. Save versioning

Her save dosyasında en az şunlar bulunmalıdır: save schema version, game version, content version, created timestamp, last saved timestamp, root career seed, RNG algorithm version, platform bilgisi, checksum metadata.

## 14.5. Migration

Migration yaklaşımı: **sequential forward migrations** (örnek yön: `V1 -> V2 -> V3`).

Kurallar: migration doğrudan eski save'i yerinde değiştirmez; önce backup alınır; eski dosya okunur ve doğrulanır; sıralı migration adımları uygulanır; yeni state doğrulanır; yeni dosya geçici path'e yazılır; checksum doğrulanır; başarıdan sonra atomik replace uygulanır; başarısızlık durumunda eski save korunur; migration idempotency test edilmelidir. Backward migration zorunlu değildir.

## 14.6. Atomik kayıt ve backup

Save işlemi: geçici dosyaya yazma; flush ve kapatma; checksum doğrulama; mevcut save'in backup'ını alma; atomik replace veya mümkün olan en güvenli platform işlemi; son dosyayı tekrar doğrulama. En az iki dönen backup yönü dokümante edilmelidir.

Bozuk save: sessizce yüklenmez, geçerli save üzerine yazılmaz, mümkünse son backup önerilir, tanı raporu oluşturulabilir, kullanıcıya bozulmanın genel nedeni gösterilir.

---

# 15. Test Stratejisi Yönü

Ana .NET test framework'ü: **xUnit.net v3**.
Property/invariant test yönü: **FsCheck + xUnit entegrasyonu**.
Performans benchmark yönü: **BenchmarkDotNet**.

Godot presentation testleri mümkün olduğunca az sayıda tutulur; sahne açılışı, temel navigation, adapter wiring ve export smoke kapsamındadır. Core iş kuralları Godot test runner'ına taşınmaz.

## 15.1. Zorunlu test aileleri

* **Unit tests:** domain invariants, value calculations, rule evaluations, deterministic RNG wrapper, application validation.
* **Integration tests:** birden fazla sistemin tanımlı işlem sınırında birlikte çalışması, event dispatch sırası, persistence adapter entegrasyonu, content loading ve validation.
* **Simulation tests:** bir sezon, çok sezon, 10 sezon, binlerce maç, farklı seed'ler, işten çıkarılma ve kulüp değişimi akışları.
* **Property/invariant tests:** örnek invariant aileleri — var olmayan oyuncu referansı bulunmaması, aynı oyuncunun iki kulübün aktif kadrosunda olmaması, negatif veya geçersiz sözleşme süreleri oluşmaması, fikstür bütünlüğünün bozulmaması, save/load sonrasında canonical state'in korunması, olay sonuçlarının iki kez uygulanmaması. Kesin invariants ilgili sistem belgelerinde tanımlanacaktır.
* **Save/load round-trip tests:** state serialize, save, load, validate, canonical state hash karşılaştır.
* **Migration tests:** eski fixture save'leri saklanır; her schema sürümü sonraki sürüme taşınır; migration sonrası validation yapılır; migration başarısızlığında eski dosyanın korunduğu doğrulanır.
* **Determinism tests:** aynı seed, content, input, version ve RNG sürümü ile aynı canonical state hash üretilmelidir.
* **10-season soak tests:** memory büyümesi, referans bütünlüğü, performans, event/history hacmi, emeklilik ve yeni oyuncu üretimi, save/load, simülasyon tamamlanması kontrol edilir.
* **Binlerce maçlık denge testleri:** bu testler oyun tasarımı sonucunu otomatik olarak "doğru" ilan etmez; sonuç dağılımları, ev sahibi avantajı, gol dağılımı, favori kazanma oranları, sakatlık dağılımı, performans süreleri ve anormal uç değerleri raporlar.

---

# 16. Loglama, Profiling ve Debug

Logging abstraction: **Microsoft.Extensions.Logging**.
Runtime structured logging implementasyonu: **Serilog**.

Loglama yalnızca composition root veya infrastructure üzerinden bağlanır. Domain katmanı Serilog'a doğrudan bağımlı olmaz.

Log context'inde mümkün olduğunda şunlar bulunmalıdır: simulation run ID, career ID, root seed, subsystem RNG stream, game version, save schema version, content version, current simulation date, current checkpoint, event correlation ID.

Log çıktıları: geliştirmede console, dönen structured file log, gerektiğinde insan tarafından okunabilir özet. Harici cloud logging veya crash reporting zorunlu bağımlılık değildir.

## 16.1. Debug seed

Her simulation test ve hata raporu seed, input senaryosu, simulation version, content version ve başarısız checkpoint bilgilerini üretmelidir.

## 16.2. Simulation raporu

Headless simulation runner en az şu raporları üretebilmelidir: toplam süre, sezon süreleri, maç sayısı, event sayıları, save boyutu, memory göstergeleri, invariant failures, final canonical state hash, kullanılan seed. Kesin rapor formatı tooling geliştirme aşamasında belirlenir.

## 16.3. Profiling

Core profiling araç yönü: BenchmarkDotNet, `dotnet-trace`, `dotnet-counters`, ölçümlü simulation runner raporları.

Godot presentation profiling yönü: Godot profiler, scene/node sayısı, frame time, UI liste güncelleme maliyeti, allocation ve redraw davranışı.

---

# 17. Build ve CI Yönü

CI sistemi: **GitHub Actions**.

PR üzerinde zorunlu olacak yön: restore, build, unit tests, integration tests, determinism tests, save/load round-trip tests, content validation.

Her PR'da çalışması gerekmeyen ağır testler: tam 10 sezon soak, büyük benchmark setleri, binlerce seed dengesi, tam Windows export. Bunlar scheduled, nightly, manuel workflow veya release gate olarak çalıştırılabilir.

Build ve dağıtım yönü Bölüm 8'de (Hedef Platform ve Dağıtım) tanımlanmıştır.

---

# 18. Teknik Doğrulama Spike'ları

Aşağıdaki altı spike, üretim domain modeli ve proje scaffold'undan önce ayrı görevler olarak planlanmalıdır. Bu belgede spike kodu yazılmamıştır.

## Spike 1 — Motor bağımsız 10 sezon simülasyonu

Doğruladığı risk: simülasyon çekirdeğinin Godot olmadan çalışması, performans, uzun dönem memory ve state büyümesi.

Başarı kriteri: temsilî MVP dünyası veya yeterli sahte dünya ile 10 sezon tamamlanır; UI ve Godot süreci gerekmez; unhandled exception oluşmaz; invariant failure oluşmaz; süre ve memory raporu üretilir; geliştirici referans makinesinde başlangıç hedefi olarak 120 saniyenin altında kalır. Bu sayı nihai ürün SLA'sı değildir; teknik risk eşiğidir.

Başarısız olursa etkilenecek karar: simulation granularity, data structures, uzak dünya ayrıntı seviyesi. Motor seçimi doğrudan değişmez.

## Spike 2 — Deterministik tekrar üretim

Doğruladığı risk: aynı seed'in farklı sonuç üretmesi, RNG stream karışması, iteration order kaynaklı nondeterminism.

Başarı kriteri: aynı build ve input ile 10 bağımsız process çalıştırması aynı canonical final hash'i üretir; farklı seed'ler anlamlı biçimde farklı sonuç üretir; seed ve RNG sürümü raporda bulunur.

Başarısız olursa etkilenecek karar: RNG implementasyonu, collection iteration politikası, event ordering.

## Spike 3 — Save/load, migration ve bozulma testi

Doğruladığı risk: kayıt bütünlüğü, migration, atomik yazma, bozuk save davranışı.

Başarı kriteri: save/load round-trip aynı canonical state hash'i üretir; örnek V1 save V2'ye migrate edilir; checksum bozulması algılanır; başarısız migration eski save'i değiştirmez; backup'tan kurtarma doğrulanır.

Başarısız olursa etkilenecek karar: JSON container yapısı, persistence DTO sınırı, migration pipeline.

## Spike 4 — 500 futbolculuk Godot UI listesi

Doğruladığı risk: Godot Control ile yoğun veri ekranlarının ergonomisi ve performansı.

Başarı kriteri: yaklaşık 500 futbolcu gösterilebilir; sıralama ve filtreleme uygulanabilir; filtre sonucu kullanıcı etkileşiminde yaklaşık 100 ms içinde görünür; scroll sırasında belirgin takılma oluşmaz; idle durumda hedef 60 FPS korunur; node ve allocation davranışı raporlanır.

Başarısız olursa etkilenecek karar: custom virtualized list yaklaşımı, UI component tasarımı, son çare olarak presentation katmanının Avalonia'ya taşınması. Domain, Simulation ve Application kararları değişmez.

## Spike 5 — Windows export ve temiz makine testi

Doğruladığı risk: paketleme, runtime bağımlılıkları, path ve save erişimi.

Başarı kriteri: Windows x64 release export üretilir; Godot editor kurulu olmayan temiz Windows ortamında başlatılır; gerekli runtime prerequisite varsa açıkça belgelenir; save ve log klasörleri oluşturulur; temel açılış smoke testi geçer.

Başarısız olursa etkilenecek karar: export preset, runtime packaging, installer veya portable package yönü.

## Spike 6 — CI core test ve Godot headless doğrulaması

Doğruladığı risk: yerel makineye bağımlı build, headless export, presentation ve core testlerinin ayrılması.

Başarı kriteri: core build ve testler Godot kurulmadan çalışır; Godot smoke/export işi ayrı job olarak çalışır; CI ortamında pencere veya GPU zorunluluğu oluşmaz; test ve export raporları artifact olarak alınabilir.

Başarısız olursa etkilenecek karar: CI job ayrımı, Godot runner kurulumu, export otomasyonu.

---

# 19. Reddedilen Alternatifler

## .NET + Avalonia

İkinci en güçlü adaydır. Avantajları: C#/.NET uyumu, yoğun tablo ve form ekranları, MVVM, kolay test, masaüstü dağıtımı.

Reddedilme gerekçesi: oyun motoru değildir; gelecekteki gelişmiş 2D sunum daha pahalı olur; animasyon, ses, oyun hissi ve görsel maç katmanı için ek altyapı gerekir.

Godot UI spike'ı (Spike 4) başarısız olursa geri dönüş adayı olarak korunur.

## Unity + C#

Teknik olarak yeterlidir.

Reddedilme gerekçesi: kullanıcının Unity deneyimi temel düzeydedir; MVP için gereksiz editör ve motor ağırlığı oluşturur; kesin bir 3D hedefi yoktur; vendor ve lisans politikası riski Godot'tan yüksektir; tek geliştirici bakım maliyeti daha yüksektir.

## Godot + GDScript

Reddedilme gerekçesi: büyük ve uzun ömürlü domain modeli, save migration, refactoring güvenliği, test tooling'i ve kullanıcının ileri C# deneyimi nedeniyle ana dil olarak C# daha uygundur.

## React/TypeScript + Tauri

Reddedilme gerekçesi: React, Rust/Tauri ve C#/.NET arasında çoklu runtime sınırı oluşturur; IPC ve process lifecycle yönetimi gerekir; web veya mobil hedefi bulunmamaktadır; gelecekteki 2D maç sunumu için ek karmaşıklık oluşturur.

## MonoGame veya benzeri düşük seviyeli yaklaşım

Reddedilme gerekçesi: UI ve tooling altyapısının önemli bölümünün elle geliştirilmesi gerekir; MVP doğrulama süresini uzatır; Godot'un hazır UI, scene, audio ve görsel araçlarına göre daha yüksek mühendislik maliyeti vardır.

---

# 20. Riskler ve Azaltma Planları

| Risk | Etkilediği Alan | Azaltma Yaklaşımı |
| --- | --- | --- |
| Godot 4.7 .NET patch sürümünün beklenmedik uyumluluk sorunu çıkarması | Motor, build | Spike 1, 5 ve 6 sırasında erken doğrulama; patch sürümünün sabitlenmesi |
| Deterministik rastlantısallığın uzun kariyerde bozulması | Simulation, kayıt bütünlüğü | Spike 2; isimlendirilmiş RNG stream'leri; canonical state hash testleri |
| Kayıt/migration hatalarının kariyer kaybına yol açması | Persistence | Spike 3; atomik yazma; sıralı migration; backup stratejisi |
| Godot standart UI kontrollerinin ~500 futbolculuk listelerde yetersiz kalması | Presentation.Godot | Spike 4; virtualization/pooling; Avalonia'ya geri dönüş adayı |
| Windows dışı ortamlarda paketleme veya çalıştırma sorunları | Build, dağıtım | Spike 5; temiz makine testi; runtime prerequisite dokümantasyonu |
| Yerel makineye bağımlı, tekrarlanamayan CI sonuçları | CI | Spike 6; core/Godot job ayrımı; headless doğrulama |
| Domain/simulation katmanlarına yanlışlıkla Godot tipi sızması | Mimari bütünlük | Katman bağımlılık kuralı (Bölüm 9, 10); code review disiplini; ileride otomatik bağımlılık denetimi değerlendirilebilir |
| Event motorunun kontrolsüz genel mesajlaşmaya dönüşmesi | Olay/kural motoru | Bölüm 11'deki sınırlar; typed event sözleşmeleri; işlem sınırı koordinasyonu |
| Uzun dönem event/history verisinin sınırsız büyümesi | Performans, save boyutu | Event log'un audit/debug amaçlı sınırlı tutulması; retention politikasının ilgili sistem belgelerinde tanımlanması |
| Content authoring hacminin büyümesiyle manuel hata oranının artması | Content/Data | Şema/referans/aralık doğrulama pipeline'ı; gerekirse ileride özel editör değerlendirmesi |

Bu risk listesi kapanmış bir liste değildir; alt sistem tasarım belgeleri ve teknik spike'lar yeni riskler ortaya çıkarabilir.

---

# 21. Açık Kalan Teknik Sorular

Aşağıdaki sorular bu kararın teknoloji ailesini veya ana mimari yönünü değiştirmez; yalnızca uygulama ayrıntısı ve sürüm pinleme doğrulamasıdır. Bu sorularda sessiz varsayım yapılamaz.

1. Godot 4.7 patch sürümünün hangisinin pinleneceği.
2. Godot ile ortak kullanılacak kesin .NET target framework.
3. Godot liste UI'sinde virtualization/pooling implementasyon yönü.
4. `.fcsave` container'ın kesin iç şeması.
5. RNG algoritmasının kesin implementasyonu ve sürüm politikası.
6. CI soak testlerinin hedef makine ve süre bütçesi.
7. Windows installer'ın MVP içinde gerekli olup olmadığı.

---

# 22. Sonraki Adım

1. Bu belgenin commit edilmesi.
2. Ardından teknik spike'ların (Bölüm 18) sırayla ve ayrı görevler hâlinde planlanması.
3. Spike'lardan önce üretim domain modelinin yazılmaması.
