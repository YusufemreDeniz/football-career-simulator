# Futbol Kariyer Simülatörü — Sosyal Performanstan Ürün Derinliğine 10 Adım

Bu sıra, `e0c01c6` maç alt-aşama profilinin sosyal sürekliliği ana darboğaz olarak göstermesinden sonra başlar.

## 1. Selection Memory İndeksleri

- İşlenmiş kaynak olayını, en güncel aktör/kural hafızasını ve sonraki kimliği O(1) indekslerle çöz.
- `ReplaceAll` sonrasında indeksleri kayıt dosyasından eksiksiz yeniden kur.
- Seçim hafızasının idempotensi ve pekiştirme sınırını değiştirme.
- Commit: `perf(social): selection memory sorgularini indeksle`

## 2. Promise ve Relationship İndeksleri

- Aktif sözleri oyuncu/kulüp/tür; ilişkileri oyuncu/menajer çiftiyle indeksle.
- Dormant/reactivate ve karar sonucu yollarında yalnızca ilgili kayıtları tara.
- Commit: `perf(social): soz ve iliski sicak yollarini indeksle`

## 3. Sakatlık İyileşme İndeksi

- Her maçta tüm lig fiziksel durumlarını taramak yerine vadesi gelen kayıtları kulüp ve gün üzerinden çöz.
- Transfer sonrası konum değişikliği ve çift slot invariant'larını koru.
- Commit: `perf(fitness): sakatlik iyilesme taramalarini indeksle`

## 4. Oyuncu Gelişim ve Sözleşme İndeksleri

- Oyuncu kimliği, kulüp/slot ve aktif sözleşme sorgularını sabit maliyetli hâle getir.
- Yıllık yaşlanmayı aynı takvim yılında tekrarlanan tam-store yazımından kurtar.
- Commit: `perf(players): kariyer ve sozlesme erisimlerini indeksle`

## 5. 10 Sezon Üretim Kapısı

- Önce/sonra telemetriyi karşılaştır ve tam koşuyu 12 dakika bütçesi içinde tamamla.
- 3.060 fikstür, 450 aktif oyuncu, emeklilik, transfer, kovulma ve kayıt invariant'larını doğrula.
- Commit: `test(acceptance): on sezon uretim kapisini tamamla`

## 6. Atomik Otomatik Kayıt ve Kurtarma

- Maç, transfer ve sezon kapanışı öncesi döngülü otomatik kayıtlar üret.
- Geçici dosya, bütünlük kontrolü ve atomik slot değişimi uygula.
- Commit: `feat(save): atomik otomatik kayit ve kurtarma ekle`

## 7. Fikstür Yoğunluğu ve Kondisyon Tahmini

- 7/14/30 günlük maç yükü ile sakatlık riskini açıklanabilir biçimde hesapla.
- Önerilen dinlenme aksiyonunu ana sayfadaki karar zincirine bağla.
- Commit: `feat(fitness): fikstur yogunlugu tahmin merkezini ekle`

## 8. Akıllı Rotasyon Planlayıcısı

- Mevki, rol, kondisyon, form, ceza ve sakatlığa göre kadro taslağı üret.
- Manuel kullanıcı seçimini koruyarak taslağı maç kadrosuna aktar.
- Commit: `feat(squad): akilli rotasyon planlayicisini ekle`

## 9. Scouting ve Pazarlık Masası

- Üç adayı değer, maaş, rol uyumu, potansiyel, risk ve rapor güveniyle karşılaştır.
- Görüşme geçmişi, rakip teklif ve pencere baskısını karşı teklife bağla.
- Commit: `feat(transfer): scouting ve pazarlik masasini birlestir`

## 10. Taktik, Sezon Hikâyesi ve Yayın Kapısı

- Taktik presetleri, maç öncesi kontrol listesi ve sezon sonu hikâye raporunu tamamla.
- Hızlı test, uzun kabul, Godot smoke, PC build, Android export ve APK imzasını ayrı kanıtlarla doğrula.
- Gerçek cihaz davranışını yalnızca cihaz üzerinde test edildiğinde tamamlandı say.
- Commit: `test(release): taktik sezon hikayesi ve yayin kapisini tamamla`

## Uygulama Kuralları

- Her performans değişikliği ölçüm öncesi/sonrası kanıt gerektirir.
- Kayıt modeli ve deterministik sonuçlar korunur.
- Önceden var olan commitlenmemiş sekiz dosya adım commit'lerine karıştırılmaz.
- Push yalnızca açık kullanıcı talebiyle yapılır.
