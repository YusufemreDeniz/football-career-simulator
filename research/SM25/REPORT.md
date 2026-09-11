# SM25 Clean-Room Sistem Araştırma Raporu: Ekonomi ve Oyuncu Gelişimi

**Tarih**: 2026-09-11  
**Kapsam**: Football Career Simulator projesi için Soccer Manager 2025 (SM25) IL2CPP AArch64 ikili dosyalarının Salt-Okunur (Read-Only) tersine mühendisliği ve Clean-Room mimari tasarımı.  
**İlkeler**: SM25 tescilli kodu kopyalanmaz, APK modifiye edilmez, lisans/güvenlik bypass edilmez, bağımsız ve üstün sistemler tasarlanır.

---

## Yönetici Özeti ve Tamamlanan Sistemler

Bu araştırma kapsamında üç devasa ana sistem AArch64 makine kodu seviyesinde tamamen çözülmüş, formülleri çıkarılmış, doğrulanmış ve kendi oyunumuza bağımsız clean-room modeller olarak kazandırılmıştır:

1. **Hedef 1: Oyuncu Piyasa Değeri (`PlayerData.get_Value`)**
   - VA: `0x02FC2E80`, RAW: `0x02FBEE80`, Boyut: 824 bayt.
   - 8. dereceden yetenek polinomu ve kuadratik yaş çarpanı `(37^2 - min(age, 36)^2) / 18`.
   - Sezon oynama süresi (%70 varsayılan) ve sezon formu (7.0 varsayılan) dinamik düzeltmeleri.
   - Moral ayrımı: Başlangıç morali `MoraleStart = 5` (%90 değer), nominal tavan `M = 10` (%100 değer).
   - Dahili değer (`int32` GBP) ile arayüz formatı (`MonetaryHelpers.GetValueText`, `exchangeRate`, `B/M/K`) ayrıldı.

2. **Hedef 2: Haftalık Maaş Üretim Hattı (`PlayerContract` & `PlayerData`)**
   - **Kritik Keşif**: `GetPlayerBasicValue` fonksiyonu piyasa değeri değil, **Haftalık Temel Referans Maaş Skalası**dır (`750,000 * (rating/99)^20`).
   - `750,000` değeri bir üst sınır (cap) değil, Rating 99 süperstarın referans çıpasıdır. Kulüp/lig çarpanlarıyla £850,000+ seviyesine çıkabilir.
   - `WageModifierForClub`: Oyuncu uyumu `(0.5 + 0.5 * Rp / Rc)`, kulüp seviyesi `Rc / 94.0`, lig seviyesi `Rl / 89.0`.
   - Sözleşme süresi: Yılda net +%2.5 haftalık maaş artışı `1.0 + (length - 1) / 40.0`.
   - Menajer özellikleri (Trait 36, Trait 38) ve personel indirimi (`WageNegotiator`).
   - Asgari maaş tabanı: `Math.Max(wage, 50.0)` (£50/hafta taban, üst tavan yoktur).
   - Bankacı yuvarlaması (`MonetaryHelpers.RoundMonetaryValue`).

3. **Hedef 3: Oyuncu Gelişimi ve Yaşlanma (`PlayerRatingCalculator` & `PlayerData`)**
   - **Büyüme Dalgası (Increasing Wave)**: Sinüs dalgasının ilk çeyreği $\sin(\text{progress} \times 90^\circ)$ kullanılır. Genç yaşta hızlı başlar, potansiyele ulaştıkça yumuşar.
   - **Zirve Dönemi (Peak Plateau)**: Potansiyel yaşı ile Gerileme yaşı arasında reyting tam potansiyel değerinde sabit kalır (düşüş yaşanmaz).
   - **Gerileme Yaşı (DeclineAge)**: `(PlayerId + 0x48) * 1234` tohumu ile deterministik üretilir (`PlayerData.cacheDeclineAge` ofset `0x7C`).
     - Standart oyuncular: 28-31 yaş.
     - Süperstarlar (Potansiyel >= 90): 30-33 yaş.
     - **Kaleci Koruması**: Position == 1 (GK) için kesin **+1 yaş eklenir**!
     - Zirve garantisi: `DeclineAge >= PotentialAge + 2`.
   - **Gerileme Dalgası (Decreasing Wave & Longevity)**:
     - `longevity == true`: Yılda 8-13 aralığında yavaş düşüş.
     - `longevity == false`: Yılda 13-18 aralığında hızlı düşüş.
     - Asgari reyting tabanı: 60 puan (gerileyen oyuncu 60 altına inmez).
   - **Antrenman Hattı**: Genç oyuncu çarpanı (`ApplyYouthTrainingMod` ile 21 yaş altına ek gelişim), tesis seviyesi (`TrainingGround`) ve antrenör bonusu (Trait 27).

4. **Kendi Oyunumuza Clean-Room Entegrasyonu**:
   - `PlayerWageModel.cs` ve `PlayerWageBalanceProfile.cs` yazıldı.
   - `PlayerWageModelTests.cs` (11 test) ve `PlayerMarketValuationModelTests.cs` (10 test) başarıyla çalıştı.
   - Repository genelindeki **1167 testin tamamı firesiz geçti** (`dotnet test`).
   - 3 adet simülasyon matrisi üretildi:
     - `comparative_player_valuation.csv`
     - `comparative_wage_matrix.csv`
     - `comparative_development_cohort.csv`

---

## 1. Oyuncu Piyasa Değeri ve Maaş Özet Tabloları

### 10 Temsili Oyuncu Piyasa Değeri Benchmark'ı
| Senaryo | Rating | Potential | Age | Peak/Decline | SM25 Değeri (M=5 Start) | SM25 Değeri (M=10 Ceil) | Bizim Modelimiz |
| :---: | :---: | :---: | :---: | :---: | :---: | :---: | :---: |
| **1** | 50 | 60 | 18 | Gelişimde | £1,000 | £1,000 | £250,000 |
| **2** | 70 | 80 | 20 | Gelişimde | £194,561 | £216,179 | £1,800,000 |
| **3** | 80 | 90 | 21 | Gelişimde | £5,550,296 | £6,166,996 | £14,600,000 |
| **4** | 85 | 90 | 24 | Gelişimde | £15,920,402 | £17,689,335 | £33,450,000 |
| **5** | 90 | 95 | 25 | Gelişimde | £49,271,787 | £54,746,430 | £73,950,000 |
| **6** | 95 | 99 | 25 | Gelişimde | £140,891,894 | £156,546,549 | £136,800,000 |
| **7** | 99 | 99 | 25 | Zirvede | £317,508,300 | £352,787,000 | £160,000,000 |
| **8** | 90 | 90 | 30 | Zirvede | £33,149,411 | £36,832,679 | £44,800,000 |
| **9** | 90 | 90 | 34 | Gerilemede | £10,030,223 | £11,144,692 | £18,150,000 |
| **10**| 80 | 80 | 36 | Gerilemede | £380,094 | £422,327 | £4,350,000 |

### Haftalık Maaş Matrisi
| Rating | Potansiyel | Kulüp Seviyesi | Sözleşme | SM25 Maaşı | Bizim Modelimiz | Tasarım Notu |
| :---: | :---: | :---: | :---: | :---: | :---: | :--- |
| **60** | 65 | Düşük (65) | 3 Yıl | £50 | £100 | SM25 £50 tabanına takılır. Bizim model £100 asgari tabanını uygular. |
| **70** | 75 | Orta (75) | 3 Yıl | £600 | £850 | Profesyonel alt lig seviyesi. |
| **80** | 85 | Yüksek (85) | 3 Yıl | £11,000 | £14,000 | Üst lig ilk 11 oyuncusu. |
| **85** | 90 | Yüksek (85) | 5 Yıl | £44,000 | £53,000 | Şampiyonlar Ligi seviyesi. |
| **90** | 95 | Elit (92) | 5 Yıl | £145,000 | £171,000 | Dünya çapında süperstar. |
| **95** | 99 | Elit (92) | 3 Yıl | £400,000 | £413,000 | Dünya elit yıldızları. |
| **99** | 99 | Elit (92) | 5 Yıl | £850,000 | £750,000 | Zirve süperstar (£750k temel skala + çarpanlar). |

---

## 2. Oyuncu Gelişimi ve Yaşlanma Sözde Kodu (Pseudocode)

```csharp
public sealed class PlayerProgressionEngine
{
    public double CalculateRating(PlayerData player, DateTime currentDate)
    {
        DateTime startDate = player.StartDate;
        DateTime potDate = player.PotentialDate;
        int declineAge = player.GetDeclineAge();
        DateTime declineDate = player.GetDeclineDate(declineAge);

        // 1. Evre: Büyüme (Sinüs Dalgası)
        if (currentDate <= potDate)
        {
            double totalDays = (potDate - startDate).TotalDays;
            double elapsedDays = (currentDate - startDate).TotalDays;
            double progress = Math.Clamp(elapsedDays / totalDays, 0.0, 1.0);

            // 0..90 derece arası sinüs dalgası
            double angleRadians = progress * 90.0 * (Math.PI / 180.0);
            double waveGain = Math.Sin(angleRadians);

            return player.StartRating + (player.Potential - player.StartRating) * waveGain;
        }

        // 2. Evre: Zirve Dönemi (Plato)
        if (currentDate < declineDate)
        {
            return (double)player.Potential;
        }

        // 3. Evre: Gerileme (Düşüş Dalgası)
        int currentAge = player.Age;
        double progressInDecline = (currentAge - declineAge) / 10.0;
        double lossRate = player.Longevity ? 1.0 : 1.5;
        double ratingLoss = progressInDecline * 10.0 * lossRate;

        return Math.Max(60.0, player.Potential - ratingLoss);
    }

    public int CalculateDeclineAge(PlayerData player)
    {
        // Önbellek kontrolü
        if (player.CacheDeclineAge > 0)
            return player.CacheDeclineAge;

        // Tohum üretimi
        int seed = (player.Id + 0x48) * 1234;
        var rng = new Random(seed);

        int baseAge = player.Potential >= 90
            ? rng.Next(30, 34)  // Süperstar zirvesi
            : rng.Next(28, 32); // Standart oyuncu zirvesi

        // Kaleci Koruması (+1 Yaş)
        if (player.Position == PositionType.Goalkeeper)
        {
            baseAge += 1;
        }

        // Güvenceler
        int declineAge = Math.Max(baseAge, player.PotentialAge + 2);
        declineAge = Math.Max(declineAge, player.Age);

        player.CacheDeclineAge = declineAge;
        return declineAge;
    }
}
```
