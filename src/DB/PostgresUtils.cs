/*
Copyright (C) 2024 Dea Brcka

This program is free software: you can redistribute it and/or modify
it under the terms of the GNU General Public License as published by
the Free Software Foundation, either version 3 of the License, or
(at your option) any later version.
This program is distributed in the hope that it will be useful,
but WITHOUT ANY WARRANTY; without even the implied warranty of
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
GNU General Public License for more details.
You should have received a copy of the GNU General Public License
along with this program.  If not, see <https://www.gnu.org/licenses/>.
*/

using System.Text.Json;
using MySqlConnector;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes.Registration;
using CounterStrikeSharp.API.Modules.Admin;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Utils;
using System.Data;
using Npgsql;

namespace SharpTimer
{
    partial class SharpTimer
    {
        private async Task<NpgsqlConnection> OpenPostgresDatabaseConnectionAsync()
        {
            var connection = new NpgsqlConnection(await GetPostgresConnectionStringFromConfigFile(postgresPath!));
            await connection.OpenAsync();

            if (connection.State != ConnectionState.Open)
            {
                usePostgres = false;
            }

            return connection;
        }

        private async Task CheckPostgresTablesAsync()
        {
            string[] playerRecordsColumns = [   @"""MapName"" VARCHAR(255) DEFAULT ''",
                                                    @"""SteamID"" VARCHAR(20) DEFAULT ''",
                                                    @"""PlayerName"" VARCHAR(32) DEFAULT ''",
                                                    @"""TimerTicks"" INT DEFAULT 0",
                                                    @"""FormattedTime"" VARCHAR(255) DEFAULT ''",
                                                    @"""UnixStamp"" INT DEFAULT 0",
                                                    @"""LastFinished"" INT DEFAULT 0",
                                                    @"""TimesFinished"" INT DEFAULT 0", 
                                                    @"""Style"" INT DEFAULT 0"
                                                ];

            string[] playerStatsColumns = [   @"""SteamID"" VARCHAR(20) DEFAULT ''",
                                                    @"""PlayerName"" VARCHAR(32) DEFAULT ''",
                                                    @"""TimesConnected"" INT DEFAULT 0",
                                                    @"""LastConnected"" INT DEFAULT 0",
                                                    @"""GlobalPoints"" INT DEFAULT 0",
                                                    @"""HideTimerHud"" BOOL DEFAULT false",
                                                    @"""HideKeys"" BOOL DEFAULT false",
                                                    @"""HideJS"" BOOL DEFAULT false",
                                                    @"""SoundsEnabled"" BOOL DEFAULT false",
                                                    @"""PlayerFov"" INT DEFAULT 0",
                                                    @"""IsVip"" BOOL DEFAULT false",
                                                    @"""BigGifID"" VARCHAR(16) DEFAULT 'x'"
                                                ];

            using (var connection = await OpenPostgresDatabaseConnectionAsync())
            {
                try
                {
                    // Check PlayerRecords
                    SharpTimerDebug($"Checking PlayerRecords Table...");
                    await CreatePostgresPlayerRecordsTableAsync(connection);
                    await UpdatePostgresTableColumnsAsync(connection, "PlayerRecords", playerRecordsColumns);
                    await AddConstraintsToPostgresRecordsTableAsync(connection, "PlayerRecords");

                    // Check PlayerStats
                    SharpTimerDebug($"Checking PlayerStats Table...");
                    await CreatePostgresPlayerStatsTableAsync(connection);
                    await UpdatePostgresTableColumnsAsync(connection, $"{PlayerStatsTable}", playerStatsColumns);
                }
                catch (Exception ex)
                {
                    SharpTimerError($"Error in CheckPostgresTablesAsync: {ex}");
                }
            }
        }

        private async Task UpdatePostgresTableColumnsAsync(NpgsqlConnection connection, string tableName, string[] columns)
        {
            if (await PostgresTableExistsAsync(connection, tableName))
            {
                foreach (string columnDefinition in columns)
                {
                    string columnName = columnDefinition.Split(' ')[0];

                    if (!await PostgresColumnExistsAsync(connection, tableName, columnName))
                    {
                        SharpTimerDebug($"column {columnName} does not exist");
                        await AddPostgresColumnToTableAsync(connection, tableName, columnDefinition);
                    }
                }
            }
        }

        private async Task<bool> PostgresTableExistsAsync(NpgsqlConnection connection, string tableName)
        {
            string query = $@"SELECT COUNT(*) FROM information_schema.tables WHERE table_schema = 'public' AND table_name = '""{tableName}""'";
            using (NpgsqlCommand command = new(query, connection))
            {
                try
                {
                    int count = Convert.ToInt32(await command.ExecuteScalarAsync());
                    return count > 0;
                }
                catch (Exception ex)
                {
                    SharpTimerError($"Error in PostgresTableExistsAsync: {ex.Message}");
                    return false;
                }
            }
        }

        private async Task<bool> PostgresColumnExistsAsync(NpgsqlConnection connection, string tableName, string columnName)
        {
            string query = $@"SELECT COUNT(*) FROM information_schema.columns WHERE table_schema = 'public' AND table_name = '{tableName}' AND column_name = '{columnName}'";
            using (NpgsqlCommand command = new(query, connection))
            {
                try
                {
                    int count = Convert.ToInt32(await command.ExecuteScalarAsync());
                    return count > 0;
                }
                catch (Exception ex)
                {
                    SharpTimerError($"Error in PostgresColumnExistsAsync: {ex.Message}");
                    return false;
                }
            }
        }

        private async Task AddPostgresColumnToTableAsync(NpgsqlConnection connection, string tableName, string columnDefinition)
        {
            var parts = columnDefinition.Split(new[] { ' ' }, 2);
            var columnName = parts[0].Trim('"');
            var columnTypeAndConstraints = parts.Length > 1 ? parts[1] : "";

            string query = $@"ALTER TABLE ""{tableName}"" ADD ""{columnName}"" {columnTypeAndConstraints}";
            using (NpgsqlCommand command = new(query, connection))
            {
                try
                {
                    await command.ExecuteNonQueryAsync();
                }
                catch (Exception ex)
                {
                    SharpTimerError($"Error in AddPostgresColumnToTableAsync: {ex.Message}");
                }
            }
        }
        private async Task AddConstraintsToPostgresRecordsTableAsync(NpgsqlConnection connection, string tableName)
        {
            string query = $@"ALTER TABLE ""{tableName}"" ADD CONSTRAINT pk_Records PRIMARY KEY (""MapName"", ""SteamID"", ""Style"")";
            using (NpgsqlCommand command = new(query, connection))
            {
                try
                {
                    await command.ExecuteNonQueryAsync();
                }
                catch (Exception ex)
                {
                    SharpTimerDebug($"Table already has primary key constraint");
                }
            }
        }

        private async Task CreatePostgresPlayerRecordsTableAsync(NpgsqlConnection connection)
        {
            string createTableQuery = @"CREATE TABLE IF NOT EXISTS ""PlayerRecords"" (
                                            ""MapName"" VARCHAR(255),
                                            ""SteamID"" VARCHAR(20),
                                            ""PlayerName"" VARCHAR(32),
                                            ""TimerTicks"" INT,
                                            ""FormattedTime"" VARCHAR(255),
                                            ""UnixStamp"" INT,
                                            ""TimesFinished"" INT,
                                            ""LastFinished"" INT,
                                            ""Style"" INT,
                                            PRIMARY KEY (""MapName"", ""SteamID"", ""Style"")
                                        )";
            using (var createTableCommand = new NpgsqlCommand(createTableQuery, connection))
            {
                try
                {
                    await createTableCommand.ExecuteNonQueryAsync();
                }
                catch (Exception ex)
                {
                    SharpTimerError($"Error in CreatePostgresPlayerRecordsTableAsync: {ex.Message}");
                }
            }
        }

        private async Task CreatePostgresPlayerStatsTableAsync(NpgsqlConnection connection)
        {
            string createTableQuery = $@"CREATE TABLE IF NOT EXISTS ""{PlayerStatsTable}"" (
                                            ""SteamID"" VARCHAR(20) UNIQUE,
                                            ""PlayerName"" VARCHAR(32),
                                            ""TimesConnected"" INT,
                                            ""LastConnected"" INT,
                                            ""GlobalPoints"" INT,
                                            ""HideTimerHud"" BOOL,
                                            ""HideKeys"" BOOL,
                                            ""HideJS"" BOOL,
                                            ""SoundsEnabled"" BOOL,
                                            ""PlayerFov"" INT,
                                            ""IsVip"" BOOL,
                                            ""BigGifID"" VARCHAR(16),
                                            PRIMARY KEY (""SteamID"")
                                        )";
            using (var createTableCommand = new NpgsqlCommand(createTableQuery, connection))
            {
                try
                {
                    await createTableCommand.ExecuteNonQueryAsync();
                }
                catch (Exception ex)
                {
                    SharpTimerError($"Error in CreatePostgresPlayerStatsTableAsync: {ex.Message}");
                }
            }
        }

        private async Task<string> GetPostgresConnectionStringFromConfigFile(string postgresPath)
        {
            try
            {
                using (JsonDocument? jsonConfig = await LoadJson(postgresPath)!)
                {
                    if (jsonConfig != null)
                    {
                        JsonElement root = jsonConfig.RootElement;

                        string host = root.TryGetProperty("PostgresHost", out var hostProperty) ? hostProperty.GetString()! : "localhost";
                        string database = root.TryGetProperty("PostgresDatabase", out var databaseProperty) ? databaseProperty.GetString()! : "database";
                        string username = root.TryGetProperty("PostgresUsername", out var usernameProperty) ? usernameProperty.GetString()! : "root";
                        string password = root.TryGetProperty("PostgresPassword", out var passwordProperty) ? passwordProperty.GetString()! : "root";
                        int port = root.TryGetProperty("PostgresPort", out var portProperty) ? portProperty.GetInt32()! : 3306;

                        string tableprefix = root.TryGetProperty("PostgresTablePrefix", out var tableprefixProperty) ? tableprefixProperty.GetString()! : "";

                        PlayerStatsTable = $"{(tableprefix != "" ? $"PlayerStats_{tableprefix}" : "PlayerStats")}";

                        return $"Server={host};Database={database};User ID={username};Password={password};Port={port};SslMode=Disable";
                    }
                    else
                    {
                        SharpTimerError($"postgres json was null");
                    }
                }
            }
            catch (Exception ex)
            {
                SharpTimerError($"Error in GetPostgresConnectionString: {ex.Message}");
            }

            return "Server=localhost;Database=database;User ID=root;Password=root;Port=3306;SslMode=Disable";
        }
    }
}