# Football Career Simulator

Uzun soluklu, oyuncunun kararlarını, ilişkilerini ve geçmişini yıllarca hatırlayan; futbolcu veya teknik direktör olarak saha içi ve saha dışı gerçek bir futbol kariyeri yaşatan, sistemik bir futbol kariyeri ve yaşam simülasyonu oyunu.

## Proje Durumu

Bu proje şu anda **dokümantasyon aşamasını tamamlamış, uygulama öncesi teknik doğrulama aşamasındadır**. Henüz üretim kodu, test projesi, Godot projesi veya CI workflow oluşturulmamıştır.

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
├── docs/          # Tasarım ve planlama dokümanları
├── src/           # Kaynak kod (henüz boş)
├── tests/         # Testler (henüz boş)
├── tools/         # Yardımcı araçlar (henüz boş)
├── assets/        # Oyun varlıkları (henüz boş)
└── prototypes/    # Küçük prototipler (henüz boş)
```
