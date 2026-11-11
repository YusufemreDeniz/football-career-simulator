# Futbol Kariyer Simülatörü — Faz 3 Profesyonel Geliştirme Yol Haritası

**Durum:** Tamamlandı (`2026-11-10` itibarıyla 10 adımın tamamı uygulandı, doğrulandı ve commitlendi — `ff898d2` .. `d02d180`).

Faz 3, Faz 2'de tamamlanan oyuncu yüzü iyileştirmelerini uzun kariyer güvenilirliği, karar derinliği ve yayın kalitesiyle birleştirir. Sıralama; önce sistemik riski düşürür, ardından birbirinin verisini kullanan oyuncu özelliklerini katmanlı biçimde kurar.

## Tamamlanma Özeti (2026-11-10)

| Adım | Başlık | Durum | Kanıt / Commit |
|---|---|---|---|
| 1 | Uzun Kariyer Gözlemlenebilirliği ve Performans Bütçesi | ✅ Tamamlandı | `ff898d2`, `e0c01c6` |
| 2 | Sezon Simülasyonu Darboğazlarını Giderme | ✅ Tamamlandı | `a50aa77`, `052d38a`, `fded83e`, `184b9e1`, `98aeb5f` (10 sezon ~11–16 sn) |
| 3 | Atomik Otomatik Kayıt ve Kurtarma Akışı | ✅ Tamamlandı | `c93476c` (`LocalCareerSaveRepository` `.tmp` → `.bak` atomik rotasyon + UI kurtarma) |
| 4 | Fikstür Yoğunluğu ve Kondisyon Tahmin Merkezi | ✅ Tamamlandı | `a1a9b9e` (`FixtureCongestionForecastDigest`, 7/14/30 gün tahmin merkezi) |
| 5 | Akıllı Rotasyon ve Kadro Derinliği Planlayıcısı | ✅ Tamamlandı | `297a9ac` (`SmartRotationPlanDigest`, tek tıkla `🎯 Akıllı Rotasyon 11'i`) |
| 6 | Scouting Güveni ve Aday Karşılaştırma Masası | ✅ Tamamlandı | `1dcb69f` (`ScoutCandidateComparisonDigest`, 3 aday matrisi + güven kademeleri) |
| 7 | Transfer Pazarlığı Zaman Çizelgesi ve Baskı Dinamikleri | ✅ Tamamlandı | `1dcb69f` (`TransferNegotiationMemoryService` canlı pazarlık entegrasyonu) |
| 8 | Oyuncu İlişkileri, Moral ve Soyunma Odası Nabzı | ✅ Tamamlandı | `d02d180` (`SocialContagionService` + `EpistemicKnowledgeService` soyunma odası ve kulis) |
| 9 | Taktik Hazırlık, Preset ve Maç Öncesi Kontrol Listesi | ✅ Tamamlandı | `5cbb80a`, `d02d180` (`PreMatchReadinessChecklistDigest` + taktik presetleri + anti-meta) |
| 10 | Sezon Hikâyesi ve Çapraz Platform Yayın Kapısı | ✅ Tamamlandı | `d02d180` (`CareerChronicleCompiler` + 1281/1281 birim testi + Android APK cihaz doğrulaması) |

## Adım 1 — Uzun Kariyer Gözlemlenebilirliği ve Performans Bütçesi

- 10 sezonluk üretim kabul koşusuna sezon, transfer, fikstür, kayıt/yükleme ve nüfus doğrulama kontrol noktaları ekle.
- Her kontrol noktasında toplam süre, adım süresi, işlenen fikstür ve bellek kullanımını kalıcı tanı günlüğüne yaz.
- Süre aşımında son tamamlanan aşamayı tek bakışta bulunabilir hâle getir; başarılı koşuda 12 dakikalık bütçeyi koru.
- Doğrulama: hızlı test paketi yeşil; uzun koşu günlüğü gerçek ilerleme üretir.
- Commit: `test(performance): uzun kariyer kabul kosusuna ilerleme telemetrisi ekle`

## Adım 2 — Sezon Simülasyonu Darboğazlarını Giderme

- Adım 1 ölçümlerinden en pahalı iki sıcak yolu belirle.
- Büyüyen koleksiyon taramalarını indeksleme, önbellekleme veya sezonluk toplu işlemle iyileştir.
- Determinizmi ve kayıt uyumluluğunu değiştirmeden 10 sezonluk koşuyu 12 dakika bütçesinin altına indir.
- Doğrulama: aynı seed ile sonuç özeti eşdeğer; hızlı paket ve 10 sezon kabul testi yeşil.
- Commit: `perf(simulation): uzun kariyer sezon darboğazlarini optimize et`

## Adım 3 — Atomik Otomatik Kayıt ve Kurtarma Akışı

- Maç, transfer imzası ve sezon kapanışı öncesinde döngülü otomatik kayıt noktaları oluştur.
- Geçici dosyaya yazıp doğruladıktan sonra atomik slot değişimi yap; bozuk son kayıt için önceki sağlam sürümü öner.
- Kayıt masasını bütünlük, kurtarma kaynağı ve otomatik kayıt yaşı rozetleriyle genişlet.
- Doğrulama: yarım yazım ve bozuk dosya senaryoları veri kaybı olmadan geri döner.
- Commit: `feat(save): atomik otomatik kayit ve kurtarma akislarini ekle`

## Adım 4 — Fikstür Yoğunluğu ve Kondisyon Tahmin Merkezi

- Önümüzdeki 7/14/30 gün için maç yükü, dinlenme aralığı ve sakatlık riskini hesapla.
- Kariyer ana sayfasında yoğunluk uyarısı ve önerilen dinlenme aksiyonu göster.
- Mevcut dinlendirme ve rejenerasyon komutlarını tahmin verisiyle aynı nedensellik zincirine bağla.
- Doğrulama: sıkışık ve normal fikstür örneklerinde risk sınıfları deterministik.
- Commit: `feat(fitness): fikstur yogunlugu ve kondisyon tahmin merkezini kur`

## Adım 5 — Akıllı Rotasyon ve Kadro Derinliği Planlayıcısı

- Derinlik kartlarından mevki başına as, rotasyon ve acil durum rollerini düzenlenebilir yap.
- Kondisyon, form, ceza ve sakatlığı hesaba katan sonraki maç rotasyon önerisi üret.
- Öneriyi tek dokunuşla maç kadrosu taslağına aktar; kullanıcı seçimini her zaman üstün tut.
- Doğrulama: eksik mevki, yorgun ilk 11 ve kaleci yokluğu senaryoları kapsanır.
- Commit: `feat(squad): akilli rotasyon ve derinlik planlayicisini ekle`

## Adım 6 — Scouting Güveni ve Aday Karşılaştırma Masası

- Rapor yaşına ve gözlem kapsamına bağlı yetenek/değer belirsizliği ekle.
- En fazla üç adayı rol uyumu, maliyet, maaş, potansiyel, risk ve transfer olasılığıyla yan yana karşılaştır.
- Yeni gözlem geldikçe belirsizlik aralığını daralt ve eski raporu görünür biçimde bayatlat.
- Doğrulama: aynı scout/seed girdisi aynı aralığı üretir; bilinmeyen veri kesin sayı gibi sunulmaz.
- Commit: `feat(scouting): aday karsilastirma ve rapor guven sistemini ekle`

## Adım 7 — Transfer Pazarlığı Zaman Çizelgesi ve Baskı Dinamikleri

- Teklif, karşı teklif, ajan talebi ve kulüp cevabını kronolojik görüşme geçmişinde tut.
- Pencere kapanışı, rakip teklif ve oyuncu ilgisine göre pazarlık baskısını hesapla.
- Sonraki hamlenin bütçe ve başarı ihtimaline etkisini karar öncesi özetle.
- Doğrulama: yeniden yüklemede görüşme geçmişi ve deterministik baskı seviyesi korunur.
- Commit: `feat(transfer): pazarlik zaman cizelgesi ve deadline baskisini ekle`

## Adım 8 — Oyuncu İlişkileri, Moral ve Soyunma Odası Nabzı

- Sözler, oynama süresi, disiplin ve basın kararlarını oyuncu moral eğiliminde birleştir.
- Takım liderleri, memnuniyetsiz gruplar ve yaklaşan talepler için açıklanabilir soyunma odası özeti kur.
- Karar masasındaki "Bu Neden Oldu?" panelini ilişki geçmişine geri bağla.
- Doğrulama: neden-sonuç zinciri kayıt/yükleme sonrasında aynı kalır.
- Commit: `feat(morale): soyunma odasi nabzi ve aciklanabilir moral sistemini ekle`

## Adım 9 — Taktik Hazırlık, Preset ve Maç Öncesi Kontrol Listesi

- Diziliş, roller ve maç planını adlandırılmış taktik preset'leri olarak kaydet/yükle.
- Maç öncesinde uygunsuz rol, yorgun oyuncu, eksik yedek ve cezalı oyuncu kontrolleri çalıştır.
- Mobilde güvenli geri alma ve onay akışı; PC'de klavye/gamepad odağı sağla.
- Doğrulama: preset round-trip, overlay geri tuşu ve odak gezinimi testleri yeşil.
- Commit: `feat(tactics): taktik presetleri ve mac oncesi kontrol listesini ekle`

## Adım 10 — Sezon Hikâyesi ve Çapraz Platform Yayın Kapısı

- Sezon sonu raporuna hedefler, rekorlar, kilit kararlar, transfer etkisi ve kariyer dönüm noktalarını ekle.
- PC ve Android için çözünürlük, dokunma alanı, geri tuşu, ses/titreşim ve kayıt uyumluluğu matrisini otomatikleştir.
- Hızlı test, uzun kabul, Godot smoke, masaüstü build, Android export ve APK imzasını tek yayın kontrol listesinde birleştir.
- Gerçek cihaz davranışını yalnızca cihaz üstünde doğrulandığında tamamlandı say.
- Commit: `test(release): sezon hikayesi ve capraz platform yayin kapisini tamamla`

## Uygulama İlkeleri

- Her adım tek başına oynanabilir ve geri alınabilir bir dikey dilim olmalıdır.
- Mevcut commitlenmemiş kullanıcı değişiklikleri adım commit'lerine karıştırılmamalıdır.
- Hızlı test paketi her adımda; pahalı uzun koşu yalnızca ilgili performans ve yayın kapılarında çalıştırılmalıdır.
- Android export, imza doğrulaması ve gerçek cihaz deneyimi birbirinden ayrı kanıtlanmalıdır.
