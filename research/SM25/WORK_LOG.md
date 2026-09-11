# SM25 Araştırma İş Günlüğü (Work Log)

---

## Oturum 1: Başlangıç ve Player Basic Value Analizi
- Ortam incelendi: `base.apk`, `split_config.arm64_v8a.apk`, `libil2cpp.so` (116 MB) ve `global-metadata.dat` (17.5 MB).
- `GetPlayerBasicValue` (VA `0x02FC3FBC`) üzerinde native ARM64 analizi yapıldı.
- Cpp2IL'in 16 olarak gösterdiği üssün native AArch64 makine kodunda `20.0` olduğu doğrulandı.
- `750000.0`, `99.0`, `0.1`, `0.9` sabitleri bellek adresleriyle (`0x01332xxx`) teyit edildi.

---

## Oturum 2: Piyasa Değeri ve Maaş Üretim Hattı Keşfi
- Kritik Keşif: `GetPlayerBasicValue` fonksiyonunun ekrandaki piyasa değeri değil, **Haftalık Temel Referans Maaş Skalası** olduğu anlaşıldı.
- `PlayerData.get_Value` (VA `0x02FC2E80`) bağımsız piyasa değeri metodu olarak çözüldü. 824 baytlık makine kodunun tamamı disassemble edildi.
- Yaş kuadratik terimi `(37^2 - A^2) / 18`, `(R - 55)^8` ve `(P - 57)^8` formülleri çıkarıldı.
- Sezon oynama süresi (%70 varsayılan), sezon formu (7.0 varsayılan) ve moral çarpanı (`0.8 + 0.02 * M`) doğrulandı.
- `PlayerContract` haftalık maaş üretim hattı çözüldü:
  - Kulüp ve lig ölçekleme katsayıları (94.0 ve 89.0 çıpaları).
  - Sözleşme süresi çarpanı (+%2.5 / yıl).
  - Menajer özellikleri (Trait 36, Trait 38).
  - Asgari maaş tabanı (`Math.Max(wage, 50)`).
  - Para birimi yuvarlama motoru (`MonetaryHelpers.RoundMonetaryValue`).

---

## Oturum 3: Clean-Room Entegrasyonu, Terminoloji Düzeltmeleri ve Testler
- **Terminoloji ve Semantik Düzeltmeler**:
  - `Math.Max(wage, 50)` ifadesinin bir tavan (cap) değil, **asgari maaş tabanı (minimum wage floor)** olduğu netleştirildi. SM25'te üst tavanın olmadığı kanıtlandı.
  - `750,000` değerinin final maaş tavanı değil, **Haftalık Temel Referans Maaş Skalası** olduğu, kulüp ve sözleşme çarpanlarıyla aşılabildiği açıklandı.
  - Moral ayrımı çözüldü: Oyun içi başlangıç değeri `MoraleStart = 5` (%90 çarpan), cezasız nominal tavan `M = 10` (1.00x çarpan).
  - Dahili ekonomik değer (`int32` GBP) ile kullanıcı arayüzü gösterimi (`MonetaryHelpers.GetValueText`, `exchangeRate`, `B/M/K` formatı) kesin sınırlarla ayrıldı.
  - `(R - 55)^8` çift üs negatif sayı anomalisinin analizi yapıldı.
- **Clean-Room Mimarisi**:
  - `PlayerWageModel.cs` ve `PlayerWageBalanceProfile.cs` yazıldı.
  - `PlayerWageModelTests.cs` (11 test) eklendi ve tüm testler geçti.
  - `PlayerMarketValuationModelTests.cs` (10 test) korundu ve çalıştırıldı.
  - Çözüm genelindeki **1167 testin tamamı firesiz geçti** (`dotnet test`).
  - Karşılaştırmalı simülasyon üretildi: `comparative_player_valuation.csv` ve `comparative_wage_matrix.csv`.
- Dokümantasyon dili tamamen Türkçe'ye çevrildi.
