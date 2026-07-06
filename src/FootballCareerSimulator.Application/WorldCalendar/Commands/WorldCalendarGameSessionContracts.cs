namespace FootballCareerSimulator.Application.WorldCalendar.Commands;

public sealed record SaveWorldCalendarGameResult(
    bool Succeeded,
    string SavePath,
    int SavedDayNumber);

public sealed record LoadWorldCalendarGameResult(
    bool Succeeded,
    string SavePath,
    int LoadedDayNumber,
    bool WasMigrated);
