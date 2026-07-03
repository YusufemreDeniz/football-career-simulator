# Football Career Simulator

Uzun soluklu, oyuncunun kararlarını, ilişkilerini ve geçmişini yıllarca hatırlayan; futbolcu veya teknik direktör olarak saha içi ve saha dışı gerçek bir futbol kariyeri yaşatan, sistemik bir futbol kariyeri ve yaşam simülasyonu oyunu.

## Proje Durumu

Bu proje şu anda **dokümantasyon aşamasını tamamlamış, uygulama öncesi teknik doğrulama aşamasındadır**. `docs/18_SPIKE_EXECUTION_PLAN.md` Kart 0–7 kapsamında minimum bir .NET çözüm iskeleti (Domain/Simulation/Application/Infrastructure/Tests, yalnızca yer tutucu içerikle), bir CI workflow'u, minimal bir Godot 4 .NET proje kabuğu, 500 kayıtlık bir yer tutucu UI listesi ve Godot editörü/.NET SDK'sı bulunmayan temiz bir ortamda çalıştığı doğrulanmış bir Windows x64 export akışı oluşturulmuştur. Henüz gerçek domain modeli veya gerçek oyun ekranları oluşturulmamıştır.

Hedef platform, oyun motoru, programlama dili ve yüksek seviyeli mimari **kesinleşmiştir**: Windows 10/11 x64, Godot 4 .NET, C# ve Godot'tan bağımsız saf .NET tabanlı bir domain/simülasyon çekirdeği. Bu kararların ayrıntısı ve gerekçesi `docs/17_TECHNOLOGY_AND_ARCHITECTURE_DECISION.md` içinde kesinleştirilmiştir; kesin sürüm pinleme ve implementasyon düzeyindeki ayrıntılar ilk teknik spike'lar tamamlandıktan sonra netleşecektir.

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

`docs/02_MVP_SCOPE.md` ile `docs/14_TEST_STRATEGY.md` arasındaki ana sistem belgeleri ve teknik mimari kararı kesinleşmiştir. Üretim koduna geçmeden önce, `docs/17_TECHNOLOGY_AND_ARCHITECTURE_DECISION.md` içinde tanımlanan altı teknik doğrulama spike'ı (headless 10 sezon, determinizm, SQLite kayıt/migration, 500 futbolculuk arayüz, Windows export, CI doğrulaması) küçük ve geri alınabilir adımlarla planlanacak ve yürütülecektir. Bu kararlar `docs/15_DECISION_LOG.md` içinde kayıt altına alınmaya devam edecektir.

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

Mevcut kod, `docs/18_SPIKE_EXECUTION_PLAN.md` Kart 0–7 kapsamında oluşturulmuş minimum bir iskelet ve tamamlanan teknik spike'lardır; gerçek domain modelini değil, katman ayrımının derlenebilir/test edilebilir olduğunu, ~20 kulüp/~500 futbolculuk dünya ölçeğinin motor bağımsız çalıştırılabildiğini, sonucun deterministik ve SQLite ile kalıcı biçimde saklanabildiğini, Godot `Tree` UI'ının 500 kayıtla performanslı çalıştığını ve paketin Godot editörü/.NET SDK'sı olmayan temiz bir ortamda açılabildiğini kanıtlayan yer tutucu bir yapıyı temsil eder (`Spike1Placeholder`/`Spike4Placeholder` alt alanları). Godot Windows export'u almak için `src/FootballCareerSimulator.Presentation` içinde `godot --headless --export-release "Windows Desktop x86_64" ../../builds/windows/FootballCareerSimulator.exe` çalıştırılabilir (Godot 4.7-stable mono export şablonları kurulu olmalıdır). Henüz gerçek oyun ekranı veya sanat varlığı yoktur.
