@echo off
setlocal EnableExtensions
cd /d "%~dp0"

set "ADB=C:\Users\yusufemredeniz\AppData\Local\Android\Sdk\platform-tools\adb.exe"
if not defined GODOT_EXE (
  for %%G in (
    "C:\Godot\Godot-4.7.2-Mono\Godot_v4.7.2-stable_mono_win64_console.exe"
    "C:\Godot\Godot_v4.7.2-stable_mono_win64_console.exe"
    "C:\Godot\Godot_v4.7-stable_mono_win64_console.exe"
  ) do if not defined GODOT_EXE if exist %%G set "GODOT_EXE=%%~G"
)
if not defined GODOT_EXE (
  echo [HATA] Godot 4.7 Mono console bulunamadi. GODOT_EXE ortam degiskenini ayarlayin.
  exit /b 1
)
set "PRESENTATION_DIR=%~dp0src\FootballCareerSimulator.Presentation"
set "APK_PATH=%PRESENTATION_DIR%\builds\android\FootballCareerSimulator-debug.apk"
set "PACKAGE_NAME=com.yusufemredeniz.footballcareersimulator"

echo [1/4] C# kodlari kontrol ediliyor ve derleniyor...
dotnet build "%PRESENTATION_DIR%\FootballCareerSimulator.Presentation.csproj" -c Debug --nologo
if errorlevel 1 (
    echo [HATA] C# derlemesi basarisiz oldu. APK paketleme iptal edildi.
    exit /b 1
)

echo [2/4] Android APK paketi hazirlaniyor (Godot Export)...
"%GODOT_EXE%" --headless --path "%PRESENTATION_DIR%" --export-debug "Android" "builds/android/FootballCareerSimulator-debug.apk"
if errorlevel 1 (
    echo [HATA] Android APK cikartma basarisiz oldu.
    exit /b 1
)

if /I "%~1"=="--build-only" (
    echo [BASARILI] C# derlemesi ve Android APK exportu tamamlandi. Cihaz kurulumu atlandi.
    exit /b 0
)

echo [3/4] APK telefona yukleniyor (ADB Install)...
"%ADB%" install -r "%APK_PATH%"
if errorlevel 1 (
    echo [HATA] ADB yuklemesi basarisiz. Telefonun bagli ve USB hata ayiklamanin acik oldugundan emin olun.
    exit /b 1
)

echo [4/4] Oyun telefonda baslatiliyor...
"%ADB%" shell monkey -p "%PACKAGE_NAME%" -c android.intent.category.LAUNCHER 1

echo.
echo ========================================================
echo [BASARILI] Oyun Android cihazinizda guncellendi ve baslatildi!
echo ========================================================
exit /b 0
