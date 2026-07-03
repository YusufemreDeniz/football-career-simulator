using FootballCareerSimulator.Domain.Spike1Placeholder;
using Microsoft.Data.Sqlite;

namespace FootballCareerSimulator.Infrastructure;

/// <summary>
/// `SqliteSaveReader` ve `SqliteSaveMigrator` arasında paylaşılan, ham satır okuma yardımcılarıdır.
/// </summary>
internal static class SqliteRowReader
{
    public static int ReadSchemaVersion(SqliteConnection connection)
    {
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT SchemaVersion FROM SaveManifest LIMIT 1;";
        var result = command.ExecuteScalar() ?? throw new SaveCorruptionException("SaveManifest tablosunda kayıt bulunamadı.");
        return Convert.ToInt32(result);
    }

    public static (int RootSeed, string RandomContextVersion, int CurrentSeason, string CanonicalStateHash) ReadManifest(SqliteConnection connection)
    {
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT RootSeed, RandomContextVersion, CurrentSeason, CanonicalStateHash FROM SaveManifest LIMIT 1;";
        using var reader = command.ExecuteReader();

        if (!reader.Read())
        {
            throw new SaveCorruptionException("SaveManifest tablosunda kayıt bulunamadı.");
        }

        var rootSeed = reader.GetInt32(0);
        var randomContextVersion = reader.GetString(1);
        var currentSeason = reader.GetInt32(2);
        var canonicalStateHash = reader.IsDBNull(3) ? string.Empty : reader.GetString(3);

        return (rootSeed, randomContextVersion, currentSeason, canonicalStateHash);
    }

    public static IReadOnlyList<ClubSnapshot> ReadClubs(SqliteConnection connection)
    {
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT ClubId, Name FROM Clubs ORDER BY ClubId;";
        using var reader = command.ExecuteReader();

        var clubs = new List<ClubSnapshot>();
        while (reader.Read())
        {
            clubs.Add(new ClubSnapshot(reader.GetInt32(0), reader.GetString(1)));
        }

        return clubs;
    }

    public static IReadOnlyList<PlayerSnapshot> ReadPlayers(SqliteConnection connection)
    {
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT PlayerId, ClubId, Age, Form FROM Players ORDER BY PlayerId;";
        using var reader = command.ExecuteReader();

        var players = new List<PlayerSnapshot>();
        while (reader.Read())
        {
            players.Add(new PlayerSnapshot(reader.GetInt32(0), reader.GetInt32(1), reader.GetInt32(2), reader.GetInt32(3)));
        }

        return players;
    }
}
