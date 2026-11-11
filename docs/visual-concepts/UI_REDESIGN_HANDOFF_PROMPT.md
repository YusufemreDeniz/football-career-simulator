# Football Career Simulator UI Redesign Handoff Prompt

`C:\Users\yusufemredeniz\Desktop\Football_Career_Simulator` deposunda çalış.

Amaç: Football Career Simulator'ın mevcut Godot/C# arayüzünü
`docs/visual-concepts/game-visual-direction-v1.png` görselindeki kaliteli mobil futbol
menajerliği tasarım diline dönüştür. Görseli arka plan olarak yapıştırma; bütün
ekranları gerçek, etkileşimli Godot `Control` bileşenleriyle uygula.

## Temel kurallar

- Mevcut oyun işlevlerini, navigasyonu, kayıtları ve gerçek verileri koru.
- Görsel değişiklikleri mümkün olduğunca
  `src/FootballCareerSimulator.Presentation` katmanında tut.
- Kullanıcının ilgisiz, commit edilmemiş değişikliklerini silme veya üzerine yazma.
- Ana hedef 844x390 yatay mobil görünüm; dokunma hedefleri en az 44-48 px olsun.
- Masaüstü ve farklı en-boy oranlarında düzen taşmamalı.
- Koyu gece stadyumu/operasyon odası atmosferi kullan: derin orman yeşili ve
  lacivert zemin, zümrüt ana eylemler, altın prestij vurgusu, cam görünümlü koyu
  kartlar, ince kenarlıklar ve ölçülü gölgeler.
- Ortak tema tokenları, kartlar, butonlar, üst navigasyon, durum rozetleri ve
  tipografi `CareerUiTheme` üzerinden merkezi ve tutarlı olsun.
- Kadro/taktik mevki renkleri: kaleci yeşil, savunma kehribar, orta saha
  camgöbeği, hücum eflatun.
- Ekranları aynı tasarım sisteminde dönüştür: Kariyer Merkezi, Kadro, Taktik,
  Maç Günü/Maç Merkezi, Transfer ve oyuncu profili, Takvim, Finans, Haberler,
  Kulüp Gelişimi, Kupalar ve Ayarlar.
- Önce ortak kabuk/tema ve Kariyer Merkezi'ni bitir; sonra diğer ekranlara ilerle.
- Her ekranda bilgi hiyerarşisi, okunabilirlik, dokunmatik kullanım,
  odak/gamepad durumu, boş/yükleniyor/hata durumları ve reduced-motion/
  high-contrast tercihlerini koru.
- Üretilmiş konseptteki uydurma takım/oyuncu verilerini koda alma; mevcut canlı
  oyun verilerini göster.
- Uygulamadan sonra Presentation build, ilgili odaklı testler, hızlı
  `Category!=LongRunning` testi ve Godot smoke çalıştır. Uzun testi
  çalıştırmadıysan geçmiş sayma. Gerçek cihazda test etmediysen bunu açıkça belirt.
- Sonuçta değişen dosyaları, testleri, kalan riskleri ve manuel mobil test
  listesini ver.
