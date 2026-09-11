# Kurtarılan Matematiksel Modeller (Recovered Mathematical Models)

Bu dokümanda Soccer Manager 2025 (SM25) IL2CPP AArch64 makine kodundan birebir çıkarılan ve matematiksel olarak doğrulanan modeller yer almaktadır.

---

## 1. Oyuncu Temel Değeri (Haftalık Temel Referans Maaş Skalası)

- **Kaynak**: `PlayerData.GetPlayerBasicValue`, native VA `0x02FC3FBC`
- **Ham Ofset (Raw Offset)**: `0x02FBFFBC`
- **Güven Seviyesi (Confidence)**: YÜKSEK (HIGH)
- **Girdiler**:
  - `playerRating` (R): `byte` (`0..255`, mantıksal `0..99`)
  - `isPeaked` (K): `bool`
  - `playerPotential` (P): `int32` (`0..99`, varsayılan 0)
- **Çıktı**: `double` (Temel Referans Maaş Değeri)

```text
ratingFactor    = (R / 99.0)^20.0
potentialFactor = (P / 99.0)^20.0

if K:
    basicValue = 750000.0 * ratingFactor
else:
    basicValue = 750000.0 * (0.9 * ratingFactor + 0.1 * potentialFactor)
```

### Kritik İsimlendirme ve Tasarım Analizi
- **750,000 Değeri**: Bu değer nihai bir maaş tavanı (maximum salary cap) **değildir**. Bu değer, oyun ekonomisindeki **Haftalık Temel Referans Maaş Skalası (Base Weekly Wage Benchmark Scale)** çıpasıdır.
- Kulüp seviyesi, lig seviyesi ve sözleşme süresi çarpanları uygulandığında, Rating 99 elit bir oyuncunun nihai maaşı £850,000+ seviyesine çıkabilmektedir.
- Bu fonksiyon `PlayerData.get_Value` (piyasa değeri) tarafından **kesinlikle çağrılmaz**. Doğrudan maaş üretim zincirine (`WageModifierForClub` ve `PlayerContract`) bağlanır.

---

## 2. Oyuncu Piyasa Değeri (PlayerData.get_Value)

- **Kaynak**: `PlayerData.get_Value`, native VA `0x02FC2E80`
- **Ham Ofset (Raw Offset)**: `0x02FBEE80`
- **Güven Seviyesi (Confidence)**: YÜKSEK (HIGH)
- **Girdiler**:
  - `age`: Oyuncunun yaşı (`int32`)
  - `R`: Rating / Genel Yetenek (`byte`, `0..99`)
  - `P`: Potansiyel (`PlayerData + 0x4C`, `byte`, `0..99`)
  - `D`: Düşüş Bayrağı (`PlayerData.get_InDecline()`, `bool`)
  - `G`: Sezon Süre Alma Yüzdesi (`SeasonGameTimePercentage()`, `double`, varsayılan 70.0)
  - `F`: Sezon Formu (`get_SeasonForm()`, `double`, varsayılan 7.0)
  - `M`: Moral (`get_Morale()`, `byte`, `1..10`; oyun başlangıç değeri 5, nötr tavan 10)
- **Çıktı**: 32-bit işaretli tamsayı (`int32`) ham ekonomik değer (GBP bazlı tamsayı)

### Adım 2.1: Yaş Terimi (Age Term)
```text
A = min(age, 36)
ageTerm = (37.0^2 - A^2) / 18.0
```

### Adım 2.2: Öz Yetenek ve Potansiyel Bileşeni (B)
```text
if D:
    B = 50.0 * (R - 55.0)^8 * ageTerm / 150000000.0
else:
    effectivePotential = max(P, R)
    currentPart   = 0.8 * 50.0 * (R - 55.0)^8 * ageTerm / 150000000.0
    potentialPart = 0.2 * 50.0 * (effectivePotential - 57.0)^8 * ageTerm / 150000000.0
    B = currentPart + potentialPart
```

### Adım 2.3: Dinamik Sezon Düzeltmeleri
```text
playingTimeAdjustment = B * ((G / 200.0)^2 - 0.15)
formAdjustment = B * 0.2 * (F / 10.0)^2 * (G / 100.0) - B * 0.1
moraleMultiplier = (double)(0.8f + 0.02f * (float)M)
```

### Adım 2.4: Moral Değeri Ayrımı (Semantik Başlangıç vs Nötr Çarpan)
- **Oyun İçi Durum (Semantic State)**: Oyuncu oluşturulduğunda `MoraleStart = 5` atanır (1-10 skalasında ortalama durum).
- **Matematiksel Çarpan Tavanı (Unpenalized Ceiling)**: `M = 10` olduğunda `0.8 + 0.02 * 10 = 1.00x` olur (yani piyasa değerinde moral kesintisi yapılmaz, nominal tavan).
- `M = 5` iken çarpan `0.8 + 0.02 * 5 = 0.90x` olur (%10 değer kaybı).
- `M = 1` iken çarpan `0.8 + 0.02 * 1 = 0.82x` olur (%18 değer kaybı).

### Adım 2.5: Asgari Taban ve Tamsayı Dönüşümü
```text
rawValue = (B + playingTimeAdjustment + formAdjustment) * moraleMultiplier
clampedValue = max(rawValue, 1000.0)

if clampedValue == double.PositiveInfinity:
    return int.MinValue
else:
    return (int)clampedValue  // FCVTZS ARM64 truncating cast
```

---

## 3. Dahili Ekonomik Değer vs Gösterilen Para Birimi (Internal vs Displayed Currency)

- **Dahili Ekonomik Değer (Internal Economic Value)**:
  `PlayerData.get_Value` fonksiyonundan dönen `int32` tamsayıdır. Bu değer oyunun temel para birimi ölçeğinde (varsayılan GBP £) ham tamsayı olarak saklanır.
- **Gösterilen Para Birimi Değeri (Displayed Currency Value)**:
  Kullanıcı arayüzünde gösterilirken `MonetaryHelpers.GetValueText(double value, bool userConversion = false)` metodu çağrılır (VA `0x031EC1BC`).
  - Eğer `userConversion == true` ise, oyuncunun ayarlarda seçtiği para birimine ait döviz kuru çarpanı (`exchangeRate`) ile çarpılır (`fmul d9, d9, d0`).
  - Ardından para birimi sembolü ("£", "€", "$" vb.) eklenir ve aşağıdaki dinamik basamak formatı uygulanır:
    - Değer >= 1,000,000,000.0 (1 Milyar): "B" son eki (Örn: "£1.2B")
    - Değer >= 10,000,000.0 (10 Milyon): "M" son eki, tek ondalık (Örn: "£15.0M")
    - Değer >= 1,000,000.0 (1 Milyon): "M" son eki (Örn: "£1.5M")
    - Değer >= 1,000.0 (1 Bin): "K" son eki (Örn: "299K", "950K")
    - Değer < 1,000.0: Düz sayı formatı (Örn: "£500")

---

## 4. Para Yuvarlama Motoru (MonetaryHelpers.RoundMonetaryValue)

Tüm finansal değerler (bonservis teklifleri, maaşlar, tazminatlar) `MonetaryHelpers.RoundMonetaryValue(double num)` üzerinden geçer:
- **Kaynak**: VA `0x031ECC38`, raw `0x031E8C38`
- **Dahili Yardımcı**: `MonetaryHelpers.toNearest`, VA `0x031EBFC8`
- **Algoritma**: Bankacı Yuvarlaması (Banker's Rounding / Round half to even)

| Sayı Aralığı (num < X) | Yuvarlama Adımı (Step) | Açıklama |
|---|---|---|
| `< 250` | 10 | 10'un katına yuvarlar |
| `< 1,000` | 25 | 25'in katına yuvarlar |
| `< 5,000` | 50 | 50'nin katına yuvarlar |
| `< 10,000` | 100 | 100'ün katına yuvarlar |
| `< 50,000` | 500 | 500'ün katına yuvarlar |
| `< 100,000` | 1,000 | 1,000'in katına yuvarlar |
| `< 5,000,000` | 5,000 | 5,000'in katına yuvarlar |
| `< 10,000,000` | 100,000 | 100,000'in katına yuvarlar |
| `< 50,000,000` | 500,000 | 500,000'in katına yuvarlar |
| `>= 50,000,000` | 1,000,000 | 1,000,000'un katına yuvarlar |

---

## 5. Haftalık Maaş Üretim Hattı (Weekly Wage Pipeline)

### Adım 5.1: Temel Maaş (Base Wage)
```text
baseWage = GetPlayerBasicValue(Rating, IsPeaked, Potential)
```

### Adım 5.2: Kulüp ve Lig Düzeltmesi (WageModifierForClub)
- **Kaynak**: VA `0x02FC08B4` (Club overload) ve VA `0x02FC03F4` (Helper overload)
```text
teamFit    = 0.5 + 0.5 * (playerRating / clubAverageRating)
teamTier   = clubAverageRating / 94.0
leagueTier = leagueRating / 89.0

wageModifier = baseWage * teamFit * teamTier * leagueTier
```

### Adım 5.3: Sözleşme Süresi Çarpanı (Contract Length)
- **Kaynak**: `PlayerContract.GenerateInitialWage` (VA `0x02FC0388`)
```text
contractLengthMultiplier = 1.0 + (contractLengthYears - 1) / 40.0
```
Her ek sözleşme yılı haftalık maaş talebini %2.5 artırır.

### Adım 5.4: Menajer Özellikleri ve Personel İndirimleri
- **Manager Traits**:
  - Trait `36` (Sözleşme Yenileme İndirimi)
  - Trait `38` (Transfer Maaş İndirimi)
- **Staff**: `ModifierType.WageNegotiator` (ID `9`)

### Adım 5.5: Asgari Maaş Tabanı (Minimum Wage Floor)
- **Kaynak**: `PlayerContract.GenerateNewContractWages` (VA `0x02FC0160`)
```text
rawWage = wageModifier * contractLengthMultiplier * traitModifiers
finalWage = Math.Max(rawWage, 50.0)  // Minimum asgari haftalık taban: £50
```

---

## 6. Oyuncu Gelişimi ve Yaşlanma Modelleri (Player Development & Aging)

Bu bölüm, `PlayerRatingCalculator`, `PlayerData` ve `Training` sınıflarından native AArch64 makine kodu analiziyle kurtarılmış kesin modelleri içerir.

### 6.1 Gelişim Dalga Modeli (Increasing Wave - SinWave)
- **Kaynak**: `PlayerRatingCalculator.UseIncreasingWave`, native VA `0x02FD9FB8`
- **Güven Seviyesi**: YÜKSEK (HIGH)
- **Sabitler**:
  - `0x01332970`: `90.0` (Derece cinsinden açı tavanı)
  - `0x013324A0`: `0.017453292519943295` ($\frac{\pi}{180}$ Deg2Rad)
- **Matematiksel Model**:
```text
progress = Math.Min(currentDaysSinceStart / totalDaysToPotentialAge, 1.0)
angleRadians = progress * 90.0 * (Math.PI / 180.0)
waveFactor = Math.Sin(angleRadians)

currentPreciseRating = startRating + (potentialRating - startRating) * waveFactor
```
- **Tasarım Özelliği**: Sinüsün ilk çeyreği kullanıldığı için gelişim genç yaşlarda ($\text{progress} < 0.5$) en hızlıdır ($\sin(45^\circ) \approx 0.707$). Potansiyele yaklaştıkça eğim yumuşar ve pürüzsüzce zirveye oturur.

### 6.2 Zirve Dönemi (Peak Plateau)
- **Kaynak**: `PlayerRatingCalculator.GetPeakRating`, native VA `0x02FDA860`
- Oyuncu `PotentialAge`'e ulaştığında potansiyel reytingine oturur.
- `DeclineDate` gününe kadar bu reytingde sabit (plateau) kalır. Herhangi bir düşüş gerçekleşmez.

### 6.3 Gerileme Yaşı ve Hesaplama Modeli (DeclineAge)
- **Kaynak**: `PlayerData.GetDeclineAge`, native VA `0x02FC67C4` ve `0x02FC6978`
- **Önbellek Alanı**: `PlayerData.cacheDeclineAge` (ofset `0x7C`)
- **Tohum (Seed)**: `(PlayerId + 0x48) * 1234`
- **Algoritma**:
```csharp
int baseDeclineAge;
if (potentialRating >= 90)
{
    baseDeclineAge = RNG.GetRand(30, 33); // Elit süperstarlar daha uzun zirvede kalır
}
else
{
    baseDeclineAge = RNG.GetRand(28, 31); // Standart oyuncular
}

// MEVKİ KONTROLÜ (KALECİ KORUMASI):
if (player.Position == 1) // 1 = Kaleci (Goalkeeper - GK)
{
    baseDeclineAge += 1; // Kaleciler 1 yaş daha geç yaşlanır!
}

// Potansiyel yaşı güvencesi: En az 2 yıl zirvede kalma kuralı
int declineAge = Math.Max(baseDeclineAge, potentialAge + 2);

// Başlangıç yaşı güvencesi: Mevcut yaştan küçük olamaz
declineAge = Math.Max(declineAge, (int)startAge);
```

### 6.4 Gerileme Dalga Modeli (Decreasing Wave & Longevity)
- **Kaynak**: `PlayerRatingCalculator.UseDecreasingWave` (VA `0x02FDA8F0`) ve `GetDecreasingRatingFromSinWavePrecise` (VA `0x02FDABA8`)
- **Uzun Ömürlülük (Longevity)**:
  - `longevity == true`: Düşüş hızı `RNG.GetRand(8, 13)` (Yavaş gerileme).
  - `longevity == false`: Düşüş hızı `RNG.GetRand(13, 18)` (Hızlı gerileme).
- **Asgari Reyting Tabanı**: `lowestRating = Math.Max(..., 60)` (Gerileyen oyuncu 60 puanın altına düşmez).
- **Gerileme Formülü**:
```text
progress = (currentAge - declineAge) / (lowestRatingAge - declineAge)
ratingLoss = progress * (peakRating - lowestRating)
currentPreciseRating = peakRating - ratingLoss
```

### 6.5 Antrenman Gelişim Modeli (Training Pipeline)
- **Kaynak**: `Training.GetTrainingIncrease`, native VA `0x02F30D0C`
- **Gençlik Bonusu (`ApplyYouthTrainingMod` at `0x02F3119C`)**:
  - Eğer `Age <= YouthAgeLimit` (21 yaş) ise: `trainingGain *= (1.0 + YouthMod)`
- **Menajer Özelliği**: Trait `27` (Training Boost).
- **Tesisler**: Kulüp antrenman tesis seviyesi (`TrainingGround`) ve antrenör kalitesi çarpanı.
