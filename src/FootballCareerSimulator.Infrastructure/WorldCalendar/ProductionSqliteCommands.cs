using Microsoft.Data.Sqlite;

namespace FootballCareerSimulator.Infrastructure.WorldCalendar;

internal static class ProductionSqliteCommands
{
    public static void ExecuteNonQuery(SqliteConnection connection, SqliteTransaction? transaction, string sql)
    {
        using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = sql;
        command.ExecuteNonQuery();
    }
}
