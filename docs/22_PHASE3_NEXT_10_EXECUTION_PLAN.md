# Futbol Kariyer Simülatörü — Faz 3 Sıradaki 10 Adım

Bu sıra, `ff898d2` ile tamamlanan uzun kariyer ilerleme telemetrisinden sonra uygulanacaktır. Önce ölçülebilir performans borcu kapatılır; ardından aynı sağlam altyapının üzerinde oyuncu deneyimi genişletilir.

## 1. Maç İşleme Alt-Aşama Profili

- `PlayFixtureMatch` zincirini hazırlık, simülasyon, puan durumu, fiziksel sonuç, gelişim, sosyal sonuç ve read-model aşamalarına ayırarak sürelerini ölç.
- Üretim çalışmasında dinleyici yokken ihmal edilebilir maliyet taşıyan `ActivitySource` tabanlı tanı noktaları kullan.
- 10 sezon testinde sezonluk aşama toplamlarını kalıcı ilerleme günlüğüne ekle.
- Commit: `test(performance): mac isleme alt asama profilini ekle`

## 2. Sosyal Hafıza ve İlişki Sıcak Yollarını İndeksle

- Seçim hafızası, sözler ve oyuncu-menajer ilişkilerindeki büyüyen koleksiyon taramalarını kaldır.
- Kaynak olay anahtarı ve oyuncu/menajer çifti için O(1) erişim sağla.
- İdempotensi, pekiştirme sınırı ve kayıt round-trip sonuçlarını değiştirme.
- Commit: `perf(social): hafiza ve iliski erisimlerini indeksle`

## 3. Sakatlık İyileşme ve Fiziksel Durum Taramalarını İndeksle

- Her maç öncesinde bütün ligdeki fiziksel kayıtları dolaşan iyileşme yolunu kulüp ve bitiş günü indeksleriyle sınırla.
- Yalnızca vadesi gelen sakatlıkları iyileştir; deterministik sakatlık ve kondisyon sonuçlarını koru.
- Eksik, çift ve transfer sonrası konum kayıtlarını invariant testleriyle doğrula.
- Commit: `perf(fitness): sakatlik iyilesme taramalarini indeksle`

## 4. Oyuncu Gelişim ve Sözleşme Erişimlerini İndeksle

- Maç başına tekrarlanan kariyer sıralama, yaşlandırma ve sözleşme aramalarını ölç.
- Oyuncu kimliği ile kulüp/mevki için tutarlı indeksler; takvim yılı başına tek yaşlandırma geçişi oluştur.
- Kayıt yükleme ve geriye tarihli yükleme davranışını regresyon testleriyle koru.
- Commit: `perf(players): kariyer ve sozlesme sicak yollarini indeksle`

## 5. 10 Sezon Üretim Kapısını Tamamla

- İlk dört adım sonrasında tam 10 sezon koşusunu 12 dakika bütçesiyle tamamla.
- Fikstür, emeklilik, transfer, menajer kovulması, nüfus ve kayıt boyutu invariant'larını doğrula.
- Başarıyı hızlı paketten ayrı raporla; süre aşımını asla başarılı sayma.
- Commit: `test(acceptance): on sezon uretim kapisini yesile cek`

## 6. Atomik Otomatik Kayıt ve Kurtarma

- Maç, transfer ve sezon kapanışından önce döngülü otomatik kayıt oluştur.
- Geçici dosya, bütünlük kontrolü ve atomik değiştirme zinciri kullan.
- Son bozuksa önceki sağlam kayıt için oyuncuya açık kurtarma seçeneği göster.
- Commit: `feat(save): atomik otomatik kayit ve kurtarma akislarini ekle`

## 7. Fikstür Yoğunluğu ve Kondisyon Tahmini

- 7/14/30 günlük maç yükü, dinlenme aralığı ve sakatlık riskini deterministik hesapla.
- Ana sayfada açıklanabilir uyarı ve önerilen dinlenme aksiyonu sun.
- Mevcut dinlendirme komutlarıyla tahmin sonrası gerçek değişimi aynı kartta karşılaştır.
- Commit: `feat(fitness): fikstur yogunlugu ve kondisyon tahminini ekle`

## 8. Akıllı Rotasyon ve Kadro Planlayıcısı

- Mevki, rol, kondisyon, form, ceza ve sakatlığa göre rotasyon taslağı üret.
- As/rotasyon/acil durum rollerini düzenlenebilir ve kayıtlı hâle getir.
- Tek dokunuşla maç kadrosuna aktar; kullanıcının manuel seçimini üstün tut.
- Commit: `feat(squad): akilli rotasyon planlayicisini ekle`

## 9. Scouting ve Transfer Karşılaştırma Masası

- Üç adaya kadar maliyet, maaş, rol uyumu, potansiyel, risk ve rapor güvenini yan yana göster.
- Görüşme zaman çizelgesi, rakip teklif ve pencere baskısını karşı teklif kararına bağla.
- Belirsiz scouting bilgisini kesin değer gibi sunma; rapor eskidikçe güveni düşür.
- Commit: `feat(transfer): scouting karsilastirma ve pazarlik baskisini birlestir`

## 10. Taktik, Sezon Hikâyesi ve Yayın Kapısı

- Taktik presetleri ile maç öncesi uygunluk kontrol listesini tamamla.
- Sezon sonu raporuna hedefler, rekorlar, kilit kararlar ve transfer etkisini ekle.
- Hızlı test, uzun kabul, Godot smoke, masaüstü build, Android export ve APK imzasını tek yayın matrisiyle doğrula.
- Gerçek cihaz davranışını yalnızca cihaz üstünde gözlemlendiğinde tamamlandı say.
- Commit: `test(release): taktik sezon hikayesi ve yayin kapisini tamamla`

## Çalışma Kuralları

- Her adım ölçülebilir kabul kriteri olan tek bir dikey dilimdir.
- Önceden var olan commitlenmemiş sekiz dosya ilgili commit'lere karıştırılmaz.
- Performans değişikliği ölçüm öncesi/sonrası kanıt olmadan tutulmaz.
- Push yalnızca açık talep üzerine yapılır.
