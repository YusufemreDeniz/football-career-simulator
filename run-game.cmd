@echo off
setlocal EnableExtensions
cd /d "%~dp0"

set "PROJECT_DIR=%~dp0src\FootballCareerSimulator.Presentation"
set "PROJECT_FILE=%PROJECT_DIR%\FootballCareerSimulator.Presentation.csproj"
set "GODOT_EXE=C:\Godot\Godot_v4.7-stable_mono_win64.exe"

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
