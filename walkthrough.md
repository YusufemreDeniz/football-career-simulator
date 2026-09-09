# Futbol Kariyer Simülatörü — Faz 2 Walkthrough

Bu belge, profesyonel 10 adımlı Faz 2 paketinin oyuncu yüzündeki akışını ve doğrulama adımlarını özetler.

## Oyuncu akışı

1. `TRANSFER` sekmesinde bütçe, maaş kotası ve pencere durumunu üst rozetlerden kontrol et. Aday seçildiğinde piyasa değeri, maaş beklentisi ve transfer ihtimali kartını incele.
2. Görüşmeyi mali onaya kadar ilerletip `İmzayı Tamamla` eylemini kullan. Başarılı imzada `HOŞ GELDİN` kartını ve `KADRO > TÜM OYUNCULAR` listesindeki yeşil `#YENİ` işaretini doğrula.
3. `KARİYER` sayfasındaki `KARAR MASASI` kartında `BU NEDEN OLDU?` gerekçesini oku; talebe uygun Kabul, Red, Uyarı, Ceza, Destek veya Karşı Teklif eylemini seç.
4. `LİG` sayfasında son beş form rozetlerini ve Şampiyonlar Ligi, Avrupa Kupaları, küme düşme sol derece şeritlerini kontrol et.
5. `KADRO > KADRO PLANI` altında Kaleci, Savunma, Orta Saha ve Hücum kartlarında as oyuncuyu, alternatifleri ve yeterlilik durumunu incele.
6. Yoğun fikstürde `Tüm Takımı Dinlendir` veya `Hafif Rejenerasyon` eylemini kullan. Alt durum çubuğunda sonraki maçsız güne ait fitness/yorgunluk telemetrisini kontrol et.
7. Ses ve titreşim açıkken sekme, maç günü, transfer imzası ve karar aksiyonlarının geri bildirimini dene.
8. Android geri tuşuyla önce diziliş modalını, sonra oyuncu dosyasını, ardından Kadro alt sekmesini kapat. Devam eden geri basışların Kariyer sayfasına ve ana menüye döndüğünü doğrula.
9. `FİNANS` sayfasındaki kayıt kartında son kayıt zamanı, aktif slot, dünya seed'i ve dosya boyutunu kontrol et. `Hızlı Kaydet` ve `Yeniden Yükle` sırasında bekleme/sonuç animasyonunu izle.

## Teknik doğrulama

PowerShell'de depo kökünden:

```powershell
dotnet test tests\FootballCareerSimulator.Tests\FootballCareerSimulator.Tests.csproj --no-restore --nologo --filter "Category!=LongRunning" --blame-hang-timeout 120s
dotnet build src\FootballCareerSimulator.Presentation\FootballCareerSimulator.Presentation.csproj --no-restore --nologo
& 'C:\Godot\Godot_v4.7-stable_mono_win64_console.exe' --headless --path '.\src\FootballCareerSimulator.Presentation' --career-smoke --quit-after 1
cmd /c run-android.cmd --build-only
```

Beklenen çıktılar:

- Hızlı birim paketi: en az 1.132 test, başarısız 0.
- Presentation build: hata 0, uyarı 0.
- Godot: `CAREER_UI_SMOKE_TEST_RESULT=PASS` ve `WORLD_CALENDAR_UI_SMOKE_TEST_RESULT=PASS`.
- Android build-only: güncel `src\FootballCareerSimulator.Presentation\builds\android\FootballCareerSimulator-debug.apk` oluşur; cihaz kurulumu/başlatma yapılmaz.

Gerçek cihaz testi için USB hata ayıklamayı açıp cihazı bağladıktan sonra `run-android.cmd` çalıştır. APK imzası, ekran ölçekleri, donanım geri tuşu, titreşim ve ses gerçek cihazda ayrıca gözlemlenmelidir.
