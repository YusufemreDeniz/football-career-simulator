using Microsoft.Data.Sqlite;

namespace FootballCareerSimulator.Tests.WorldCalendar;

/// <summary>
/// Production loader'ın spike placeholder save'leri reddettiğini test etmek için minimal fixture.
/// </summary>
internal static class LegacySpikeSaveFixture
{
    public static void CreateMinimalFile(string filePath)
    {
        if (File.Exists(filePath))
        {
            File.Delete(filePath);
        }

        using var connection = new SqliteConnection($"Data Source={filePath}");
        connection.Open();

        using var command = connection.CreateCommand();
        command.CommandText = """
            CREATE TABLE SaveManifest (
                SchemaVersion INTEGER NOT NULL,
                CreatedAtUtc TEXT NOT NULL,
                RootSeed INTEGER NOT NULL,
                RandomContextVersion TEXT NOT NULL,
                CurrentSeason INTEGER NOT NULL,
                CanonicalStateHash TEXT NOT NULL
            );

            INSERT INTO SaveManifest (
                SchemaVersion, CreatedAtUtc, RootSeed, RandomContextVersion, CurrentSeason, CanonicalStateHash)
            VALUES (2, '2026-01-01T00:00:00.0000000Z', 42, '1', 1, 'spike-placeholder-hash');
            """;
        command.ExecuteNonQuery();
    }
}
