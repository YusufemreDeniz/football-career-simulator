# Oynanış ve Ekonomi Çağrı Grafikleri (Call Graphs)

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

## 2. Haftalık Maaş Üretim Hattı Çağrı Grafiği (Weekly Wage Pipeline)

```text
PlayerData.RatingForWageCalculation (VA 0x02FC2AB4)
  └─> Güncel yetenek puanını döner (veya UseFixedWageCalculation ayarlıysa başlangıç yeteneğini döner)
                               │
                               v
PlayerData.GetPlayerBasicValue (VA 0x02FC3FBC)
  ├─> 750,000 * (rating/99)^20 hesaplar (peaked veya non-peaked potansiyel harmanı)
  └─> Çıktı: Haftalık temel referans maaş skalası (Base weekly wage benchmark)
                               │
                               v
PlayerData.WageModifierForClub(Club) (VA 0x02FC08B4) / (Helper) (0x02FC03F4)
  ├─> Oyuncunun kulübe uyumu: (0.5 + 0.5 * playerRating / clubAverageRating)
  ├─> Kulüp itibar seviyesi: (clubAverageRating / 94.0)
  └─> Lig itibar seviyesi: (leagueRating / 89.0)
                               │
                               v
PlayerContract.Generate / GenerateInitialWage / GenerateNewContractWages
  ├─> Sözleşme Süresi Ölçekleme:
  │     lengthMultiplier = 1.0 + (contractLength - 1) / 40.0 (yılda +%2.5)
  │     scaledWage = wageModifier * lengthMultiplier
  │
  ├─> Menajer Özellikleri (Kulüp kullanıcı tarafından yönetiliyorsa):
  │     ├─> Yeni transfer: TraitId.38 (Wage Negotiation)
  │     └─> Sözleşme yenileme: TraitId.36 (Contract Renewal Negotiation)
  │     scaledWage = Helpers.ApplyPercentageModifier(scaledWage, traitPercentage)
  │
  ├─> Personel Özellikleri (Kulüp kullanıcı tarafından yönetiliyorsa):
  │     └─> ModifierType.WageNegotiator (Personel Tipi 9)
  │
  ├─> Asgari Taban Kontrolü (Minimum Wage Floor):
  │     wage = Math.Max(wage, 50.0)  // £50/hafta taban (Üst sınır/tavan yoktur)
  │
  └─> Para Birimi Yuvarlama (MonetaryHelpers.RoundMonetaryValue, VA 0x031ECC38):
        └─> Bankacı Yuvarlaması (toNearest) ile nihai tamsayı haftalık maaş üretilir
```

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
