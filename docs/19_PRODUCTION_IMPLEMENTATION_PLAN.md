# Üretim Implementasyon Planı

**Belge:** `docs/19_PRODUCTION_IMPLEMENTATION_PLAN.md`
**Durum:** Kesinleşti (planlama düzeyinde) — bu belge hiçbir üretim kodu içermez
**Ana referans:** `docs/01_GAME_DESIGN_DOCUMENT.md`
**Doküman haritası:** `docs/00_PROJECT_INDEX.md`
**MVP kapsamı:** `docs/02_MVP_SCOPE.md`
**Domain sınırları:** `docs/03_DOMAIN_MODEL.md`
**Olay ve kural sözleşmeleri:** `docs/04_EVENT_RULE_ENGINE.md`
**Hafıza ve söz sözleşmeleri:** `docs/05_MEMORY_AND_PROMISE_SYSTEM.md`
**İlişki sözleşmeleri:** `docs/06_RELATIONSHIP_SYSTEM.md`
**Diyalog ve karar sözleşmeleri:** `docs/07_DIALOGUE_SYSTEM.md`
**Dünya simülasyonu ve zaman akışı sözleşmeleri:** `docs/12_WORLD_SIMULATION.md`
**Kayıt sözleşmeleri:** `docs/13_SAVE_SYSTEM.md`
**Test sözleşmeleri:** `docs/14_TEST_STRATEGY.md`
**Teknik mimari:** `docs/17_TECHNOLOGY_AND_ARCHITECTURE_DECISION.md`
**Tamamlanan teknik spike kanıtları:** `docs/18_SPIKE_EXECUTION_PLAN.md`
**İlgili karar günlüğü:** `docs/15_DECISION_LOG.md`

---

## Notasyon Notu

Bu görev tanımında referans verilen `docs/04_EVENT_AND_RULE_ENGINE.md` ve `docs/07_DIALOGUE_AND_DECISION_SYSTEM.md` dosya adları repository'de bu tam adlarla mevcut değildir. Bu belge, aynı içeriğe karşılık gelen gerçek dosyaları kullanır: `docs/04_EVENT_RULE_ENGINE.md` ve `docs/07_DIALOGUE_SYSTEM.md`. Bu, bir tutarsızlık değildir; yalnızca isimlendirme farkıdır.

---

## 1. Belgenin Amacı

### 1.1. Spike aşaması neden sona erdi?

`docs/18_SPIKE_EXECUTION_PLAN.md`'deki Kart 0–8'in tamamı ve `docs/17_TECHNOLOGY_AND_ARCHITECTURE_DECISION.md` Bölüm 16'daki altı teknik doğrulama spike'ının (motor bağımsız headless simülasyon, determinizm, SQLite save/load/migration, 500 futbolculuk Godot UI, Windows x64 export, CI'da saf .NET + Godot headless doğrulaması) tamamı somut kanıtla kapatılmıştır (`docs/15_DECISION_LOG.md` D-331–D-340). Spike aşamasının amacı — teknoloji seçiminin (Godot 4 .NET, C#, SQLite, Windows x64) gerçekten çalışabilir olduğunu kanıtlamak — tamamlanmıştır. Bu spike'lar hiçbir noktada gerçek oyun domain modelini (14 bounded context) implemente etmeyi hedeflememiştir; yalnızca mimari risklerin teknik olarak doğrulanmasını hedeflemiştir.

### 1.2. Üretim implementasyonu neden ayrı bir aşamadır?

Spike kodu (`Spike1Placeholder`, `Spike4Placeholder` ve isimsiz yer tutucular) kasıtlı olarak gerçek domain modelini temsil etmeyecek şekilde tasarlanmıştır (`docs/18_SPIKE_EXECUTION_PLAN.md` Kart 2, 6 "Önemli sınırlama" notları). `docs/03_DOMAIN_MODEL.md` Bölüm 5 ve `docs/12_WORLD_SIMULATION.md` Bölüm 4.3'te tanımlanan 14 bounded context'in gerçek implementasyonu; spike'ların doğruladığı teknik riskten tamamen ayrı bir tasarım ve mühendislik çalışmasıdır. Bu iki aşamayı aynı kart setinde birleştirmek `01_GAME_DESIGN_DOCUMENT.md` Kural 3'ü (paralel çok sistem açmama) ihlal eder ve teknik doğrulamayla domain doğruluğunu karıştırma riski taşır.

### 1.3. Bu belge hangi kararları verir?

* İlk üretim dikey kesitinin hangi bounded context olacağını (mevcut belgelere dayanarak, Bölüm 4).
* Bu ilk dikey kesitin kapsamını, veri modelini, olaylarını, sınır durumlarını ve test matrisini kavramsal düzeyde (Bölüm 5).
* Spike kodunun her parçası için geçiş stratejisini (Bölüm 6).
* Üretim implementasyonunun küçük, geri alınabilir çalışma kartlarına bölünmesini (Bölüm 7).
* İlk üç kart için dosya/klasör etki alanını (Bölüm 8, kavramsal düzeyde — dosyalar bu görevde oluşturulmaz).
* **(Production Kart 0 kapsamında, `docs/15_DECISION_LOG.md` D-342–D-351 ile)** World & Calendar terminoloji kilidi (D-342), proleptic Gregorian `GameDate`/`DayNumber` takvim modeli (D-343), günlük granularity ve same-day ordering (D-344, D-345), `net10.0` Target Framework kararı (D-346) ve manuel composition root/third-party container kullanılmaması kararları (D-348, D-349). **Exact .NET SDK sürümü kanıt yetersizliği nedeniyle kapatılmamıştır** (D-347 — Açık); bu tek madde Kart 0'ı "Bloke" durumunda bırakır (D-351, Bölüm 7).

### 1.4. Bu belge hangi kararları vermez?

* Kesin C# sınıf, interface, enum veya record tanımları.
* Kesin veritabanı tablo şeması, migration script'i veya persistence provider implementasyonu.
* Kesin sayısal formüller, olasılık katsayıları, cooldown süreleri veya performans eşikleri.
* `docs/03_DOMAIN_MODEL.md`, `docs/12_WORLD_SIMULATION.md`, `docs/13_SAVE_SYSTEM.md` veya `docs/17_TECHNOLOGY_AND_ARCHITECTURE_DECISION.md` içinde açık bırakılmış herhangi bir kararı sessizce kapatmaz (bkz. Bölüm 3).
* İkinci veya sonraki bounded context'in kesin implementasyon sırasını (yalnızca genel önceliklendirme yönü verir; World & Calendar sonrası kesin sıralama ayrı bir çalışmadır).

### 1.5. Bu belge neden geniş bir scaffold veya toplu kod üretimi başlatmaz?

`01_GAME_DESIGN_DOCUMENT.md` Kural 1, Kural 3 ve Kural 9; `docs/15_DECISION_LOG.md` D-010 (küçük, geri alınabilir kilometre taşları); ve bu görevin doğrudan talimatı ("Bu görevde üretim kodu yazma") birlikte şunu gerektirir: implementasyon, her biri tek bir PR içinde anlaşılabilir ve geri alınabilir küçük çalışma kartlarına bölünmelidir (Bölüm 7). Geniş bir scaffold, hem hangi kararların hâlâ açık olduğunu (Bölüm 3) gizler hem de yanlış tasarlanmış bir temelin yeniden yazım maliyetini artırır (Bölüm 9). Bu belge bu riski kabul etmez.

---

## 2. Mevcut Durum Özeti

Bu özet, `git log`, gerçek dosya sistemi içeriği ve `docs/15_DECISION_LOG.md` D-331–D-340 kayıtlarına dayanır.

### 2.1. Tamamlanan spike'lar (`docs/18_SPIKE_EXECUTION_PLAN.md`)

| Kart | Spike | Kanıt |
|---|---|---|
| 0 | Minimum repository iskeleti | `FootballCareerSimulator.slnx`; Domain/Simulation/Application/Tests projeleri; 3/3 test |
| 1 | CI-lite (saf .NET) | `.github/workflows/ci.yml` `dotnet` job'ı |
| 2 | Spike 1 — Motor bağımsız 10 sezonluk headless simulation | `tools/FootballCareerSimulator.SimulationRunner`; 20 kulüp/500 futbolcu, 1 ms'de 10 sezon |
| 3 | Spike 2 — Deterministik sonuç ve seed doğrulaması | `CanonicalStateHasher`, `WorldSnapshotSerializer`, `SimulationCheckpointResumer`; kesintili/kesintisiz koşu birebir aynı hash |
| 4 | Spike 3 — SQLite save/load, migration ve corruption davranışı | `FootballCareerSimulator.Infrastructure`; atomik yazma, backup+swap migration, hash doğrulamalı bozulma tespiti |
| 5 | Minimum Godot proje kabuğu | Godot 4.7-stable (mono/.NET), `src/FootballCareerSimulator.Presentation` |
| 6 | Spike 4 — 500 futbolculuk Godot UI listesi | `PlayerListScreen`, gerçek Vulkan GPU'da p95 ~8,3 ms |
| 7 | Spike 5 — Windows x64 export ve temiz ortam çalıştırma | Self-contained `.exe`; Godot/dotnet olmayan izole ortamda hatasız çalıştı; Godot **4.7-stable** pinlendi (D-339) |
| 8 | CI Tamamlama (Godot headless) | `godot-headless` CI job'ı; canlı çalıştırmada `SPIKE5_SMOKE_TEST_RESULT=PASS` kanıtı |

### 2.2. Doğrulanan teknik riskler

* Saf .NET domain/simulation çekirdeği Godot olmadan, ~20 kulüp/~500 futbolcu ölçeğinde headless çalışabiliyor.
* Aynı seed + aynı komut dizisi, süreçler arası ve (kesintili/kesintisiz) senaryolar arası aynı canonical semantic sonucu üretiyor.
* SQLite tabanlı save/load, atomik yazma, backup+swap migration ve bütünlük hash doğrulamasıyla güvenle çalışabiliyor; bozulmuş veya kurcalanmış save reddediliyor.
* Godot `Control`/`Tree` tabanlı UI, gerçek GPU'da 500 kayıtlık listede performans hedeflerinin (p95 < 33 ms) çok altında kalıyor.
* Self-contained Windows x64 export, Godot editörü ve .NET SDK'sı olmayan bir ortamda çalışabiliyor.
* Bütün bu doğrulamalar artık CI'da her push'ta otomatik olarak tekrar kanıtlanıyor (saf .NET job'ı + `godot-headless` job'ı).

### 2.3. Çalışan Godot UI prototipi

`src/FootballCareerSimulator.Presentation/PlayerListScreen.tscn`, ana sahne olarak açılan, 500 yer tutucu futbolcu kaydını filtreleyen/sıralayan/sayfalayan ve kendi içine gömülü bir öz-kontrol (`SPIKE5_SMOKE_TEST_RESULT`) çalıştıran bir ekrandır. Bu, **gerçek bir oyun ekranı değildir**; gerçek futbolcu verisi, gerçek isimlendirme veya gerçek UI/UX tasarımı içermez.

### 2.4. Windows export

`export_presets.cfg` ile "Windows Desktop x86_64" preset'i mevcuttur ve hem yerel makinede hem CI'da (`godot-headless` job'ı) doğrulanmıştır. Bu export akışı üretim sürümünün paketleme stratejisi için bir **başlangıç noktasıdır**, nihai paketleme kararı değildir (`docs/17_TECHNOLOGY_AND_ARCHITECTURE_DECISION.md` Bölüm 7'deki installer/code signing kararları hâlâ açıktır).

### 2.5. CI pipeline

`.github/workflows/ci.yml` iki job içerir: `dotnet` (saf .NET restore/build/test) ve `godot-headless` (Godot editör/export şablonu indirme, headless import, Windows x64 export, export doğrulama, smoke test). Her ikisi de her push/PR'da otomatik çalışır ve artefakt üretir.

### 2.6. Test durumu

36/36 test yeşil (Debug ve Release). Testlerin tamamı spike kod tabanına aittir; **gerçek domain modeline ait hiçbir test henüz mevcut değildir.**

### 2.7. Placeholder alanları

| Alan | İçerik |
|---|---|
| `src/FootballCareerSimulator.Domain/SimulationStep.cs` | Kart 0 yer tutucusu (katman bağlantısı kanıtı) |
| `src/FootballCareerSimulator.Domain/Spike1Placeholder/` | `World`, `Club`, `Player`, `ClubId`, `PlayerId`, `WorldSnapshot` — 20 kulüp/500 futbolcu ölçeğini simüle eden kurgusal veri modeli |
| `src/FootballCareerSimulator.Simulation/PlaceholderWorldLoop.cs` | Kart 0 yer tutucusu |
| `src/FootballCareerSimulator.Simulation/SimulationRandomContext.cs` | **Placeholder değildir** — D-058'in gerçek, seeded/versioned Random Context implementasyonu |
| `src/FootballCareerSimulator.Simulation/Spike1Placeholder/` | `WorldFactory`, `SeasonAdvancer`, `WorldInvariantChecker`, `HeadlessSimulationRunner`, `CanonicalStateHasher`, `WorldSnapshotSerializer`, `SimulationCheckpointResumer`, `SimulationRunReport` |
| `src/FootballCareerSimulator.Application/AdvancePlaceholderSimulationUseCase.cs` | Kart 0 yer tutucusu |
| `src/FootballCareerSimulator.Application/Spike4Placeholder/` | `PlayerListRow`, `PlayerListQuery`, `PlayerListSortColumn` |
| `src/FootballCareerSimulator.Infrastructure/*.cs` (namespace önekisiz) | `SqliteSaveWriter`, `SqliteSaveReader`, `SqliteSaveMigrator`, `SqliteRowReader`, `SqliteSaveSchema`, `SqliteLoadResult`, `SaveIntegrityExceptions` — Spike 3'e özel geçici SQLite şeması |
| `src/FootballCareerSimulator.Presentation/Shell.cs`, `PlayerListScreen.cs` | Kart 5/6 yer tutucu ekranları |
| `tools/FootballCareerSimulator.SimulationRunner/Program.cs` | Spike 1 headless çalıştırma aracı |
| `tests/FootballCareerSimulator.Tests/*.cs` | Tüm testler yukarıdaki placeholder'ları hedefler |

### 2.8. Henüz başlamamış gerçek domain implementasyonu

`docs/03_DOMAIN_MODEL.md` Bölüm 5'teki 14 bounded context'ten (World & Calendar, Competition, Club & Governance, Player Career, Manager Career & Employment, Contract & Registration, Team Preparation, Training & Physical State, Match, Transfer, Social Continuity, Interaction & Narrative, Event & Rule Evaluation, Save Integrity) **hiçbiri henüz üretim kodu olarak implemente edilmemiştir.**

### 2.9. "Çalışan prototip" ile "üretim oyun sistemi" arasındaki fark

| Boyut | Çalışan prototip (mevcut durum) | Üretim oyun sistemi (hedef) |
|---|---|---|
| Veri modeli | Kurgusal, sabit sayıda (20 kulüp/500 futbolcu), gerçek isim/kimlik yok | `docs/03_DOMAIN_MODEL.md`'deki gerçek aggregate/entity/value object modeli |
| İş kuralları | Yok (yaş +1 gibi kurgusal işlemler dışında) | GDD/MVP/alt sistem belgelerindeki gerçek domain invariant'ları |
| Olay üretimi | Yok (Spike 2'nin RNG akışı hariç gerçek Domain Event yok) | `docs/04_EVENT_RULE_ENGINE.md` sözleşmesine uygun gerçek Domain/Integration Event'ler |
| Bounded context sınırları | Yok — tek bir yer tutucu "World" sınıfı her şeyi temsil ediyor | 14 ayrı authoritative owner context |
| Save şeması | Spike'a özel, iki sürümlü, geçici (`SaveManifest`/`Clubs`/`Players`) | `docs/13_SAVE_SYSTEM.md` gereksinimlerini karşılayan gerçek şema (hâlâ açık, bkz. Bölüm 3) |
| UI | 500 kurgusal satırlık performans kanıtı ekranı | Gerçek oyun ekranları (haftalık kontrol merkezi, kadro, taktik, vb.) |
| Amaç | Teknoloji/mimarinin çalıştığını kanıtlamak | Oyunu oynanabilir kılmak |

Bu ayrım bu belgenin temelidir: **mevcut kod tabanının hiçbir parçası, isim değiştirilerek veya genişletilerek üretim koduna "yükseltilemez"** (Bölüm 6).

---

## 3. Açık Kararlar ve Teknik Borçlar

Aşağıdaki tablo, mevcut belgelerde açıkça "açık" bırakılmış veya spike kapsamı dışında tutulmuş kararları listeler. Bu belge bunlardan hiçbirini sessizce kapatmaz.

| # | Konu | Hangi karttan önce çözülmeli | Çözülmezse risk | Mümkün seçenekler | Önerilen karar zamanı |
|---|---|---|---|---|---|
| 1 | **Deterministik RNG stream stratejisi** — kesin PRNG algoritması, stream/sequence bölme yaklaşımı (`docs/12_WORLD_SIMULATION.md` Bölüm 38, `docs/17_TECHNOLOGY_AND_ARCHITECTURE_DECISION.md` Bölüm 9.1) | Production Kart 4 (Deterministic Simulation entegrasyonu) | Farklı sistemlerin random tüketimi birbirini kontrolsüz etkiler; save/load sonrası determinizm bozulabilir | (a) Spike 2'nin "seed'den replay" tekniğini resmi stream stratejisi yapmak, (b) sistem/context bazlı ayrı RNG stream'leri (splittable PRNG), (c) tek global stream + sıra disiplini | Kart 4 başlamadan önce, küçük bir teknik not olarak (bu belgenin kapsamı dışında, ayrı bir kısa spike/karar notu) |
| 2 | **Kalıcı save schema** — exact SQLite tablo/index şeması, canonical serialization biçimi (`docs/13_SAVE_SYSTEM.md` Bölüm 44, `docs/15_DECISION_LOG.md` D-284) | Production Kart 5 (Persistence entegrasyonu) | Spike 3'ün geçici şeması yanlışlıkla kalıcı şema sayılabilir; erken donmuş şema pahalı migration gerektirir | (a) Kart 5'te World & Calendar'a özel minimal şema tasarlamak ve genişlemeye açık bırakmak, (b) tüm 14 context için önce kavramsal şema taslağı çıkarmak | Kart 5 başlamadan hemen önce, yalnızca World & Calendar kapsamı için |
| 3 | **Target Framework (`net10.0`) — Kart 0'da kapatıldı (D-346).** Exact .NET SDK pin'i — **Açık kalmıştır (D-347), kanıt yetersiz.** `net10.0`, bir Target Framework Moniker'dır ve dokuz çalışma kartının (D-331–D-340) tamamında sorunsuz doğrulanmıştır — bu kapanmıştır. Ancak exact SDK sürümü (`10.0.xxx`) hiçbir spike kaydında sabitlenmemiştir; yerel makinede eşzamanlı olarak `10.0.300` ve `10.0.301` kurulu bulunmuş, `dotnet --version` ortama göre değişebilecek şekilde en yeniyi (`10.0.301`) göstermiştir, ve CI (`dotnet-version: "10.0.x"`) sabit bir exact sürüm değil, kayan bir feature-band joker karakteri kullanır. Bu nedenle exact SDK pin kararı kanıt yetersizliği nedeniyle **Açık** bırakılmıştır; sessizce varsayılmamıştır. | **TFM: kapandı. Exact SDK: Production Kart 1'in ön koşulu (ayrı küçük bir konfigürasyon kartıyla).** | TFM kapanmazsa spike'lar boyunca biriken örtük bağımlılık belgesiz kalır (bu risk artık yok); exact SDK kapanmazsa `global.json` yazılamaz ve derleme ortamları arasında sessiz kayma riski sürer | (a) Bir sonraki iyi bilinen CI çalıştırmasının tam SDK sürümünü loglardan çıkarıp exact pinlemek, (b) yerel geliştirme makinesinde tek bir SDK sürümüne indirmek ve o sürümü kanıt olarak kaydetmek | Exact SDK: Kart 1 başlamadan önce, ayrı bir küçük konfigürasyon kartı olarak (bu görevin ve bu belgenin kapsamı dışında — `global.json` burada oluşturulmaz) |
| 4 | **Persistence provider pinlemesi** — `Microsoft.Data.Sqlite` sürümü ve `SQLitePCLRaw.lib.e_sqlite3` güvenlik pin'i (D-335) resmi olarak "üretim kararı" değil, spike kararı olarak kayıtlı | Production Kart 5 | Güvenlik pin'i zamanla eskiyebilir; sürüm sessizce üretime taşınabilir | (a) Aynı sürümleri üretim için de resmen benimsemek ve bir bağımlılık güncelleme politikası eklemek, (b) provider'ı yeniden değerlendirmek | Kart 5 başlamadan önce |
| 5 | **Placeholder kodunun kaldırılma stratejisi** — hangi sırayla, hangi güvenlik ağıyla | Production Kart 2 tamamlanmadan (Bölüm 6) | Placeholder ve gerçek kod aynı isim alanında çakışabilir; build kararsız hâle gelebilir | Bölüm 6'daki sınıflandırma ve Bölüm 8'deki "dokunulmayacak dosyalar" listesi | Bu belgede kesinleştirildi (Bölüm 6) |
| 6 | ~~**Composition root ve dependency injection yaklaşımı**~~ — **Bu madde Kart 0 kapsamında tam kapatıldı (D-348, D-349); Kart 0'ın GENEL durumu ise madde 3'teki exact SDK boşluğu nedeniyle Bloke'dur (bkz. Bölüm 7).** Manuel composition root ve constructor injection kullanılacaktır; başlangıçta third-party DI container (Microsoft.Extensions.DependencyInjection dahil) kullanılmayacaktır. Her executable host (Godot Presentation host, saf .NET headless simulation runner, test host/factory) kendi composition root'una sahip olacaktır. Bu artık bir mimari seçim SORUSU değildir; yalnızca fiziksel implementasyonu (exact registration kodu) Production Kart 3'e aittir. | *(Kapatıldı)* | *(Kapatıldı)* | *(Kapatıldı — üç seçenekten (a) reddedildi, (b) kabul edildi, (c) reddedildi)* | **Bu madde: kapatıldı.** Fiziksel implementasyon: Kart 3 |
| 7 | **Gerçek read model sınırları** — Presentation'ın hangi query/read model tiplerini göreceği yalnızca kavramsal olarak (`docs/03_DOMAIN_MODEL.md` Bölüm 15.3) tanımlı | Production Kart 3 | Read model'lerin yanlışlıkla domain/aggregate nesnelerinin kendisi olması riski | Bölüm 5.8'deki contract sınırı yönü; kesin şekil Kart 3'te tasarlanır | Kart 3 sırasında |
| 8 | **Domain event persistence gereksinimleri** — hangi event'lerin snapshot'a, hangilerinin yalnızca seçici history'ye gireceği (`docs/04_EVENT_RULE_ENGINE.md` Bölüm 18, `docs/13_SAVE_SYSTEM.md` Bölüm 29) | Production Kart 5 | Event log'un kontrolsüz büyümesi veya gerekli audit bilgisinin kaybolması | Bölüm 5.9'daki yön; kesin retention D-072/D-284 ile birlikte açık | Kart 5 sırasında, D-072/D-284 ile eş zamanlı |
| 9 | **Save sürüm geçiş politikası** — Spike 3'ün V1→V2 migration'ı üretim şemasına doğrudan taşınamaz (`docs/13_SAVE_SYSTEM.md` Bölüm 25) | Production Kart 5 | Spike migration deseni yanlışlıkla "üretim migration stratejisi" sayılabilir | Spike'ın "backup + çalışma kopyası + atomik swap" TEKNİĞİ yeniden kullanılabilir; ŞEMA yeniden kullanılamaz (bkz. Bölüm 6) | Kart 5 sırasında |
| 10 | **Bounded context implementasyon sırası (World & Calendar sonrası)** — bu belge yalnızca 1. sırayı doğrular | Production Kart 6 tamamlandıktan sonra | Erken ve sessiz bir "2. sıra" kararı, gerçek bağımlılık analizini atlayabilir | Bölüm 4'teki matris ikinci bir planlama çalışmasının girdisi olabilir | Kart 6 tamamlandıktan sonra, ayrı bir planlama görevi olarak |

**Güncelleme (Production Kart 0 sonrası, bkz. `docs/15_DECISION_LOG.md` D-342–D-351):** Madde 3'ün TFM alt maddesi ve madde 6'nın tamamı kapatılmıştır. Madde 3'ün exact SDK alt maddesi **kanıt yetersizliği nedeniyle Açık bırakılmıştır** — bu, Production Kart 0'ın genel durumunu **"Bloke — exact .NET SDK pin kanıtı eksik"** yapar (bkz. Bölüm 7 ve Bölüm 10). Kalan açık maddeler (1, 2, 3'ün exact SDK alt maddesi, 4, 5 [zaten bu belgede kesinleşmişti], 7, 8, 9, 10) hâlâ açıktır ve kendi ilgili kartında veya kartından önce ayrıca ele alınmalıdır. Ayrıca bkz. Bölüm 5.7 (takvim modeli — Kart 0'da tam kapatıldı, D-343/D-344/D-345).

**Bilinçli olarak kapatılmayan ek kararlar (D-350 ile teyit edildi):** Kart 0, yukarıdakilerin dışında şunları da KAPATMAZ: exact RNG algoritması, named RNG stream listesi, üretim SQLite şeması, migration formatı, exact SQLite provider/paket sürümü, exact season başlangıç tarihi, fixture takvim tarihleri, exact namespace/klasör yapısı, exact command/event sınıf listesi, (yeniden değerlendirilirse) DI container paketi ve persistence repository interface listesi. Bunlar ilgili kartlarında (Kart 1-6) veya ayrı planlama çalışmalarında ele alınacaktır.

---

## 4. Bounded Context Implementasyon Sırası

### 4.1. 14 Bounded Context Matrisi

Kaynak: `docs/03_DOMAIN_MODEL.md` Bölüm 5, 7, 11.

| Context | Temel sorumluluk | Sahip olduğu veriler | Bağımlı olduğu sistemler | Ona bağımlı sistemler | Zaman sistemine bağımlılığı | Persistence gereksinimi | MVP önceliği |
|---|---|---|---|---|---|---|---|
| **World & Calendar** | Oyun tarihi, planlama dönemleri, simulation ordering | GameDate, PlanningPeriod, root seed, RNG state | *(yok — bağımsız)* | **Tümü** | Kendisi zaman kaynağıdır | Kritik (Bölüm 7.1) | Kesinleşmiş, ilk dikey kesit |
| Competition | Season, fixture, standings | Season, Fixture, accepted result, standings | World & Calendar, Match | Match, Manager, Club, Team Prep | Yüksek | Kritik | Kesinleşmiş, ilk dikey kesit |
| Club & Governance | Kulüp kimliği, politika, bütçe sınırı | Club profile, budget limits, policies | Competition, Match, Transfer, Manager | Transfer, Contract, Manager, Interaction | Orta | Yüksek | Kesinleşmiş, ilk dikey kesit |
| Player Career | Futbolcu kalıcı kimliği, gelişim, emeklilik | Player profile, development, retirement | Training, Match, Competition, World | Contract, Team Prep, Match, Transfer, Social | Orta | Yüksek | Kesinleşmiş, ilk dikey kesit |
| Manager Career & Employment | Manager kariyeri, employment, board trust | Manager career, employment, offers, board trust | Competition, Match, Club, Interaction, Social | Club, Team Prep, Transfer, Interaction, Social | Orta | Yüksek | Kesinleşmiş, ilk dikey kesit |
| Contract & Registration | Player-Club hukuki bağlılık | Contract, registration, active club | Transfer, World, Player Career | Player, Team Prep, Transfer, Club | Orta | Kritik | Kesinleşmiş, ilk dikey kesit |
| Team Preparation | Squad, match selection, tactic plan | Squad membership, selection, tactic plan | Contract, Training, Competition, Manager, World | Match, Social, Interaction | Orta | Yüksek | Kesinleşmiş, ilk dikey kesit |
| Training & Physical State | Antrenman, fatigue, fitness, injury | Training plan, physical state | World, Match, Team Prep | Team Prep, Match, Player Career | Orta | Yüksek | Kesinleşmiş, ilk dikey kesit |
| Match | Tek maçın çalışma state'i ve sonucu | Match state, timeline, result | Competition, Team Prep, Training, World | Competition, Training, Player, Social, Manager | Yüksek | Kritik (10 sezon/binlerce maç) | Kesinleşmiş, ilk dikey kesit |
| Transfer | Transfer ihtiyacı, teklif, müzakere | TransferProcess | World, Club, Manager, Social, Contract | Contract, Club, Team Prep, Player, Social | Orta | Yüksek | Kesinleşmiş, ilk dikey kesit (sınırlı) |
| Social Continuity | Relationship, Memory, Promise | Relationship, memory, promise | Match, Team Prep, Interaction, Transfer, Manager | Interaction, Transfer, Manager, Player, Event Eval | Düşük–Orta | Yüksek (uzun dönem büyüme riski) | Kesinleşmiş, ilk dikey kesit (sınırlı) |
| Interaction & Narrative | Görüşme, DecisionRequest, public narrative | Interaction, decision request, narrative | Social, Match, Manager, Transfer, Club | Social, Manager, Transfer, Event Eval | Düşük | Orta | Kesinleşmiş, ilk dikey kesit |
| Event & Rule Evaluation | Event değerlendirme, causation, idempotency | Event metadata, rule evaluation ledger | **Tümü (event üreten context'ler)** | Application (tüm context'lere consequence command) | Yüksek (her simulation step'te çalışır) | Kritik ama seçici | Kesinleşmiş, ertelenemez fakat *ölçek olarak* minimal başlar |
| Save Integrity | Snapshot, schema version, migration, bütünlük | Save manifest, migration/integrity metadata | **Tümü (rehydrate edilen context'ler)** | Application, tüm context'ler (load sonrası) | Yüksek (checkpoint ile eşleşir) | Kritik ama ertelenemez | Kesinleşmiş, ertelenemez fakat *ölçek olarak* minimal başlar |

### 4.2. Sıralama Kriterleri

Sıralama yalnızca kullanıcıya görünen özelliklere göre yapılmamıştır. Kullanılan kriterler:

1. **Diğer sistemlerin temel bağımlılığı olma** — bir context'in kaç başka context tarafından okunduğu/beklendiği (`docs/03_DOMAIN_MODEL.md` Bölüm 11 Veri Sahipliği Matrisi).
2. **Deterministik simülasyon ihtiyacı** — context'in doğru çalışması için deterministik zaman/seed altyapısının önceden var olması gerekip gerekmediği.
3. **Kayıt bütünlüğü** — context'in save/load'a erken dahil edilmesi gerekip gerekmediği (`docs/13_SAVE_SYSTEM.md` Bölüm 21 Rehydration Sıralaması).
4. **Olay üretme yoğunluğu** — context'in event akışının diğer context'ler için ne kadar merkezi olduğu.
5. **Test edilebilirlik** — context'in diğer context'ler olmadan izole test edilip edilemeyeceği.
6. **MVP gerekliliği** — `docs/02_MVP_SCOPE.md` Bölüm 17/20/22'deki kesin MVP kapsamına dahil olup olmadığı.
7. **Yanlış tasarlanırsa yeniden yazım maliyeti** — context'in temelini oluşturan kavramların (zaman, kimlik) sonradan değiştirilmesinin diğer bütün context'leri etkileyip etkilemeyeceği.

### 4.3. World & Calendar Neden İlk Sıradadır

Üç bağımsız kaynak, birbirini doğrulayan biçimde `World & Calendar`'ı ilk sıraya yerleştirir:

1. **`docs/03_DOMAIN_MODEL.md` Bölüm 23** ("MVP ve İlk Dikey Kesit Ayrımı"), ilk dikey kesitte uygulanması gereken aggregate/lifecycle alt kümesini listelerken `WorldTimeline`'ı **listenin ilk öğesi** olarak verir.
2. **`docs/12_WORLD_SIMULATION.md`** bütünüyle bu context'in ayrıntılı sözleşmesidir ve Bölüm 34.1'de ilk dikey kesitin "gerçek GameDate ilerlemesi, gün çözünürlüğü, Planning Period" içermesi gerektiğini açıkça belirtir.
3. **`docs/13_SAVE_SYSTEM.md` Bölüm 21** (Rehydration Sıralaması), World & Calendar'ı, manifest/version metadata ve stable identity katalogları hemen ardından, **bütün diğer 12 business context'ten önce** (3. sırada) rehydrate edilmesi gereken context olarak listeler.

Bunun ötesinde, Bölüm 4.2'deki kriterlere göre değerlendirme:

* **Temel bağımlılık:** `docs/03_DOMAIN_MODEL.md` Bölüm 11'deki Veri Sahipliği Matrisi'nde "Oyun tarihi" satırının "Okuyabilen context'ler" sütunu **"Tümü"** değerini taşır — bu, matristeki tek bu değere sahip satırdır. `docs/12_WORLD_SIMULATION.md` Bölüm 7.1'deki "Etkilediği sistemler" alanı da **"Tüm zaman bağımlı context'ler"** der. Ayrıca `docs/05_MEMORY_AND_PROMISE_SYSTEM.md` ve `docs/07_DIALOGUE_SYSTEM.md`'de "oyun zamanı" kavramı (duvar saatinden ayrı) deadline ve zaman damgası olarak tekrarlanan biçimde kullanılır — bu context'lerin World & Calendar'ı bounded-context adıyla anmaması, ona bağımlı olmadığı anlamına gelmez.
* **Deterministik simülasyon ihtiyacı:** Root seed ve RNG version World & Calendar'ın authoritative verisidir (`docs/12_WORLD_SIMULATION.md` Bölüm 21); hiçbir başka context kendi determinizmini bu olmadan sağlayamaz.
* **Kayıt bütünlüğü:** Yukarıdaki rehydration sıralaması kanıtı.
* **Test edilebilirlik:** World & Calendar, `docs/03_DOMAIN_MODEL.md` Bölüm 7.1 "Sahip olmadığı veriler" listesine göre Season, Fixture, Contract, Squad gibi HİÇBİR başka context verisine ihtiyaç duymaz; bu onu **diğer 13 context'ten hiçbiri var olmadan bağımsız olarak inşa edilebilen ve test edilebilen tek context** yapar (bkz. Spike 1'in zaten bunu 20 kulüp/500 futbolcu ölçeğinde headless kanıtlamış olması, D-333).
* **MVP gerekliliği:** `docs/02_MVP_SCOPE.md` Bölüm 20 (İlk Dikey Kesitin Kesin Sınırı) "tek sezon" ve "tek maçlı standart planlama dönemleri" gerektirir; bunların hiçbiri World & Calendar'ın gün çözünürlüklü zaman ilerletmesi olmadan anlamlı değildir.
* **Yeniden yazım maliyeti:** GameDate ve Simulation Step kavramları, `docs/04_EVENT_RULE_ENGINE.md` Bölüm 6.1'deki **her** event'in zorunlu `OccurredAtGameTime` ve `SimulationStepId` alanlarının temelidir. Bu kavramlar yanlış tasarlanırsa, üzerine yazılan her event ve her context'in event üretimi yeniden yazılmak zorunda kalır — bu, en yüksek yeniden yazım maliyetine sahip context'i World & Calendar yapar ve onu erken doğru kurmanın önemini artırır.

**Sonuç:** Mevcut dokümanlar arasında bu konuda gerçek bir çelişki tespit edilmemiştir. `World & Calendar`, ilk üretim dikey kesiti olarak doğrulanmıştır ve Bölüm 5'te ayrıntılandırılmıştır.

**Kart 0 kapanış notu:** Bu konumlandırma kararı (D-341) ve Bölüm 5.1'de kullanılan terminoloji kümesi, Production Kart 0 kapsamında `docs/15_DECISION_LOG.md` D-342 ile resmi olarak kilitlenmiştir. Kilitleme, terimlerin anlamını DEĞİŞTİRMEZ; yalnızca Production Kart 1'den itibaren bu terimlerin tutarlı ve bağlayıcı biçimde kullanılacağını teyit eder.

### 4.4. Event & Rule Evaluation ve Save Integrity Notu

Bu iki context "Tümü" tarafından kullanılsa da, `docs/03_DOMAIN_MODEL.md` Bölüm 7.13/7.14 ve `docs/12_WORLD_SIMULATION.md` Bölüm 4.3'e göre bunlar **yeni bir on beşinci context değildir** ve kendi başlarına zengin bir business domain'e sahip değildir — büyük ölçüde Application/Infrastructure orkestrasyon sorumluluğudur. Bu nedenle bu ikisi "sıra 1" veya "sıra 2" olarak ayrı bir kart gerektirmez; World & Calendar'ın kendi olay üretimi ve save/load ihtiyacını karşılayacak **minimal bir kesit** olarak Production Kart 2, 4 ve 5 içinde birlikte büyütülürler (bkz. Bölüm 7). Bu, Bölüm 3 madde 8 ile tutarlıdır ve yeni bir bounded context oluşturmaz.

### 4.5. World & Calendar Sonrası Sıralama

Bu belgenin görevi yalnızca 1. sırayı doğrulamaktır (bkz. görev talimatı). Bölüm 4.1'deki matris, World & Calendar'dan SONRAKİ sıralama için ön bilgi sağlar ancak bu belge o sıralamayı **kesinleştirmez**: Club & Governance, Player Career, Manager Career & Employment ve Competition gibi context'lerin her biri birbirine ve World & Calendar'a bağımlıdır (Bölüm 4.1 sütunlarına bakınız) ve aralarındaki kesin ikinci sıra, World & Calendar tamamlandıktan sonra ayrı bir planlama çalışmasıyla belirlenmelidir (bkz. Bölüm 3, madde 10).

---

## 5. İlk Üretim Dikey Kesiti — World & Calendar

### 5.1. Sistemin Amacı

Kaynak: `docs/12_WORLD_SIMULATION.md` Bölüm 1, 4.2, 6.1–6.6, 9; `docs/03_DOMAIN_MODEL.md` Bölüm 7.1.

* **Oyundaki yetkili zaman kaynağı nedir?** `World & Calendar` bounded context'i. Gerçek dünya duvar saati, frame rate veya UI hiçbir zaman authoritative zaman kaynağı olamaz (`docs/12_WORLD_SIMULATION.md` Bölüm 5.6, 9 madde 3-4).
* **Takvim hangi çözünürlükte ilerler?** Gün çözünürlüğü bağlayıcıdır (Bölüm 6.1, 9 madde 1). Saat/dakika düzeyi MVP için zorunlu değildir; aynı gün içi sıralama `Simulation Phase` ve stable sequence ile çözülür (Bölüm 6.5, 12).
* **Bir simülasyon adımı neyi temsil eder?** `Simulation Step`, bir frame veya thread iteration'ı değildir; benzersiz kimlikli, bir kez tamamlanabilen, checkpoint kaynağı bilinen mantıksal bir ilerleme birimidir (Bölüm 6.4).
* **Gün, hafta, planlama dönemi ve sezon ilişkisi nedir?** `GameDate` (gün) → `Planning Period` (oyuncunun bir sonraki anlamlı planlama penceresi; her zaman 7 gün değildir, `docs/02_MVP_SCOPE.md` Bölüm 7.1'deki "oyun haftası"nın domain karşılığıdır) → Season (Competition context'inin authoritative verisi, World & Calendar'ın parçası değildir). Bu üç kavram AYRI seviyelerdir ve World & Calendar yalnızca ilk ikisinin (GameDate, Planning Period) sahibidir.
* **Sistem yalnız tarih mi tutar, yoksa dönem geçişlerini de yönetir mi?** Her ikisini de yönetir: GameDate ilerletme VE Planning Period yaşam döngüsü (`Created → Open → AwaitingRequiredDecisions → ReadyToAdvance → Processing → Interrupted → Completed → Archived`, Bölüm 10.1) birlikte bu context'in ve ona eşlik eden Application/Simulation orkestrasyonunun sorumluluğundadır. Season lifecycle'ı (Preseason → Active → Completed → Archived) World & Calendar'ın SAHİBİ değildir; World & Calendar bu geçişlerin ZAMANINI koordine eder (Bölüm 4.2, 23).

Bu sorulara verilen cevaplarda mevcut dokümanlar arasında belirsizlik yoktur; hepsi `docs/12_WORLD_SIMULATION.md`'den doğrudan alınmıştır.

**Terminoloji kilidi (D-342 ile kapatıldı; Production Kart 0'ın genel durumu Bölüm 7'de Bloke olarak işaretlidir):** Bu bölümde ve Bölüm 5'in devamında kullanılan terimler — `GameDate`, `Planning Period`, `Simulation Horizon`, `Simulation Step`, `Simulation Phase`, `Simulation Checkpoint`, `Due Work Item`, `Scheduled Evaluation`, `Background Actor Decision`, `World Event Candidate`/`World Event`, `Interruption`, `Blocker`, `Simulation Fidelity`, `World Summary`/`News Projection` (`docs/12_WORLD_SIMULATION.md` Bölüm 6'daki tanımlarla birebir) — Production Kart 1'den itibaren bağlayıcı sözlük olarak kilitlenmiştir. Kod yazan bir çalışma kartı bu terimler için farklı bir anlam veya farklı bir isim türetemez; yeni bir terim gerekiyorsa önce bu belge güncellenmelidir.

### 5.2. Kullandığı Veriler

Kaynak: `docs/12_WORLD_SIMULATION.md` Bölüm 6, 7, 10, 21, 32; `docs/03_DOMAIN_MODEL.md` Bölüm 7.1, 8, 9.

| Kavram | Sahibi olan context | Kalıcı mı? | Snapshot'a girer mi? | Semantic state mi? | Türetilmiş/read model mi? |
|---|---|---|---|---|---|
| `GameDate` (immutable value object; canonical temsili integer `DayNumber`, proleptic Gregorian — bkz. Bölüm 5.7 "Takvim Modeli Kararı", D-343) | World & Calendar | Evet (canonical `DayNumber` olarak) | Evet (`DayNumber`; ISO `yyyy-MM-dd` yalnız projection/export) | Evet | Hayır |
| `SimulationStep` (kimlik + checkpoint kaynağı + phase) | World & Calendar | Kısmen (aktif olan/son tamamlanan) | Evet (cursor olarak) | Evet | Hayır |
| `Planning Period` (`PlanningPeriodId`, başlangıç/bitiş GameDate, Season referansı, blocker referansları, status) | World & Calendar | Evet (aktifse) | Evet | Evet | Hayır |
| `Calendar Window` / transfer penceresi gibi zaman pencereleri | World & Calendar | Evet | Evet | Evet | Hayır |
| Root seed | World & Calendar | Evet | Evet | Evet | Hayır |
| RNG version | World & Calendar | Evet | Evet | Evet | Hayır |
| Runtime random state / stream derivation bilgisi | World & Calendar | Evet | Evet | Evet | Hayır |
| `Simulation Checkpoint` referansı | World & Calendar | Evet (son güvenli checkpoint) | Evet | Evet | Hayır |
| Save schema version ile ilişkili zaman verisi (bkz. `docs/13_SAVE_SYSTEM.md` Bölüm 23 `SchemaVersion`/`SimulationVersion`) | Save Integrity (World & Calendar değil) | Evet | Evet (manifest'te) | Evet | Hayır |
| Simulation Horizon (hedef tarih/checkpoint) | Application (istekte bulunan use case'in geçici girdisi) | Hayır (kalıcı authoritative state değil) | Hayır | Hayır | Hayır — geçici bir istek parametresidir |
| "Bugünün özeti" gibi UI'a sunulan projection'lar | Application/Presentation read model | Hayır (gerektiğinde yeniden üretilebilir) | Hayır | Hayır | Evet |

Bu tablo `docs/03_DOMAIN_MODEL.md` Bölüm 15 (Güncel State, Geçmiş, Türetilmiş Veri) sınıflandırmasıyla uyumludur. **Kesin C# tip adları, alan adları veya sınıf hiyerarşisi bu belgede belirlenmez** — bunlar Production Kart 1'de tasarlanacaktır.

### 5.3. Etkilediği Sistemler

Kaynak: `docs/12_WORLD_SIMULATION.md` Bölüm 7.1 madde 10, Bölüm 26-29; `docs/03_DOMAIN_MODEL.md` Bölüm 11.

Zaman ilerlediğinde, aşağıdaki sistemler World & Calendar'ın ürettiği event/query'lere **tepki verebilir**. Bu liste implementasyon kapsamına alınmaz; yalnızca olay/contract düzeyindeki etkiler tanımlanır:

| Kategori | Etkilenme biçimi (olay/contract düzeyinde) |
|---|---|
| Player development | Season/checkpoint bazlı development/decline değerlendirme tetiklenir (kendi context'i tarafından yürütülür) |
| Fitness ve fatigue | Günlük/haftalık due-work olarak training load ve recovery değerlendirmesi tetiklenir |
| Injury | Recovery due-work değerlendirmesi tetiklenir |
| Fixtures ve matches | Fixture due-date'e ulaşıldığı bildirimi (Competition ve Team Preparation kendi hazırlığını yapar) |
| Contracts | Contract expiration due-work değerlendirmesi tetiklenir |
| Transfers | Transfer window open/close event'leri tetiklenir |
| Relationships | Dolaylı — zamanla ilişkili decay/reinforcement due-work'ü (Social Continuity'nin kendi sorumluluğu) |
| Memory | Dolaylı — zamanla ilişkili unutma/reinforcement due-work'ü |
| Promises | Promise deadline due-work değerlendirmesi tetiklenir |
| Dialogue availability | Interaction & Narrative'in Decision Request deadline'ları GameDate'e bağlıdır |
| Finances | Club bütçe/policy değerlendirme checkpoint'leri (season sınırlarında) |
| Training | Haftalık/dönemsel training plan uygulama tetiklenir |
| Reputation | Season sonu değerlendirme checkpoint'i |
| Notifications | World Summary/News projection'ları için tetikleyici |
| Save checkpoints | Güvenli checkpoint oluşturma noktaları World & Calendar'ın ilerletme akışıyla eşleşir |

Bu ilişkilerin HİÇBİRİ World & Calendar'ın kendisi tarafından implemente edilmez; World & Calendar yalnızca zamanın ilerlediğini ve hangi due-work'ün zamanı geldiğini bildirir (`docs/12_WORLD_SIMULATION.md` Bölüm 4.1, 26-28: "World Simulation yalnız değerlendirme zamanını ve orkestrasyonu sağlar; doğrudan mutation yapmaz").

### 5.4. Etkilendiği Sistemler

Kaynak: `docs/12_WORLD_SIMULATION.md` Bölüm 14.

**"Zaman her koşulda ilerler" varsayımı yapılmaz.** Aşağıdaki sınıflandırma bağlayıcıdır:

| Sınıflandırma | Zamanı durdurur mu? | Örnekler |
|---|---|---|
| **Hard Blocker** | Evet — ilerleme başlamadan durdurur | Zorunlu ve geçerli match squad bulunmaması, illegal squad/registration state, unresolved critical transfer finalization conflict, bozuk authoritative referans |
| **Player Decision Interruption** | Evet — güvenli checkpoint'te durdurur, oyuncu kararı bekler | Kritik futbolcu talebi, Promise/deadline kararı, kritik transfer onayı, kritik Board kararı, iş teklifi, maç hazırlığı |
| **Non-blocking Development** | Hayır | Rutin background transfer haberi, düşük önem Relationship değişimi, routine Player development, background match sonucu |
| **Technical Interruption** | Evet — güvenli devam edilemeyen teknik/invariant hatası | Event storm limiti, duplicate identity conflict, determinism violation, missing authoritative owner |

İlk üretim dikey kesitinde (Production Kart 1-6, Bölüm 7) **en az bir player-facing blocker** bulunmalıdır (`docs/12_WORLD_SIMULATION.md` Bölüm 34.1). World & Calendar bu blocker'ların İÇERİĞİNİ üretmez (bu, ilgili authoritative context'in işidir); yalnızca bunları **sorgulayıp** ilerlemeden önce doğrular.

### 5.5. Ürettiği Olaylar

Kaynak: bu görevin verdiği aday liste + `docs/04_EVENT_RULE_ENGINE.md` Bölüm 4.2, 6.1 sözleşmesi + `docs/12_WORLD_SIMULATION.md` terminolojisi.

Aşağıdaki isimler **adaydır**; mevcut belgelerde birebir bu isimlerle kesinleşmiş değildir, ancak `docs/04_EVENT_RULE_ENGINE.md`'nin geçmiş-zamanlı Domain Event adlandırma kuralıyla (Bölüm 4.2) ve `docs/12_WORLD_SIMULATION.md` terminolojisiyle (Bölüm 6) tutarlıdır.

| Aday olay | Producer | Payload (kavramsal) | Semantic anlam | Ordering | Idempotency | Persistence | Sync/Async |
|---|---|---|---|---|---|---|---|
| `GameTimeAdvanceRequested` | Application (`AdvanceSimulationTime` use case) — **Command**, Domain Event değil | Hedef Simulation Horizon | Bir ilerletme isteğinin başladığını temsil eder | N/A (bu bir command'dır) | CommandId ile | Aktif işlem sürdükçe | Sync |
| `GameTimeAdvanced` | World & Calendar | Yeni `GameDate`, kat edilen `SimulationStepId` aralığı, `CorrelationId` | Bir veya daha fazla `Simulation Step`'in başarıyla commit edildiği gerçekleşmiş gerçek | Aynı `CorrelationId` içinde monoton | `SimulationStepId` + completion | Seçici (Bölüm 5.9) | Sync (checkpoint ile birlikte commit) |
| `GameDayStarted` | World & Calendar | Yeni `GameDate` | Yeni bir günün işlenmeye başladığı | GameDate monoton | `GameDate` + "started" | Genellikle gerekmez (türetilebilir) | Sync |
| `GameDayCompleted` | World & Calendar | `GameDate`, işlenen due-work özeti | Bir günün bütün due-work'ünün işlendiği | GameDate monoton | `GameDate` + "completed" | Seçici | Sync |
| `PlanningPeriodStarted` | World & Calendar (Application orkestrasyonuyla) | `PlanningPeriodId`, başlangıç `GameDate`, `Season` referansı | Yeni bir Planning Period'un `Open` durumuna geçtiği | Önceki dönem `Completed`/`Archived` olmalı | `PlanningPeriodId` + "started" | Evet (aktif dönem snapshot'a girer) | Sync |
| `PlanningPeriodCompleted` | World & Calendar | `PlanningPeriodId`, tamamlanma `GameDate` | Bir Planning Period'un `Completed` durumuna geçtiği | Aynı dönem ikinci kez tamamlanamaz | `PlanningPeriodId` + "completed" | Evet | Sync |
| `SeasonBoundaryReached` (aday isim; "SeasonStarted"/"SeasonEnded" yerine — bkz. not) | World & Calendar | `GameDate`, boundary türü (start/end) | Season sınırına ulaşıldığı bildirimi | GameDate monoton | `GameDate` + boundary türü | Seçici | Sync |
| `SimulationCheckpointReached` | World & Calendar / Application | `SimulationCheckpointId`, `GameDate`, tamamlanmış step kimlikleri | Güvenli, yeniden yüklenebilir bir state noktasına ulaşıldığı | Checkpoint kimlikleri artan | `SimulationCheckpointId` | Evet (save'in temelidir) | Sync |
| `TimeAdvanceBlocked` | Application (World & Calendar'ın blocker sorgusu sonucunda) | Blocker türü, açıklama kodu, ilgili context referansı | İlerlemenin neden durduğu | N/A | Aynı blocker duplicate Decision Request üretmemelidir (Bölüm 14.5) | Notification/audit düzeyinde | Sync |

**Önemli not:** `SeasonStarted`/`SeasonEnded` görev talimatında aday olarak verilmiştir, ancak `docs/03_DOMAIN_MODEL.md` Bölüm 7.2 ve 8'e göre **Season'ın authoritative owner'ı Competition'dır, World & Calendar değil**. Bu nedenle bu iki olay adı, World & Calendar'ın DEĞİL, Competition'ın üreteceği olaylar olarak sınıflandırılmalıdır; World & Calendar yalnızca "season sınırına ulaşıldığını" (`SeasonBoundaryReached` gibi bir zaman-bildirim olayıyla) bildirir, Competition ise bu bildirime tepki olarak kendi `SeasonStarted`/`SeasonEnded` olaylarını üretir. Bu ayrım `docs/12_WORLD_SIMULATION.md` Bölüm 22 ve 23 ile birebir uyumludur ve bu belgede **düzeltilerek** not edilmiştir; sessizce göz ardı edilmemiştir.

Her olay, `docs/04_EVENT_RULE_ENGINE.md` Bölüm 6.1'deki zorunlu alanları (EventId, EventType, EventSchemaVersion, OccurredAtGameTime, SourceContext, CorrelationId, CausationId, SimulationStepId, Payload) taşımalıdır. Kesin payload alan adları bu belgede belirlenmez (Production Kart 2).

### 5.6. Tepki Verdiği Olaylar

Kaynak: `docs/12_WORLD_SIMULATION.md` Bölüm 7.1 madde 11, Bölüm 14.

World & Calendar'ın **dinlemesi gereken** olay/durum kategorileri (yalnızca World & Calendar'ın kendi ilerletme akışını etkileyenler):

* Zorunlu karar açıldı/kapatıldı (Interaction & Narrative — Player Decision Interruption kaynağı).
* Maç hazırlığı tamamlandı/tamamlanmadı (Team Preparation/Match — Hard Blocker veya ilerlemeye izin sinyali).
* Kritik kesinti/teknik hata oluştu (Event & Rule Evaluation — Technical Interruption kaynağı).

**World & Calendar başka context'lerin internal state'ini doğrudan okumaz.** Bu üç entegrasyon seçeneği karşılaştırılmıştır:

| Yaklaşım | Açıklama | Değerlendirme |
|---|---|---|
| **Blocker sorgu contract'ı (önerilen)** | Application, `AdvanceSimulationTime` use case'i içinde, ilgili context'lere (Team Preparation, Interaction & Narrative, Event & Rule Evaluation) açık bir **query** (örn. `HasPendingHardBlocker`, `HasPendingCriticalDecision`) sorar; sonuçları toplar ve World & Calendar'a ilerlemenin güvenli olup olmadığını bildirir. | `docs/12_WORLD_SIMULATION.md` Bölüm 5.5 ("Context'ler arası her orkestrasyon Application-owned use case üzerinden yürütülür") ve Bölüm 3 ("foreign mutation yasaktır") ile tam uyumlu. World & Calendar hiçbir foreign state'i okumaz; yalnızca Application'ın topladığı sonucu değerlendirir. |
| **Command validation** | Her context, kendi Command'lerini işlerken "zaman ilerletilemez" durumunu kendi reddiyle bildirir. | Yalnızca REACTİF çalışır (zaman ilerletmeyi DENEDIKTEN sonra öğrenilir); PROAKTİF blocker sorgusu (ilerlemeden ÖNCE bilme) için yetersizdir. Tek başına yeterli değildir. |
| **Policy tabanlı entegrasyon** | Her context, "ben şu an blocker'ım" bilgisini kendi policy/registry'sine yazar; World & Calendar bu registry'yi okur. | Bu, gizli bir ikinci "blocker state" kopyası oluşturma riski taşır (`docs/03_DOMAIN_MODEL.md` Bölüm 24.4 "Çift authoritative owner" riski). Önerilmez. |

**Önerilen yön:** Blocker sorgu contract'ı, Application katmanında bir **"Blocker Aggregator" query deseni** olarak modellenir: `AdvanceSimulationTime` use case'i ilerlemeden önce ilgili context'lerin salt-okunur query'lerini çağırır, sonuçları toplar, ve World & Calendar'a yalnızca "ilerlemeye izin var/yok + neden" bilgisini iletir. Bu, hem `docs/03_DOMAIN_MODEL.md` foreign-mutation yasağını hem `docs/12_WORLD_SIMULATION.md` Bölüm 5.5 orkestrasyon kuralını korur. **Kesin query sözleşmesi bu belgede tasarlanmaz** (Production Kart 3).

### 5.7. İş Kuralları ve Invariant'lar

Kaynak: `docs/12_WORLD_SIMULATION.md` Bölüm 9, 33, 36; `docs/03_DOMAIN_MODEL.md` Bölüm 13.

| # | Durum | Yön |
|---|---|---|
| 1 | Tarih geriye alınamaz | `GameDate` geriye gidemez (Bölüm 36 madde 1). Eski save yüklemek AYRI bir rehydration işlemidir, "geriye alma" değildir (Bölüm 9 madde 14). |
| 2 | Negatif veya sıfır dışı geçersiz ilerletme | Geçersiz `Simulation Horizon` (geçmiş tarih, negatif adım) command reddedilir; state transition oluşmaz. |
| 3 | Aynı command'in iki kez uygulanması | `CommandId`/idempotency key ile aynı ilerletme isteği ikinci kez commit edilmez (Bölüm 5 madde 10, Bölüm 36 madde 2). |
| 4 | Sezon başlangıç/bitiş sınırları | Season boundary normal günlük ilerleme içinde kaybolamaz (Bölüm 9 madde 15); Competition'ın prerequisite'leri karşılanmadan sınır aşılamaz (Bölüm 24.3). |
| 5 | Ay/yıl geçişleri | **Kart 0'da kapatıldı (D-343):** Yıl, ay ve gün, canonical integer `DayNumber`'dan proleptic Gregorian kurallarıyla deterministik olarak türetilir; standart Gregoryen ay uzunlukları (28/29/30/31 gün) kullanılır. Ayrı bir domain kuralı gerekmez, türetme kuralı kapanmıştır. |
| 6 | Artık gün (leap year) davranışı | **Kart 0'da kapatıldı (D-343):** Proleptic Gregorian artık yıl kuralları (4/100/400 bölünebilirlik) desteklenir; `DayNumber`'dan türetilen yıl/ay/gün bu kuralı otomatik yansıtır. Bu, .NET `DateOnly` gibi hazır bir adapter/helper ile doğrulanabilir ancak domain sözleşmesinin (canonical `DayNumber`) yerine geçmez. |
| 7 | Save/load sonrasında aynı tarihten devam | Load sonrasında aynı due work ikinci kez uygulanmaz, kaçırılan due work sessizce atlanmaz (Bölüm 32 madde 3-4). |
| 8 | Aynı seed ve aynı command dizisiyle aynı sonuç | Determinizm Sözleşmesi (Bölüm 33) — bağlayıcı. |
| 9 | Zorunlu karar varken zaman ilerletme | Hard Blocker çözülmeden ilerleme başlayamaz (Bölüm 36 madde 19). |
| 10 | Bir olay handler'ının başarısız olması | Failed consumer, source event'i "gerçekleşmemiş" hâle getirmez (Bölüm 13); güvenli checkpoint kritik effect tamamlanmadan oluşturulamaz (Bölüm 13, Bölüm 31). |
| 11 | Kısmi ilerleme veya transaction sınırı | Simulation Step atomik veya açık tutarlılık sınırında işlenir (Bölüm 6.4); yarım Step save edilemez (Bölüm 32 madde 1). |
| 12 | Uzun çalıştırmada overflow | Canonical `DayNumber` (integer) ve `SimulationStepId` için sayısal taşma riski; kesin integer genişliği (örn. `int` vs `long`) bu belgede belirlenmez (Production Kart 1), ancak 10+ sezonluk (yaklaşık 3650+ gün, on binlerce Step) ölçek için yeterli aralık gereksinimi not edilir — proleptic Gregorian `DayNumber` hesaplaması bu ölçekte hiçbir standart integer genişliğinde taşma riski taşımaz. |
| 13 | Timezone kullanılmaması veya kullanım gerekçesi | `GameDate` duvar saatinden bağımsızdır (Bölüm 6.1); timezone kavramı GEÇERSİZDİR — oyun tarihi tek, global, timezone'suz bir domain kavramıdır. |
| 14 | Gerçek dünya tarihi ile oyun tarihi ayrımı | Save Manifest'teki `CreatedAtUtc` (gerçek dünya, teknik metadata) ile domain `GameDate` (oyun içi, business veri) kesinlikle ayrı alanlardır; Spike 3'ün `SaveManifest.CreatedAtUtc` alanı bu ayrımın zaten doğru yapıldığını gösterir (bkz. Bölüm 6). |

**Takvim Modeli Kararı (Bu madde Kart 0 kapsamında tam kapatıldı, bkz. `docs/15_DECISION_LOG.md` D-343, D-344, D-345; Kart 0'ın GENEL durumu Bölüm 7'de Bloke'dur):**

Bir önceki Kart 0 denemesinde seçilen "sadeleştirilmiş, Gregoryen olmayan futbol takvimi" kararı **düzeltilmiş ve kaldırılmıştır**. Bağlayıcı karar şu şekildedir:

* Authoritative tarih tipi immutable **`GameDate`** value object'idir.
* Canonical temsil, integer **`DayNumber`** değeridir (proleptic Gregorian epoch'tan itibaren gün sayısı).
* Takvim semantiği **proleptic Gregorian**'dır (Gregoryen takvim kuralları, tarihsel takvim geçişleri olmadan geriye ve ileriye doğru tutarlı biçimde uzatılır).
* Yıl, ay ve gün, canonical `DayNumber`'dan deterministik olarak türetilir; bu üçü ayrıca kalıcı/mutable alan olarak SAKLANMAZ.
* Gregoryen artık yıl kuralları (4/100/400 bölünebilirlik) desteklenir.
* Ay uzunlukları standart Gregoryen ay uzunluklarıdır (Ocak 31, Şubat 28/29, vb.).
* .NET `DateOnly`, internal helper veya adapter olarak kullanılabilir (örn. `DayNumber ↔ DateOnly` dönüşümü için); domain sözleşmesinin yerine geçmez — authoritative veri her zaman `DayNumber`'dır.
* Save içindeki canonical değer `DayNumber`'dır; ISO `yyyy-MM-dd` biçimi yalnızca projection, debug veya export amaçlıdır, authoritative save alanı değildir.
* Domain zamanı timezone, DST, saat, dakika veya saniye taşımaz (Bölüm 5.7 satır 13 ile birebir uyumlu).
* MVP calendar granularity bir oyun günüdür.

**Karar ve gerekçe:** Bu karar, bu görevin doğrudan ve açık talimatıyla bağlayıcı olarak verilmiştir ve önceki Kart 0 denemesindeki "kanıta dayalı çıkarım" (MVP'nin kurgusal-ülke doğasından sadeleştirilmiş takvimi türetme) yerini almıştır. Önceki çıkarım, `docs/02_MVP_SCOPE.md` Bölüm 7.1'in "Takvim gerçek günler üzerinden ilerler" ifadesini doğru biçimde soyut "Özel Season Calendar" seçeneğine karşı kullanmıştı; bu belge bunu geçersiz kılmaz. Ancak Gregoryen ile sadeleştirilmiş seçenek arasındaki ikinci adım artık açık bir mimari talimatla proleptic Gregorian yönünde kesinleştirilmiştir. Bu değişiklik mevcut hiçbir belgeyle (`docs/02_MVP_SCOPE.md`, `docs/12_WORLD_SIMULATION.md`, `docs/11_PLAYER_CAREER.md`) çelişmez; bu belgelerin hiçbiri Gregoryen olmayan bir takvimi ZORUNLU KILMAMIŞTI, yalnızca kesin seçimi açık bırakmışlardı.

**Artık kapsam dışı bırakılmayan (bu kartta kapanan) alt maddeler:** Ay uzunlukları ve artık yıl semantiği tamamen kapanmıştır (bkz. Bölüm 5.7 satır 5-6); "exact ay uzunluklarının Kart 1'e bırakıldığı" ifadesi kaldırılmıştır.

**Hâlâ açık kalan (bilinçli olarak kapatılmayan) alt maddeler:** Exact kariyer başlangıç tarihi ve competition fixture tarihleri (`docs/08_TRANSFER_SYSTEM.md` Bölüm 47, `docs/12_WORLD_SIMULATION.md` Bölüm 38 ile tutarlı — bkz. Bölüm 8'deki genel açık karar listesi).

### 5.8. Application Use Case Sınırı

Kaynak: `docs/12_WORLD_SIMULATION.md` Bölüm 11; `docs/17_TECHNOLOGY_AND_ARCHITECTURE_DECISION.md` Bölüm 8.3.

Aşağıdaki use case türleri (kod yazılmadan) değerlendirilir:

| Use case türü | Girdi (kavramsal) | Çıktı (kavramsal) | Notlar |
|---|---|---|---|
| **Zaman ilerletme command'i** (`AdvanceSimulationTime` veya eşdeğeri) | Hedef `Simulation Horizon` | Committed advancement sonucu (yeni GameDate, oluşan event'ler, varsa interruption nedeni) | Bölüm 11'deki 19 adımlı akışı orkestre eder |
| **Mevcut oyun tarihi query'si** | Yok | Güncel `GameDate` (read model) | Salt okunur, hiçbir state değiştirmez |
| **Mevcut dönem query'si** | Yok | Aktif `Planning Period` özeti (read model) | Salt okunur |
| **Zaman ilerletme uygunluk query'si** | Hedef horizon (opsiyonel) | İlerlemenin şu an mümkün olup olmadığı + varsa blocker özeti | Bölüm 5.6'daki Blocker Aggregator deseniyle çalışır; UI'ın "İlerlet" butonunu aktif/pasif göstermesi için kullanılabilir |
| **İlerletmeyi engelleyen nedenlerin read model'i** | Yok | Aktif blocker'ların insan-okunabilir/kod listesi | `docs/12_WORLD_SIMULATION.md` Bölüm 14.5 "Blocking nedeni oyuncuya açıklanabilir olmalıdır" ile uyumlu |

**Presentation'ın domain/simulation nesnelerini doğrudan oluşturmamasını sağlayacak contract sınırı:** Presentation, yukarıdaki use case'lere yalnızca Application'ın tanımladığı **Command/Query DTO'ları** (henüz adlandırılmamış, Kart 3'te tasarlanacak) üzerinden erişir. Presentation hiçbir zaman `World & Calendar` domain nesnesini (örn. bir "Calendar" veya "SimulationState" aggregate'ini) doğrudan `new` ile oluşturamaz veya referans tutamaz — bu, immutable kural #6'nın ("Presentation yalnızca Application command/query ve read model'leri üzerinden çalışacak") doğrudan uygulamasıdır.

**Godot UI'ın gelecekte kullanacağı read model'ler (yalnızca contract düzeyinde):**

* "Güncel tarih" görüntüleme read model'i (Kart 6'nın minimal zaman kontrolü ekranı için).
* "İlerlet" eyleminin sonucu (başarılı mı, hangi event'ler oluştu, blocker var mı).
* Blocker açıklama read model'i.

Bu read model'lerin kesin şekli, alanları veya Godot tarafındaki gösterim biçimi bu belgede tasarlanmaz (Production Kart 6).

### 5.9. Persistence ve Save Uyumluluğu

Kaynak: `docs/13_SAVE_SYSTEM.md` Bölüm 7, 21, 23, 27, 32; `docs/12_WORLD_SIMULATION.md` Bölüm 32.

| Konu | Yön |
|---|---|
| Game date'in snapshot'a eklenmesi | Zorunlu — güncel `GameDate`'in canonical `DayNumber` temsili her snapshot'ın parçasıdır (`docs/13_SAVE_SYSTEM.md` Bölüm 10 kapsamına girer; D-343 ile uyumlu). ISO `yyyy-MM-dd` save'e authoritative alan olarak yazılmaz. |
| Season identity'nin saklanması | World & Calendar bunu SAKLAMAZ — Season identity Competition'ın authoritative verisidir. World & Calendar yalnızca aktif Season'a bir REFERANS taşıyabilir (`docs/12_WORLD_SIMULATION.md` Bölüm 32: "active Season referansı"). |
| Simulation step/cursor saklanması | Zorunlu — tamamlanmış Simulation Step kimlikleri veya eşdeğer cursor bilgisi (Bölüm 32). |
| RNG state ile zaman state'inin atomik kaydı | Zorunlu — "RNG state kaybı farklı dünya sonucu üretmemelidir" (Bölüm 32 madde 5); ikisi aynı checkpoint/transaction sınırında commit edilmelidir. |
| Save schema version etkisi | `SchemaVersion` değişimi migration veya explicit compatibility policy gerektirir (`docs/13_SAVE_SYSTEM.md` Bölüm 23 madde 8); World & Calendar'ın kendi veri alanları bu genel kuralın dışında değildir. |
| Eski spike save dosyalarının üretim save dosyası kabul edilip edilmeyeceği | **Hayır.** Spike 3'ün `SaveManifest`/`Clubs`/`Players` şeması (`Spike1Placeholder` veri modeline dayanır) üretim World & Calendar/Player Career/Club & Governance şemasıyla semantik olarak uyumsuzdur. Spike save dosyaları üretim yükleyicisi tarafından TANINMAMALIDIR. |
| Eski save'lerin reddedilmesi veya migration seçeneği | **Reddedilmeli.** Spike save'leri için migration YAZILMAZ; bunlar zaten "geçici teknik kanıt" olarak işaretlenmiştir (`docs/18_SPIKE_EXECUTION_PLAN.md` Kart 4 "Önemli sınırlama"). Üretim save formatı, spike formatından farklı bir `SchemaVersion`/`GameVersion` alanıyla başlar ve spike dosyalarını "Unsupported Old Save" (`docs/13_SAVE_SYSTEM.md` Bölüm 24) olarak sınıflandırıp reddeder. |
| Bozuk veya tutarsız tarih verisi | Determinism Validation katmanı (Bölüm 22.7) root seed, RNG version, random state/derivation ve simulation cursor uyumunu kontrol eder; eksikse reddedilir, tahmin edilmez. |
| Calendar configuration değiştiğinde migration davranışı | Eğer takvim modeli (Bölüm 5.7 "Takvim Modeli Seçenekleri") ileride değiştirilirse, bu bir `SchemaVersion` veya `ContentVersion` değişikliği sayılır ve `docs/13_SAVE_SYSTEM.md` Bölüm 25 (Migration Stratejisi) genel kurallarına tabidir. |

**Spike 3'ün SQLite şemasının kalıcı şema olarak kabul EDİLMEDİĞİ açıkça teyit edilir.** Spike 3'ten üretime taşınabilecek olan şey şema değil, **teknik**: atomik temp+move yazma, backup+çalışma-kopyası+atomik-swap migration, semantic canonical hash doğrulaması (bkz. Bölüm 6).

### 5.10. Sınır Durumları

Kaynak: `docs/12_WORLD_SIMULATION.md` Bölüm 37; `docs/03_DOMAIN_MODEL.md` Bölüm 21; ek analiz.

**Tarih ve takvim**

1. Aynı gün içinde birden fazla due work item oluşması — deterministik Simulation Phase + stable sequence ile sıralanır, handler sırası kullanılmaz.
2. Büyük zaman atlaması (örn. birkaç ay) talep edilmesi — aradaki due work atlanmadan sırayla işlenir.
3. Artık gün/takvim kenar durumu — Bölüm 5.7'deki seçilecek takvim modeline bağlıdır.
4. Season sınırı ile bir Promise deadline'ının aynı GameDate'e denk gelmesi — deterministic ordering + owner conflict policy.

**Command validation**

5. Geçmiş bir tarihe "ilerletme" istenmesi — reddedilir (invariant 1).
6. Negatif veya sıfır `Simulation Horizon` adımı istenmesi — reddedilir.
7. Aynı `CommandId` ile ikinci kez ilerletme istenmesi — idempotent olarak aynı sonucu döner, ikinci kez uygulanmaz.

**Event ordering**

8. `GameTimeAdvanced` ile aynı anda birden fazla context'in due-work event'i üretmesi — CorrelationId ile gruplanır, stable sequence korunur.
9. Bir Integration Event'in tüketici context tarafından "unsupported" olarak reddedilmesi — Bölüm 5 tablosundaki genel kural uygulanır; World & Calendar'ın kendi ilerlemesini durdurmaz (non-blocking sayılmadıkça).

**Persistence**

10. Save isteği bir Simulation Step'in ortasında gelmesi — mevcut atomik işlem tamamlanana kadar reddedilir veya beklenir (Bölüm 32 madde 2).
11. Load sonrasında RNG version'ın bulunamaması — reddedilir, sessizce tahmin edilmez.
12. Spike formatındaki eski bir save dosyasının üretim yükleyicisine verilmesi — "Unsupported Old Save" olarak reddedilir (Bölüm 5.9).

**Determinizm**

13. Aynı seed, farklı çalıştırma ortamı (yerel/CI) — aynı semantic sonucu üretmelidir (zaten Spike 2/8'de CI ↔ yerel eşleşmesiyle kanıtlanmış bir desendir, D-334/D-340).
14. Farklı koleksiyon iterasyon sırası — sonucu etkileyemez (Bölüm 12).

**Uzun dönem simülasyon**

15. 10+ sezonluk çalıştırmada `SimulationStepId`/`GameDate` sayısal taşması — Bölüm 5.7 invariant 12.
16. Checkpoint/processing kayıtlarının kontrolsüz büyümesi — retention/compaction politikasına tabidir (Bölüm 12.6 "Uzun dönem veri riski").

**UI/Application entegrasyonu**

17. UI'ın "İlerlet" düğmesine art arda birden fazla kez tıklanması — Application, aynı isteğin tekrarını idempotency ile veya buton devre dışı bırakma read model'iyle yönetir (kesin UI davranışı Kart 6'da).
18. Presentation'ın kapanması/crash olması sırasında devam eden bir ilerletme — Simulation Step atomikliği korunur; UI'ın varlığı domain işleminin tamamlanmasını etkilemez.

**Hata ve recovery**

19. Bir due-work handler'ının exception fırlatması — Failed consumer source event'i "gerçekleşmemiş" yapmaz; güvenli checkpoint kritik effect tamamlanmadan oluşmaz (invariant 10).
20. Event chain depth veya step work budget limitinin aşılması — Step başarısız kabul edilir, sessizce yok sayılmaz (Bölüm 31).

Bu liste 20 maddedir; ek sınır durumları Production Kart 2 sırasında (gerçek invariant testleri yazılırken) ortaya çıkabilir ve bu belgeyi güncellemeden ilgili kartın kendi test dosyalarına eklenebilir.

### 5.11. Test Senaryoları

Kaynak: `docs/14_TEST_STRATEGY.md` Bölüm 6, 9, 23, 26; `docs/12_WORLD_SIMULATION.md` Bölüm 35.

#### Domain Unit Tests

* `GameDate` value object doğrulaması (geçersiz tarih, geriye gitme reddi).
* Tarih ilerleme kuralları (bir sonraki güne geçiş, monotonluk).
* Sezon sınırı invariant'ları (World & Calendar'ın kendi sorumluluğu kadarıyla — season'ın KENDİSİ değil, sınıra ulaşıldığı bildirimi).
* `Simulation Step` kimlik benzersizliği ve tekrar tamamlanamazlık invariant'ı.
* `Planning Period` lifecycle geçiş kuralları (geçerli/geçersiz state transition).

#### Application Tests

* `AdvanceSimulationTime` command orchestration (mock/stub context'lerle).
* Blocker davranışı — Hard Blocker varken ilerlemenin başlamaması.
* Event üretimi — başarılı ilerlemenin doğru event kümesini ürettiği.
* Query/read model çıktıları — güncel tarih, aktif dönem, blocker özeti.
* Tekrar uygulanan command davranışı — aynı `CommandId` ikinci kez idempotent sonuç.

#### Simulation Tests

* Aynı başlangıç state'i ve aynı command dizisiyle deterministik sonuç (Spike 2'nin `CanonicalStateHasher` tekniği yeniden kullanılabilir, Bölüm 6).
* Uzun dönem zaman ilerletme (Spike 1'in `HeadlessSimulationRunner` mimarisi yeniden kullanılabilir).
* Event ordering — aynı gün içi birden fazla due-work'ün stable sırayla işlenmesi.
* Başarısız handler davranışı — bir due-work değerlendirmesi hata verirse Step'in güvenli biçimde durması.

#### Infrastructure Tests

* Save/load round-trip (Spike 3'ün tekniği yeniden kullanılabilir; şema YENİDEN tasarlanır).
* Migration (üretim şema sürümleri arası; spike V1→V2 şeması DEĞİL).
* Corruption (bozuk World & Calendar verisiyle save'in reddi).
* Atomik kayıt (temp+move deseni, Spike 3'ten yeniden kullanılabilir).
* Tarih ve RNG state tutarlılığı (birlikte commit edildiğinin doğrulanması).

#### Presentation Tests

* UI'ın yalnız Application contract'larını (Command/Query DTO) kullandığının doğrulanması — domain/simulation tipi import edilmediğinin statik/derleme zamanı kontrolü.
* Zaman ilerletme butonunun doğru command'i (doğru parametrelerle) çağırdığının doğrulanması.
* Blocker nedenlerinin kullanıcıya (Godot Label/Control üzerinde) gösterildiğinin doğrulanması.
* UI içinde iş kuralı bulunmadığının doğrulanması — örn. "ilerlemeye izin var mı" kararının UI'da DEĞİL, Application query sonucunda verildiğinin kod incelemesiyle/testle teyidi.

#### Uzun Dönem Testleri

* 10 sezon boyunca deterministik ilerletme (World & Calendar + minimal stub context'lerle).
* 20 sezon boyunca aynı testin tekrarı (population/veri büyümesi sınırlarını görmek için, `docs/12_WORLD_SIMULATION.md` Bölüm 30 ile uyumlu).
* Yüz binlerce `Simulation Step` işleyen bir headless çalıştırma (10 sezon × ~365 gün ≈ 3650 gün'den daha büyük bir Step hacmi; Bölüm 5.7 invariant 12'nin performans/overflow varsayımlarını test eder).
* Periyodik save/load (her N sezonda bir "kapat-aç" döngüsü).
* Aynı seed ile tekrar koşma ve **canonical hash karşılaştırması** (Spike 2 tekniği, D-334).
* Bellek büyümesi ölçümü (Spike 1'in `Run_RepeatedFullSimulations_DoNotLeakMemoryUnboundedly` deseni yeniden kullanılabilir).
* Event sayısı ve kuyruk büyümesi raporlanması.
* Sezon sınırlarında invariant kontrolü (her sınırda tam invariant taraması).

**Testlerde gerçek zaman saatine, `DateTime.Now`'a, sistem timezone'una veya kararsız thread scheduling'e bağımlılık OLMAMALIDIR** — bu, `docs/12_WORLD_SIMULATION.md` Bölüm 33 Determinizm Sözleşmesi'nin ve Spike 2/8'in zaten kanıtladığı desenin (seeded `SimulationRandomContext`, gerçek saat kullanılmaması) doğrudan devamıdır.

---

## 6. Placeholder'dan Üretim Koduna Geçiş

Aşağıdaki sınıflandırmalar kullanılır: **(A) Tamamen silinecek**, **(B) Test fixture olarak korunacak**, **(C) Prototype klasörüne taşınacak**, **(D) Ortak teknik parçaları çıkarılacak**, **(E) Production contract ile değiştirilecek**, **(F) Geçici compatibility adapter arkasında tutulacak**.

| Namespace / Sınıf | Sınıflandırma | Gerekçe |
|---|---|---|
| `Spike1Placeholder` (tüm namespace — `Domain.Spike1Placeholder` + `Simulation.Spike1Placeholder`: `World`, `Club`, `Player`, `ClubId`, `PlayerId`, `WorldSnapshot`, `WorldFactory`, `SeasonAdvancer`, `WorldInvariantChecker`, `WorldInvariantViolationException`, `WorldSnapshotSerializer`, `SimulationRunReport`, `HeadlessSimulationRunner`, `SimulationCheckpointResumer`, `CanonicalStateHasher`) | **(B) → (A)** Test fixture olarak korunacak, ardından tamamen silinecek | Gerçek World & Calendar/Player Career/Club & Governance domain modelini TEMSİL ETMEZ (`docs/18_SPIKE_EXECUTION_PLAN.md` Kart 2/3 "Önemli sınırlama"). Mevcut 8+13+8 testin CI'ı yeşil tutması için gerekli olduğundan Production Kart 2 tamamlanana kadar dokunulmaz; Kart 2 tamamlandığında bu namespace ve ona bağlı test dosyaları BİRLİKTE kaldırılır (bkz. Bölüm 8 "dokunulmayacak dosyalar"). |
| `Spike4Placeholder` (`Application.Spike4Placeholder`: `PlayerListRow`, `PlayerListQuery`, `PlayerListSortColumn`) | **(B) → (E)** Test fixture olarak korunacak, sonra production contract ile değiştirilecek | Godot UI'ının gerçek bir read model'e bağlanması gerektiğinde (Production Kart 6 sonrası, gerçek futbolcu listesi ekranı geldiğinde) bu yer tutucu query gerçek Application query/read model'iyle değiştirilir. World & Calendar dikey kesitinde DOKUNULMAZ. |
| `Domain/SimulationStep.cs` (Kart 0, `Spike1Placeholder` DIŞINDA, isim benzerliği riski taşır) | **(A) Tamamen silinecek** | Kart 0'ın "herhangi bir placeholder domain kavramı" kanıtı dışında amacı yoktu. Gerçek `GameDate`/`SimulationStepId` (Production Kart 1) bu ismi ve kavramı gerçek anlamıyla üstlenecektir; ikisinin bir arada bulunması kafa karışıklığı riski taşıdığından **Production Kart 1'in ilk adımı bu dosyayı silmektir**. |
| `PlaceholderWorldLoop`, `AdvancePlaceholderSimulationUseCase` (Kart 0) | **(A) Tamamen silinecek** | Katman bağlantısını (Domain←Simulation←Application) kanıtlamak dışında amacı yoktu; gerçek `AdvanceSimulationTime` use case'i (Production Kart 3) bu rolü üstlenir. `PlaceholderSkeletonTests.cs` bunlarla birlikte silinir. |
| `HeadlessSimulationRunner`, `WorldFactory` (Spike1Placeholder) | **(D) Ortak teknik parçaları çıkarılacak**, sonra **(A)** | İçindeki "headless runner: `CreateWorld`/`AdvanceSeasons` ayrımı, Godot'suz çalıştırma" MİMARİ DESENİ, gerçek World & Calendar'ın Production Kart 4'teki headless test runner'ı için yeniden kullanılabilir bir ŞEKİLDİR. İÇERİK (yer tutucu `World`/`Club`/`Player`) silinecektir; yalnızca desen (ayrık "kurulum" ve "ilerletme" adımları) ilham kaynağı olur, kod kopyalanmaz. |
| `SimulationRandomContext` (Simulation, top-level, **placeholder DEĞİL**) | **(D) Ortak teknik parçaları çıkarılacak** — zaten üretime uygun | Bu, D-058'in gerçek implementasyonudur (seeded, versioned Random Context). Production World & Calendar'ın Bölüm 5.2'deki "root seed/RNG version/random state" ihtiyacını karşılamak üzere AYNI SINIF, muhtemelen küçük bir isim/namespace düzenlemesiyle (örn. `FootballCareerSimulator.Simulation` kök namespace'inde kalabilir), yeniden kullanılabilir. Production Kart 1/4'te "silinecek" değil, "olduğu gibi veya küçük düzenlemeyle taşınacak" olarak değerlendirilmelidir. |
| `CanonicalStateHasher`, `WorldSnapshotSerializer`, `SimulationCheckpointResumer` (Spike2, `Spike1Placeholder` içinde) | **(D) Ortak teknik parçaları çıkarılacak**, sonra **(A)** | "Semantic canonical hash", "snapshot capture/restore ayrımı" ve "seed'den replay ile RNG cursor kurma" TEKNİKLERİ gerçek Save Integrity/World & Calendar tasarımına (Production Kart 4/5) girdi sağlayabilir. Bu, Bölüm 3 madde 1/2'deki açık kararları (RNG stream stratejisi, save schema) KAPATMAZ — yalnızca bir kanıtlanmış YAKLAŞIM sunar. Kesin implementasyon gerçek domain modeli üzerinde yeniden yazılır; bu sınıfların kendisi kopyalanıp yeniden adlandırılmaz. |
| Spike SQLite save sınıfları (`SqliteSaveWriter`, `SqliteSaveReader`, `SqliteSaveMigrator`, `SqliteRowReader`, `SqliteSaveSchema`, `SqliteLoadResult`, `SaveIntegrityExceptions`) | **(D) Ortak teknik parçaları çıkarılacak** + **(F) kısa süreli compatibility adapter** | Atomik temp+move yazma, backup+çalışma-kopyası+atomik-swap migration, hash tabanlı bozulma tespiti PATTERN'leri gerçek Save Integrity implementasyonuna (Production Kart 5) doğrudan girdi sağlar. Şemanın KENDİSİ (`SaveManifest`/`Clubs`/`Players`, Spike1Placeholder'a özgü) production'a taşınamaz (Bölüm 5.9, Bölüm 3 madde 2). Kart 5 tamamlanana kadar, CI'ın Spike 3 testlerini (21 test) çalıştırmaya devam etmesi için bu sınıflar silinmez; Kart 5'in gerçek Save Integrity'si hazır olduğunda spike sınıfları ve testleri BİRLİKTE kaldırılır. |
| `Shell.cs`, `Shell.tscn` (Presentation, Kart 5) | **(A) Tamamen silinecek** (düşük risk, hâlâ kullanımda değil) | `PlayerListScreen` ana sahne olduğu için `Shell` zaten kullanılmıyor; Kart 6 gerçek zaman kontrolü ekranını yazarken bu dosyalar kaldırılabilir. |
| `PlayerListScreen.cs`, `PlayerListScreen.tscn` (Presentation, Kart 6) | **(C) Prototype klasörüne taşınacak** (silinmeden) | UI wiring PATTERN'i (Tree, sayfalama, gömülü öz-kontrol) `prototypes/` klasörüne (repository'de zaten mevcut, boş) referans olarak taşınabilir; Production Kart 6'da gerçek bir "zaman kontrolü" ekranı AYNI DOSYALAR ÜZERİNDE DEĞİL, yeni dosyalarda yazılır. Bu ekran silinmez çünkü Spike 4'ün (500 futbolculuk UI performansı) kanıtı olarak GDD/mimari doğrulama değeri taşımayı sürdürür. |
| `tools/FootballCareerSimulator.SimulationRunner` | **(D) Ortak teknik parçaları çıkarılacak** | "Headless çalıştırılabilir konsol aracı" KAVRAMI (Godot olmadan simülasyonu manuel tetikleme) gerçek World & Calendar için de değerli bir geliştirici aracıdır (`docs/17_TECHNOLOGY_AND_ARCHITECTURE_DECISION.md` Bölüm 13'teki "ayrı headless simulation runner" kararıyla uyumlu). İçeriği (Spike1Placeholder'a bağımlılığı) güncellenmeden bu araç production World & Calendar'ı çalıştıramaz; Production Kart 4'te aracın İÇİ güncellenir, aracın KENDİSİ silinmez. |
| Test dosyaları (`Spike1HeadlessTenSeasonSimulationTests`, `Spike2DeterminismAndSeedTests`, `Spike3SqliteSaveLoadTests`, `Spike4PlayerListQueryTests`, `PlaceholderSkeletonTests`, `LegacySaveFixture`) | **(B) Test fixture olarak korunacak**, ardından ilgili placeholder koduyla BİRLİKTE **(A)** | Placeholder kod kaldırıldığında bu testler de birlikte kaldırılır; "önce testi bozup sonra sil" değil, "kod ve testleri birlikte, aynı PR'da kaldır" ilkesi izlenir (aşağıdaki genel ilkeye bkz.). |

### Genel Geçiş İlkesi

> Gerçek model implementasyonu tamamlanmadan placeholder namespace'leri kaldırılmaz; her kart sonunda build ve CI çalışır kalır.

Somut olarak: Production Kart 1-4 boyunca, **yeni** gerçek Domain/Simulation/Application kodu **placeholder'ların YANINDA, onlarla çakışmayan isim alanlarında** (örn. gerçek `World & Calendar` kodu `FootballCareerSimulator.Domain.WorldCalendar` gibi bir namespace'te, `Spike1Placeholder`'dan tamamen ayrı) yazılır. Placeholder namespace'leri yalnızca gerçek karşılıkları TAMAMEN çalışır ve test edilir hâle geldiğinde (yani ilgili Production Kart'ın kabul kriterleri karşılandığında) kaldırılır. Bu, her kartın sonunda hem eski hem yeni kodun bir arada derlenebildiği, CI'ın hiçbir noktada kırılmadığı bir geçiş sağlar.

---

## 7. Çalışma Kartları

Aşağıdaki kartlar, görev talimatındaki başlangıç yapısını temel alır ve mevcut belgelere göre doğrulanıp küçük düzeltmelerle sunulur. **Bu kartlardan hiçbiri bu görevde başlatılmamıştır.**

Bu kart sırası `docs/01_GAME_DESIGN_DOCUMENT.md` Bölüm 36'daki (Geliştirme Yaklaşımı) 12 adımla doğrudan eşleşir: adım 1-7 (amaç, veri, bağımlılıklar, olaylar, senaryolar) bu belgenin Bölüm 5'inde zaten tamamlanmıştır; adım 8-12 (veri modeli, iş kuralları, testler, uzun dönem test, UI) aşağıdaki Kart 1-6'ya karşılık gelir.

### Production Kart 0 — Terminoloji ve Karar Kapanışları — Bloke

**Durum: Bloke — exact .NET SDK pin kanıtı eksik** (bkz. `docs/15_DECISION_LOG.md` D-342–D-351). Aşağıdaki altı koşuldan BEŞİ karşılanmıştır; exact SDK sürümü kanıtlanamadığından Kart 0 **"Tamamlandı" değildir**.

* **Amaç:** World & Calendar için kullanılacak terminolojiyi (Bölüm 5.1) ve açık kararları (Bölüm 3, madde 1/3/6) görünür ve kayıtlı hâle getirmek; herhangi bir kodu etkilemeden.
* **Ön koşul:** Bu belgenin (docs/19) onaylanması. — Karşılandı.
* **Kapsam içi:** Takvim modeli kararı (Bölüm 5.7 — proleptic Gregorian `DayNumber`), Target Framework/SDK ayrımı ve exact SDK kanıt değerlendirmesi (Bölüm 3 madde 3), composition root ve DI yaklaşımının kapatılması (Bölüm 3 madde 6) — bunlar KOD DEĞİL, karar günlüğü kayıtlarıdır.
* **Kapsam dışı:** Herhangi bir `.cs`, `.csproj`, `global.json` veya `.tscn` değişikliği. — Korundu; bu kartta hiçbir kaynak/yapılandırma dosyası değişmemiştir.
* **Etkilenecek dosyalar:** Yalnızca `docs/15_DECISION_LOG.md` (D-342–D-351) ve `docs/19_PRODUCTION_IMPLEMENTATION_PLAN.md` (bu belgenin kendisi, ilgili bölümlerin senkronize edilmesi).
* **Üretilecek domain/application olayları:** Yok.
* **Testler:** Yok (kod yok).
* **Kapanış koşulları ve gerçek durumu:**
  1. **Proleptic Gregorian GameDate modeli kabul edildi** — ✅ Karşılandı (D-343). Authoritative tip immutable `GameDate`; canonical temsil integer `DayNumber`; proleptic Gregorian artık yıl ve standart ay uzunlukları desteklenir; `.NET DateOnly` yalnız adapter'dır.
  2. **Günlük granularity kabul edildi** — ✅ Karşılandı (D-344). MVP calendar granularity bir oyun günüdür; domain zamanı timezone/DST/saat/dakika/saniye taşımaz.
  3. **Same-day ordering kabul edildi** — ✅ Karşılandı (D-345). `ProcessingPhase`, priority, stable sequence ve stable ID ile belirlenir; `GameDate`, `SeasonId`, `SimulationStep` ve wall-clock timestamp ayrı kavramlardır; negatif zaman ilerletme normal domain command olarak desteklenmez.
  4. **Exact SDK version kanıtlandı ve karar kaydına işlendi** — ❌ **Karşılanmadı.** Target Framework (`net10.0`) kapatıldı (D-346), ancak exact SDK sürümü kanıtlanamadı (bkz. Bölüm 3 madde 3 ve `docs/15_DECISION_LOG.md` D-347). Bu tek madde Kart 0'ın genel durumunu Bloke yapar.
  5. **Manuel composition root kararı kabul edildi** — ✅ Karşılandı (D-348).
  6. **Third-party container kararı kabul edildi** — ✅ Karşılandı (D-349 — başlangıçta kullanılmayacak).
* **Kabul kriterleri:** Yukarıdaki altı koşuldan beşi `docs/15_DECISION_LOG.md`'de "Kabul edildi" kaydına sahiptir; madde 4 **kanıt yetersizliği nedeniyle "Açık"** kaydına sahiptir (D-347) — **kart genel olarak karşılanmamıştır, Bloke'dur.**
* **Geri alma stratejisi:** D-342–D-351 kayıtlarını ve bu bölümdeki güncellemeleri geri almak (yalnızca belge revert).
* **Sonraki kart için önkoşul:** Kart 0 **tamamlanmamıştır**; Production Kart 1 **"başlatılabilir" olarak gösterilmez** (Bölüm 11, Kural 13). Blokun kaldırılması için ayrı, küçük bir "exact SDK pin" konfigürasyon kartı gereklidir (Bölüm 7 madde 7 ile uyumlu).

### Production Kart 1 — Saf Domain Zaman Value Object'leri

**Ön koşul durumu: karşılanmamış.** Kart 0 hâlâ Bloke durumundadır (exact SDK pin kanıtı eksik, Bölüm 7 madde 7). Bu kart, Kart 0'ın blokunun kalkması ve `docs/15_DECISION_LOG.md`'de exact SDK kararının kapanmasından ÖNCE başlatılamaz.

* **Amaç:** `GameDate`, `SimulationStepId` gibi saf, framework'ten bağımsız Domain value object'lerini, Kart 0'da kapatılan proleptic Gregorian `DayNumber` modeline göre oluşturmak.
* **Ön koşul:** Kart 0 tamamlanmış (exact SDK kararı dahil), `global.json` için ayrı bir küçük konfigürasyon kartı tanımlanmış/tamamlanmış, çalışma ağacı temiz, kararlar commitlenmiş.
* **Kapsam içi:** Yalnızca `FootballCareerSimulator.Domain` projesinde yeni value object'ler (Bölüm 5.2'deki kavramlara karşılık gelen; canonical `DayNumber` temsili dahil); `Domain/SimulationStep.cs`'in (Kart 0 placeholder'ı — dikkat: bu isim, karar günlüğündeki Production Kart numaralarıyla karıştırılmamalıdır) silinmesi.
* **Kapsam dışı:** Godot, SQLite, UI; Application use case'leri; `Spike1Placeholder`/`Spike4Placeholder` namespace'lerine dokunma.
* **Etkilenecek dosyalar:** Yeni `Domain/WorldCalendar/` (veya eşdeğer) klasörü; `Domain/SimulationStep.cs` silinir; `PlaceholderSkeletonTests.cs` içindeki `SimulationStep` referansları güncellenir veya bu test dosyası bu noktada elenir (Bölüm 6).
* **Üretilecek domain/application olayları:** Yok (value object'ler event üretmez).
* **Testler:** Domain unit testleri (Bölüm 5.11 "Domain Unit Tests").
* **Kabul kriterleri:** Yeni value object'ler invariant'larıyla (Bölüm 5.7) test edilmiş; `dotnet test` tüm çözümde yeşil; `Spike1Placeholder` hâlâ dokunulmamış ve çalışır durumda.
* **Geri alma stratejisi:** Yeni namespace'i silmek; `SimulationStep.cs` geri getirilebilir (git revert).
* **Sonraki kart için önkoşul:** Kart 1'in bütün value object'leri ve testleri yeşil olmadan Kart 2 başlamaz.

### Production Kart 2 — Calendar State ve Zaman İlerletme Domain Davranışı

* **Amaç:** World & Calendar'ın authoritative aggregate/entity'sini (Bölüm 5.2, `docs/03_DOMAIN_MODEL.md` Bölüm 7.1 aggregate adayları) ve invariant'larını (Bölüm 5.7) oluşturmak.
* **Ön koşul:** Kart 1.
* **Kapsam içi:** Domain aggregate/entity (`WorldTimeline`/`SimulationState` adayları), invariant kontrolü, minimal Domain Event tanımları (Bölüm 5.5 — yalnızca en az `GameTimeAdvanced` ve `PlanningPeriodStarted`/`Completed`), Application/Infrastructure YOK.
* **Kapsam dışı:** SQLite persistence, Godot, gerçek Event & Rule Evaluation pipeline'ı (event'ler bu kartta yalnızca in-memory bir liste olarak toplanabilir; tam causation/correlation altyapısı Kart 4'e bırakılır).
* **Etkilenecek dosyalar:** Kart 1'in namespace'i genişler; henüz `Spike1Placeholder` silinmez (hâlâ testler ona bağımlı olabilir).
* **Üretilecek domain/application olayları:** `GameTimeAdvanced`, `GameDayStarted`, `GameDayCompleted`, `PlanningPeriodStarted`, `PlanningPeriodCompleted` (Bölüm 5.5).
* **Testler:** Domain unit + invariant testleri (Bölüm 5.11); Bölüm 5.10'daki "Tarih ve takvim" + "Command validation" kategorisi sınır durumları.
* **Kabul kriterleri:** Aggregate, Bölüm 5.7'deki tüm invariant'ları test kapsıyor; aynı command iki kez uygulanınca idempotent davranış kanıtlanmış; `dotnet test` yeşil.
* **Geri alma stratejisi:** Kart 2'nin eklediği dosyaları silmek; Kart 1 state'ine dönmek.
* **Sonraki kart için önkoşul:** Kart 2'nin invariant testleri yeşil olmadan Kart 3 başlamaz. **Bu kart tamamlandığında `Spike1Placeholder` ve ilişkili testler kaldırılabilir hâle gelir (Bölüm 6), ancak kaldırma işlemi ayrı, açık bir alt-adım olarak yapılmalı ve CI'da doğrulanmalıdır.**

### Production Kart 3 — Application Command/Query Sınırı

* **Amaç:** `AdvanceSimulationTime` command'i, `CurrentGameDate`/`CurrentPlanningPeriod` query'leri ve Bölüm 5.6'daki Blocker Aggregator contract'ını oluşturmak.
* **Ön koşul:** Kart 2.
* **Kapsam içi:** `FootballCareerSimulator.Application` projesinde yeni use case'ler; Bölüm 3 madde 6/7'deki composition root ve read model sınırı kararlarının fiziksel implementasyonu. Mimari yaklaşımın TAMAMI (manuel composition root, constructor injection, third-party container kullanılmaması, host-başına composition root — D-348, D-349) Kart 0'da kapatıldı; bu kartın kendi görevi yalnızca exact registration kodunu ve host-başına composition root dosyalarını (Godot Presentation host, headless simulation runner, test host/factory) yazmaktır — yeni bir mimari seçim yapılmaz.
* **Kapsam dışı:** Gerçek başka bounded context'lerin blocker query'lerinin implementasyonu (henüz yoklar); bu kartta Blocker Aggregator, henüz var olmayan context'ler için test-only stub/sahte (fake, gerçek değil) implementasyonlarla çalışır ve bu açıkça belgelenir.
* **Etkilenecek dosyalar:** Yeni `Application/WorldCalendar/` (veya eşdeğer) klasörü; `AdvancePlaceholderSimulationUseCase.cs` (Kart 0) bu kartta silinir.
* **Üretilecek domain/application olayları:** `TimeAdvanceBlocked` (Application seviyesinde, Bölüm 5.5).
* **Testler:** Application/use case testleri (Bölüm 5.11 "Application Tests").
* **Kabul kriterleri:** Command/query DTO'ları Domain/Simulation tiplerini presentation'a sızdırmıyor (statik kontrol/test); blocker davranışı stub context'lerle test edilmiş.
* **Geri alma stratejisi:** Yeni Application namespace'ini silmek.
* **Sonraki kart için önkoşul:** Kart 3'ün command/query testleri yeşil olmadan Kart 4 başlamaz.

### Production Kart 4 — Deterministic Simulation Entegrasyonu

* **Amaç:** Olay sırasını (Simulation Phase, Bölüm 5.1), Simulation koordinasyonunu ve tekrar üretilebilirliği (Bölüm 3 madde 1'deki RNG stream stratejisi kararıyla birlikte) entegre etmek; uzun dönem headless test.
* **Ön koşul:** Kart 3. **Ayrıca Bölüm 3 madde 1'deki (RNG stream stratejisi) kararın bu karttan önce netleşmiş olması gerekir.**
* **Kapsam içi:** `SimulationRandomContext`'in (Bölüm 6, sınıflandırma D) production World & Calendar'a bağlanması; `tools/FootballCareerSimulator.SimulationRunner`'ın gerçek World & Calendar'ı çalıştıracak şekilde güncellenmesi; minimal Event & Rule Evaluation pipeline'ının (yalnızca causation/correlation/idempotency — Bölüm 4.4) bu kapsamda ilk kez gerçek biçimde ortaya çıkması.
* **Kapsam dışı:** SQLite persistence (Kart 5); Godot UI (Kart 6); tam Event & Rule Evaluation (bütün 14 context'in event'lerini işleyen genel motor) — yalnızca World & Calendar'ın kendi event'lerini işleyen minimal kesit.
* **Etkilenecek dosyalar:** `tools/FootballCareerSimulator.SimulationRunner/Program.cs` güncellenir (silinmez, Bölüm 6).
* **Üretilecek domain/application olayları:** Kart 2'deki event'lerin artık gerçek CorrelationId/CausationId/SimulationStepId ile üretilmesi.
* **Testler:** Simulation testleri (Bölüm 5.11 "Simulation Tests" + "Uzun Dönem Testleri" — en az 10 sezon headless).
* **Kabul kriterleri:** Aynı seed + aynı komut dizisi aynı canonical semantic sonucu üretiyor (Spike 2 tekniğiyle doğrulanmış); 10 sezonluk headless çalıştırma exception vermeden tamamlanıyor.
* **Geri alma stratejisi:** Kart 4'ün eklediği entegrasyon kodunu silmek; Kart 3 state'ine dönmek.
* **Sonraki kart için önkoşul:** Kart 4'ün determinizm ve 10-sezon testleri yeşil olmadan Kart 5 başlamaz.

### Production Kart 5 — Persistence ve Save Schema Entegrasyonu

* **Amaç:** Snapshot, schema version, migration; corruption ve round-trip testleri.
* **Ön koşul:** Kart 4. **Ayrıca Bölüm 3 madde 2/4/8/9'daki (save schema, persistence provider, event persistence, migration politikası) kararların netleşmiş olması gerekir.**
* **Kapsam içi:** `FootballCareerSimulator.Infrastructure`'da gerçek World & Calendar save/load port implementasyonu; minimal Save Integrity kesiti (Bölüm 4.4); Spike 3'ün tekniklerinin (atomik yazma, backup+swap migration, Bölüm 6) YENİDEN KULLANILMASI, şemanın YENİDEN TASARLANMASI.
* **Kapsam dışı:** Diğer 13 context'in save'e dahil edilmesi (henüz yoklar); yalnızca World & Calendar + gerekli minimal manifest.
* **Etkilenecek dosyalar:** Yeni Infrastructure sınıfları (mevcut Spike 3 sınıflarıyla AYNI isim alanında değil — Bölüm 6 "F: geçici compatibility adapter" notuna göre, geçiş süresinde ikisi bir arada bulunabilir).
* **Üretilecek domain/application olayları:** Save/load ile ilgili teknik event'ler (Domain Event değil, Bölüm 4.4/4.7 ayrımına uygun audit/technical kayıt).
* **Testler:** Infrastructure testleri (Bölüm 5.11 "Infrastructure Tests").
* **Kabul kriterleri:** Round-trip semantic eşdeğerlik; bozuk save reddi; migration testi (üretim şema sürümleri arası, spike şeması DEĞİL) yeşil.
* **Geri alma stratejisi:** Yeni Infrastructure sınıflarını silmek; Kart 4 state'ine dönmek (spike SQLite sınıfları hâlâ mevcut olduğundan CI kırılmaz).
* **Sonraki kart için önkoşul:** Kart 5'in save/load testleri yeşil olmadan Kart 6 başlamaz. **Bu kart tamamlandığında spike SQLite sınıfları ve `Spike3SqliteSaveLoadTests`/`LegacySaveFixture` kaldırılabilir hâle gelir (Bölüm 6).**

### Production Kart 6 — Minimum Godot Zaman Kontrolü

* **Amaç:** Yalnız Application contract'ları üzerinden çalışan küçük bir UI; gerçek oyun ekranı tasarımı yok.
* **Ön koşul:** Kart 5.
* **Kapsam içi:** Godot `Presentation` projesinde, Kart 3'ün Command/Query'lerini çağıran minimal bir ekran (güncel tarih gösterimi + "ilerlet" düğmesi + blocker mesajı); `PlayerListScreen`'in prototype'a taşınması (Bölüm 6).
* **Kapsam dışı:** Haftalık kontrol merkezi, kadro ekranı, taktik ekranı gibi gerçek oyun ekranları (bunlar ayrı, sonraki context'lerin kapsamıdır).
* **Etkilenecek dosyalar:** Yeni Presentation sahnesi/script'i; `Shell.cs`/`Shell.tscn` silinir (Bölüm 6); `PlayerListScreen.*` `prototypes/`e taşınır.
* **Üretilecek domain/application olayları:** Yok (Presentation event üretmez, yalnızca command çağırır).
* **Testler:** Presentation testleri (Bölüm 5.11 "Presentation Tests").
* **Kabul kriterleri:** UI, Domain/Simulation tipi import etmiyor (statik kontrol); "ilerlet" düğmesi doğru command'i çağırıyor; blocker mesajı gösteriliyor; Godot editöründe ve headless'te hatasız açılıyor (Kart 5/6/7/8 spike desenleri yeniden kullanılabilir).
* **Geri alma stratejisi:** Yeni sahneyi silmek; `PlayerListScreen`'i geri taşımak.
* **Sonraki kart için önkoşul:** Kart 6 tamamlandığında World & Calendar'ın ilk üretim dikey kesiti biter; bir sonraki bounded context için YENİ bir planlama görevi (Bölüm 4.5) gereklidir — bu belge o planlamayı içermez.

---

## 8. Dosya Etki Haritası

Bu harita **kavramsaldır**; bu görevde hiçbir gerçek dosya oluşturulmamıştır.

### İlk üç kart için olası dosya/klasör etkileri

| Kart | Olası yeni klasör/dosya kategorisi |
|---|---|
| Production Kart 0 | Yok (yalnızca `docs/15_DECISION_LOG.md`'ye yeni kayıtlar) |
| Production Kart 1 | Yeni Domain namespace (örn. `src/FootballCareerSimulator.Domain/WorldCalendar/`); `Domain/SimulationStep.cs`'in silinmesi; Tests projesinde yeni Domain unit test dosyaları |
| Production Kart 2 | Kart 1 namespace'inin genişlemesi (aggregate/entity dosyaları); Tests projesinde yeni invariant test dosyaları; henüz `Spike1Placeholder` silinmez |
| Production Kart 3 | Yeni Application namespace (örn. `src/FootballCareerSimulator.Application/WorldCalendar/`); `Application/AdvancePlaceholderSimulationUseCase.cs`'in silinmesi; yeni Application test dosyaları; composition root için olası yeni bir küçük dosya (Kart 0'ın kararına bağlı) |

### İlk üç kartta KESİNLİKLE değiştirilmemesi gereken mevcut dosyalar

* `src/FootballCareerSimulator.Infrastructure/*` (tüm dosyalar) — Kart 5'e kadar dokunulmaz.
* `src/FootballCareerSimulator.Presentation/*` (tüm dosyalar) — Kart 6'ya kadar dokunulmaz.
* `tools/FootballCareerSimulator.SimulationRunner/Program.cs` — Kart 4'e kadar dokunulmaz.
* `src/FootballCareerSimulator.Domain/Spike1Placeholder/*`, `src/FootballCareerSimulator.Simulation/Spike1Placeholder/*` — Kart 2 tamamlanıp gerçek karşılığı doğrulanana kadar dokunulmaz (Bölüm 6 genel geçiş ilkesi).
* `src/FootballCareerSimulator.Application/Spike4Placeholder/*` — bu dikey kesitte hiç dokunulmaz (Bölüm 6).
* `.github/workflows/ci.yml` — bu üç kartta CI değişikliği gerekmez; mevcut `dotnet` ve `godot-headless` job'ları yeni testleri otomatik olarak kapsar.
* `docs/00_PROJECT_INDEX.md`, `docs/15_DECISION_LOG.md` dışındaki tüm `docs/*.md` dosyaları — Production Kart 0-3 kapsamında domain/mimari kararı DEĞİŞMEZ, dolayısıyla bu belgeler güncellenmez.

---

## 9. Riskler

| # | Risk | Olasılık | Etki | Azaltma | Hangi kartta ele alınır |
|---|---|---|---|---|---|
| 1 | Takvim modelinin erken aşırı genelleştirilmesi (örn. çok ülke/çok takvim desteği için gereksiz esneklik) | Orta | Orta | Bölüm 5.7'deki üç seçenekten yalnızca MVP'nin gerektirdiğini (1 kurgusal ülke, `docs/02_MVP_SCOPE.md` Bölüm 17.1) seçmek; GDD Kural 8 (her özellik MVP/genişletilmiş/nihai kategorisine atanır) | Kart 0 (karar), Kart 1 (implementasyon) |
| 2 | Simulation tick ile game date'in birbirine karıştırılması | Orta | Yüksek | Bölüm 6.4'teki net ayrım (`Simulation Step` ≠ frame/tick); Kart 1'de bu ikisinin AYRI value object'ler olarak tasarlanması | Kart 1, Kart 2 |
| 3 | Event fırtınası (event storm) | Düşük (World & Calendar'ın kendi event hacmi düşüktür) | Yüksek (eğer oluşursa) | Bölüm 5.10 madde 20; `docs/12_WORLD_SIMULATION.md` Bölüm 31 güvenlik limitleri | Kart 4 |
| 4 | Bounded context'lerin zaman servisine aşırı bağlanması (World & Calendar'ın gizli bir "god object" hâline gelmesi) | Orta | Yüksek | Bölüm 4.4, Bölüm 5.6 — World & Calendar foreign state okumaz/yazmaz; yalnızca orkestrasyon | Kart 3 (Blocker Aggregator tasarımı) |
| 5 | Save schema'nın erken donması (Spike 3 şemasının yanlışlıkla kalıcı sayılması) | Orta-Yüksek (görev talimatının özellikle uyardığı risk) | Yüksek | Bölüm 5.9'daki açık ret; Bölüm 3 madde 2; Kart 5'in "şema YENİDEN TASARLANIR" kabul kriteri | Kart 5 |
| 6 | Placeholder modellerin production'a sızması (isim değiştirerek "yükseltme") | Orta | Yüksek | Bölüm 6 "Genel Geçiş İlkesi"; Kart 1'in `SimulationStep.cs` silme adımı; ayrı namespace kuralı | Kart 1-5 (her kartta) |
| 7 | UI'ın Simulation nesnelerini doğrudan oluşturması | Düşük (immutable kural #1/#6 zaten net) | Yüksek | Bölüm 5.8 contract sınırı; Kart 6'nın statik kontrol testi | Kart 3 (contract tasarımı), Kart 6 (doğrulama) |
| 8 | Deterministic RNG ve zaman state'inin ayrışması (ikisinin farklı checkpoint'lerde commit edilmesi) | Orta | Yüksek | Bölüm 5.9 "RNG state ile zaman state'inin atomik kaydı"; Kart 5'in atomik yazma tekniği | Kart 4, Kart 5 |
| 9 | Uzun dönem testlerinin yavaşlaması (10-20 sezon, yüz binlerce step) | Orta | Orta | Spike 1'in zaten kanıtladığı performans profili (1 ms/10 sezon, yer tutucu ölçekte); gerçek domain modeliyle yeniden ölçüm | Kart 4 |
| 10 | Sezon geçişlerinde kısmi state güncellemesi | Düşük (World & Calendar Season'ın sahibi değildir, bu risk esas olarak Competition'a aittir) | Yüksek (eğer World & Calendar sonrası context'lerde oluşursa) | Bölüm 4.4; Season Transition Process'in (`docs/12_WORLD_SIMULATION.md` Bölüm 24) World & Calendar'ın kapsamı DIŞINDA olduğunun netliği | Bu dikey kesitin kapsamı dışında — not olarak taşınır |
| 11 | Blocker mekanizmasının merkezi monolite dönüşmesi | Orta | Orta | Bölüm 5.6'daki üç seçenek karşılaştırması; "Blocker Aggregator" query deseninin (foreign mutation yasağı korunarak) tercih edilmesi | Kart 3 |

---

## 10. Definition of Done

Bu plan belgesi, aşağıdaki koşulların TAMAMI karşılandığında tamamlanmış sayılır:

* [x] Bounded context sırası gerekçelendirilmiş (Bölüm 4.3, üç bağımsız belge kaynağıyla).
* [x] İlk dikey kesit seçilmiş (World & Calendar, Bölüm 4.3-4.4).
* [x] Sistem amacı/verisi/bağımlılıkları/olayları tanımlanmış (Bölüm 5.1-5.6).
* [x] Sınır durumları çıkarılmış (Bölüm 5.10, 20 madde).
* [x] Test matrisi hazırlanmış (Bölüm 5.11, 6 katman).
* [x] Placeholder geçiş stratejisi belirlenmiş (Bölüm 6).
* [x] Çalışma kartları küçük ve geri alınabilir (Bölüm 7, 7 kart, her biri tek PR ölçeğinde).
* [x] Açık kararlar görünür bırakılmış (Bölüm 3, 10 madde; hiçbiri sessizce kapatılmadı).
* [x] Hiçbir production kodu yazılmamış (bu görev boyunca `git diff --name-only` ile doğrulanacak, bkz. aşağıdaki rapor).
* [x] Dokümanlar birbiriyle çelişmiyor (Bölüm 4.3'teki üç kaynak birbirini doğruluyor; hiçbir çelişki tespit edilmedi).

---

## 11. Sonraki Adım

**Production Kart 0, "Bloke — exact .NET SDK pin kanıtı eksik" durumundadır** (bkz. Bölüm 7 ve `docs/15_DECISION_LOG.md` D-342–D-351). Altı kapanış koşulundan beşi karşılanmıştır: World & Calendar terminolojisi kilitlendi (D-342), takvim modeli proleptic Gregorian `DayNumber` olarak bağlayıcı biçimde kapatıldı (D-343), günlük granularity ve same-day ordering kapatıldı (D-344, D-345), Target Framework `net10.0` resmileştirildi (D-346), manuel composition root ve third-party container kullanılmaması kararları kapatıldı (D-348, D-349). **Ancak exact .NET SDK sürümü kanıtla doğrulanamamıştır** (D-347): yerel ortamda eşzamanlı olarak iki farklı SDK (`10.0.300`, `10.0.301`) kurulu bulunmuş ve CI yalnızca kayan bir `10.0.x` feature-band özelliği kullanmıştır; hiçbir spike kaydı tek bir exact sürümü sabitlememiştir.

Bu nedenle **Production Kart 1 bu belge tarafından "başlatılabilir" olarak gösterilmez.** Bu belge onaylandıktan sonraki en küçük mantıklı adım, Production Kart 1'in KENDİSİ DEĞİL, Kart 0'ın blokunu kaldıracak **küçük, ayrı bir "exact .NET SDK pin" konfigürasyon kartı**dır: bir CI çalıştırmasının veya kontrollü yerel ortamın exact SDK sürümünü (`10.0.xxx`) sabitleyip kanıtla `docs/15_DECISION_LOG.md`'ye işlemek (D-347'yi "Kabul edildi" durumuna taşımak) ve gerekirse `global.json` oluşturmak — **bu adım bu belgenin ve bu görevin kapsamı dışındadır.**

Bu adımdan önce:

* Production Kart 1 **başlatılmamıştır ve başlatılamaz** — Kart 0 Bloke durumdayken hiçbir sonraki kart "hazır" sayılmaz,
* `Spike1Placeholder`/`Spike4Placeholder` namespace'leri kaldırılmamalı,
* Bölüm 3'teki kalan açık kararlar (madde 1, 2, 3'ün exact SDK alt maddesi, 4, 7, 8, 9, 10) sessizce kapatılmamalı,
* Bölüm 8'de listelenen açık kararlar (RNG algoritması, RNG stream listesi, üretim SQLite şeması, migration formatı, exact SQLite provider/paket sürümü, exact season başlangıç tarihi, fixture takvim tarihleri, exact namespace/klasör yapısı, exact command/event sınıf listesi, DI container paketi, persistence repository interface listesi) kapatılmamalı,
* GDD, MVP kapsamı veya kesinleşmiş alt sistem belgeleri değiştirilmemelidir.
