# SM25 Araştırma İş Günlüğü

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

## Oturum 3: Temiz Oda Model Başlangıcı, Terminoloji Düzeltmeleri ve Testler
- **Terminoloji ve Semantik Düzeltmeler**:
  - `Math.Max(wage, 50)` ifadesinin bir tavan (cap) değil, **asgari maaş tabanı (minimum wage floor)** olduğu netleştirildi. SM25'te üst tavanın olmadığı kanıtlandı.
  - `750,000` değerinin final maaş tavanı değil, **Haftalık Temel Referans Maaş Skalası** olduğu, kulüp ve sözleşme çarpanlarıyla aşılabildiği açıklandı.
  - Moral ayrımı çözüldü: Oyun içi başlangıç değeri `MoraleStart = 5` (%90 çarpan), cezasız nominal tavan `M = 10` (1.00x çarpan).
  - Dahili ekonomik değer (`int32` GBP) ile kullanıcı arayüzü gösterimi (`MonetaryHelpers.GetValueText`, `exchangeRate`, `B/M/K` formatı) kesin sınırlarla ayrıldı.
  - `(R - 55)^8` çift üs negatif sayı anomalisinin analizi yapıldı.
- **Clean-Room Mimarisi**:
  - `PlayerWageModel.cs` içinde `PlayerWageModel` ve `PlayerWageBalanceProfile` başlangıç modeli yazıldı; bu aşamada production çağrısına henüz bağlanmadı.
  - `PlayerWageModelTests.cs` (11 test) eklendi ve tüm testler geçti.
  - `PlayerMarketValuationModelTests.cs` (10 test) korundu ve çalıştırıldı.
  - Çözüm genelindeki **1167 testin tamamı firesiz geçti** (`dotnet test`).
  - Karşılaştırmalı simülasyon üretildi: `comparative_player_valuation.csv` ve `comparative_wage_matrix.csv`.
- İlgili dokümantasyon bölümleri Türkçeleştirildi.

---

## Oturum 4: Nihai Haftalık Ücret Zinciri ve Production Adaptörü

- `PlayerContract.Wage +0x18` alanının saklı mevcut ücret olduğu doğrudan getter talimatıyla doğrulandı.
- `PlayerContract.Generate`, kompleks üretici, personel değiştiricisi ve parasal yuvarlama sırası ARM64 seviyesinde kapatıldı.
- Sözleşme süresi formülü 1-5 yıl için `1 + (yıl-1)/40` olarak kesinleştirildi.
- Transfer `TraitId 38` ile yenileme `TraitId 36` yolları ayrıldı; özellik yüzdesinin `1 + yüzde/100` formülü doğrulandı.
- `WageNegotiator = 9` değiştiricisinin kaynak veri `+0x18` çarpanını ücrete doğrudan uyguladığı bulundu.
- Yeni sözleşme talebinin deterministik `[1,00,1,10]`, görüşme ekranı beklentisinin `[1,10,1,25]` aralığı çözüldü.
- Kullanıcı görüşmesinde kaydedilen nihai ücretin, talep hesabı değil kabul edilmiş `ContractInfo.Wage` teklifi olduğu kanıtlandı.
- İki `WageModifierForClub` overload'unun farklı oran yönleri ve farklı çağrı yerleri dokümante edildi.
- `PlayerWageModel` kırılımı ham, yuvarlanmış ve sınırlandırılmış değerlerle genişletildi; magic number'lar profile taşındı.
- `ScoutTransferValuationModel` içindeki eski kuadratik öneri kaldırıldı ve temiz oda ücret modeline minimal adaptörle bağlandı.
- Menajer özelliği, personel, alt/üst sınır, yuvarlama ve adapter senaryolarını kapsayan testler eklendi.
- `weekly_wage_simulation.csv` için 2.340 satırlık kontrollü grid üretildi.
- `verify_wage_chain.py`, belirli VA'lardaki talimatları ve doğrudan çağrı hedeflerini doğrulayacak şekilde güçlendirildi.
- Son doğrulamada native verifier geçti; 29/29 odaklı test, 1.171/1.171 seri hızlı test ve 1/1 seri 10 sezon kabul testi başarılı oldu.
- Production Presentation build 0 hata/0 uyarıyla, Godot career smoke `PASS` sonucu ile tamamlandı.
- Commit, push veya branch değişikliği yapılmadı.

---

## Oturum 5: Kod–Rapor Uygunluk Denetimi ve Düzeltmeler (2026-11-09)

- Raporlar (REPORT, DESIGN_TRANSLATION, CONTRACT_NEGOTIATION, DEMANDED_WAGES) üretim koduyla satır satır karşılaştırıldı; sapmalar REPORT.md sonundaki tabloda listelendi.
- Sözleşme talebi native kabul eşiğiyle hizalandı; menajer özelliği birim çelişkisi giderildi; sabır kayıtlı teklif geçmişinden kuruluyor.
- SinWave büyüme üretim yoluna bağlandı (evre bonusu çift sayılmıyor); mevki gerileme profili DESIGN_TRANSLATION ile hizalandı.
- Tesis seviyesi, menajer ilerlemesi ve kıtasal turnuva V50 save'e alındı; çift XP giderildi.
- Doğrulama: 1247/1247 hızlı test, 10 sezon soak 1/1, çözüm derlemesi 0 hata/0 uyarı.
- `iap_native/` klasörü oyunla ilgisiz üçüncü taraf ödeme akışı analizi olduğu için depoya alınmadı (`.gitignore`).
