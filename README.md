# Football Career Simulator

Uzun soluklu, oyuncunun kararlarını, ilişkilerini ve geçmişini yıllarca hatırlayan; futbolcu veya teknik direktör olarak saha içi ve saha dışı gerçek bir futbol kariyeri yaşatan, sistemik bir futbol kariyeri ve yaşam simülasyonu oyunu.

## Proje Durumu

Bu proje şu anda **dokümantasyon aşamasını ve uygulama öncesi teknik doğrulama aşamasını (altı spike) tamamlamış** durumdadır. `docs/18_SPIKE_EXECUTION_PLAN.md` Kart 0–8'in tamamı tamamlanmıştır: minimum bir .NET çözüm iskeleti (Domain/Simulation/Application/Infrastructure/Tests), saf .NET + Godot doğrulamasını içeren bir CI pipeline'ı, minimal bir Godot 4 .NET proje kabuğu, 500 kayıtlık bir yer tutucu UI listesi ve Godot editörü/.NET SDK'sı bulunmayan temiz bir ortamda hem yerel makinede hem CI'da çalıştığı doğrulanmış bir Windows x64 export akışı. Henüz gerçek domain modeli veya gerçek oyun ekranları oluşturulmamıştır; mevcut kod tamamen yer tutucu/kanıt niteliğindedir (`Spike1Placeholder`, `Spike4Placeholder`).

Hedef platform, oyun motoru, programlama dili ve yüksek seviyeli mimari **kesinleşmiştir**: Windows 10/11 x64, Godot 4.7-stable (mono/.NET, D-339 ile pinlendi), C# ve Godot'tan bağımsız saf .NET tabanlı bir domain/simülasyon çekirdeği. Bu kararların ayrıntısı ve gerekçesi `docs/17_TECHNOLOGY_AND_ARCHITECTURE_DECISION.md` içinde kesinleştirilmiştir; exact .NET hedef sürümü ve persistence provider pinlemesi hâlâ ayrıca ele alınacaktır.

## Ana Referans Belge

Projenin tüm tasarım kararlarının başlangıç noktası:

- [`docs/01_GAME_DESIGN_DOCUMENT.md`](docs/01_GAME_DESIGN_DOCUMENT.md)

Dokümantasyonun tamamına genel bakış için:

- [`docs/00_PROJECT_INDEX.md`](docs/00_PROJECT_INDEX.md)

Teknoloji ve mimari kararı için:

- [`docs/17_TECHNOLOGY_AND_ARCHITECTURE_DECISION.md`](docs/17_TECHNOLOGY_AND_ARCHITECTURE_DECISION.md)

Alınan tüm kararların günlüğü için:

- [`docs/15_DECISION_LOG.md`](docs/15_DECISION_LOG.md)

## Sonraki Adımlar

`docs/02_MVP_SCOPE.md` ile `docs/14_TEST_STRATEGY.md` arasındaki ana sistem belgeleri, teknik mimari kararı ve `docs/17_TECHNOLOGY_AND_ARCHITECTURE_DECISION.md` içinde tanımlanan altı teknik doğrulama spike'ının (headless 10 sezon, determinizm, SQLite kayıt/migration, 500 futbolculuk arayüz, Windows export, CI doğrulaması) tamamı kesinleşmiş ve tamamlanmıştır (bkz. `docs/18_SPIKE_EXECUTION_PLAN.md`). Bir sonraki adım, gerçek 14 bounded context domain modelinin üretim implementasyonuna geçilmesidir; bu, ayrı bir çalışma/karar gerektirir ve bu depoda henüz başlamamıştır. Alınan tüm kararlar `docs/15_DECISION_LOG.md` içinde kayıt altına alınmaya devam edecektir.

## Klasör Yapısı

```
Football_Career_Simulator/
├── README.md
├── FootballCareerSimulator.slnx           # Ana .NET çözümü
├── Directory.Build.props                  # Projeler arası ortak derleme ayarları
├── docs/          # Tasarım ve planlama dokümanları
├── src/
│   ├── FootballCareerSimulator.Domain/         # Domain katmanı (dış teknolojiye bağımlı değil)
│   ├── FootballCareerSimulator.Simulation/     # Simulation katmanı
│   ├── FootballCareerSimulator.Application/    # Application / use case katmanı
│   ├── FootballCareerSimulator.Infrastructure/ # SQLite save/load, migration (Spike 3)
│   └── FootballCareerSimulator.Presentation/   # Godot 4 .NET proje kabuğu (Kart 5)
├── tests/
│   └── FootballCareerSimulator.Tests/          # xUnit test projesi
├── tools/
│   └── FootballCareerSimulator.SimulationRunner/  # Spike 1 headless simülasyon aracı
├── builds/        # Export çıktıları (git tarafından yok sayılır, bkz. Kart 7)
├── assets/        # Oyun varlıkları (henüz boş)
└── prototypes/    # Küçük prototipler (henüz boş)
```

Mevcut kod, `docs/18_SPIKE_EXECUTION_PLAN.md` Kart 0–8'in tamamı kapsamında oluşturulmuş minimum bir iskelet ve tamamlanan teknik spike'lardır; gerçek domain modelini değil, katman ayrımının derlenebilir/test edilebilir olduğunu, ~20 kulüp/~500 futbolculuk dünya ölçeğinin motor bağımsız çalıştırılabildiğini, sonucun deterministik ve SQLite ile kalıcı biçimde saklanabildiğini, Godot `Tree` UI'ının 500 kayıtla performanslı çalıştığını, paketin Godot editörü/.NET SDK'sı olmayan temiz bir ortamda açılabildiğini ve bunların tamamının CI'da da otomatik doğrulandığını kanıtlayan yer tutucu bir yapıyı temsil eder (`Spike1Placeholder`/`Spike4Placeholder` alt alanları). Godot Windows export'u almak için `src/FootballCareerSimulator.Presentation` içinde `godot --headless --export-release "Windows Desktop x86_64" ../../builds/windows/FootballCareerSimulator.exe` çalıştırılabilir (Godot 4.7-stable mono export şablonları kurulu olmalıdır); CI'da bu, `.github/workflows/ci.yml` içindeki `godot-headless` job'ı tarafından her push'ta otomatik yapılır. Henüz gerçek oyun ekranı veya sanat varlığı yoktur.
