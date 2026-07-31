# Football Career Simulator

Uzun soluklu, oyuncunun kararlarını, ilişkilerini ve geçmişini yıllarca hatırlayan; futbolcu veya teknik direktör olarak saha içi ve saha dışı gerçek bir futbol kariyeri yaşatan, sistemik bir futbol kariyeri ve yaşam simülasyonu oyunu.

## Proje Durumu

Dokümantasyon ve teknik spike aşaması tamamlanmıştır. Üretim implementasyonu **aktif ilerliyor**: World & Calendar, Competition, Match, Club Governance, Manager Career, Team Preparation (kadro özeti) ve birleşik Career SQLite kaydı kodlanmıştır.

Godot Presentation katmanında ince bir kariyer döngüsü vardır: **ana menü → kariyer merkezi → maç günü → maç sonuçları** (yeni kariyer / devam et, lig kurma, maç öncesi kadro onayı, ayrı maç günü kontrol noktası, son kadro/formasyon/yaklaşım dokunuşları, maç oynatma, temel maç raporu ve öne çıkan oyuncu, zaman ilerletme, kaydet/yükle). Bu bir görsel ürün UI'sı değil; oynanabilir dikey kesit kontrol yüzeyidir. `Spike1Placeholder` / `Spike4Placeholder` yalnızca eski spike kanıtı için durur; asıl oyun akışı üretim context'leri üzerinden yürür.

Hedef yığın: Windows 10/11 x64, Godot 4.7-stable (mono/.NET), C#, Godot'tan bağımsız saf .NET domain/simülasyon çekirdeği. Ayrıntı: `docs/17_TECHNOLOGY_AND_ARCHITECTURE_DECISION.md`.

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

Komut, Presentation projesini zorunlu olarak yeniden derler ve güncel assembly ile yeni bir oyun penceresi açar. Yalnız derleme doğrulaması için `run-game.cmd --build-only` kullanılabilir. Godot 4.7-stable mono gerekir. Ana sahne: `src/FootballCareerSimulator.Presentation/CareerAppRoot.tscn`.

## Sonraki Adımlar

İnce kariyer döngüsü (MVP: teknik direktör) + nabız→CTA + transfer CTA + **maç günü → HT → sonuç** + **sakatlık / iyileşme yolu** + **auto-swap** + **görsel XI şeridi** kilitlenmiştir (Bugün ↔ Ofis: **Hikâye** / **Hava** + “Sıradaki”; sakin haftada gün bazlı **Ofis Notu**). Futbolcu kariyeri ertelenmiştir (D-028). Uzun diyalog ağacı / kapsamlı gazeteci ağı açılmaz (D-118, `02_MVP_SCOPE`); D-150 transfer fiyat formüllerini açık bırakır. Sıradaki aday: Ofis notunu Bugün nabız satırına da yansıtmak veya sakin haftada küçük staff fısıltısı (tek satır).

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
