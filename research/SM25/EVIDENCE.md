# SM25 Native Makine Kodu Kanıtları (Native Evidence)

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
| `PlayerContract.GenerateInitialWage` | `0x02FC0388` | `0x02FBC388` | 108 B | YÜKSEK |
| `PlayerContract.Generate` | `0x02FBFB30` | `0x02FBBB30` | 420 B | YÜKSEK |
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
