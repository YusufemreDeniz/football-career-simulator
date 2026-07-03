using FootballCareerSimulator.Domain.Spike1Placeholder;
using Microsoft.Data.Sqlite;

namespace FootballCareerSimulator.Tests;

/// <summary>
/// Yalnızca testler için: `docs/18_SPIKE_EXECUTION_PLAN.md` Kart 4'ün (Spike 3) migration testlerinde
/// kullanılan, elle oluşturulmuş bir "V1" (Form ve bütünlük hash'i olmayan) save dosyası üretir.
/// Üretim kodunda V1 yazma yeteneği yoktur; yalnızca V1'den okuma/migration desteklenir.
/// </summary>
internal static class LegacySaveFixture
{
    public static void CreateV1File(
        string filePath,
        int rootSeed,
        string randomContextVersion,
        int currentSeason,
        IReadOnlyList<ClubSnapshot> clubs,
        IReadOnlyList<PlayerSnapshot> players,
        bool poisonWithConflictingFormColumn = false)
    {
        if (File.Exists(filePath))
        {
            File.Delete(filePath);
        }

        using var connection = new SqliteConnection($"Data Source={filePath}");
        connection.Open();

        using (var transaction = connection.BeginTransaction())
        {
            Execute(connection, transaction, """
                CREATE TABLE SaveManifest (
                    SchemaVersion INTEGER NOT NULL,
                    CreatedAtUtc TEXT NOT NULL,
                    RootSeed INTEGER NOT NULL,
                    RandomContextVersion TEXT NOT NULL,
                    CurrentSeason INTEGER NOT NULL
                );
                """);

            Execute(connection, transaction, """
                CREATE TABLE Clubs (
                    ClubId INTEGER PRIMARY KEY,
                    Name TEXT NOT NULL
                );
                """);

            var playersColumns = poisonWithConflictingFormColumn
                ? "PlayerId INTEGER PRIMARY KEY, ClubId INTEGER NOT NULL, Age INTEGER NOT NULL, Form TEXT"
                : "PlayerId INTEGER PRIMARY KEY, ClubId INTEGER NOT NULL, Age INTEGER NOT NULL";

            Execute(connection, transaction, $"CREATE TABLE Players ({playersColumns});");

            using (var manifestCommand = connection.CreateCommand())
            {
                manifestCommand.Transaction = transaction;
                manifestCommand.CommandText = """
                    INSERT INTO SaveManifest (SchemaVersion, CreatedAtUtc, RootSeed, RandomContextVersion, CurrentSeason)
                    VALUES (1, $createdAtUtc, $rootSeed, $randomContextVersion, $currentSeason);
                    """;
                manifestCommand.Parameters.AddWithValue("$createdAtUtc", DateTime.UtcNow.ToString("O"));
                manifestCommand.Parameters.AddWithValue("$rootSeed", rootSeed);
                manifestCommand.Parameters.AddWithValue("$randomContextVersion", randomContextVersion);
                manifestCommand.Parameters.AddWithValue("$currentSeason", currentSeason);
                manifestCommand.ExecuteNonQuery();
            }

            using (var clubCommand = connection.CreateCommand())
            {
                clubCommand.Transaction = transaction;
                clubCommand.CommandText = "INSERT INTO Clubs (ClubId, Name) VALUES ($id, $name);";
                var idParam = clubCommand.Parameters.Add("$id", SqliteType.Integer);
                var nameParam = clubCommand.Parameters.Add("$name", SqliteType.Text);

                foreach (var club in clubs)
                {
                    idParam.Value = club.ClubId;
                    nameParam.Value = club.Name;
                    clubCommand.ExecuteNonQuery();
                }
            }

            using (var playerCommand = connection.CreateCommand())
            {
                playerCommand.Transaction = transaction;
                playerCommand.CommandText = poisonWithConflictingFormColumn
                    ? "INSERT INTO Players (PlayerId, ClubId, Age, Form) VALUES ($id, $clubId, $age, 'poisoned');"
                    : "INSERT INTO Players (PlayerId, ClubId, Age) VALUES ($id, $clubId, $age);";
                var idParam = playerCommand.Parameters.Add("$id", SqliteType.Integer);
                var clubIdParam = playerCommand.Parameters.Add("$clubId", SqliteType.Integer);
                var ageParam = playerCommand.Parameters.Add("$age", SqliteType.Integer);

                foreach (var player in players)
                {
                    idParam.Value = player.PlayerId;
                    clubIdParam.Value = player.ClubId;
                    ageParam.Value = player.Age;
                    playerCommand.ExecuteNonQuery();
                }
            }

            transaction.Commit();
        }

        SqliteConnection.ClearAllPools();
    }

    private static void Execute(SqliteConnection connection, SqliteTransaction transaction, string sql)
    {
        using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = sql;
        command.ExecuteNonQuery();
    }
}
