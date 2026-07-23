@echo off
setlocal
cd /d "%~dp0"

echo Building Presentation...
dotnet build "%~dp0src\FootballCareerSimulator.Presentation\FootballCareerSimulator.Presentation.csproj" -c Debug --nologo
if errorlevel 1 (
  echo.
  echo Build failed - Godot will not start.
  exit /b 1
)

"C:\Godot\Godot_v4.7-stable_mono_win64.exe" --path "%~dp0src\FootballCareerSimulator.Presentation" %*
