namespace FootballCareerSimulator.Application.Career.Commands;

using FootballCareerSimulator.Application.CareerHub.Queries;

public sealed record SaveCareerGameResult(
    bool Succeeded,
    string SavePath,
    int SavedDayNumber,
    int SavedFixtureCount,
    int RollingAutosaveCount = 0);

public sealed record LoadCareerGameResult(
    bool Succeeded,
    string SavePath,
    int LoadedDayNumber,
    int LoadedFixtureCount,
    bool WasMigrated,
    HubNarrativeUiState? HubNarrativeUiState = null,
    bool RecoveredFromBackup = false,
    string? RecoverySourcePath = null);
