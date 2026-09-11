# Açık Sorular (Open Questions)

Bu dokümanda, Soccer Manager 2025 (SM25) IL2CPP tersine mühendislik sürecinde çözülen konular ve sıradaki araştırma aşamaları için aktif açık sorular yer almaktadır.

---

## 1. Aktif Araştırma Soruları (Sıradaki Aşama: Oyuncu Gelişimi ve Yaşlanma)

### Oyuncu Gelişimi ve Yaşlanma (Player Development & Aging)
- **Rating Gelişimi**: Oyuncunun genel yeteneği (rating) sezon içinde ve antrenmanlarda tam olarak hangi formülle artmaktadır?
- **Potansiyel (Potential)**: Potansiyel değeri aşılamaz katı bir üst tavan (hard cap) mıdır? `SupposedPotential` alanı ile gerçek potansiyel arasındaki fark nedir?
- **Zirve (Peak)**: Oyuncunun gelişiminin durduğu ve gerilemenin başladığı `Peak` anı nasıl tetiklenir?
- **Düşüş Yaşı (DeclineAge)**: `PlayerData.cacheDeclineAge` (ofset `0x7C`) değeri her oyuncu için nerede, ne zaman ve hangi dağılımla üretilmektedir?
- **Mevki Farklılıkları (Position Specifics)**: Kaleciler (GK) saha içi oyunculara göre daha geç mi yaşlanır ve geriler?
- **Yaşa Bağlı Gelişim Eğrisi**: Yaşa bağlı gelişim çarpanı (growth multiplier) var mıdır? Min/maks gelişim yaşı sınırları nelerdir?
- **Antrenman Etkisi (Training)**: Antrenman tesisleri, antrenör kalitesi ve antrenman yoğunluğu gelişimi nasıl etkiler?
- **Maç Tecrübesi (Match Experience)**: Oynanan dakika, ilk 11 başlama sayısı, maç reytingi ve lig zorluk seviyesi gelişime doğrudan puan ekler mi?
- **Gerileme Türleri (Decline Types)**: Fiziksel, zihinsel ve teknik özellikler farklı hızlarda mı geriler?

### Transfer Yapay Zekası ve Piyasa Fiyatlandırması (Gelecek Aşama)
- Kalan sözleşme süresi, transfer listesi durumu ve transfer talebine göre satıcı kulübün talep ettiği bonservis çarpanı (`PlayerData.Value` üzerine uygulanan) nedir?
- `ClubTransferAI` kulübün maksimum teklif sınırını, transfer bütçesini ve mevki önceliğini nasıl hesaplar?
- Yetiştirme tazminatı (`CalculatePlayerCompensationFee`), imza parası (`CalculatePlayerSigningFee`) ve kiralık satın alma opsiyonu (`get_LoanOptionToBuyFee`) formülleri nelerdir?

---

## 2. Çözülen ve Doğrulanan Konular (Resolved)

- **Temel Oyuncu Değeri Amacı (GetPlayerBasicValue)**: Bu fonksiyon piyasa değeri değil, tüm oyun ekonomisinin **Haftalık Temel Referans Maaş Skalası**dır (`750,000.0 * (rating/99)^20`).
- **750,000 Değeri**: Nihai bir maaş tavanı (cap) değil, Rating 99 elit oyuncu için temel referans çıpadır. Kulüp, lig ve sözleşme çarpanlarıyla £850,000+ seviyesine çıkabilir.
- **Üs ve Ağırlıklar**: Üs kesin olarak `20.0`'dir. Peaked oyuncu `rating^20`, non-peaked oyuncu `%90 rating^20 + %10 potential^20` kullanır.
- **Piyasa Değeri Formülü (PlayerData.get_Value)**: Bağımsız olarak `0x02FC2E80` adresinde yer alır. 8. dereceden yetenek polinomu ve `(37^2 - min(age, 36)^2) / 18` kuadratik yaş terimi kullanır.
- **Dahili Değer vs Gösterilen Değer**: `get_Value` fonksiyonu ham `int32` GBP değeri üretir. `MonetaryHelpers.GetValueText` kullanıcının seçtiği para biriminin kur katsayısını (`exchangeRate`) uygulayarak "B", "M", "K" formatında metne dönüştürür.
- **Moral Semantiği ve Çelişkinin Çözümü**:
  - Oyun içi başlangıç morali: `MoraleStart = 5` (1-10 skalasında ortalama durum).
  - Matematiksel çarpan tavanı: `M = 10` olduğunda `0.8 + 0.02 * 10 = 1.00x` (kesintisiz nominal tavan değer).
  - Başlangıç morali M=5 iken değer %90'dır (`0.8 + 0.02 * 5 = 0.90x`).
- **Form Çarpanı Sabitleri**: Native ARM64 kodu `0x02FC3110` adresinde formülün `B * 0.2 * (F / 10)^2 * (G / 100) - B * 0.1` olduğunu doğrular.
- **Para Yuvarlama Motoru**: Bankacı Yuvarlaması (`MonetaryHelpers.toNearest`) ve katmanlı basamaklar kullanılır. Kod sırası hatasından dolayı 10,000 adımı ölü koddur.
- **Sözleşme Süresi Maaş Çarpanı**: Yılda net +%2.5 haftalık maaş artışı: `1.0 + (length - 1) / 40.0`.
- **Asgari Maaş Tabanı (Minimum Wage Floor)**: `Math.Max(wage, 50.0)` haftalık £50 taban sağlar. Üst sınır/tavan (cap) yoktur.
- **Menajer ve Personel Özellikleri**:
  - Trait `36`: Sözleşme Yenileme Müzakere İndirimi
  - Trait `38`: Transfer Maaş Müzakere İndirimi
  - Personel: `ModifierType.WageNegotiator` (ID `9`)
- **Negatif Çift Üs Kusuru Çözüldü**: SM25'in `(rating - 55)^8` hatası kendi clean-room modelimizde `normalizedAbility` ve pozitif güç fonksiyonu ile giderildi.
