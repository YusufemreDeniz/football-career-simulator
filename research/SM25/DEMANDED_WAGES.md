# SM25 Demanded Wages (+0x24) Native ARM64 Araştırma Raporu

Bu belge, Soccer Manager 2025 (SM25) `libil2cpp.so` (v25.2.2 ARM64) içerisinde oyuncu sözleşmesi görüşmelerinde kullanılan `ContractInfo.DemandedWages (+0x24)` alanının native üretim, güncelleme, ret ve kabul matematiğini eksiksiz açıklar.

---

## 1. Native Entry Points ve Writer Analizi

`ContractInfo.DemandedWages` alanı nesne başlangıcından itibaren `+0x24` ofsetindedir (`int`).
`libil2cpp.so` içerisinde bu ofsete doğrudan yazan (`STR W?, [X?, #0x24]`) tüm yerler aşağıda listelenmiştir:

| Fonksiyon | Native VA | Instruction | Yazılan Değer Kaynağı | Çağrılma Durumu / Bağlam | Güven |
|---|---|---|---|---|---|
| `ContractInfo.set_DemandedWages(int)` | `0x030278F4` | `str w1, [x0, #0x24]` | Parametre `w1` | Standart IL2CPP property setter | KESİN |
| `ContractInfo.RecalculateContract` | `0x03027B74` | `str w0, [x20, #0x24]` | `PlayerData.GenerateNewContractWages(byte)` | `ContractInfo.GetDefaultNewContractDemands` (görüşme ilk açılışı) | KESİN |
| `ContractInfo.GetNegotiationWageForUserManager` | `0x03027DB0` | `str w9, [x19, #0x24]` | `PlayerContract.Wage (+0x18)` | `OfferContractModal.UpdateDemandsPanel` (UI ekranında gösterilen baz talep) | KESİN |
| `TransferBid.RejectContractOffer` | `0x0303A964` | `str w8, [x9, #0x24]` | `demandedWages` (parametre) | `TransferBid.TryAcceptContractOffer` (ret dalı sonrası yeni talep) | KESİN |
| `TransferBid.TryAcceptContractOffer` | `0x0303B2D4` | `str w0, [x20, #0x24]` | `PlayerData.GetAcceptableNewContractWage` | Teklif kabul kapısı (`WillAcceptWage`) öncesi eşik yazımı | KESİN |
| `OfferContractModal.Populate` | `0x030E0B94` | `str w0, [x8, #0x24]` | Sıfırlama / Başlangıç | Modal açılışında ContractInfo reset | KESİN |

---

## 2. İlk Demanded Wage vs Sonraki Demanded Wage Ayrımı

SM25'te kontrat görüşmesi boyunca 3 farklı wage aşaması vardır:

1. **Görüşme İlk Açıldığında Gösterilen Demanded Wage (UI Initial Demand)**:
   - Fonksiyon: `ContractInfo.GetNegotiationWageForUserManager(PlayerData)` (`VA: 0x03027D28`).
   - Taban ücret: `PlayerContract.GenerateNewContractWages(playerData, currentUserClub, lengthMultiplier)`.
   - Seed: `playerData.Id + Game.Seed`.
   - Çarpan: `RNG.GetRand(baseWage * 1.10, baseWage * 1.25)`.
   - Yuvarlama: `Extensions.Round(randWage, 100)` (en yakın 100'e yuvarlama).
   - Kullanıcı ilk açılışta modalda bu şişirilmiş müzakere talebini görür.

2. **Kabul Eşiği (Acceptable Wage Gate)**:
   - Fonksiyon: `PlayerData.GetAcceptableNewContractWage(short clubFromId, int contractLength)` (`VA: 0x02FC3E9C`).
   - Taban: `PlayerContract.Generate(playerData, club, contractLength)` -> `contract.Wage`.
   - Ceza: Eğer `player.IsOwnedByManagedClub` VE `!player.WillPlayerRenewContract()` ise:
     `wage = wage + (wage / 2)` (**%50 ceza / artış**).
   - Eşik: Kullanıcının teklifi bu değerden küçükse teklif doğrudan `Wages (2)` veya yarıdan azsa `Insulted (7)` olarak reddedilir.

3. **Ret Sonrası Yeni Talep Edilen Ücret (Rejection Counter Demand)**:
   - Fonksiyon: `PlayerData.GetRequestedNewContractWage(short clubFromId, int contractLength, bool considerConcerns = true)` (`VA: 0x02FC3F80`).
   - Eşik ücreti alınır: `acceptableWage = GetAcceptableNewContractWage(...)`.
   - Müzakere çarpanı: `multiplier = GetRequestedWageMultiplier()` (`VA: 0x02FC3A0C`).
     - Seed: `playerData.Id + playerData.ClubId`.
     - Çarpan: `RNG.GetRand(1.0, 1.1)` (%0 ile %10 arası marj).
     - Eski seed restore edilir: `RNG.SetSeed(oldSeed)`.
   - Parasal yuvarlama: `MonetaryHelpers.RoundMonetaryValue(acceptableWage * multiplier)`.
   - Sonuç `TransferBid.RejectContractOffer` üzerinden `ContractInfo.DemandedWages (+0x24)` alanına yazılır.

---

## 3. Demanded Wage Girdi Değişkenleri Analizi

| Girdi | Kullanım Durumu | Hangi Fonksiyonda? | Nasıl Etkiler? | Güven |
|---|---|---|---|---|
| **Rating** | **KULLANILIYOR** | `WageModifierForClub` -> `GetPlayerBasicValue` | Taban piyasa değerini ve maaşı belirler | KESİN |
| **Potential** | **KULLANILIYOR** | `WageModifierForClub` -> `GetPlayerBasicValue` | Genç ve yüksek potansiyelli oyuncunun tabanını yükseltir | KESİN |
| **Age** | **KULLANILIYOR** | `WageModifierForClub` -> `GetPlayerBasicValue` | Yaş eğrisine göre taban değeri çarpar | KESİN |
| **Player Market Value** | **KULLANILIYOR** | `GetPlayerBasicValue` üzerinden | Doğrudan taban ücretin temel girdisidir | KESİN |
| **Current Wage** | **KULLANILMIYOR** | Hiçbir yerde okunmaz | Oyuncunun eski maaşı talep formülüne girmez | KESİN |
| **Target Club** | **KULLANILIYOR** | `WageModifierForClub(Club)` | Teklif veren kulübün `WageQuality` ve lig çarpanı uygulanır | KESİN |
| **Current Club** | **KISMİ** | `GetRequestedWageMultiplier` | Rejection RNG seed'i için `Id + ClubId` olarak kullanılır | KESİN |
| **Contract Length** | **KULLANILIYOR** | `PlayerContract.LengthMultiplier` | `1.0 + (length - 1) / 40.0` (Her ekstra yıl +%2.5) | KESİN |
| **Manager Traits** | **KULLANILIYOR** | `GenerateNewContractWages` | Transfer: Trait 38 (`HardBargain`, -5%), Renewal: Trait 36 (`ToughNegotiator`, -5%) | KESİN |
| **Staff Modifiers** | **KULLANILMIYOR** | Demanded wage içinde yok | WageNegotiator staff'ı acceptance/patience aşamasındadır | KESİN |
| **Squad Status** | **KULLANILMIYOR** | Demanded wage içinde yok | Rol kabulü bağımsız kapıdır (`WillAcceptSquadStatus`) | KESİN |
| **Form / Morale** | **KULLANILMIYOR** | Demanded wage içinde yok | Taban maaşa doğrudan girmez | KESİN |
| **Playing Time** | **KULLANILMIYOR** | Demanded wage içinde yok | Yalnızca `WillPlayerRenewContract` kararını dolaylı etkileyebilir | KESİN |
| **Unhappy / Refusal** | **KULLANILIYOR** | `GetAcceptableNewContractWage` | `!WillPlayerRenewContract()` ise `+50%` ceza (`wage + wage/2`) | KESİN |
| **RNG (İlk Açılış)** | **KULLANILIYOR** | `GetNegotiationWageForUserManager` | Seed `PlayerId + Game.Seed`, Aralık `[1.10, 1.25]`, Round to 100 | KESİN |
| **RNG (Ret Sonrası)**| **KULLANILIYOR** | `GetRequestedWageMultiplier` | Seed `PlayerId + ClubId`, Aralık `[1.00, 1.10]`, Monetary Round | KESİN |

---

## 4. Contract Length Demanded Wage Etkisi

ARM64 kanıtı: `PlayerContract.LengthMultiplier` (`VA: 0x02FBFCB4`):
```arm64
0x02FBFCB4: sub  w8, w1, #1               ; length - 1
0x02FBFCB8: scvtf d0, w8                  ; (double)(length - 1)
0x02FBFCBC: mov  x8, #0x4044000000000000  ; 40.0 (IEEE-754 double)
0x02FBFCC0: fmov d1, x8
0x02FBFCC4: fdiv d0, d0, d1               ; (length - 1) / 40.0
0x02FBFCC8: fmov d1, #1.0
0x02FBFCCC: fadd d0, d0, d1               ; 1.0 + (length - 1) / 40.0
0x02FBFCD0: ret
```

Matematiksel tablo:
- **1 Yıl**: `1.000` (+%0.0)
- **2 Yıl**: `1.025` (+%2.5)
- **3 Yıl**: `1.050` (+%5.0)
- **4 Yıl**: `1.075` (+%7.5)
- **5 Yıl**: `1.100` (+%10.0)

**Sonuç**: Kontrat süresi demanded wage'i **kesin olarak** artırır. 5 yıllık bir sözleşme teklif edildiğinde oyuncu 1 yıllık sözleşmeye göre tam olarak **%10 daha fazla** haftalık maaş talep eder.

---

## 5. Trait ve Staff Modifier'larının Katman Ayrımı

`PlayerContract.GenerateNewContractWages` (`VA: 0x02FBFCD4`):
- Teklif veren kulüp kullanıcının kulübü ise (`club.Id == currentManagedClubId`):
  - **Eğer Transfer ise** (`player.ClubId != currentManagedClubId`):
    `ManagerTraitsController.TryGetTrait(38, out data)` (`0x02FBFDFC` -> **Trait 38 HardBargain**).
    Seviye çarpanı (ör. -%5) okunur ve `Helpers.ApplyPercentageModifier(wage, -5.0f)` uygulanır.
  - **Eğer Renewal ise** (`player.ClubId == currentManagedClubId`):
    `ManagerTraitsController.TryGetTrait(36, out data)` (`0x02FBFEC4` -> **Trait 36 ToughNegotiator**).
    Seviye çarpanı (ör. -%5) okunur ve `Helpers.ApplyPercentageModifier(wage, -5.0f)` uygulanır.
- **Staff (WageNegotiator = 9)**: `GenerateNewContractWages` içinde çağrılmaz; sabır hesaplama ve kabul toleransı tarafındadır.

---

## 6. Rounding, Min / Max ve Clamp Kuralları

1. **Taban Ücret Clamp'i**:
   - Instruction: `0x02FBFF48`: `mov w1, #0x32` (`50`).
   - Çağrı: `0x02FBFF50`: `bl Math.Max(wage, 50)`.
   - **£50 kesin native minimumdur**. Hesaplanan değer 50'den düşükse 50'ye yükseltilir.
2. **UI İlk Demanded Wage Yuvarlaması**:
   - Instruction: `0x03027E1C`: `mov w0, #0x64` (`100`).
   - Fonksiyon: `Extensions.Round(randWage, 100)`.
   - En yakın 100'e yuvarlanır.
3. **Rejection Sonrası Yuvarlama**:
   - Fonksiyon: `MonetaryHelpers.RoundMonetaryValue(double)` (`VA: 0x031ECC38`).
   - Basamak tablosu:
     - `< 250`: 10'a yuvarla
     - `< 1,000`: 25'e yuvarla
     - `< 5,000`: 50'ye yuvarla
     - `< 10,000`: 100'e yuvarla
     - `< 50,000`: 500'e yuvarla
     - `< 100,000`: 1,000'e yuvarla
     - `< 5,000,000`: 5,000'e yuvarla
4. **Simulator Balance Farkı**:
   - SM25 Native'de global maksimum ücret tavanı yoktur (yalnızca integer overflow koruması vardır).
   - Football Career Simulator'da aşırı uçları dizginlemek için opsiyonel `£1,000,000` balance cap uygulanır.

---

## 7. Reddedilme Sonrası Tekrar Hesaplama (Recalculation)

Kullanıcının teklifi reddedildiğinde:
1. `ContractInfo.NegotiationCount (+0x28)` 1 artırılır (`0x0303B410`).
2. `PlayerData.GetRequestedNewContractWage(clubId, contractLength, true)` çağrılır.
3. RNG tohumu: `seed = PlayerId + ClubId`.
4. Çarpan: `RNG.GetRand(1.0, 1.1)`.
5. **Kritik Davranış**:
   - Eğer kullanıcı contract length'i ve kulübü değiştirmeden **aynı teklifi tekrar verirse**:
     `PlayerId + ClubId` sabit olduğu için talep edilen ücret **SABİT KALIR** (değişmez).
   - Eğer kullanıcı sözleşme süresini 3 yıldan 5 yıla çıkarırsa:
     `LengthMultiplier` (1.05 -> 1.10) arttığı için talep edilen ücret artar.
   - Her ret sabrı tüketir; sabır 0'ın altına düştüğünde oyuncu masadan kalkar.

---

## 8. Adım Adım Demanded Wage Matematik Zinciri

```text
[AŞAMA 1 - Taban Kulüp Ücreti] (KESİN)
BaseWage = WageModifierForClub(TargetClub)
-> Girdiler: Rating, Potential, Age -> GetPlayerBasicValue -> TargetClub.WageQuality & LeagueMultiplier

[AŞAMA 2 - Süre Düzeltmesi] (KESİN)
LengthMultiplier = 1.0 + (ContractLength - 1) / 40.0
LengthWage = (int)(BaseWage * LengthMultiplier)

[AŞAMA 3 - Menajer Özellikleri İndirimi] (KESİN)
If IsTargetUserClub:
    If IsTransfer: LengthWage = ApplyPercentageModifier(LengthWage, HardBargainPercentage) [-5%]
    If IsRenewal:  LengthWage = ApplyPercentageModifier(LengthWage, ToughNegotiatorPercentage) [-5%]

[AŞAMA 4 - Minimum Taban Sınırı] (KESİN)
BaseContractWage = Math.Max(50, LengthWage)

[AŞAMA 5 - Kabul Edilebilir Eşik ve Unhappy Cezası] (KESİN)
AcceptableWage = BaseContractWage
If IsRenewal And UnwillingToRenew:
    AcceptableWage = AcceptableWage + (AcceptableWage / 2)   [+50% ceza]

[AŞAMA 6A - UI İlk Gösterim Talebi] (KESİN)
DisplayedSeed = PlayerId + Game.Seed
DisplayedRand = RNG.GetRand(1.10, 1.25)
InitialDemandedWage = Extensions.Round(BaseContractWage * DisplayedRand, 100)

[AŞAMA 6B - Ret Sonrası Yeni Talep] (KESİN)
RejectionSeed = PlayerId + ClubId
RejectionRand = RNG.GetRand(1.00, 1.10)
RequestedWage = MonetaryHelpers.RoundMonetaryValue(AcceptableWage * RejectionRand)
ContractInfo.DemandedWages = RequestedWage
```
