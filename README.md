# Football Career Simulator

## Canlı Süper Lig kadro kontrolü

Kadro veri paketini canlı kaynaklarla karşılaştırmak için:

```powershell
./tools/Update-TurkeySuperLig202627DataPack.ps1 -SkipAssets -CheckOnly -AsJson
```

Kontrol endpoint'ini çalıştırmak için:

```powershell
dotnet run --project tools/FootballCareerSimulator.DataRefreshApi
```

Adres: `http://127.0.0.1:5287` (kök, durum endpoint’ine yönlendirir; Kestrel varsayılanı `localhost:5000` kullanılmaz).
`GET /api/data-refresh/turkey-super-lig-2026-27/status` canlı kadroları 15 dakikalık
önbellekle kontrol eder; yeni/ayrılan futbolcuları ve `UpdateRequired` durumunu JSON döndürür.
Güncelleme gerektiğinde veri paketini yeniden üretmek için:

```powershell
./tools/Update-TurkeySuperLig202627DataPack.ps1 -SkipAssets
```

Uzun soluklu, oyuncunun kararlarını, ilişkilerini ve geçmişini yıllarca hatırlayan; futbolcu veya teknik direktör olarak saha içi ve saha dışı gerçek bir futbol kariyeri yaşatan, sistemik bir futbol kariyeri ve yaşam simülasyonu oyunu.

## Proje Durumu

Kişisel kullanım için geliştirilen, oynanabilir dikey kesit aşamasında bir teknik direktör kariyer simülasyonu. Çekirdek döngü: **kariyer merkezi → hafta (antrenman, kadro, transfer) → maç günü → maç → sonuç → hafıza ve ilişki tepkisi → sezon geçişi**.

| Alan | Durum (2026-11-09) |
|---|---|
| Test kapısı | 1256 hızlı test + 10 sezon soak (nüfus sağlığı kontrolüyle) yeşil |
| Godot | `CAREER_UI_SMOKE_TEST_RESULT=PASS`, çözüm derlemesi 0 hata/0 uyarı |
| Kayıt | SQLite **V51**; V1'den itibaren otomatik migration ve canonical hash bütünlük kontrolü |
| Araç zinciri | `net9.0`, Godot 4.7.x mono/.NET, `Microsoft.Data.Sqlite 10.0.9` |

### Oyunda çalışan sistemler

- **Maç ve hazırlık:** Maç günü, devre arası müdahale ve değişiklik, 2D saha anları, rakip dosyası, eşleşme planı, gelişmiş taktik tahtası. Rol uyumu maç gücüne -4..+2 etki eder.
- **Kadro:** Gerçek mevkiler (12 rol), serbest ilk 11/yedek kurma, sakatlık ve yük yönetimi, takım kimyası göstergesi.
- **Oyuncu gelişimi:** SinWave büyüme eğrisi, mevkiye göre yaşlanma (kaleci geç, kanat erken), tesis seviyesine bağlı antrenman çarpanı, genç akademisi.
- **Ekonomi ve transfer:** Scout, kısa liste, teklif ve onay zinciri. SM25 araştırmasından türetilen ücret, piyasa değeri ve sözleşme görüşmesi modeli (sabır, ret nedenleri, imza parası).
- **Menajer kariyeri:** Yönetim güveni, kovulma ve iş teklifleri, itibar, XP/seviye ve beceriler. Pazarlık becerisi ücret talebini düşürür.
- **Kulüp kimliği (Club DNA):** Her kulübün kalıcı karakteri (sabır, altyapı, transfer, maaş kültürü). Sabırsız yönetim yenilgiye sert tepki verir.
- **Hafıza ve ilişkiler:** Verilen sözler, seçim ve kadro dışı kalma anıları, güven. Kırgın bir oyuncuya ilk 11'de şans vermek anıyı telafi eder; kritik maçta tamamen, diğer maçlarda kısmen (Redemption Memory).
- **Avrupa kupası:** Grup aşaması ve elemeler, primler, kayda yazılan sonuçlar.

### Bilinçli olarak kapsam dışı

Futbolcu kariyeri modu (D-028), uzun diyalog ağaçları (D-118), Post-MVP Faz 1, 2, 5, 6, 8, 9 ve 10 servislerinin oyuna bağlanması (D-421; Faz 3 ve 4 bağlandı, D-423).

Hedef yığın ve mimari: `docs/17_TECHNOLOGY_AND_ARCHITECTURE_DECISION.md`. Güncel ayrıntılı durum: `docs/19_PRODUCTION_IMPLEMENTATION_PLAN.md` kontrol noktası.

## Ana Referans Belge

Projenin tüm tasarım kararlarının başlangıç noktası:

- [`docs/01_GAME_DESIGN_DOCUMENT.md`](docs/01_GAME_DESIGN_DOCUMENT.md)

Dokümantasyonun tamamına genel bakış için:

- [`docs/00_PROJECT_INDEX.md`](docs/00_PROJECT_INDEX.md)

Teknoloji ve mimari kararı için:

- [`docs/17_TECHNOLOGY_AND_ARCHITECTURE_DECISION.md`](docs/17_TECHNOLOGY_AND_ARCHITECTURE_DECISION.md)

Alınan tüm kararların günlüğü için:

- [`docs/15_DECISION_LOG.md`](docs/15_DECISION_LOG.md)

## Çalıştırma

```bat
run-game.cmd
```

Komut, Presentation projesini yeniden derler ve yeni bir oyun penceresi açar. Godot 4.7 mono kurulumu `C:\Godot` altında otomatik bulunur; farklı bir yerdeyse `GODOT_EXE` ortam değişkenini ayarla. Yalnız derleme için `run-game.cmd --build-only`. Ana sahne: `src/FootballCareerSimulator.Presentation/CareerAppRoot.tscn`.

## Test Kapıları

Hızlı geliştirme kapısı:

```powershell
dotnet test tests\FootballCareerSimulator.Tests\FootballCareerSimulator.Tests.csproj --filter "Category!=LongRunning"
```

10 sezonluk production soak kapısı (Debug yerel baseline: yaklaşık 7 dakika, bütçe: 10 dakika):

```powershell
dotnet test tests\FootballCareerSimulator.Tests\FootballCareerSimulator.Tests.csproj --filter "Category=LongRunning"
```

## Geliştirme Kuralları (Definition of Done)

Her değişiklik için:

1. **Yeni kalıcı alan** → `CareerSqlitePersistence` yazma/okuma, migration ve canonical hash. `Quality/CoreLoopGuardTests` kayıt döngüsü testi bunu yakalar.
2. **Presentation store'a doğrudan yazmaz** → yazımlar Application servislerinden geçer. Mimari koruma testi bunu denetler.
3. **Yeni formül veya denge kuralı** → `docs/15_DECISION_LOG.md` kaydı. Araştırmadan geliyorsa `research/SM25` belgesi de güncellenir.
4. Hızlı test kapısı ve 10 sezon soak yeşil. UI değiştiyse Godot duman testi de yeşil olmalı.

## Sonraki Adımlar

Özellik dondurma: Yeni sistem eklemek yerine çekirdek döngünün hissi ve dengesi. Öncelik, uzun bir kariyeri oynayıp notlanan akış, okunabilirlik ve denge bulgularıdır.

## Klasör Yapısı

```
Football_Career_Simulator/
├── README.md
├── FootballCareerSimulator.slnx           # Ana .NET çözümü
├── Directory.Build.props                  # Projeler arası ortak derleme ayarları
├── run-game.cmd                           # Godot ile Presentation'ı açar
├── docs/          # Tasarım ve planlama dokümanları
├── src/
│   ├── FootballCareerSimulator.Domain/         # Domain katmanı (dış teknolojiye bağımlı değil)
│   ├── FootballCareerSimulator.Simulation/     # Simulation katmanı
│   ├── FootballCareerSimulator.Application/    # Application / use case katmanı
│   ├── FootballCareerSimulator.Infrastructure/ # SQLite career save/load / migration
│   └── FootballCareerSimulator.Presentation/   # Godot 4 .NET (menü / hub / maç günü / sonuç)
├── tests/
│   └── FootballCareerSimulator.Tests/          # xUnit test projesi
├── tools/
│   └── FootballCareerSimulator.SimulationRunner/  # Headless simülasyon aracı
├── builds/        # Export çıktıları (git tarafından yok sayılır)
├── assets/        # Oyun varlıkları (henüz boş)
└── prototypes/    # Küçük prototipler (henüz boş)
```

Windows export: `src/FootballCareerSimulator.Presentation` içinde  
`godot --headless --export-release "Windows Desktop x86_64" ../../builds/windows/FootballCareerSimulator.exe`  
(Godot 4.7-stable mono export şablonları gerekli). CI: `.github/workflows/ci.yml` (`dotnet` + `godot-headless` job'ları).
