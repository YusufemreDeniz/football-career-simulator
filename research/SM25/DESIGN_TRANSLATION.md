# Temiz Oda Oyun Tasarımı Çevirisi

Bu doküman, SM25 referans uygulamasında tespit edilen matematiksel davranışların, kendi oyunumuz (*Football Career Simulator*) için bağımsız, sürdürülebilir ve daha dengeli sistem tasarımlarına dönüştürülmesini açıklar.

---

## 1. Genel Karşılaştırma Özeti Tablosu

| Faktör | SM25 Davranışı | Bizim Modelimiz (Football Career Simulator) | Neden Bizim Modelimiz Daha İyi? |
|---|---|---|---|
| **Genel yetenek** | `(Rating - 55)^8` formülü. Çift üs nedeniyle 55 altı ratinglerde negatif farklar pozitif değere dönüşür (R=50 ile R=60 aynı katkıyı verir). | `normalizedAbility = (rating - min) / range` üzerinden `Math.Pow(normalized, exponent)` pozitif tabanlı eğri. | Monotonluk garantilenir. Düşük lig oyuncuları için anlamsız £1,000 tabanına çökme veya sahte değer sıçramaları önlenir. |
| **Potansiyel** | `(max(P, R) - 57)^8` terimi. Çift üs anomalisi vardır. `InDecline` bayrağı aktif olduğunda potansiyel tek gecede sıfırlanır (uçurum etkisi). | `potentialGap * potentialWeightCurve(age)` enterpolasyonu ile yaş ilerledikçe potansiyel etkisi yumuşakça söner. | Ani piyasa değeri çöküşleri engellenir; genç oyuncu primi yaşla orantılı ve kesintisiz azalır. |
| **Yaş eğrisi** | `(37^2 - A^2) / 18` formülü. Değer en yüksek 16-18 yaşlarında çıkar ve yaş ilerledikçe sürekli monoton düşer. | `AgeMultiplierCurve` parçalı eğri (zirve 24-26 yaş aralığında 1.16x, 30 yaş 0.90x, 36 yaş 0.46x). | Gerçek futbolda 25 yaşındaki elit oyuncu, henüz fiziksel olgunluğa erişmemiş 18 yaşındaki oyuncudan daha yüksek transfer bedeli görür. |
| **Gelişim Eğrisi (Growth Curve)** | Sinüs dalgasının ilk çeyreği: $\sin(\text{progress} \times 90^\circ)$. Başta hızlı, potansiyele yaklaştıkça yavaşlayan artış. | `SinWaveGrowthCalculator`: haftalık kazanç × $\sin(\text{kalan potansiyel oranı} \times 90^\circ)$ × mevki zirve yaşına göre yaş çarpanı. Üretim gelişim yolu budur (D-418). | Genç oyuncuların ilk yıllarda hızla gelişmesi ve potansiyele ulaştıkça olgunlaşması gerçekçi bir gelişim dinamiği yaratır. |
| **Zirve Dönemi (Peak Plateau)** | Potansiyel yaşı ile Gerileme yaşı arasında reyting sabit kalır (düşüş olmaz). | Zirve yaş aralığında form ve maç sürekliliğine bağlı mikro-dalgalanmalarla desteklenen plato. | Oyuncuların prime döneminde yapay reyting dalgalanmaları olmadan istikrarlı kalması sağlanır. |
| **Gerileme Yaşı (Decline Age)** | RNG ile 28-31 (standart) veya 30-33 (süperstar). Kalecilere (GK) kesin +1 yaş koruması. | `PositionDeclineProfile`: orta saha/forvet 30, stoper 31 (+1), kanat/bek 28 (erken), kaleci zirve 30 ve gerileme 35. Deterministiktir; RNG yaş aralığı kullanılmaz. | SM25'in kaleci koruması doğru bir prensiptir. Kendi oyunumuzda mevki bazlı fiziksel yıpranma farklarını daha detaylı modelleyebiliriz. |
| **Gerileme Hızı (Decline Speed)** | `Longevity` özelliğine göre yılda 8-13 (yavaş) veya 13-18 (hızlı) düşüş aralığı; reyting tabanı 60. | Uygulanan: mevkiye göre yıllık 1–3 puan yetenek ve 0–2 puan potansiyel düşüşü, taban `MinAbility`. Fiziksel/zihinsel ayrışma **henüz uygulanmadı** (hedef tasarım). | Yaşlanan oyuncuların salt reytinginin düşmesi yerine zihinsel olarak ustalaşıp fiziksel olarak yavaşlaması kariyer hikayesini zenginleştirir. |
| **Antrenman (Training)** | Günlük/haftalık antrenman seansları; 21 yaş altına ek gençlik çarpanı (`ApplyYouthTrainingMod`); tesis ve personel çarpanları. | Antrenman yoğunluğu/odağı + kulüp tesis seviyesi çarpanı (1,0x–1,4x, `PlayerCareerDevelopmentService`) + SinWave yaş/potansiyel eğrisi. Antrenör kalitesi **henüz uygulanmadı**. | Antrenman salt otomatik reyting artışı olmak yerine oyuncu gelişiminde taktiksel bir karar haline gelir. |
| **Kulüp kalitesi** | `teamFit = 0.5 + 0.5 * rating / clubAvgRating`, `teamTier = clubAvgRating / 94.0`. | `teamFit` ve sınırlandırılmış `clubTier` (`Math.Clamp(club / 92.0, 0.40, 1.30)`). | Düşük lig kulüpleri için orantısız çarpan patlamaları engellenir, gerçekçi bütçe limitleri korunur. |
| **Lig kalitesi** | `leagueRating / 89.0` düz doğrusal çarpan. | `Math.Clamp(leagueRating / 88.0, 0.40, 1.30)` sınırlandırılmış lig çarpanı. | Aşırı zayıf liglerde sıfıra çökme, süper liglerde kontrolsüz enflasyon önlenir. |
| **Sözleşme süresi** | `1.0 + (length - 1) / 40.0` (yılda +%2.5 haftalık maaş artışı). | `1.0 + (length - 1) * 0.025` parametrik ölçekleme. | Uzun sözleşme taahhüdü için makul risk primi korunur, konfigüre edilebilir yapıdadır. |
| **Asgari ücret tabanı** | `Math.Max(wage, 50)` (£50/hafta taban). Global üst tavan yoktur. | `PlayerWageBalanceProfile.MinimumWeeklyWage` (£100/hafta) ve `MaximumWeeklyWage` (£1,000,000). | Alt lig oyuncuları için profesyonel asgari geçim tabanı sağlanır; bütçe aşımını engelleyen bağımsız güvenlik tavanı vardır. |
| **Yuvarlama** | Bankacı yuvarlaması; kod sırası nedeniyle `10.000` adımı ölüdür. | Büyüklüğe göre profile bağlı basamaklar ve sıfırdan uzağa yarım yuvarlama. | Ölü kod içermez, finansal raporlarda doğal ve temiz basamaklar üretir. |
| **Görüşme kararı** | Wage, squad status ve contract length sıralı kapıları; aggregate score yok. | `ContractOfferEvaluator`, aynı kapıları bağımsız ve test edilebilir biçimde çalıştırır; üretimde `PlayerContractProposalService` kabul kapısıdır. | Ret nedeni ve state geçişi UI/persistence'tan ayrıdır. |
| **Menajer pazarlık becerisi** | Native'de yoktur (yalnız `TraitId 36/38` %−5). | `NegotiationSkill` 50 üstü her puan %0,3 indirim, en çok %15; native trait ile aynı birim (yüzde, negatif = indirim), yalnız kullanıcı kulübü. | Menajer gelişimi pazarlıkta hissedilir; native parity iddiası taşımaz. |
| **Sabır** | Her teklif öncesi `Current - 1 - rol - süre - ücret` cezası. | `ContractNegotiationState` ve profile taşınmış native matrisler. | Tur davranışı tekrar üretilebilir ve denetlenebilirdir. |
| **İmza/bonus kabulü** | Signing fee saklanıp ödenir fakat gate değildir; bonus alanı yoktur. | Native parity profilinde kapalı; `SimulatorBalance` profilinde opsiyonel eşikler. | Denge kuralı native kanıt gibi sunulmaz. |

---

## 2. Oyuncu Gelişimi ve Yaşlanma Prensipleri

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
mantığı, kalecilerin fiziksel efordan çok pozisyon alma ve tecrübeye dayalı oynaması gerçeğini yansıtır. Kendi oyunumuzda bu fark `PositionDeclineProfile.Goalkeeper` ile uygulanır: zirve 30, gerileme 35, yıllık düşüş 35–36 yaşta 1, 37+ yaşta 2 puan.

---

## 3. Haftalık Ücret Temiz Oda Uygulaması

`PlayerWageModel` bağımsız, durumsuz ve deterministiktir. Native koddan kopyalanan bir uygulama değildir; yalnız aynı bağımlılık ailelerini açıkça modelleyip denge değerlerini `PlayerWageBalanceProfile` içine taşır:

- Genel yetenek ve potansiyel, pozitif tabanlı monoton eğri üretir.
- Zirve durumu potansiyel etkisini kapatır; yaş doğrudan ücret çarpanı değildir.
- Kulüp ve lig seviyesi ayrı, sınırlandırılmış çarpanlardır.
- Sözleşme süresi 1-5 yıl aralığında profile bağlı artar.
- Menajer özelliği, personel etkisi, kadro rolü ve serbest oyuncu durumu ayrı girdiler ve ayrı kırılım alanlarıdır.
- Ham, yuvarlanmış ve alt/üst sınıra uygulanmış ücret ayrı ayrı döndürülür.
- Piyasa değeri modeli ücret modeline bağlanmamıştır; native zincir de `PlayerData.get_Value` çağırmaz.

`ScoutTransferValuationModel` içindeki eski kuadratik ücret tahmini kaldırılmış ve önerilen haftalık ücret bu modelden alınmıştır. Scout yüzeyinde lig gücü yoksa yönetilen kulüp gücü açık bir geri dönüş değeri olarak kullanılır. Bu adaptör dışında UI veya persistence katmanı değiştirilmemiştir.

Analiz matrisi `research/SM25/simulations/weekly_wage_simulation.csv` dosyasına 2.340 kontrollü kombinasyon yazar. `CA`, `PA`, yaş, kulüp/lig gücü, sözleşme süresi, özellik/durum, ham, yuvarlanmış ve sınırlandırılmış ücret sütunları bulunur.

---

## 4. Sözleşme Görüşmesi Temiz Oda Uygulaması

`PlayerContractDemandModel`, `ContractOfferEvaluator` ve `ContractNegotiationState` application katmanında UI ve persistence bağımsızdır. `Sm25NativeParity` profilinde yalnız doğrulanan ücret, rol, süre ve sabır davranışları etkindir. Aynı player/game seed ve aynı girdiler aynı sonucu üretir; bu sabit değil seeded deterministic random davranıştır.

`SimulatorBalance` profili, native `ContractInfo` içinde bulunmayan imza parası ve `SimulatorBonus` kabul eşiklerini isteğe bağlı olarak açar. Bu iki kapı Football Career Simulator denge kuralıdır. Mevcut proposal persistence'ı, UI ve kariyer akışı değiştirilmemiştir.

Kontrollü görüşme matrisi `research/SM25/simulations/contract_negotiation_matrix.csv` dosyasına 288 satır yazar. Wage teklifi, signing fee, squad status, patience, tur, RNG girdisi, kabul/ret ve ret nedeni ayrı sütunlardır.
