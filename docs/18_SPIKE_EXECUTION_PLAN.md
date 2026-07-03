# Teknik Doğrulama Spike Planı ve Çalışma Kartları

**Belge:** `docs/18_SPIKE_EXECUTION_PLAN.md`
**Durum:** Kesinleşti (uygulama sırası)
**Ana referans:** `docs/01_GAME_DESIGN_DOCUMENT.md`
**Mimari ve spike tanımları:** `docs/17_TECHNOLOGY_AND_ARCHITECTURE_DECISION.md` (Bölüm 16, D-040)
**İlgili karar günlüğü:** `docs/15_DECISION_LOG.md`

---

## 1. Belgenin Amacı

Bu belge, `docs/17_TECHNOLOGY_AND_ARCHITECTURE_DECISION.md` Bölüm 20'de önerilen "bir sonraki en küçük adım"ı — altı teknik spike'ın sırasını ve çalışma kartlarını — hazırlar.

Bu belge:

* yeni bir domain veya oyun sistemi tasarlamaz,
* `docs/17_TECHNOLOGY_AND_ARCHITECTURE_DECISION.md` içinde tanımlanan spike kapsamlarını, başarı kriterlerini veya "başarısız olursa etkilenecek karar" notlarını değiştirmez ya da gevşetmez,
* GDD veya MVP kapsamını değiştirmez,
* exact sürüm, exact eşik, exact format veya exact CI workflow ayrıntısı kesinleştirmez; bunlar ilgili kartın kendisi yürütülürken üretilir,
* geniş bir üretim scaffold'u tanımlamaz.

Bu belgenin tek işi, zaten kesinleşmiş altı spike'ın hangi sırayla ve hangi küçük, geri alınabilir adımlarla yürütüleceğini planlamaktır.

---

## 2. Genel Çalışma Kuralları

1. Aynı anda yalnızca bir kart aktif olur (`01_GAME_DESIGN_DOCUMENT.md` Kural 3 ile uyumlu — paralel çok sistem/çalışma açılmaz).
2. Bir kart, kendi küçük ve geri alınabilir biriminde (ayrı branch/PR) tamamlanır; bir sonraki kart başlamadan mevcut kart derlenebilir ve çalışır durumda bırakılır.
3. Bir kart, yalnızca `docs/17_TECHNOLOGY_AND_ARCHITECTURE_DECISION.md` Bölüm 16'da o spike için tanımlanmış başarı kriterlerinin tamamı karşılandığında "tamamlandı" sayılır.
4. Bir spike başarısız olursa, ilgili spike için Bölüm 16'da tanımlanmış "başarısız olursa etkilenecek karar" notu referans alınır; sonuç sessizce atlanmaz, `docs/15_DECISION_LOG.md`'ye yeni bir karar kaydı olarak işlenir.
5. Kartlar sırasında ortaya çıkan ve henüz açık bırakılmış kararlar (örn. D-072, D-283, D-284, D-329'daki exact implementasyon ayrıntıları) bu plan üzerinden sessizce kapatılmaz; yalnızca ilgili spike'ın ürettiği somut kanıtla kesinleştirilebilir.
6. Domain, Simulation, Application katmanları `docs/17_TECHNOLOGY_AND_ARCHITECTURE_DECISION.md` Bölüm 8'deki katman sınırlarını ilk karttan itibaren korur.

---

## 3. Benimsenen Sıra ve Gerekçe

Altı spike arasındaki bağımlılıklar dikkate alınarak aşağıdaki sıra benimsenmiştir:

* Spike 1 ve Spike 2 aynı headless .NET çekirdeği üzerinde çalışır; Spike 2, Spike 1'in ürettiği minimal dünya döngüsünü ve seed altyapısını kullanır.
* Spike 3, Spike 2'de kurulan canonical state kavramını SQLite kalıcılığına taşır.
* Spike 4 ve Spike 5, bir Godot projesinin var olmasını gerektirir; bu nedenle Domain/Simulation çekirdeği bir miktar olgunlaşmadan (Spike 1–3) başlatılmaları erken ve gereksiz risklidir.
* Spike 6 (CI), tamamı Godot'suz saf .NET testleri kapsayan bölümüyle en erken; Godot headless import/export doğrulamasını kapsayan bölümüyle ise bir Godot projesi ortaya çıktıktan sonra tamamlanabilir. Bu nedenle Spike 6 iki artımda ele alınır.

Bu gerekçeyle CI ve iskelet, numaralandırılmış altı spike'a ek olarak ayrı hazırlık/uzatma kartları şeklinde plana dahil edilmiştir; bunlar yeni bir spike tanımlamaz, yalnızca mevcut Spike 6'nın ve genel çalışma disiplininin yürütülüş biçimini planlar.

---

## 4. Çalışma Kartları

### Kart 0 — Minimum Repository İskeleti — Tamamlandı

**Ön koşul:** Yok.

**Amaç:** Spike 1'in çalışabileceği en küçük derlenebilir taban.

**Kapsam içi:**

* `docs/17_TECHNOLOGY_AND_ARCHITECTURE_DECISION.md` Bölüm 8'deki katman ayrımına uygun boş/minimal Domain, Simulation, Application ve Automated Tests projeleri.
* Tek bir çözüm (solution) dosyası; projelerin birbirine doğru yönde referans vermesi (Domain dışa bağımlı değil).
* Placeholder düzeyinde tek bir domain kavramı veya sabit-adım döngüsü (gerçek bounded context'lerin tam implementasyonu değil).

**Kapsam dışı:** Godot projesi, SQLite, gerçek domain modeli, UI.

**Kabul kriteri:** Çözüm derlenir; en az bir yer tutucu (placeholder) test çalışır ve geçer.

**Sonuç:** `FootballCareerSimulator.slnx` altında Domain/Simulation/Application/Tests projeleri oluşturuldu; `dotnet build` ve `dotnet test` başarılı (3/3 test geçti). Bkz. `docs/15_DECISION_LOG.md` D-331. Geçici hedef çerçeve `net10.0`'dır; exact pinleme Kart 2–4'e bırakılmıştır.

### Kart 1 — CI-lite (yalnız saf .NET) — Tamamlandı

**Ön koşul:** Kart 0.

**Amaç:** Spike 6'nın Godot'suz bölümünü en erken devreye almak; bundan sonraki her kart otomatik doğrulanır.

**Kapsam içi:** GitHub Actions Windows runner üzerinde restore/build/test adımları (`docs/17_TECHNOLOGY_AND_ARCHITECTURE_DECISION.md` Bölüm 16, Spike 6'nın saf .NET bölümüyle uyumlu).

**Kapsam dışı:** Godot headless import/export job'ı (Kart 8'de eklenir).

**Kabul kriteri:** Her push/PR'da CI otomatik çalışır; hatalı adım non-zero exit code ile başarısız olur.

**Sonuç:** `.github/workflows/ci.yml` eklendi — `master`'a push/PR ve manuel tetikleme (`workflow_dispatch`) ile Windows runner üzerinde restore/build (Release)/test çalışır; test sonuçları (`.trx`) artefact olarak saklanır. Adımlar yerel olarak da doğrulandı. Bkz. `docs/15_DECISION_LOG.md` D-332.

### Kart 2 — Spike 1: Motor bağımsız 10 sezonluk headless simulation — Tamamlandı

**Ön koşul:** Kart 0–1.

**Kapsam ve kabul kriterleri:** `docs/17_TECHNOLOGY_AND_ARCHITECTURE_DECISION.md` Bölüm 16, Spike 1 ile birebir aynıdır; burada tekrar edilmez.

**Not:** Bu kart, 14 bounded context'in tam implementasyonunu değil, mimarinin ve uzun dönem çalıştırılabilirliğin doğrulanmasına yetecek minimal bir dikey kesiti hedefler.

**Sonuç:** `src/FootballCareerSimulator.Domain/Spike1Placeholder` ve `src/FootballCareerSimulator.Simulation/Spike1Placeholder` altında yer tutucu bir dünya modeli (20 kulüp, 500 futbolcu), seeded `SimulationRandomContext` (D-058 ile uyumlu) ve `HeadlessSimulationRunner` eklendi; `tools/FootballCareerSimulator.SimulationRunner` konsol aracı Godot/UI olmadan çalıştırılıp seed=42 ile 10 sezonu 1 ms'de, ~0,07 MB bellekle tamamladı. `Spike1HeadlessTenSeasonSimulationTests` beş testle tek çalıştırmayı, 10 ardışık çalıştırmayı, performans bütçesini, bellek büyümesini ve seed determinizmini doğruluyor (8/8 test yeşil). Bkz. `docs/15_DECISION_LOG.md` D-333.

### Kart 3 — Spike 2: Deterministik sonuç ve seed doğrulaması

**Ön koşul:** Kart 2.

**Kapsam ve kabul kriterleri:** `docs/17_TECHNOLOGY_AND_ARCHITECTURE_DECISION.md` Bölüm 16, Spike 2 ile birebir aynıdır.

**Not:** Bu kartta kullanılan save/load, tam SQLite kalıcılığı değil; canonical state'in serileştirilip geri yüklenebildiğini kanıtlayacak en küçük mekanizmadır (D-276, D-294 ile uyumlu semantic/canonical state ayrımı korunur).

### Kart 4 — Spike 3: SQLite save/load, migration ve corruption davranışı

**Ön koşul:** Kart 3.

**Kapsam ve kabul kriterleri:** `docs/17_TECHNOLOGY_AND_ARCHITECTURE_DECISION.md` Bölüm 16, Spike 3 ile birebir aynıdır.

### Kart 5 — Minimum Godot Proje Kabuğu

**Ön koşul:** Kart 4.

**Amaç:** Spike 4 ve Spike 5'in çalışabileceği en küçük Godot 4 .NET proje kabuğu.

**Kapsam içi:** Boş/minimal bir Godot 4 .NET projesi; Presentation katmanının Domain/Simulation/Application'a yalnızca command/query ve read model üzerinden bağlandığını gösteren tek bir yer tutucu ekran.

**Kapsam dışı:** Gerçek oyun ekranları, sanat varlıkları, ayrıntılı sahne yapısı.

**Kabul kriteri:** Proje Godot editöründe açılır ve yer tutucu ekran hatasız çalışır.

### Kart 6 — Spike 4: 500 futbolculuk Godot UI listesi

**Ön koşul:** Kart 5.

**Kapsam ve kabul kriterleri:** `docs/17_TECHNOLOGY_AND_ARCHITECTURE_DECISION.md` Bölüm 16, Spike 4 ile birebir aynıdır.

### Kart 7 — Spike 5: Windows x64 export ve temiz ortam çalıştırma

**Ön koşul:** Kart 6.

**Kapsam ve kabul kriterleri:** `docs/17_TECHNOLOGY_AND_ARCHITECTURE_DECISION.md` Bölüm 16, Spike 5 ile birebir aynıdır.

### Kart 8 — CI Tamamlama (Spike 6'nın Godot bölümü)

**Ön koşul:** Kart 5–7.

**Amaç:** Spike 6'yı Bölüm 16'daki tam kapsamıyla tamamlamak.

**Kapsam içi:** CI'a Godot headless import ve export job'ının eklenmesi; saf .NET ve Godot doğrulamalarının aynı pipeline'da, ayrı adımlar olarak yer alması.

**Kabul kriteri:** `docs/17_TECHNOLOGY_AND_ARCHITECTURE_DECISION.md` Bölüm 16, Spike 6'daki tüm başarı kriterleri karşılanır.

---

## 5. Bu Plana Dahil Olmayanlar

Aşağıdakiler bu belgenin kapsamı dışındadır ve ilgili kart yürütülürken kendi küçük ve geri alınabilir adımlarıyla ele alınır:

* Domain modelin somut C# sınıf/arayüz tasarımı,
* Godot proje klasör/sahne yapısının ayrıntıları,
* CI workflow YAML'ının tam içeriği,
* exact Godot/.NET/SQLite provider sürüm pinlemesi (spike sonuçlarına göre yapılır, bkz. `docs/17_TECHNOLOGY_AND_ARCHITECTURE_DECISION.md` Bölüm 20).

---

## 6. Sonraki Adım

Kart 0 (Minimum Repository İskeleti) ile başlanması.
