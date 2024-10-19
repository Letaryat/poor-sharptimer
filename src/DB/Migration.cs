using CounterStrikeSharp.API;
using MySqlConnector;
using Npgsql;
using System.Data;
using System.Data.Common;
using System.Data.SQLite;
using System.Threading.Tasks;

namespace SharpTimer;

public partial class SharpTimer
{
    public void ExecuteMigrations(IDbConnection connection)
    {
        string migrationsDirectory = "";
        switch (dbType)
        {
            case DatabaseType.MySQL:
                migrationsDirectory = ModuleDirectory + "/Database/Migrations/MySQL";
                break;
            case DatabaseType.PostgreSQL:
                migrationsDirectory = ModuleDirectory + "/Database/Migrations/PostgreSQL/";
                break;
            case DatabaseType.SQLite:
                migrationsDirectory = ModuleDirectory + "/Database/Migrations/SQLite/";
                break;
        }

        var files = Directory.GetFiles(migrationsDirectory, "*.sql")
                             .OrderBy(f => f);

        using (connection)
        {
            DbCommand? cmd = null;
            switch (dbType)
            {
                case DatabaseType.MySQL:
                    cmd = new MySqlCommand("""
                                                     CREATE TABLE IF NOT EXISTS st_migrations (
                                                         id INT PRIMARY KEY AUTO_INCREMENT,
                                                         version VARCHAR(255) NOT NULL
                                                     );
                                         """, (MySqlConnection)connection);
                    break;
                case DatabaseType.PostgreSQL:
                    cmd = new NpgsqlCommand("""
                                                     CREATE TABLE IF NOT EXISTS st_migrations (
                                                         id SERIAL PRIMARY KEY,
                                                         version VARCHAR(255) NOT NULL
                                                     );
                                         """, (NpgsqlConnection)connection);
                    break;
                case DatabaseType.SQLite:
                    cmd = new SQLiteCommand("""
                                                     CREATE TABLE IF NOT EXISTS st_migrations (
                                                         id INTEGER PRIMARY KEY AUTOINCREMENT,
                                                         version VARCHAR(255) NOT NULL
                                                     );
                                         """, (SQLiteConnection)connection);
                    break;
            }

            using (cmd)
            {
                cmd?.ExecuteNonQuery();
            }

            // Get the last applied migration version
            var lastAppliedVersion = GetLastAppliedVersion(connection);

            foreach (var file in files)
            {
                var version = Path.GetFileNameWithoutExtension(file);

                // Check if the migration has already been applied
                if (string.Compare(version, lastAppliedVersion, StringComparison.OrdinalIgnoreCase) <= 0) continue;
                var sqlScript = File.ReadAllText(file);
                DbCommand? cmdMigration = null;
                switch (dbType)
                {
                    case DatabaseType.MySQL:
                        cmdMigration = new MySqlCommand(sqlScript, (MySqlConnection)connection);
                        break;
                    case DatabaseType.PostgreSQL:
                        cmdMigration = new NpgsqlCommand(sqlScript, (NpgsqlConnection)connection);
                        break;
                    case DatabaseType.SQLite:
                        cmdMigration = new SQLiteCommand(sqlScript, (SQLiteConnection)connection);
                        break;

                }
                using (cmdMigration)
                {
                    cmdMigration?.ExecuteNonQuery();
                }

                // Update the last applied migration version
                UpdateLastAppliedVersion(connection, version);

                SharpTimerDebug($"Migration \"{version}\" successfully applied.");
            }
        }
    }

    private string GetLastAppliedVersion(IDbConnection connection)
    {
        DbCommand? cmd = null;
        switch (dbType)
        {
            case DatabaseType.MySQL:
                cmd = new MySqlCommand("SELECT version FROM st_migrations ORDER BY id DESC LIMIT 1;", (MySqlConnection)connection);
                break;
            case DatabaseType.PostgreSQL:
                cmd = new NpgsqlCommand("SELECT version FROM st_migrations ORDER BY id DESC LIMIT 1;", (NpgsqlConnection)connection);
                break;
            case DatabaseType.SQLite:
                cmd = new SQLiteCommand("SELECT version FROM st_migrations ORDER BY id DESC LIMIT 1;", (SQLiteConnection)connection);
                break;
        }
        using (cmd)
        {
            var result = cmd?.ExecuteScalar();
            return result?.ToString() ?? string.Empty;
        }
    }

    private void UpdateLastAppliedVersion(IDbConnection connection, string version)
    {
        DbCommand? cmd = null;
        switch (dbType)
        {
            case DatabaseType.MySQL:
                cmd = new MySqlCommand("INSERT INTO st_migrations (version) VALUES (@Version);", (MySqlConnection)connection);
                break;
            case DatabaseType.PostgreSQL:
                cmd = new NpgsqlCommand("INSERT INTO st_migrations (version) VALUES (@Version);", (NpgsqlConnection)connection);
                break;
            case DatabaseType.SQLite:
                cmd = new SQLiteCommand("INSERT INTO st_migrations (version) VALUES (@Version);", (SQLiteConnection)connection);
                break;

        }
        using (cmd)
        {
            cmd?.AddParameterWithValue("@Version", version);
            cmd?.ExecuteNonQuery();
        }
    }
}