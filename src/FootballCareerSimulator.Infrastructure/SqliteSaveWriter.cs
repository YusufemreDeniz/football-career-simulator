using FootballCareerSimulator.Domain.Spike1Placeholder;
using FootballCareerSimulator.Simulation.Spike1Placeholder;
using Microsoft.Data.Sqlite;

namespace FootballCareerSimulator.Infrastructure;

/// <summary>
/// Spike 3 (bkz. docs/18_SPIKE_EXECUTION_PLAN.md Kart 4) için yer tutucu, her zaman güncel şema
/// sürümüne ("<see cref="SqliteSaveSchema.CurrentVersion"/>") yazan SQLite save yazıcısıdır.
///
/// "Geçici dosya veya yarım işlem geçerli save olarak kalmaz" kriterini karşılamak için tüm yazma işi
/// `filePath + ".tmp"` üzerinde yapılır; yalnızca tamamı başarıyla tamamlandıktan sonra atomik bir
/// `File.Move` ile gerçek `filePath` değiştirilir. Yarım kalmış bir yazma denemesi asla `filePath`'i
/// etkilemez.
/// </summary>
public static class SqliteSaveWriter
{
    public static void Save(string filePath, World world, int rootSeed, string randomContextVersion)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);
        ArgumentNullException.ThrowIfNull(world);
        ArgumentException.ThrowIfNullOrWhiteSpace(randomContextVersion);

        var snapshot = WorldSnapshotSerializer.Capture(world);
        var canonicalHash = CanonicalStateHasher.ComputeHash(world);

        var tempPath = filePath + ".tmp";

        if (File.Exists(tempPath))
        {
            File.Delete(tempPath);
        }

        using (var connection = new SqliteConnection($"Data Source={tempPath}"))
        {
            connection.Open();

            using var transaction = connection.BeginTransaction();

            CreateSchema(connection, transaction);
            InsertManifest(connection, transaction, rootSeed, randomContextVersion, snapshot.CurrentSeason, canonicalHash);
            InsertClubs(connection, transaction, snapshot.Clubs);
            InsertPlayers(connection, transaction, snapshot.Players);

            transaction.Commit();
        }

        SqliteConnection.ClearAllPools();
        File.Move(tempPath, filePath, overwrite: true);
    }

    private static void CreateSchema(SqliteConnection connection, SqliteTransaction transaction)
    {
        ExecuteNonQuery(connection, transaction, """
            CREATE TABLE SaveManifest (
                SchemaVersion INTEGER NOT NULL,
                CreatedAtUtc TEXT NOT NULL,
                RootSeed INTEGER NOT NULL,
                RandomContextVersion TEXT NOT NULL,
                CurrentSeason INTEGER NOT NULL,
                CanonicalStateHash TEXT NOT NULL
            );
            """);

        ExecuteNonQuery(connection, transaction, """
            CREATE TABLE Clubs (
                ClubId INTEGER PRIMARY KEY,
                Name TEXT NOT NULL
            );
            """);

        ExecuteNonQuery(connection, transaction, """
            CREATE TABLE Players (
                PlayerId INTEGER PRIMARY KEY,
                ClubId INTEGER NOT NULL,
                Age INTEGER NOT NULL,
                Form INTEGER NOT NULL
            );
            """);
    }

    private static void InsertManifest(
        SqliteConnection connection,
        SqliteTransaction transaction,
        int rootSeed,
        string randomContextVersion,
        int currentSeason,
        string canonicalHash)
    {
        using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = """
            INSERT INTO SaveManifest (SchemaVersion, CreatedAtUtc, RootSeed, RandomContextVersion, CurrentSeason, CanonicalStateHash)
            VALUES ($schemaVersion, $createdAtUtc, $rootSeed, $randomContextVersion, $currentSeason, $canonicalHash);
            """;
        command.Parameters.AddWithValue("$schemaVersion", SqliteSaveSchema.CurrentVersion);
        command.Parameters.AddWithValue("$createdAtUtc", DateTime.UtcNow.ToString("O"));
        command.Parameters.AddWithValue("$rootSeed", rootSeed);
        command.Parameters.AddWithValue("$randomContextVersion", randomContextVersion);
        command.Parameters.AddWithValue("$currentSeason", currentSeason);
        command.Parameters.AddWithValue("$canonicalHash", canonicalHash);
        command.ExecuteNonQuery();
    }

    private static void InsertClubs(SqliteConnection connection, SqliteTransaction transaction, IReadOnlyList<ClubSnapshot> clubs)
    {
        using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = "INSERT INTO Clubs (ClubId, Name) VALUES ($id, $name);";
        var idParam = command.Parameters.Add("$id", SqliteType.Integer);
        var nameParam = command.Parameters.Add("$name", SqliteType.Text);

        foreach (var club in clubs)
        {
            idParam.Value = club.ClubId;
            nameParam.Value = club.Name;
            command.ExecuteNonQuery();
        }
    }

    private static void InsertPlayers(SqliteConnection connection, SqliteTransaction transaction, IReadOnlyList<PlayerSnapshot> players)
    {
        using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = "INSERT INTO Players (PlayerId, ClubId, Age, Form) VALUES ($id, $clubId, $age, $form);";
        var idParam = command.Parameters.Add("$id", SqliteType.Integer);
        var clubIdParam = command.Parameters.Add("$clubId", SqliteType.Integer);
        var ageParam = command.Parameters.Add("$age", SqliteType.Integer);
        var formParam = command.Parameters.Add("$form", SqliteType.Integer);

        foreach (var player in players)
        {
            idParam.Value = player.PlayerId;
            clubIdParam.Value = player.ClubId;
            ageParam.Value = player.Age;
            formParam.Value = player.Form;
            command.ExecuteNonQuery();
        }
    }

    internal static void ExecuteNonQuery(SqliteConnection connection, SqliteTransaction? transaction, string sql)
    {
        using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = sql;
        command.ExecuteNonQuery();
    }
}
