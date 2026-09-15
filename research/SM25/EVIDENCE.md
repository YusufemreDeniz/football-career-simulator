# SM25 Yerel Makine Kodu Kanıtları

Bu dokümanda `research/SM25/il2cpp/libil2cpp.so` (ELF64 AArch64) ikili dosyasından doğrudan çıkartılan ve doğrulanan kanıtlar yer almaktadır.

---

## 1. Dosya Envanteri

| Dosya | Boyut (Byte) | Rolü / Tanımı |
| :--- | ---: | :--- |
| `original/base.apk` | 54,339,000 | Orijinal temel APK paketi |
| `original/split_config.arm64_v8a.apk` | 44,840,718 | ARM64 yerel kütüphanelerini içeren split APK |
| `original/split_UnityDataAssetPack.apk` | 758,203,628 | Unity asset ve veri paketi |
| `il2cpp/global-metadata.dat` | 17,552,832 | IL2CPP metadata v39 (Tip, sınıf ve alan bilgisi) |
| `il2cpp/libil2cpp.so` | 116,408,544 | AArch64 yerel çalıştırılabilir ELF64 kütüphanesi |
| `isil/IsilDump/Assembly-CSharp/PlayerData.txt` | 819,141 | Cpp2IL ISIL ara temsil dökümü |
| `isil/IsilDump/Assembly-CSharp/PlayerContract.txt` | 80,287 | Cpp2IL ISIL sözleşme dökümü |
| `isil/IsilDump/Assembly-CSharp/MonetaryHelpers.txt` | 102,989 | Cpp2IL ISIL finansal yardımcı dökümü |
| `isil/IsilDump/Assembly-CSharp/PlayerRatingCalculator.txt` | 118,520 | Cpp2IL ISIL reyting gelişim motoru |
| `call_analysis/DiffableCs/Assembly-CSharp/PlayerData.cs` | 372,304 | Yeniden oluşturulmuş C# başlık ve çağrı analizi |
| `call_analysis/DiffableCs/Assembly-CSharp/PlayerContract.cs` | 31,071 | Yeniden oluşturulmuş sözleşme C# referansı |
| `call_analysis/DiffableCs/Assembly-CSharp/MonetaryHelpers.cs` | 10,381 | Yeniden oluşturulmuş finans C# referansı |
| `call_analysis/DiffableCs/Assembly-CSharp/PlayerRatingCalculator.cs` | 12,410 | Yeniden oluşturulmuş reyting gelişim C# referansı |

---

## 2. ELF Segment ve Adres Eşlemesi (ELF Segment Mapping)

İncelenen tüm sanal adresler (VA), ELF64 `PT_LOAD` segmentleri üzerinden doğrulanmıştır:
- Formül: `RawOffset = SegmentRaw + (VA - SegmentVA)`
- İlgili Metot Segmenti: `VA: 0x02988A10`, `Raw: 0x02984A10`, Bayraklar: `0x5 R-X` (Okunabilir ve Çalıştırılabilir Kod)

| Metot / Bileşen | Sanal Adres (VA) | Ham Dosya Ofseti (RAW) | Boyut | Güven |
| :--- | :--- | :--- | ---: | :--- |
| `PlayerData.get_Value` | `0x02FC2E80` | `0x02FBEE80` | 824 B | YÜKSEK |
| `PlayerData.GetPlayerBasicValue` | `0x02FC3FBC` | `0x02FBFFBC` | 344 B | YÜKSEK |
| `PlayerData.get_BasicValue` | `0x02FC4108` | `0x02FC0108` | 136 B | YÜKSEK |
| `PlayerData.WageModifierForClub(Club)` | `0x02FC08B4` | `0x02FBC8B4` | 272 B | YÜKSEK |
| `PlayerData.WageModifierForClub(Helper)` | `0x02FC03F4` | `0x02FBC3F4` | 320 B | YÜKSEK |
| `PlayerData.RatingForWageCalculation` | `0x02FC2AB4` | `0x02FBEAB4` | 48 B | YÜKSEK |
| `PlayerData.SeasonGameTimePercentage` | `0x02FC4214` | `0x02FC0214` | 124 B | YÜKSEK |
| `PlayerData.get_SeasonForm` | `0x02FC4290` | `0x02FC0290` | 172 B | YÜKSEK |
| `PlayerData.get_InDecline` | `0x02FC29A0` | `0x02FBE9A0` | 88 B | YÜKSEK |
| `PlayerData.get_Morale` | `0x02FC433C` | `0x02FC033C` | 560 B | YÜKSEK |
| `PlayerData.GetDeclineAge()` | `0x02FC67C4` | `0x02FC27C4` | 436 B | YÜKSEK |
| `PlayerData.GetDeclineAge(seed, potAge, startAge)` | `0x02FC6978` | `0x02FC2978` | 312 B | YÜKSEK |
| `PlayerData.get_PotentialAge()` | `0x02FC1B44` | `0x02FBDB44` | 716 B | YÜKSEK |
| `PlayerData.GetPotentialAgeRatingBracket` | `0x02FC1B34` | `0x02FBDB34` | 16 B | YÜKSEK |
| `PlayerRatingCalculator.UseIncreasingWave` | `0x02FD9FB8` | `0x02FD5FB8` | 408 B | YÜKSEK |
| `PlayerRatingCalculator.GetDecreasingRatingFromSinWavePrecise` | `0x02FDABA8` | `0x02FD6BA8` | 48 B | YÜKSEK |
| `PlayerRatingCalculator.UseDecreasingWave` | `0x02FDA8F0` | `0x02FD68F0` | 700 B | YÜKSEK |
| `Training.GetTrainingIncrease` | `0x02F30D0C` | `0x02F2CD0C` | 500 B | YÜKSEK |
| `Training.ApplyYouthTrainingMod` | `0x02F3119C` | `0x02F2D19C` | 196 B | YÜKSEK |
| `PlayerContract.GenerateNewContractWages(double, double)` | `0x02FC0160` | `0x02FBC160` | 136 B | YÜKSEK |
| `PlayerContract.GenerateNewContractWages(PlayerData, Club, double)` | `0x02FBFCD4` | `0x02FBBCD4` | 672 B | KESİN |
| `PlayerContract.GenerateInitialWage` | `0x02FC0388` | `0x02FBC388` | 108 B | YÜKSEK |
| `PlayerContract.Generate` | `0x02FBFB30` | `0x02FBBB30` | 420 B | YÜKSEK |
| `PlayerContract.LengthMultiplier` | `0x02FBFCB4` | `0x02FBBCB4` | 32 B | KESİN |
| `PlayerContract.get_Wage` | `0x02FBFB18` | `0x02FBBB18` | 8 B | KESİN |
| `PlayerContract.CreateNew` | `0x02FC01EC` | `0x02FBC1EC` | 28 B | KESİN |
| `PlayerData.GetRequestedWageMultiplier` | `0x02FC3A0C` | `0x02FBFA0C` | 256 B | KESİN |
| `PlayerData.GetAcceptableNewContractWage` | `0x02FC3E9C` | `0x02FBFE9C` | 224 B | KESİN |
| `PlayerData.GetRequestedNewContractWage` | `0x02FC3F80` | `0x02FBFF80` | 60 B | KESİN |
| `ContractInfo.set_DemandedWages(int)` | `0x030278F4` | `0x030238F4` | 16 B | KESİN |
| `ContractInfo.RecalculateContract` | `0x03027B50` | `0x03023B50` | 60 B | KESİN |
| `ContractInfo.AddNegotiation` | `0x03027CE8` | `0x03023CE8` | 64 B | KESİN |
| `ContractInfo.WillAcceptWage(bool)` | `0x03027CA4` | `0x03023CA4` | 68 B | KESİN |
| `ContractInfo.GetNegotiationWageForUserManager` | `0x03027D28` | `0x03023D28` | 268 B | KESİN |
| `TransferBid.RejectContractOffer` | `0x0303A940` | `0x03036940` | 120 B | KESİN |
| `PlayerPatience.CalculatePatience` | `0x02F98714` | `0x02F94714` | 44 B | KESİN |
| `Negotiation..ctor(PlayerData,ContractInfo,double)` | `0x03032E74` | `0x0302EE74` | — | KESİN |
| `TransferBid.TryAcceptContractOffer` | `0x0303B218` | `0x03037218` | 612 B | KESİN |
| `TransfersController.ConfirmContractOffer` | `0x03048E60` | `0x03044E60` | — | KESİN |
| `OfferContractModal.SubmitContractOffer` | `0x030E0C5C` | `0x030DCC5C` | — | KESİN |
| `PlayerData.CalculatePlayerSigningFee` | `0x02FC3438` | `0x02FBF438` | 612 B | KESİN |
| `PlayerData.CalculatePlayerCompensationFee` | `0x02FC2E0C` | `0x02FBEE0C` | 96 B | KESİN |
| `PlayerData.CalculateTotalSigningFee` | `0x02FC3B10` | `0x02FBFB10` | 352 B | KESİN |
| `ValueModifierController.ApplyModifier` | `0x02F20E54` | `0x02F1CE54` | 164 B | KESİN |
| `Helpers.ApplyPercentageModifier` | `0x02FD6058` | `0x02FD2058` | 28 B | KESİN |
| `MonetaryHelpers.RoundMonetaryValue` | `0x031ECC38` | `0x031E8C38` | 236 B | YÜKSEK |
| `MonetaryHelpers.toNearest` | `0x031EBFC8` | `0x031E7FC8` | 228 B | YÜKSEK |
| `MonetaryHelpers.GetValueText` | `0x031EC1BC` | `0x031E81BC` | 1320 B | YÜKSEK |

---

## 3. Oyuncu Gelişimi ve Yaşlanma Native Kanıtları

### 3.1 Sinüs Dalgası (Increasing Wave) Talimatları (`0x02FDA0E0..0x02FDA144`)

| Sanal Adres (VA) | Makine Kodu | AArch64 Talimatı | Anlamı ve Rolü |
| :--- | :--- | :--- | :--- |
| `0x02FDA0E0` | `60 02 62 1E` | `scvtf d0, w19` | `d0 = (double)currentDaysSinceStart` |
| `0x02FDA0E4` | `81 02 62 1E` | `scvtf d1, w20` | `d1 = (double)totalDaysToPotentialAge` |
| `0x02FDA0F4` | `00 18 61 1E` | `fdiv d0, d0, d1` | `progress = currentDays / totalDays` |
| `0x02FDA0F8` | `01 B9 44 FD` | `ldr d1, [x8, #0x970]` | Sabit `90.0` (Derece tavanı, VA `0x01332970`) |
| `0x02FDA100` | `00 08 61 1E` | `fmul d0, d0, d1` | `angleDegrees = progress * 90.0` |
| `0x02FDA104` | `01 51 42 FD` | `ldr d1, [x8, #0x4a0]` | Sabit $\frac{\pi}{180}$ (Deg2Rad = `0.01745329...`, VA `0x013324A0`) |
| `0x02FDA10C` | `01 08 61 1E` | `fmul d1, d0, d1` | `angleRadians = angleDegrees * (PI / 180)` |
| `0x02FDA124` | `17 DD DA 94` | `bl #0x6691580` | `Math.Sin(angleRadians)` |
| `0x02FDA128` | `61 02 63 1E` | `ucvtf d1, w19` | `d1 = (double)(potentialRating - startRating)` |
| `0x02FDA12C` | `A2 02 63 1E` | `ucvtf d2, w21` | `d2 = (double)startRating` |
| `0x02FDA13C` | `00 08 61 1E` | `fmul d0, d0, d1` | `sinGain = sinVal * (potential - start)` |
| `0x02FDA144` | `00 28 62 1E` | `fadd d0, d0, d2` | `currentRating = startRating + sinGain` |

### 3.2 DeclineAge ve Kaleci (+1 Yaş) Talimatları (`0x02FC6A68..0x02FC6A88`)

| Sanal Adres (VA) | Makine Kodu | AArch64 Talimatı | Anlamı ve Rolü |
| :--- | :--- | :--- | :--- |
| `0x02FC6A2C` | `81 03 80 52` | `mov w1, #0x1c` | Sabit `28` (Standart min decline yaşı) |
| `0x02FC6A30` | `E2 03 80 52` | `mov w2, #0x1f` | Sabit `31` (Standart maks decline yaşı) |
| `0x02FC6A58` | `C1 03 80 52` | `mov w1, #0x1e` | Sabit `30` (Potansiyel >= 90 süperstar min decline) |
| `0x02FC6A5C` | `22 04 80 52` | `mov w2, #0x21` | Sabit `33` (Potansiyel >= 90 süperstar maks decline) |
| `0x02FC6A64` | `F9 B4 08 94` | `bl #0x31f3e48` | `RNG.GetRand(min, max)` |
| `0x02FC6A68` | `88 42 40 B9` | `ldr w8, [x20, #0x40]` | `player.Position` |
| `0x02FC6A70` | `1F 05 00 71` | `cmp w8, #1` | **Mevki Kontrolü: Position == 1 (GK - Kaleci)** |
| `0x02FC6A74` | `08 14 80 1A` | `cinc w8, w0, eq` | **Eğer Kaleci ise: DeclineAge += 1!** |
| `0x02FC6A6C` | `A9 0A 00 11` | `add w9, w21, #2` | `potentialAge + 2` |
| `0x02FC6A78` | `1F 01 09 6B` | `cmp w8, w9` | `declineAge >= potentialAge + 2` kontrolü |
| `0x02FC6A90` | `1F 01 33 6B` | `cmp w8, w19, uxtb`| `declineAge >= startAge` kontrolü |

### 3.3 Gerileme Hızı ve Longevity Talimatları (`0x02FDAA74..0x02FDAB24`)

| Sanal Adres (VA) | Makine Kodu | AArch64 Talimatı | Anlamı ve Rolü |
| :--- | :--- | :--- | :--- |
| `0x02FDAA74` | `B6 01 00 36` | `tbz w22, #0, #0x2fdaaa8`| `longevity` boolean kontrolü |
| `0x02FDAA9C` | `01 01 80 52` | `mov w1, #8` | `longevity == true` min düşüş hızı |
| `0x02FDAAA0` | `A2 01 80 52` | `mov w2, #0xd` (13) | `longevity == true` maks düşüş hızı (8..13) |
| `0x02FDAACC` | `A1 01 80 52` | `mov w1, #0xd` (13) | `longevity == false` min düşüş hızı |
| `0x02FDAAD0` | `42 02 80 52` | `mov w2, #0x12` (18) | `longevity == false` maks düşüş hızı (13..18) |
| `0x02FDAB1C` | `81 07 80 52` | `mov w1, #0x3c` (60) | Asgari Reyting Tabanı (`60` reyting) |
| `0x02FDAB24` | `13 FB A3 94` | `bl #0x58d9770` | `Math.Max(rating, 60)` |

---

## 4. Nihai Haftalık Ücret Yerel Kanıtları

### 4.1 Saklanan ücret ve kabul edilen teklif

| VA | Bayt | AArch64 talimatı | Anlam |
|---|---|---|---|
| `0x02FBFB18` | `00 18 40 B9` | `ldr w0, [x0, #0x18]` | `PlayerContract.Wage` mevcut saklı ücreti okur. |
| `0x02FC01F8` | `28 18 40 B9` | `ldr w8, [x1, #0x18]` | `PlayerContract.CreateNew`, kabul edilmiş `ContractInfo.Wage` teklifini okur. |
| `0x02FC01FC` | `08 18 00 B9` | `str w8, [x0, #0x18]` | Teklif değerini yeni sözleşmenin ücret alanına yazar. |
| `0x03027CA4` | `08 18 40 B9` | `ldr w8, [x0, #0x18]` | `WillAcceptWage`, teklif edilen ücreti okur. |
| `0x03027CA8` | `09 24 40 B9` | `ldr w9, [x0, #0x24]` | Ayrı `DemandedWages` kabul eşiğini okur. |
| `0x03027CDC` | `48 01 80 52` | `mov w8, #0xa` | Eşiği geçen teklif `eContractRejectReason.Accepted = 10` sonucuna gider. |

Güven `KESİN`: kullanıcı görüşmesinde hesaplanan talep ile kaydedilen nihai ücret aynı kavram değildir; nihai ücret kabul edilmiş teklifin kopyasıdır.

### 4.2 Sözleşme süresi ve kompleks üretici

| VA | Bayt | AArch64 talimatı | Anlam |
|---|---|---|---|
| `0x02FBFCB4` | `28 04 00 51` | `sub w8, w1, #1` | Yıldan bir çıkarır. |
| `0x02FBFCBC` | `88 08 E8 D2` | `mov x8, #0x4044000000000000` | `40,0` bölenini kurar. |
| `0x02FBFCC4` | `00 18 61 1E` | `fdiv d0, d0, d1` | `(Years-1)/40` hesaplar. |
| `0x02FBFCCC` | `00 28 61 1E` | `fadd d0, d0, d1` | `1,0` ekler. |
| `0x02FBFD40` | `DD 02 00 94` | `bl #0x2fc08b4` | `WageModifierForClub(Club)` çağrısıdır. |
| `0x02FBFD44` | `00 08 68 1E` | `fmul d0, d0, d8` | Kulüp ücretini süre çarpanıyla çarpar. |
| `0x02FBFD5C` | `09 00 78 1E` | `fcvtzs w9, d0` | Sonucu sıfıra doğru keser. |
| `0x02FBFF48` | `41 06 80 52` | `mov w1, #0x32` | Haftalık `50` tabanını yükler. |
| `0x02FBFF50` | `08 66 A4 94` | `bl #0x58d9770` | `Math.Max(wage, 50)` uygular. |
| `0x02FBFF54` | `60 1A 00 B9` | `str w0, [x19, #0x18]` | Sonucu sözleşme ücret alanına yazar. |

Güven `KESİN`. Bu fonksiyonda global üst tavan veya bütçe tavanı talimatı yoktur.

### 4.3 Menajer özelliği ve personel sırası

| VA | Bayt | AArch64 talimatı | Anlam |
|---|---|---|---|
| `0x02FBFDFC` | `C1 04 80 52` | `mov w1, #0x26` | Başka kulüpten transferde `TraitId 38`. |
| `0x02FBFEC4` | `81 04 80 52` | `mov w1, #0x24` | Mevcut oyuncu yenilemesinde `TraitId 36`. |
| `0x02FBFE24` | `01 24 40 BD` | `ldr s1, [x0, #0x24]` | Aktif menajer özelliğinin seviye yüzdesini okur. |
| `0x02FD6060` | `21 18 22 1E` | `fdiv s1, s1, s2` | Yüzdeyi `100,0` değerine böler. |
| `0x02FD6068` | `21 28 22 1E` | `fadd s1, s1, s2` | `1 + yüzde/100` çarpanını kurar. |
| `0x02FBFBF8` | `20 01 80 52` | `mov w0, #9` | `ModifierType.WageNegotiator` kimliğidir. |
| `0x02FBFC00` | `95 84 FD 97` | `bl #0x2f20e54` | Personel/veri değiştiricisini çağırır. |
| `0x02F20EDC` | `00 18 40 BD` | `ldr s0, [x0, #0x18]` | Kaynak verideki çarpanı okur. |
| `0x02F20EEC` | `00 08 68 1E` | `fmul d0, d0, d8` | Ücreti personel çarpanıyla çarpar. |
| `0x02FBFC28` | `04 B4 08 94` | `bl #0x31ecc38` | Personel etkisinden sonra parasal yuvarlama yapar. |

İşlem sırası `KESİN`. Asset incelemesi `TraitId 36 = %−5`, `TraitId 38 = %−5` ve `WageNegotiator[9] = 0,90` değerlerini de kesinleştirmiştir.

### 4.4 Talep ve görüşme beklentisi

| VA | Bayt | AArch64 talimatı | Anlam |
|---|---|---|---|
| `0x02FC3AC0` | `00 10 6E 1E` | `fmov d0, #1.00000000` | Yeni sözleşme talep çarpanı alt sınırı. |
| `0x02FC3AC8` | `01 B1 44 FD` | `ldr d1, [x8, #0x960]` | VA `0x01332960` adresindeki `1,10` üst sınırı. |
| `0x02FC3ACC` | `28 C1 08 94` | `bl #0x31f3f6c` | Tohumlu deterministik rastgele kayan nokta RNG çağrısı. |
| `0x02FC3F8C` | `C4 FF FF 97` | `bl #0x2fc3e9c` | Kabul edilebilir yeni sözleşme ücretini alır. |
| `0x02FC3F98` | `9D FE FF 97` | `bl #0x2fc3a0c` | `[1,00,1,10]` talep çarpanını alır. |
| `0x02FC3FA8` | `24 A3 08 94` | `bl #0x31ecc38` | Talebi parasal basamağa yuvarlar. |
| `0x03027DF8` | `02 90 6E 1E` | `fmov d2, #1.25000000` | Görüşme ekranı beklentisinin üst sınırı. |
| `0x03027E08` | `00 B1 44 FD` | `ldr d0, [x8, #0x960]` | Beklentinin `1,10` alt sınırı. |
| `0x03027E1C` | `80 0C 80 52` | `mov w0, #0x64` | Beklentiyi `100` adımına yuvarlar. |

RNG tohumu oyuncu kimliği ile `Game.<Seed>k__BackingField +0x48` toplamından kuruluyor. Metadata alan adı, aralıklar, çağrılar ve seeded deterministic davranış `KESİN`dir.

---

## 5. Sözleşme Görüşmesi Native Kanıtları

### 5.1 ContractInfo ve teklif snapshot'ı

| VA | Bayt | Kanıt |
|---:|---|---|
| `0x03027CE8` | `08 50 40 79` | `NegotiationCount +0x28` okunur. |
| `0x03027CF0` | `08 50 00 79` | Tur sayısı artırılıp `+0x28` alanına yazılır. |
| `0x03027D00` | `08 18 00 B9` | Snapshot wage değeri `ContractInfo.Wage +0x18` alanına yazılır. |
| `0x03027D04` | `09 20 00 B9` | Snapshot signing fee değeri `+0x20` alanına yazılır. |
| `0x03027D08` | `0A 70 00 39` | Contract length `+0x1C` alanına yazılır. |
| `0x03027D18` | `08 2C 00 B9` | Squad status `+0x2C` alanına yazılır. |

Cpp2IL metadata yerleşiminde `ContractInfo` son alanı `RejectionReason +0x30`dur. Ardından bonus, clause veya agent fee alanı yoktur. Güven: `KESİN`.

### 5.2 Acceptance kapıları

| VA | Bayt / çağrı | Anlam |
|---:|---|---|
| `0x0303B364` | `bl 0x03027CA4` | Önce `WillAcceptWage`. |
| `0x0303B39C` | `bl 0x02FBC6FC` | Sonra `WillAcceptSquadStatus`. |
| `0x0303B3D0` | `bl 0x02FC386C` | Son olarak `WillAcceptContractLength`. |
| `0x0303B438` | `bl 0x02FC3F80` | Ret sonrası requested wage yeniden üretilir. |
| `0x0303B45C` | `bl 0x0303A940` | Sonuç `RejectContractOffer` yoluna gider. |

`0x0303B408..0x0303B414` aralığı, ret halinde `ContractInfo +0x28` sayacını ikinci kez artırır. İlk artış `AddNegotiation` içindedir; accepted gönderim `+1`, rejected gönderim `+2` sayaç farkı üretir. Güven: `KESİN`.

Bu çağrı aralığında signing-fee veya bonus kabul çağrısı yoktur. Güven: `KESİN`.

### 5.3 Sabır

| VA | Bayt | Anlam |
|---:|---|---|
| `0x02F9862C` | `48 00 80 52` | Concern başlangıcı `2`. |
| `0x02F98634` | `A9 00 80 52` | Normal başlangıç `5`. |
| `0x02F9865C` | `02 10 2D 1E` | Ücret cezası `%75` eşiği. |
| `0x02F98670` | `02 10 2C 1E` | Ücret cezası `%50` eşiği. |
| `0x02F98714` | `00 10 7E 1E` | Her gönderimde temel `−1`. |
| `0x02F98720/28/30` | `ldr d1,[x0,#0x38/48/58]` | Rol, süre ve ücret cezaları sırayla düşülür. |

Rol ve süre matrisleri Unity TextAsset verisiyle çapraz doğrulanmıştır. `ContractLengthData` içindeki `OneYears` yazım kusuru nedeniyle istenen iki yıl/teklif bir yıl hücresi deserialization sonrası `0` olur. Güven: `KESİN`.

### 5.4 İmza parası, tazminat ve veri katsayıları

| Kanıt | Sonuç |
|---|---|
| `0x02FC34F8 ldr w8,[x8,#0x48]` ve metadata `Game.Seed +0x48` | Tohum `PlayerId + Game.Seed`. |
| `0x01332160/0x01332950/0x013331E8` | Yenileme aralıkları `0,05–0,10` ve `0,025–0,05`. |
| `0x0133626C` | Genç free-agent player fee oranı `0,10`. |
| `0x02FC3664 mov w0,#8` | `TransferNegotiator = 8`. |
| `0x01335FD4` | 23 yaş veya altı free-agent tazminatı `Value × 0,20`. |
| `ValueModifiersJson` SHA-256 kilidi | `TransferNegotiator[8]=0,90`, `WageNegotiator[9]=0,90`. |
| `WageNegotiationsTraitData` | `TraitId 38`, `%−5`. |
| `ContractRenewalTraitData` | `TraitId 36`, `%−5`. |
| `SigningBonusTraitData` | `TraitId 33`, `%−5/%−10`. |

`verify_contract_negotiation.py`, hem `libil2cpp.so` ve asset-pack SHA-256 değerlerini hem bu talimat/veri sabitlerini denetler. Sürüm değişikliğinde sessiz yanlış pozitif yerine `FAIL` üretir.
