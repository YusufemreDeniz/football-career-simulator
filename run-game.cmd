@echo off
setlocal EnableExtensions
cd /d "%~dp0"

set "PROJECT_DIR=%~dp0src\FootballCareerSimulator.Presentation"
set "PROJECT_FILE=%PROJECT_DIR%\FootballCareerSimulator.Presentation.csproj"
rem GODOT_EXE ortam degiskeni verilmisse onu kullan; yoksa bilinen kurulum yollarini sirayla dene.
if not defined GODOT_EXE (
  for %%G in (
    "C:\Godot\Godot-4.7.2-Mono\Godot_v4.7.2-stable_mono_win64.exe"
    "C:\Godot\Godot_v4.7.2-stable_mono_win64.exe"
    "C:\Godot\Godot_v4.7-stable_mono_win64.exe"
  ) do if not defined GODOT_EXE if exist %%G set "GODOT_EXE=%%~G"
)

if not defined GODOT_EXE (
  echo Godot 4.7 Mono bulunamadi. GODOT_EXE ortam degiskenini Godot exe yoluna ayarlayin.
  exit /b 1
)

if not exist "%GODOT_EXE%" (
  echo Godot 4.7 Mono executable not found:
  echo %GODOT_EXE%
  exit /b 1
)

echo [1/2] Rebuilding the latest Presentation code...
dotnet build "%PROJECT_FILE%" -c Debug --nologo -t:Rebuild
if errorlevel 1 (
  echo.
  echo Build failed - Godot will not start.
  exit /b 1
)

if /I "%~1"=="--build-only" (
  echo Build completed. Godot launch skipped.
  exit /b 0
)

echo [2/2] Starting a fresh game instance...
start "Football Career Simulator" /D "%PROJECT_DIR%" "%GODOT_EXE%" --path "%PROJECT_DIR%" %*

exit /b 0
