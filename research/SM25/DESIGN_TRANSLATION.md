# Temiz Oda Oyun Tasarımı Çevirisi (Clean-Room Game Design Translation)

Bu doküman, SM25 referans uygulamasında tespit edilen matematiksel davranışların, kendi oyunumuz (*Football Career Simulator*) için bağımsız, sürdürülebilir ve daha dengeli sistem tasarımlarına dönüştürülmesini açıklar.

---

## 1. Genel Karşılaştırma Özeti Tablosu

| Faktör | SM25 Davranışı | Bizim Modelimiz (Football Career Simulator) | Neden Bizim Modelimiz Daha İyi? |
|---|---|---|---|
| **Rating (Genel Yetenek)** | `(Rating - 55)^8` formülü. Çift üs nedeniyle 55 altı ratinglerde negatif farklar pozitif değere dönüşür (R=50 ile R=60 aynı katkıyı verir). | `normalizedAbility = (rating - min) / range` üzerinden `Math.Pow(normalized, exponent)` pozitif tabanlı eğri. | Monotonluk garantilenir. Düşük lig oyuncuları için anlamsız £1,000 tabanına çökme veya sahte değer sıçramaları önlenir. |
| **Potential (Potansiyel)** | `(max(P, R) - 57)^8` terimi. Çift üs anomalisi vardır. `InDecline` bayrağı aktif olduğunda potansiyel tek gecede sıfırlanır (uçurum etkisi). | `potentialGap * potentialWeightCurve(age)` enterpolasyonu ile yaş ilerledikçe potansiyel etkisi yumuşakça (smooth) söner. | Ani piyasa değeri çöküşleri engellenir; genç oyuncu primi yaşla orantılı ve kesintisiz azalır. |
| **Age (Yaş Eğrisi)** | `(37^2 - A^2) / 18` formülü. Değer en yüksek 16-18 yaşlarında çıkar ve yaş ilerledikçe sürekli monoton düşer. | `AgeMultiplierCurve` parçalı eğri (peak 24-26 yaş aralığında 1.16x, 30 yaş 0.90x, 36 yaş 0.46x). | Gerçek futbolda 25 yaşındaki elit oyuncu, henüz fiziksel olgunluğa erişmemiş 18 yaşındaki oyuncudan daha yüksek transfer bedeli görür. |
| **Gelişim Eğrisi (Growth Curve)** | Sinüs dalgasının ilk çeyreği: $\sin(\text{progress} \times 90^\circ)$. Başta hızlı, potansiyele yaklaştıkça yavaşlayan artış. | Parametrik sinüs/sigmoid eğrisi ile desteklenmiş gelişim modeli. | Genç oyuncuların ilk yıllarda hızla gelişmesi ve potansiyele ulaştıkça olgunlaşması gerçekçi bir gelişim dinamiği yaratır. |
| **Zirve Dönemi (Peak Plateau)** | Potansiyel yaşı ile Gerileme yaşı arasında reyting sabit kalır (düşüş olmaz). | Zirve yaş aralığında form ve maç sürekliliğine bağlı mikro-dalgalanmalarla desteklenen plato. | Oyuncuların prime döneminde yapay reyting dalgalanmaları olmadan istikrarlı kalması sağlanır. |
| **Gerileme Yaşı (Decline Age)** | RNG ile 28-31 (standart) veya 30-33 (süperstar). Kalecilere (GK) kesin +1 yaş koruması. | Konfigüre edilebilir mevki bazlı gerileme profili (Kaleciler +2, Stoperler +1, Kanatlar erken gerileme). | SM25'in kaleci koruması doğru bir prensiptir. Kendi oyunumuzda mevki bazlı fiziksel yıpranma farklarını daha detaylı modelleyebiliriz. |
| **Gerileme Hızı (Decline Speed)** | `Longevity` özelliğine göre yılda 8-13 (yavaş) veya 13-18 (hızlı) düşüş aralığı; reyting tabanı 60. | Fiziksel (hızlı) vs Zihinsel (yavaş/artan) ayrışmalı gerileme; konfigüre edilebilir asgari taban. | Yaşlanan oyuncuların salt reytinginin düşmesi yerine zihinsel olarak ustalaşıp fiziksel olarak yavaşlaması kariyer hikayesini zenginleştirir. |
| **Antrenman (Training)** | Günlük/haftalık antrenman seansları; 21 yaş altına ek gençlik çarpanı (`ApplyYouthTrainingMod`); tesis ve personel çarpanları. | Antrenman yoğunluğu, antrenör kalitesi, sakatlık riski ve gençlik akademisi bonusları içeren modüler yapı. | Antrenman salt otomatik reyting artışı olmak yerine oyuncu gelişiminde taktiksel bir karar haline gelir. |
| **Club Quality (Kulüp Kalitesi)** | `teamFit = 0.5 + 0.5 * rating / clubAvgRating`, `teamTier = clubAvgRating / 94.0`. | `teamFit` ve sınırlandırılmış `clubTier` (`Math.Clamp(club / 92.0, 0.40, 1.30)`). | Düşük lig kulüpleri için orantısız çarpan patlamaları engellenir, gerçekçi bütçe limitleri korunur. |
| **League Quality (Lig Kalitesi)** | `leagueRating / 89.0` düz doğrusal çarpan. | `Math.Clamp(leagueRating / 88.0, 0.40, 1.30)` sınırlandırılmış lig çarpanı. | Aşırı zayıf liglerde sıfıra çökme, süper liglerde kontrolsüz enflasyon önlenir. |
| **Contract Length (Sözleşme Süresi)** | `1.0 + (length - 1) / 40.0` (yılda +%2.5 haftalık maaş artışı). | `1.0 + (length - 1) * 0.025` parametrik ölçekleme. | Uzun sözleşme taahhüdü için makul risk primi korunur, konfigüre edilebilir yapıdadır. |
| **Wage Floor (Asgari Maaş Tabanı)** | `Math.Max(wage, 50)` (£50/hafta taban). Herhangi bir tavan (cap) yoktur. | `PlayerWageBalanceProfile.MinimumWeeklyWage` (£100/hafta) ve `MaximumWeeklyWage` (£1,000,000). | Alt lig oyuncuları için profesyonel asgari geçim tabanı sağlanır; bütçe aşımını engelleyen güvenlik tavanı vardır. |
| **Rounding (Yuvarlama)** | Banker's Rounding, ancak kod sırası hatası nedeniyle 10,000 adımı ölü koddur. | Net katmanlı basamak profili: `<1k: 25`, `<10k: 100`, `<50k: 500`, `>=50k: 1,000`. | Ölü kod içermez, finansal raporlarda doğal ve temiz basamaklar üretir. |

---

## 2. Oyuncu Gelişimi ve Yaşlanma Prensipleri (Development & Aging Principles)

### Sinüs Dalgası (SinWave) Büyüme Modeli Analizi
SM25'in `UseIncreasingWave` metodunda kullandığı:
$$\text{Gain} = (\text{Potential} - \text{Start}) \times \sin\left(\text{Progress} \times 90^\circ\right)$$
formülü, oyun tasarımı açısından son derece başarılı bir bulgudur:
- **Doğrusal (Linear) Artış Yerine Sinüs Tercihi**:
  Doğrusal artışta oyuncu 17 yaşında da 24 yaşında da her yıl aynı puanı kazanır; bu sıkıcı ve gerçek dışıdır.
  Sinüs dalgasında ise eğim başta çok diktir ($\sin'(0) = 1.0$), sona doğru yataylaşır ($\sin'(90^\circ) = 0.0$).
  Böylece 16-19 yaşındaki genç yetenekler hızla parlar, 22-24 yaşında gelişim yavaşlayarak olgunlaşır.

### Kalecilerin Yaşlanma Koruması (+1 Yaş)
SM25 native kodundaki:
```text
if (player.Position == 1) // Kaleci
    declineAge += 1;
```
mantığı, kalecilerin fiziksel efordan çok pozisyon alma ve tecrübeye dayalı oynaması gerçeğini yansıtır. Kendi oyunumuzda bu farkı sadece +1 yıl ile sınırlı tutmayıp, kalecilerin zirve yaşını 30-34, gerileme başlangıcını 35-37 olarak konfigüre edilebilir profillere bağlamalıyız.
