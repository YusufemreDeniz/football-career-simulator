# Football Career Simulator

Uzun soluklu, oyuncunun kararlarını, ilişkilerini ve geçmişini yıllarca hatırlayan; futbolcu veya teknik direktör olarak saha içi ve saha dışı gerçek bir futbol kariyeri yaşatan, sistemik bir futbol kariyeri ve yaşam simülasyonu oyunu.

## Proje Durumu

Dokümantasyon ve teknik spike aşaması tamamlanmıştır. Üretim implementasyonu **aktif ilerliyor**: 14 bounded context'in production temelleri, birleşik Career SQLite kaydı ve Godot ince kariyer akışı kodlanmıştır. Kilometre Taşı 1 büyük ölçüde tamamlanmış; çok sezon bütünlüğü ve kulüp değiştirme kısmi; tam 10 sezonluk MVP kabul koşusu açıktır. **Forma Sözü Zinciri** (Promise → maç/deadline → Memory → Relationship → Decision/Transfer → satış terminali → save/load) otomatik çapraz senaryoda kilitlenmiştir. Güncel araç zinciri `net9.0`, SDK `9.0.317/latestPatch`, Godot 4.7-stable mono/.NET ve `Microsoft.Data.Sqlite 10.0.9`'dur (D-384).

Godot Presentation katmanında ince bir kariyer döngüsü vardır: **ana menü → kariyer merkezi → maç günü → maç sonuçları** (yeni kariyer / devam et, lig kurma, maç öncesi kadro onayı, ayrı maç günü kontrol noktası, son kadro/formasyon/yaklaşım dokunuşları, maç oynatma, temel maç raporu ve öne çıkan oyuncu, zaman ilerletme, kaydet/yükle). Kariyer merkezi D-363 ile progressive disclosure kullanır: tek `Sıradaki Adım` CTA, bağlamsal karar seçenekleri ve yalnız uygulanabilir alt eylemler; manuel domain/lifecycle tetikleri oyuncu yüzünde gösterilmez. Bu bir görsel ürün UI'sı değil; oynanabilir dikey kesit kontrol yüzeyidir. `Spike1Placeholder` / `Spike4Placeholder` yalnızca eski spike kanıtı için durur; asıl oyun akışı üretim context'leri üzerinden yürür.

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

Güncel öncelik sırası: tam test kapısını yeşil tutmak; repository EOL/import hijyenini kapatmak; Kilometre Taşı 2 için emeklilik ve yeni futbolcu üretimini tamamlamak; Kilometre Taşı 3 kariyer geçmişini kapatmak; ardından tüm context'leri kapsayan 10 sezonluk MVP kabul koşusunu çalıştırmaktır. Ayrıntılı ve authoritative durum `docs/19_PRODUCTION_IMPLEMENTATION_PLAN.md` üstündeki güncel kontrol noktasındadır.

İnce kariyer döngüsü (MVP: teknik direktör) + nabız→CTA + transfer CTA + **maç günü → HT → sonuç** + **sakatlık / iyileşme yolu** + **auto-swap** + **görsel XI şeridi** + **Maç Günü tempo flash’ı** + **Maç Nabzı (düdük anı)** + **İlk Yarı Anları (devre arası özeti)** + **Yarılara bölünmüş Maç Gecesi Anlar** + **Sakin hafta staff fısıltısı** + **Maç sonrası sabah manşeti** + **Transfer penceresi hafta ritmi** + **Lig Akşamı (hafta sonu tablo özeti)** + **Soyunma Odası (kaptan tepkisi)** + **Antrenman Sahası (hafta içi raporu)** + **Stadyum (maç gecesi giriş atmosferi)** + **Tribün Tepkisi (maç içi atmosfer vuruşları)** + **Teknik Alan (devre arası karar-etki özeti)** + **Rakip Dosyası (maç öncesi veri özeti)** + **Eşleşme Planı (düdük öncesi taktik uyumu)** + **Planın Sahadaki İzi (maç sonu öğrenme döngüsü)** + **Teknik Direktör Defteri (kalıcı son üç maç dersi)** + **Tekrarlanan Desen Uyarısı (bağlama özel koç mesajı)** + **Alternatif Plan Reçetesi (somut taktik önerisi)** + **Öneriyi Uygula (tek dokunuşla taktik geçişi)** + **Kadro Uyumu Önizlemesi (gerçek mevki ve XI etkisi)** + **Gerçek Kadro Kurma Ekranı (serbest XI-yedek seçimi)** + **Ayrıntılı Futbolcu Mevkileri (12 gerçek rol)** + **Kadro Kararları → Maç Motoru (rol uyumu etkisi)** + **Gelişmiş Taktik Tahtası (pres · hat · pas)** + **Maç İçi Teknik Direktörlük (canlı taktik · seçilebilir değişiklik)** + **Futbolcu Yönetimi (oyuncu merkezli kariyer · fizik · sözleşme · ilişki · sözler)** + **Scout ve Transfer Sistemi (mevkisel ihtiyaç · izleme bilgisi · gerçek aday kısa listesi)** + **Lig ve İstatistik Merkezi (form · iç/dış saha · hücum/savunma liderleri)** + **Uzun Süreli Kariyer Döngüsü (sezon mirası · görev rekoru · gelişim eşikleri)** + **Oyun Hissi ve Mobil Kalite (güvenli alan · 48px+ dokunma · eksen kilitli kaydırma · eylem geri bildirimi)** kilitlenmiştir (Bugün ↔ Ofis tempo: Calm→Match + **Kadro Onayla→düdük** köprüleri; Not flash; düdük anında “kadro kilitli” vurgusu; düdük sonrası sahaya giriş satırları; devre arasına ilk yarı anahtar anları — gol/kart/sakatlık; maç sonucu Anlar “1. Yarı / 2. Yarı” başlıklı, HT kararı ve değişikliği ikinci yarı başında; sakin hafta Not’u gerçek durumdan besleniyor — sıradaki maç, sakatlık listesi, lig durumu; ofis dönüşünde maç sonucuna göre deterministik sabah manşeti — kovulma/basın öncelikli; ofis nabzı pencereyi hissettiriyor — kapanış baskısı, açık ritim, yeni kapanmış pencere hükmü; maç gecesi sonunda Lig Akşamı — lider değişimi, yönetilenin sıra hareketi, küme hattı, sıradaki rakibin sonucu; maç gecesi kaptanın sesiyle kapanır — farka göre sahiplenme, uyarı, sessizlik, kovulmada boş koridor; Hazırlık Masası artık sahanın sesini taşır — yorgunluk, form, sakatlık ve yük tonuna göre deterministik “Sahadan:” raporu; maç gecesi yönetilen kulübün ev/deplasman durumunu ve maç öncesi lig bölgesini okuyarak sonuçtan bağımsız tribün sesini açar; gol ve kırmızı kartlardan sonra, ayrıca devre düdüğünde skor ile saha tarafına göre tribün tepkisi Anlar akışına girer; maç sonucu Teknik Alan bölümü devre skorunu, yaklaşım kararını ve ikinci yarı skorunu yönetilen takım açısından birlikte yorumlar; Maç Günü ekranındaki Rakip Dosyası rakibin gerçek lig sırası, son beş formu, göreli kulüp gücü ve veri öncelikli tek tehdidini gösterir; Eşleşme Planı seçili formasyon ve yaklaşımı bu tehditle karşılaştırıp tek, renk kodlu risk/fırsat odağı verir ve taktik değişiminde anında yenilenir; Planın Sahadaki İzi düdük öncesi sinyali ilk gol/kırmızı kart, ikinci yarı skoru ve final sonuçla karşılaştırarak maç sonuna kritik bir öğrenme satırı taşır; Teknik Direktör Defteri son üç plan sonucunu taktik, rakip tehdidi ve sonuç sinyaliyle V42 save şemasında saklar ve sonraki maçın Son Kontroller akışına geri getirir; Tekrarlanan Desen Uyarısı aynı tehdit, plan sinyali ve taktik seçiminin son üç kayıtta yeniden uyarı üretmesini maç öncesinde tek bir koç mesajına dönüştürür; Alternatif Plan Reçetesi bu uyarıyı rakip tehdidine göre mevcut seçimden farklı, somut bir formasyon ve yaklaşım önerisine çevirir; Öneriyi Uygula reçetedeki iki taktik seçimini tek güncellemede uygular ve eşleşme sonucunu anında yeniler; Kadro Uyumu Önizlemesi seçili XI'ı gerçek oyuncu mevkileriyle karşılaştırıp yüzde uyum, hat dengesi, eksik mevki ve pozisyon dışı oyuncuları taktik değişiminde anında yeniler; Gerçek Kadro Kurma Ekranı ilk 11 ile yedekler arasında mobilde dokun-seç, masaüstünde sürükle-bırak değişimini gerçek isim, mevki, güç ve fizik durumuyla sunar; Ayrıntılı Futbolcu Mevkileri kaynak kadrolardaki 12 özgün rolü korur ve formasyon yuvalarını çoklu rol uyumuyla değerlendirir; Kadro Kararları → Maç Motoru seçilen XI'ın oyuncu gücü ve fizik etkisine ek olarak rol uyumunu -4..+2 maç gücü katsayısıyla hem devre önizlemesine hem final simülasyonuna taşır; Gelişmiş Taktik Tahtası formasyon ve yaklaşıma pres yoğunluğu, savunma hattı ve pas stilini ekler, seçili durumları mobil segmentlerle gösterir ve sinerjiyi maç gücüne yansıtır; Maç İçi Teknik Direktörlük devre arasında formasyon ve presi değiştirir, sahadaki XI ile kulübeden istenen oyuncuyu seçerek tek değişiklik yaptırır ve hücum/savunma kararını kilitli ilk yarı skorundan sonraki simülasyona uygular; Futbolcu Yönetimi tam A takımını gerçek isim ve ayrıntılı mevkilerle listeler, seçili oyuncunun gelişim, fizik, sözleşme, ilişki ve aktif sözlerini tek panelde birleştirir ve yönetim kararlarını doğrudan seçili oyuncuya yöneltir; Scout ve Transfer Sistemi kadronun sayısal olarak en zayıf mevki grubunu bulur, diğer lig kulüplerindeki gerçek oyuncuları potansiyel ve güçle sıralar, izleme süresine göre tahmin aralığını daraltır ve seçili adayı mevcut kısa liste, süreç, sportif onay, teklif ve imza zincirine bağlar; Lig ve İstatistik Merkezi gerçek fikstür sonuçlarından son beş formu, iç/dış saha puanını, maç başı golü, en iyi hücum ve en sıkı savunmayı üretir ve puan tablosuna FORM sütunu ekler; Uzun Süreli Kariyer Döngüsü save dosyasındaki sezon, fikstür, oyuncu kariyeri ve sözleşme kayıtlarından yalnız teknik direktörün görev dönemine ait toplam rekoru, sezon derecelerini, gelişen oyuncuları, yaş/sözleşme yükünü ve sıradaki kariyer eşiğini üretir; Oyun Hissi ve Mobil Kalite dar viewportta üst kabuğu ve alt gezinmeyi yeniden ölçer, ekran güvenli alanını korur, tüm ana dokunma hedeflerini en az 48px tutar, puan tablosuna bağımsız yatay sürükleme verir, dikey/yatay hareketi eksen kilidiyle ayırır ve eylem sonucunu renk ile mobil titreşimle bildirir). Futbolcu kariyeri ertelenmiştir (D-028). Uzun diyalog ağacı / kapsamlı gazeteci ağı açılmaz (D-118, `02_MVP_SCOPE`); D-150 transfer fiyat formüllerini açık bırakır. On özelliklik profesyonel oynanış paketi tamamlanmıştır. Sıradaki ürün adımı: gerçek cihaz oyun testinden çıkan denge, okunabilirlik ve akış bulgularını önceliklendirmek.

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
