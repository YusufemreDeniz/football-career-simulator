# Kurtarılan Matematiksel Modeller

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

## 5. Nihai Haftalık Ücret Sistemi

### 5.1 Ücret kavramlarının ayrımı

| Kavram | Kaynak / alan | Anlam | Güven |
|---|---|---|---|
| Mevcut sözleşme ücreti | `PlayerContract.Wage`, alan `+0x18`; getter VA `0x02FBFB18` | Önceden hesaplanıp sözleşmeye kaydedilmiş haftalık tamsayıdır. Getter yeni hesap yapmaz. | KESİN |
| Otomatik yeni sözleşme ücreti | `PlayerContract.Generate`, VA `0x02FBFB30` | AI/otomatik üretimde kompleks ücret, personel çarpanı ve parasal yuvarlama sonucudur. | KESİN |
| Kabul edilebilir ücret | `PlayerData.GetAcceptableNewContractWage`, VA `0x02FC3E9C` | Hedef kulüp ve süreyle üretilen eşiktir. Yönetilen kulüpte yenilemeyi reddeden oyuncu için `1,5x` yapılır. | KESİN |
| Talep edilen yeni sözleşme ücreti | `PlayerData.GetRequestedNewContractWage`, VA `0x02FC3F80` | Kabul edilebilir ücretin tohumlu deterministik rastgele `[1,00, 1,10]` çarpanla yükseltilip yuvarlanmış biçimidir. | KESİN |
| Görüşme ekranı beklentisi | `ContractInfo.GetNegotiationWageForUserManager`, VA `0x03027D28` | Kompleks tabanın tohumlu deterministik rastgele `[1,10, 1,25]` aralığındaki, `100` adımına yuvarlanmış gösterimidir. `DemandedWages` alanını bu rastgele değere çevirmeden döner. | KESİN |
| Kullanıcı görüşmesindeki nihai ücret | `PlayerContract.CreateNew`, VA `0x02FC01EC` | `WillAcceptWage` kabulünden sonra `ContractInfo.Wage +0x18` teklifinin sözleşmenin `Wage +0x18` alanına kopyalanmış halidir. | KESİN |

### 5.2 Temel ücret

```text
ratingFactor    = (R / 99,0)^20
potentialFactor = (P / 99,0)^20

Peaked ise:
    Base = 750000 × ratingFactor
Değilse:
    Base = 750000 × (0,9 × ratingFactor + 0,1 × potentialFactor)
```

Kaynak `PlayerData.GetPlayerBasicValue`, VA `0x02FC3FBC`; güven `KESİN`.

### 5.3 İki `WageModifierForClub` overload'u

Overload'lar birbirlerini çağırmaz ve takım uyumu kesrinin yönü farklıdır.

`PlayerData.WageModifierForClub(Club)`, VA `0x02FC08B4`:

```text
ClubAdjusted = Base
    × (0,5 + 0,5 × PlayerRating / ClubAverageRating)
    × (ClubAverageRating / 94,0)
    × (LeagueRating / 89,0)
```

Bu overload `PlayerContract.Generate` ve kullanıcı sözleşme görüşmesi zincirinde kullanılır. Güven `KESİN`.

`PlayerData.WageModifierForClub(PlayerContractHelper)`, VA `0x02FC03F4`:

```text
HelperAdjusted = Base
    × (0,5 + 0,5 × Helper.TeamAverageRating / PlayerRating)
    × Helper.TeamRatingMultiplier
    × Helper.LeagueRatingMultiplier
```

`TeamRatingMultiplier = ClubAverageRating / 94,0`, `LeagueRatingMultiplier = LeagueRating / 89,0`; `SetupClub` takım çarpanını en az `0,7`, takım ortalamasını en az `60` yapar. Bu yol toplu lig/AI sözleşme üretiminde kullanılır. Güven `YÜKSEK GÜVEN`.

### 5.4 Sözleşme süresi

`PlayerContract.LengthMultiplier`, VA `0x02FBFCB4`:

```text
LengthMultiplier = 1,0 + (ContractLengthYears - 1) / 40,0
```

| Yıl | Çarpan |
|---:|---:|
| 1 | 1,000 |
| 2 | 1,025 |
| 3 | 1,050 |
| 4 | 1,075 |
| 5 | 1,100 |

Güven `KESİN`.

### 5.5 Menajer özelliği ve personel değiştiricisi

Kompleks üretici `PlayerContract.GenerateNewContractWages(PlayerData, Club, double)`, VA `0x02FBFCD4`, yalnızca hedef kullanıcı kulübüyse bir menajer özelliği uygular:

- Oyuncu başka kulüptense `TraitId 38`.
- Oyuncu kullanıcı kulübündeyse `TraitId 36`.
- İki özellik aynı hesapta birlikte uygulanmaz.
- Seviye verisindeki `float +0x24` yüzde alanı `Helpers.ApplyPercentageModifier`, VA `0x02FD6058` ile uygulanır.

```text
TraitAdjusted = Trunc(LengthAdjusted)
TraitAdjusted = Trunc(TraitAdjusted × (1 + TraitPercentage / 100))
TraitAdjusted = max(TraitAdjusted, 0)
```

`PlayerContract.Generate` daha sonra yalnızca kullanıcı tarafından yönetilen hedef kulüpte `ModifierType.WageNegotiator = 9` çağırır. `ValueModifierController.ApplyModifier`, VA `0x02F20E54`, kaynak veriden aldığı `float +0x18` çarpanını doğrudan uygular:

```text
StaffAdjusted = TraitAdjusted × WageNegotiatorDataMultiplier
```

Yerleşim ve işlem `KESİN`; asset verisinde `TraitId 36 = %−5`, `TraitId 38 = %−5` ve `WageNegotiator = 0,90` doğrulanmıştır.

Oyuncunun hırsı, sadakati, açgözlülüğü, profesyonelliği, morali, formu, itibarı, yaşı ve kadro rolü bu nihai üretici içinde doğrudan okunmaz. Potansiyel ve zirve durumu yalnızca `Base` içinde etkilidir. Sadakat, ayrı `PlayerConcernContractWage` memnuniyet eşiğinde kullanılır; final ücret formülüne dahil değildir. Güven `YÜKSEK GÜVEN`.

### 5.6 Tamsayı dönüşümü, taban, tavan ve yuvarlama

Kompleks üreticide kulüp ve süre çarpımı `FCVTZS` ile sıfıra doğru kesilir. Menajer özelliğinden sonra yeniden `FCVTZS` uygulanır. Sonra `Math.Max(value, 50)` ile haftalık `£50` tabanı konur.

`PlayerContract.Generate` zincirinde personel çarpanından sonra ikinci bir `£50` kontrolü yoktur; sonuç doğrudan `MonetaryHelpers.RoundMonetaryValue` ile yuvarlanır. Native zincirde global üst ücret tavanı, kulüp bütçe tavanı veya mevcut ücrete bağlı alt sınır bulunmadı. Güven `KESİN`.

Yuvarlama bankacı yuvarlamasıdır ve bölüm 4'teki büyüklüğe bağlı basamakları kullanır. `5.000.000` kontrolünden sonra yer alan `1.000.000` karşılaştırması erişilemez olduğundan `10.000` adımı fiilen kullanılmaz. Güven `KESİN`.

### 5.7 Otomatik nihai ücret formülü

```text
Base              = GetPlayerBasicValue(R, IsPeaked, P)
ClubAdjusted      = Base × TeamFit × ClubTier × LeagueTier
LengthAdjusted    = ClubAdjusted × (1 + (Years - 1) / 40)
IntegerAdjusted   = Trunc(LengthAdjusted)
TraitAdjusted     = hedef kullanıcı kulübüyse
                      max(Trunc(IntegerAdjusted × (1 + TraitPct / 100)), 0)
                    değilse IntegerAdjusted
Floored           = max(TraitAdjusted, 50)
StaffAdjusted     = hedef kullanıcı kulübüyse Floored × WageNegotiatorMultiplier
                    değilse Floored
FinalWeeklyWage   = RoundMonetaryValue(StaffAdjusted)
```

Bu otomatik/AI sözleşme formülü ve veri katsayıları `KESİN`dir.

Kullanıcı görüşmesinde kabul edilen nihai haftalık ücret için:

```text
DemandedWages   = GetAcceptableNewContractWage(...)
Acceptance      = ContractInfo.WillAcceptWage(OfferedWage, DemandedWages, IsTransferWanted)
Accepted        = Acceptance == eContractRejectReason.Accepted (10)
FinalWeeklyWage = Accepted ise ContractInfo.Wage teklifinin PlayerContract.Wage alanına kopyası
```

### 5.8 Etkileşimli görüşme karar formülü

```text
PatienceAfter = PatienceBefore
                - 1
                - SquadStatusPenalty
                - ContractLengthPenalty
                - WagePenalty

if OfferedWage <= DemandedWages / 2:
    Result = Insulted(7)
else if !WillPlayerRenewContract and OfferedWage < 2 * DemandedWages:
    Result = Wages(2)
else if OfferedWage < DemandedWages:
    Result = Wages(2)
else if OfferedSquadStatus kabul edilmiyorsa:
    Result = SquadStatus(4)
else if OfferedYears < MinimumContractOfferLength:
    Result = ContractTooShort(1)
else if OfferedYears > (Age < 32 ? 5 : 3):
    Result = ContractTooLong(0)
else:
    Result = Accepted(10)

Terminate = Result == Accepted
            or Result == Insulted
            or PatienceAfter <= 0
```

Bu formülde signing fee ve bonus kabul kapısı yoktur. Güven `KESİN`.

### 5.9 Signing-on fee

```text
if Renewal:
    Factor = Rating > ClubAverage
        ? SeededRand(0.05, 0.10)
        : SeededRand(0.025, 0.05)
else if FreeAgent:
    Factor = Age < 23 ? 0.10 : 0.25
else:
    Factor = 0.10

PlayerFee = RoundMonetaryValue(PlayerValue * Factor)
PlayerFee = UserClub ? RoundMonetaryValue(PlayerFee * 0.90) : PlayerFee
Compensation = FreeAgent and Age <= 23
    ? RoundMonetaryValue(PlayerValue * 0.20)
    : 0
Total = PlayerFee + Compensation
Total = UserClub and Trait33
    ? max(Total * (1 + Trait33Percentage / 100), 0)
    : Total
SigningFee = Trunc(Total)
```

`TraitId 33` seviyeleri `%−5/%−10`; `TransferNegotiator=8` çarpanı `0,90`dır. Tohum `PlayerId + Game.Seed`dir. Güven `KESİN`.

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

---

## 7. Sözleşme Talebi ve Müzakere Ücret Modeli (ContractInfo.DemandedWages +0x24)

- **Kaynaklar**:
  - `PlayerContract.LengthMultiplier` (VA `0x02FBFCB4`)
  - `PlayerContract.GenerateNewContractWages` (VA `0x02FBFCD4`)
  - `PlayerData.GetAcceptableNewContractWage` (VA `0x02FC3E9C`)
  - `PlayerData.GetRequestedNewContractWage` (VA `0x02FC3F80`)
  - `PlayerData.GetRequestedWageMultiplier` (VA `0x02FC3A0C`)
  - `ContractInfo.GetNegotiationWageForUserManager` (VA `0x03027D28`)
- **Güven Seviyesi**: KESİN (ARM64 disassembled and verified)

### 7.1 Sözleşme Süresi Çarpanı (LengthMultiplier)
```text
LengthMultiplier = 1.0 + (ContractYears - 1) / 40.0
```
- 1 Yıl: 1.000 (+%0.0)
- 2 Yıl: 1.025 (+%2.5)
- 3 Yıl: 1.050 (+%5.0)
- 4 Yıl: 1.075 (+%7.5)
- 5 Yıl: 1.100 (+%10.0)

### 7.2 Taban Kontrat Ücreti ve Menajer Trait İndirimi
```text
WageBase = WageModifierForClub(TargetClub)
WageLength = (int)(WageBase * LengthMultiplier)

if IsTargetUserClub:
    if IsTransfer and HasTrait(38):  // HardBargain (-5%)
        WageLength = ApplyPercentageModifier(WageLength, -5.0)
    if IsRenewal and HasTrait(36):   // ToughNegotiator (-5%)
        WageLength = ApplyPercentageModifier(WageLength, -5.0)

BaseContractWage = Math.Max(50, WageLength)
```

### 7.3 Kabul Edilebilir Eşik ve Ayrılmak İsteyen Oyuncu Cezası
```text
AcceptableWage = BaseContractWage
if IsRenewal and not WillPlayerRenewContract():
    AcceptableWage = AcceptableWage + (AcceptableWage / 2)  // +50% ceza
```

### 7.4 İlk Görüşme Gösterim Talebi (UI Initial Demanded Wage)
```text
Seed = PlayerId + Game.Seed
RandMultiplier = RNG.GetRand(1.10, 1.25)
InitialDemandedWage = Extensions.Round(BaseContractWage * RandMultiplier, 100)
```

### 7.5 Ret Sonrası Yeni Talep (Rejection Recalculation)
```text
Seed = PlayerId + ClubId
RandMultiplier = RNG.GetRand(1.00, 1.10)
RequestedWage = MonetaryHelpers.RoundMonetaryValue(AcceptableWage * RandMultiplier)
ContractInfo.DemandedWages = RequestedWage
```
