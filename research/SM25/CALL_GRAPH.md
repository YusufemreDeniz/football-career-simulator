# Oynanış ve Ekonomi Çağrı Grafikleri

Bu dokümanda Soccer Manager 2025 (SM25) IL2CPP AArch64 makine kodunda tespit edilen çağrı zincirleri (call graphs) yer almaktadır.

---

## 1. Oyuncu Piyasa Değeri Çağrı Grafiği (Player Market Value)

```text
PersonData.get_Age (VA 0x02F8B420) ───────────────┐
PlayerData.get_RatingQuick (0x02FBB110) ─────────┤
PlayerData.get_InDecline (0x02FC29A0) ───────────┤
PlayerData potential alanı (ofset 0x4C) ─────────┤
PlayerData.SeasonGameTimePercentage (0x02FC4214) ─┤
PlayerData.get_SeasonForm (0x02FC4290) ──────────┤
PlayerData.get_Morale (0x02FC433C) ──────────────┤
                                                 v
                                       PlayerData.get_Value
                                       VA 0x02FC2E80
                                                 │
                                                 │ 32-bit işaretli tamsayı döndürür (GBP bazlı int)
                                                 │
                        ┌────────────────────────┼────────────────────────┐
                        v                        v                        v
            PlayerCardSmall.Populate     SquadPlayerRow.Populate    Club / Transfer AI
                        │                        │                        │
                        v                        v                        v
            MonetaryHelpers.GetValueText MonetaryHelpers.GetValueText Club.CanAffordToBuyPlayer
            VA 0x031EC1BC                VA 0x031EC1BC                RequiredTransferBidAmount
                        │                        │                        │
                        v                        v                        v
            "£15.0M" metin formatı       "£15.0M" metin formatı       Bütçe ve Teklif Kontrolü
```

---

## 2. Nihai Haftalık Ücret Çağrı Grafiği

```text
PlayerData.RatingForWageCalculation (VA 0x02FC2AB4)
  └─> PlayerData.GetPlayerBasicValue (VA 0x02FC3FBC)
        └─> Rating^20 ve peaked değilse potansiyel harmanı
              │
              ├─> Tekil kulüp / sözleşme yolu
              │     PlayerData.WageModifierForClub(Club) (VA 0x02FC08B4)
              │       └─> PlayerContract.GenerateNewContractWages(PlayerData, Club, double)
              │             VA 0x02FBFCD4
              │             ├─> LengthMultiplier (VA 0x02FBFCB4)
              │             ├─> hedef kullanıcı kulübüyse TraitId 38 veya TraitId 36
              │             ├─> Helpers.ApplyPercentageModifier (VA 0x02FD6058)
              │             └─> Trunc + Math.Max(50) + PlayerContract.Wage alanı +0x18
              │                   │
              │                   └─> PlayerContract.Generate (VA 0x02FBFB30)
              │                         ├─> kullanıcı kulübüyse ModifierType.WageNegotiator = 9
              │                         │     └─> ValueModifierController.ApplyModifier (VA 0x02F20E54)
              │                         └─> MonetaryHelpers.RoundMonetaryValue (VA 0x031ECC38)
              │                               └─> otomatik nihai haftalık ücret
              │
              └─> Toplu lig / AI üretim yolu
                    PlayerData.WageModifierForClub(PlayerContractHelper) (VA 0x02FC03F4)
                      ├─> takım uyumu ters oran yönü: TeamAverageRating / PlayerRating
                      └─> önceden hesaplanmış takım ve lig çarpanları
```

```text
Kullanıcı sözleşme görüşmesi

ContractInfo.RecalculateContract (VA 0x03027B50)
  └─> PlayerData.GenerateNewContractWages(byte) (VA 0x02FC416C)
        └─> geçici PlayerContract.Wage
              └─> ContractInfo.DemandedWages alanı +0x24

PlayerData.GetAcceptableNewContractWage (VA 0x02FC3E9C)
  └─> PlayerContract.Generate
        └─> yönetilen kulüpte yenilemeyi reddediyorsa 1,5x
              │
              └─> PlayerData.GetRequestedNewContractWage (VA 0x02FC3F80)
                    ├─> GetRequestedWageMultiplier (VA 0x02FC3A0C): seeded deterministic random [1,00, 1,10]
                    └─> MonetaryHelpers.RoundMonetaryValue

ContractInfo.GetNegotiationWageForUserManager (VA 0x03027D28)
  ├─> kompleks tabanı DemandedWages +0x24 alanına yazar
  └─> yalnız gösterim dönüşü: taban × seeded deterministic random [1,10, 1,25], 100 adımı

TransferBid.TryAcceptContractOffer
  ├─> DemandedWages eşiğini belirler
  ├─> ContractInfo.WillAcceptWage (VA 0x03027CA4)
  │     ├─> ContractInfo.Wage +0x18: kullanıcı teklifi
  │     └─> ContractInfo.DemandedWages +0x24: kabul eşiği
  └─> kabul edilirse PlayerContract.CreateNew (VA 0x02FC01EC)
        └─> teklif edilen Wage +0x18 değerini PlayerContract.Wage +0x18 alanına kopyalar
```

### Otomatik Sözleşme Üretimi

```text
PlayerContract.Generate (0x02FBFB30)
  -> GenerateNewContractWages(PlayerData,Club,length) (0x02FBFCD4)
       -> WageModifierForClub(Club) (0x02FC08B4)
       -> LengthMultiplier (0x02FBFCB4)
       -> transfer: TraitId 38 = -5%
       -> renewal: TraitId 36 = -5%
       -> Math.Max(wage, 50)
  -> kullanıcı kulübü: WageNegotiator[9] = 0.90
  -> RoundMonetaryValue (0x031ECC38)
  -> PlayerContract.Wage +0x18
```

### Etkileşimli Oyuncu Sözleşmesi Görüşmesi

```text
OfferContractModal.Populate
  -> ContractInfo.GetDefaultNewContractDemands (0x0302792C)
  -> PlayerData.CalculateTotalSigningFee (0x02FC3B10)
  -> PlayerPatience..ctor (0x02F98470)

OfferContractModal.SubmitContractOffer (0x030E0C5C)
  -> PlayerPatience.CalculatePatience (0x02F98714)
  -> Negotiation..ctor (0x03032E74)
  -> TransfersController.ConfirmContractOffer (0x03048E60)
       -> TransferBid/ContractInfo bul veya oluştur
       -> ContractInfo.AddNegotiation (0x03027CE8)
  -> TransferBid.TryAcceptContractOffer (0x0303B218)
       -> [free agent] rakip teklif -> OtherOffer(8)
       -> GetAcceptableNewContractWage (0x02FC3E9C)
       -> ContractInfo.WillAcceptWage (0x03027CA4)
       -> PlayerData.WillAcceptSquadStatus (0x02FBC6FC)
       -> PlayerData.WillAcceptContractLength (0x02FC386C)
       -> ACCEPT
            -> TransferBid.AcceptContractOffer (0x0303AAA4)
            -> transfer: TransferPlayer -> Club.SignedPlayer
                 -> PlayerContract.CreateNew (0x02FC01EC)
            -> renewal: PlayerData.AcceptContractOffer (0x02FC2C70)
                 -> mevcut PlayerContract expiry/wage güncelle
                 -> SigningFee öde
       -> REJECT
            -> NegotiationCount++
            -> GetRequestedNewContractWage (0x02FC3F80)
            -> TransferBid.RejectContractOffer (0x0303A940)
            -> normal ret: modal açık, kullanıcı yeniden gönderir
            -> Insulted veya sabır bitti: RefuseContract (0x030E0D70)
```

Transfer, renewal ve free-agent dalları aynı acceptance kapılarını paylaşır. Free-agent yolu ayrıca `OtherOffer`; renewal yolu `!WillPlayerRenewContract` olduğunda iki kat wage eşiği; kabul sonrası yollar ise mevcut kontratı güncelleme/yeni kontrat yaratma olarak ayrılır. Native ayrı counter-offer nesnesi üretmez.

---

## 3. Oyuncu Gelişimi ve Yaşlanma Çağrı Grafiği (Player Development & Aging)

```text
PlayerData.GetRatingPrecise (VA 0x02FC5AE8)
  │
  └─> PlayerRatingCalculator.CalcRating (VA 0x02FD9A50)
        │
        ├─> [Evre 1: Gelişim Aşaması (Date <= PotentialDate)]
        │     PlayerRatingCalculator.ShouldUseIncreasingWave
        │       └─> PlayerRatingCalculator.UseIncreasingWave (VA 0x02FD9FB8)
        │             ├─> progress = currentDays / totalDays
        │             ├─> angle = progress * 90.0 * Deg2Rad
        │             ├─> wave = Math.Sin(angle)
        │             └─> Rating = StartRating + (PotentialRating - StartRating) * wave
        │
        ├─> [Evre 2: Zirve Aşaması (PotentialDate < Date < DeclineDate)]
        │     PlayerRatingCalculator.GetNonImprovingRating
        │       └─> PlayerRatingCalculator.NotAtDeclineDate
        │             └─> PlayerRatingCalculator.GetPeakRating (VA 0x02FDA860)
        │                   └─> Rating = PotentialRating (Sabit Plato)
        │
        └─> [Evre 3: Gerileme Aşaması (Date >= DeclineDate)]
              PlayerRatingCalculator.ShouldGetDeclineRating
                └─> PlayerRatingCalculator.GetDecliningRating
                      └─> PlayerRatingCalculator.UseDecreasingWave (VA 0x02FDA8F0)
                            ├─> PlayerData.GetDeclineAge (VA 0x02FC67C4)
                            │     ├─> Potential >= 90 ? RNG(30..33) : RNG(28..31)
                            │     ├─> Position == 1 (GK) ? +1 Yaş Koruması
                            │     ├─> Math.Max(declineAge, potentialAge + 2)
                            │     └─> Önbellek: PlayerData.cacheDeclineAge (0x7C)
                            │
                            ├─> PlayerData.get_Longevity:
                            │     ├─> Longevity == true  => Düşüş Aralığı RNG(8..13)
                            │     └─> Longevity == false => Düşüş Aralığı RNG(13..18)
                            │
                            └─> GetDecreasingRatingFromSinWavePrecise (VA 0x02FDABA8)
                                    ├─> Rating = PeakRating - loss
                                   └─> Math.Max(rating, 60.0) (60 Taban Puanı)
```

---

## 4. Talep Edilen Ücret (ContractInfo.DemandedWages +0x24) Çağrı Grafiği

```text
A) İLK GÖRÜŞME GÖSTERİMİ (UI Displayed Demand)
OfferContractModal.UpdateDemandsPanel
  └─> ContractInfo.GetNegotiationWageForUserManager (VA 0x03027D28)
        ├─> Game.get_CurrentUserClubData (VA 0x02F56594)
        ├─> PlayerContract.LengthMultiplier (VA 0x02FBFCB4)
        │     └─> 1.0 + (length - 1) / 40.0 (+2.5%/yıl)
        ├─> PlayerContract.GenerateNewContractWages (VA 0x02FBFCD4)
        │     ├─> PlayerData.WageModifierForClub(Club) (VA 0x02FC08B4)
        │     ├─> TraitId 38 HardBargain (-5%) veya TraitId 36 ToughNegotiator (-5%)
        │     └─> Math.Max(wage, 50) (£50 taban floor)
        ├─> str w9, [ContractInfo, #0x24] (Baz DemandedWages yazımı)
        ├─> RNG.SetSeed(PlayerId + Game.Seed) (VA 0x03027DD8)
        ├─> RNG.GetRand(BaseWage * 1.10, BaseWage * 1.25) (VA 0x03027E14)
        └─> Extensions.Round(RandWage, 100) (VA 0x031DC4B4) -> UI ekranda gösterilir

B) KABUL EŞİĞİ DEĞERLENDİRMESİ (Acceptable Wage Gate)
TransferBid.TryAcceptContractOffer (VA 0x0303B218)
  ├─> PlayerData.GetAcceptableNewContractWage (VA 0x02FC3E9C)
  │     ├─> PlayerContract.Generate(player, club, length)
  │     └─> IsOwnedByManagedClub && !WillPlayerRenewContract?
  │           └─> wage = wage + (wage / 2) (+50% ceza)
  ├─> str w0, [ContractInfo, #0x24] (Eşik DemandedWages yazımı)
  └─> ContractInfo.WillAcceptWage(bool) (VA 0x03027CA4)
        ├─> OfferedWage < DemandedWages / 2 => Insulted (7)
        ├─> OfferedWage < DemandedWages     => Wages (2)
        └─> OfferedWage >= DemandedWages    => Accepted (10)

C) REDDEDİLME SONRASI YENİDEN HESAPLAMA (Rejection Recalculation)
TransferBid.TryAcceptContractOffer (Ret Dalı VA 0x0303B408)
  ├─> ContractInfo.NegotiationCount++ (Ofset +0x28)
  ├─> PlayerData.GetRequestedNewContractWage (VA 0x02FC3F80)
  │     ├─> PlayerData.GetAcceptableNewContractWage (VA 0x02FC3E9C)
  │     ├─> PlayerData.GetRequestedWageMultiplier (VA 0x02FC3A0C)
  │     │     ├─> RNG.SetSeed(PlayerId + ClubId) (VA 0x02FC3AA0)
  │     │     ├─> RNG.GetRand(1.0, 1.1) (VA 0x02FC3ACC)
  │     │     └─> RNG.SetSeed(oldSeed) (Restore)
  │     └─> MonetaryHelpers.RoundMonetaryValue (VA 0x031ECC38)
  └─> TransferBid.RejectContractOffer (VA 0x0303A940)
        ├─> BidStatus = eBidStatus.ContractOfferRejected (13)
        └─> str w8, [ContractInfo, #0x24] (Yeni talep DemandedWages yazımı)
```
