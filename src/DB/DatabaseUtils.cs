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
using System.Data;
using Npgsql;
using System.Data.Common;
using Microsoft.Data.Sqlite;
using System.Text.RegularExpressions;

namespace SharpTimer
{
    public enum DatabaseType
    {
        MySQL,
        PostgreSQL,
        SQLite
    }

    partial class SharpTimer
    {
        public async Task<IDbConnection> OpenConnectionAsync()
        {
            IDbConnection? connection = null;
            switch (dbType)
            {
                case DatabaseType.MySQL:
                    connection = new MySqlConnection(await GetConnectionStringFromConfigFile());
                    await (connection as MySqlConnection)!.OpenAsync();
                    break;
                case DatabaseType.PostgreSQL:
                    connection = new NpgsqlConnection(await GetConnectionStringFromConfigFile());
                    await (connection as NpgsqlConnection)!.OpenAsync();
                    break;
                case DatabaseType.SQLite:
                    connection = new SqliteConnection(await GetConnectionStringFromConfigFile());
                    await (connection as SqliteConnection)!.OpenAsync();
                    break;
            }

            if (connection!.State != ConnectionState.Open)
            {
                useMySQL = false;
                usePostgres = false;
                enableDb = false;
            }

            return connection;
        }

        public IDbConnection OpenConnection()
        {
            IDbConnection? connection = null;
            switch (dbType)
            {
                case DatabaseType.MySQL:
                    connection = new MySqlConnection(GetConnectionStringOnMainThread());
                    (connection as MySqlConnection)!.Open();
                    break;
                case DatabaseType.PostgreSQL:
                    connection = new NpgsqlConnection(GetConnectionStringOnMainThread());
                    (connection as NpgsqlConnection)!.Open();
                    break;
                case DatabaseType.SQLite:
                    connection = new SqliteConnection(GetConnectionStringOnMainThread());
                    (connection as SqliteConnection)!.Open();
                    break;
            }

            if (connection!.State != ConnectionState.Open)
            {
                useMySQL = false;
                usePostgres = false;
                enableDb = false;
            }

            return connection;
        }

        private string GetConnectionStringOnMainThread()
        {
            try
            {
                if (dbType.Equals(DatabaseType.SQLite))
                {
                    return $"Data Source={dbPath}";
                }
                else
                {
                    using (JsonDocument? jsonConfig = Utils.LoadJsonOnMainThread(dbPath)!)
                    {
                        if (jsonConfig != null)
                        {
                            JsonElement root = jsonConfig.RootElement;

                            string host = root.TryGetProperty("Host", out var hostProperty)
                                ? hostProperty.GetString()!
                                : "localhost";
                            string database = root.TryGetProperty("Database", out var databaseProperty)
                                ? databaseProperty.GetString()!
                                : "database";
                            string username = root.TryGetProperty("Username", out var usernameProperty)
                                ? usernameProperty.GetString()!
                                : "root";
                            string password = root.TryGetProperty("Password", out var passwordProperty)
                                ? passwordProperty.GetString()!
                                : "root";
                            int port = root.TryGetProperty("Port", out var portProperty)
                                ? portProperty.GetInt32()!
                                : 3306;
                            string tableprefix = root.TryGetProperty("TablePrefix", out var tableprefixProperty)
                                ? tableprefixProperty.GetString()!
                                : "";

                            PlayerStatsTable = $"{(tableprefix != "" ? $"PlayerStats_{tableprefix}" : "PlayerStats")}";

                            if (dbType.Equals(DatabaseType.MySQL))
                            {
                                int timeout = root.TryGetProperty("Timeout", out var timeoutProperty)
                                    ? timeoutProperty.GetInt32()!
                                    : 30;
                                return
                                    $"Server={host};Database={database};User ID={username};Password={password};Port={port};CharSet=utf8mb4;Connection Timeout={timeout};";
                            }
                            else if (dbType.Equals(DatabaseType.PostgreSQL))
                            {
                                return
                                    $"Server={host};Database={database};User ID={username};Password={password};Port={port};SslMode=Disable";
                            }
                            else if (dbType.Equals(DatabaseType.SQLite))
                            {
                                return $"Data Source={dbPath}";
                            }
                            else
                            {
                                Utils.LogError($"Database type not supported");
                            }
                        }
                        else
                        {
                            Utils.LogError($"Database json was null");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Utils.LogError($"Error in GetConnectionString: {ex.Message}");
            }

            return "Server=localhost;Database=database;User ID=root;Password=root;Port=3306;";
        }

        private async Task<string> GetConnectionStringFromConfigFile()
        {
            try
            {
                if (dbType.Equals(DatabaseType.SQLite))
                {
                    return $"Data Source={dbPath}";
                }
                else
                {
                    using (JsonDocument? jsonConfig = await Utils.LoadJson(dbPath)!)
                    {
                        if (jsonConfig != null)
                        {
                            JsonElement root = jsonConfig.RootElement;

                            string host = root.TryGetProperty("Host", out var hostProperty)
                                ? hostProperty.GetString()!
                                : "localhost";
                            string database = root.TryGetProperty("Database", out var databaseProperty)
                                ? databaseProperty.GetString()!
                                : "database";
                            string username = root.TryGetProperty("Username", out var usernameProperty)
                                ? usernameProperty.GetString()!
                                : "root";
                            string password = root.TryGetProperty("Password", out var passwordProperty)
                                ? passwordProperty.GetString()!
                                : "root";
                            int port = root.TryGetProperty("Port", out var portProperty)
                                ? portProperty.GetInt32()!
                                : 3306;
                            string tableprefix = root.TryGetProperty("TablePrefix", out var tableprefixProperty)
                                ? tableprefixProperty.GetString()!
                                : "";
                            string sslMode = root.TryGetProperty("SslMode", out var sslModeProperty)
                                ? sslModeProperty.GetString()!
                                : "Preferred";

                            PlayerStatsTable = $"{(tableprefix != "" ? $"PlayerStats_{tableprefix}" : "PlayerStats")}";

                            if (dbType.Equals(DatabaseType.MySQL))
                            {
                                int timeout = root.TryGetProperty("Timeout", out var timeoutProperty)
                                    ? timeoutProperty.GetInt32()!
                                    : 30;
                                return
                                    $"Server={host};Database={database};User ID={username};Password={password};Port={port};CharSet=utf8mb4;Connection Timeout={timeout};SslMode={sslMode}";
                            }
                            else if (dbType.Equals(DatabaseType.PostgreSQL))
                            {
                                return
                                    $"Server={host};Database={database};User ID={username};Password={password};Port={port};SslMode=Disable";
                            }
                            else if (dbType.Equals(DatabaseType.SQLite))
                            {
                                return $"Data Source={dbPath}";
                            }
                            else
                            {
                                Utils.LogError($"Database type not supported");
                            }
                        }
                        else
                        {
                            Utils.LogError($"Database json was null");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Utils.LogError($"Error in GetConnectionString: {ex.Message}");
            }

            return "Server=localhost;Database=database;User ID=root;Password=root;Port=3306;";
        }

        public async Task CheckTablesAsync()
        {
            string[]? playerRecords;
            string[]? playerStats;
            string[]? playerStageTimes;

            switch (dbType)
            {
                case DatabaseType.MySQL:
                    playerRecords =
                    [
                        "MapName VARCHAR(255) DEFAULT ''",
                        "SteamID VARCHAR(20) DEFAULT ''",
                        "PlayerName VARCHAR(32) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci DEFAULT ''",
                        "TimerTicks INT DEFAULT 0",
                        "FormattedTime VARCHAR(255) DEFAULT ''",
                        "UnixStamp INT DEFAULT 0",
                        "LastFinished INT DEFAULT 0",
                        "TimesFinished INT DEFAULT 0",
                        "Style INT DEFAULT 0"
                    ];
                    playerStats =
                    [
                        "SteamID VARCHAR(20) DEFAULT ''",
                        "PlayerName VARCHAR(32) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci DEFAULT ''",
                        "TimesConnected INT DEFAULT 0",
                        "LastConnected INT DEFAULT 0",
                        "GlobalPoints INT DEFAULT 0",
                        "HideTimerHud BOOL DEFAULT false",
                        "HideKeys BOOL DEFAULT false",
                        "SoundsEnabled BOOL DEFAULT false",
                        "PlayerFov INT DEFAULT 0",
                        "IsVip BOOL DEFAULT false",
                        "BigGifID VARCHAR(16) DEFAULT 'x'"
                    ];
                    playerStageTimes =
                    [
                        "MapName VARCHAR(255) DEFAULT ''",
                        "SteamID VARCHAR(20) DEFAULT ''",
                        "PlayerName VARCHAR(32) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci DEFAULT ''",
                        "Stage INT DEFAULT 0",
                        "TimerTicks INT DEFAULT 0",
                        "FormattedTime VARCHAR(255) DEFAULT ''",
                        "Velocity VARCHAR(255) DEFAULT ''"
                    ];
                    break;
                case DatabaseType.PostgreSQL:
                    playerRecords =
                    [
                        @"""MapName"" VARCHAR(255) DEFAULT ''",
                        @"""SteamID"" VARCHAR(20) DEFAULT ''",
                        @"""PlayerName"" VARCHAR(32) DEFAULT ''",
                        @"""TimerTicks"" INT DEFAULT 0",
                        @"""FormattedTime"" VARCHAR(255) DEFAULT ''",
                        @"""UnixStamp"" INT DEFAULT 0",
                        @"""LastFinished"" INT DEFAULT 0",
                        @"""TimesFinished"" INT DEFAULT 0",
                        @"""Style"" INT DEFAULT 0"
                    ];
                    playerStats =
                    [
                        @"""SteamID"" VARCHAR(20) DEFAULT ''",
                        @"""PlayerName"" VARCHAR(32) DEFAULT ''",
                        @"""TimesConnected"" INT DEFAULT 0",
                        @"""LastConnected"" INT DEFAULT 0",
                        @"""GlobalPoints"" INT DEFAULT 0",
                        @"""HideTimerHud"" BOOL DEFAULT false",
                        @"""HideKeys"" BOOL DEFAULT false",
                        @"""SoundsEnabled"" BOOL DEFAULT false",
                        @"""PlayerFov"" INT DEFAULT 0",
                        @"""IsVip"" BOOL DEFAULT false",
                        @"""BigGifID"" VARCHAR(16) DEFAULT 'x'"
                    ];
                    playerStageTimes =
                    [
                        @"""MapName"" VARCHAR(255) DEFAULT ''",
                        @"""SteamID"" VARCHAR(20) DEFAULT ''",
                        @"""PlayerName"" VARCHAR(32) DEFAULT ''",
                        @"""Stage"" INT DEFAULT 0",
                        @"""TimerTicks"" INT DEFAULT 0",
                        @"""FormattedTime"" VARCHAR(255) DEFAULT ''",
                        @"""Velocity"" VARCHAR(255) DEFAULT ''"
                    ];
                    break;
                case DatabaseType.SQLite:
                    playerRecords =
                    [
                        "MapName TEXT DEFAULT ''",
                        "SteamID TEXT DEFAULT ''",
                        "PlayerName TEXT DEFAULT ''",
                        "TimerTicks INT DEFAULT 0",
                        "FormattedTime TEXT DEFAULT ''",
                        "UnixStamp INT DEFAULT 0",
                        "LastFinished INT DEFAULT 0",
                        "TimesFinished INT DEFAULT 0",
                        "Style INT DEFAULT 0"
                    ];
                    playerStats =
                    [
                        "SteamID TEXT DEFAULT ''",
                        "PlayerName TEXT DEFAULT ''",
                        "TimesConnected INTEGER DEFAULT 0",
                        "LastConnected INTEGER DEFAULT 0",
                        "GlobalPoints INTEGER DEFAULT 0",
                        "HideTimerHud INTEGER DEFAULT 0",
                        "HideKeys INTEGER DEFAULT 0",
                        "SoundsEnabled INTEGER DEFAULT 1",
                        "PlayerFov INTEGER DEFAULT 0",
                        "IsVip INTEGER DEFAULT 0",
                        "BigGifID TEXT DEFAULT 'x'"
                    ];
                    playerStageTimes =
                    [
                        "MapName TEXT DEFAULT ''",
                        "SteamID TEXT DEFAULT ''",
                        "PlayerName TEXT DEFAULT ''",
                        "Stage INTEGER DEFAULT 0",
                        "TimerTicks INTEGER DEFAULT 0",
                        "FormattedTime TEXT DEFAULT ''",
                        "Velocity TEXT DEFAULT ''"
                    ];
                    break;
                default:
                    playerRecords = null;
                    playerStats = null;
                    playerStageTimes = null;
                    Utils.LogError($"Database type not supported");
                    break;
            }

            using (var connection = await OpenConnectionAsync())
            {
                try
                {
                    // Check PlayerRecords
                    Utils.LogDebug($"Checking PlayerRecords Table...");
                    await CreatePlayerRecordsTableAsync(connection);
                    await UpdateTableColumnsAsync(connection, "PlayerRecords", playerRecords!);

                    // Check PlayerStats
                    Utils.LogDebug($"Checking PlayerStats Table...");
                    await CreatePlayerStatsTableAsync(connection);
                    await UpdateTableColumnsAsync(connection, $"{PlayerStatsTable}", playerStats!);

                    // Check PlayerStageTimes
                    Utils.LogDebug($"Checking PlayerStageTimes Table...");
                    await CreatePlayerStageTimesTableAsync(connection);
                    await UpdateTableColumnsAsync(connection, "PlayerStageTimes", playerStageTimes!);
                }
                catch (Exception ex)
                {
                    Utils.LogError($"Error in CheckTablesAsync: {ex}");
                }
            }
        }

        private async Task CreatePlayerRecordsTableAsync(IDbConnection connection)
        {
            DbCommand? createTableCommand;
            string createTableQuery;
            switch (dbType)
            {
                case DatabaseType.MySQL:
                    createTableQuery = @"CREATE TABLE IF NOT EXISTS PlayerRecords (
                                            MapName VARCHAR(255),
                                            SteamID VARCHAR(20),
                                            PlayerName VARCHAR(32) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci,
                                            TimerTicks INT,
                                            FormattedTime VARCHAR(255),
                                            UnixStamp INT,
                                            TimesFinished INT,
                                            LastFinished INT,
                                            Style INT,
                                            PRIMARY KEY (MapName, SteamID, Style)
                                        )";
                    createTableCommand = new MySqlCommand(createTableQuery, (MySqlConnection)connection);
                    break;
                case DatabaseType.PostgreSQL:
                    createTableQuery = @"CREATE TABLE IF NOT EXISTS ""PlayerRecords"" (
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
                    createTableCommand = new NpgsqlCommand(createTableQuery, (NpgsqlConnection)connection);
                    break;
                case DatabaseType.SQLite:
                    createTableQuery = @"CREATE TABLE IF NOT EXISTS PlayerRecords (
                                            MapName TEXT,
                                            SteamID TEXT,
                                            PlayerName TEXT,
                                            TimerTicks INT,
                                            FormattedTime TEXT,
                                            UnixStamp INT,
                                            TimesFinished INT,
                                            LastFinished INT,
                                            Style INT,
                                            PRIMARY KEY (MapName, SteamID, Style)
                                        )";
                    createTableCommand = new SqliteCommand(createTableQuery, (SqliteConnection)connection);
                    break;
                default:
                    createTableCommand = null;
                    break;
            }

            using (createTableCommand)
            {
                try
                {
                    await createTableCommand!.ExecuteNonQueryAsync();
                }
                catch (Exception ex)
                {
                    Utils.LogError($"Error in CreatePlayerRecordsTableAsync: {ex.Message}");
                }
            }
        }

        private async Task UpdateTableColumnsAsync(IDbConnection connection, string tableName, string[] columns)
        {
            if (await TableExistsAsync(connection, tableName))
            {
                foreach (string columnDefinition in columns)
                {
                    string columnName = columnDefinition.Split(' ')[0];
                    if (!await ColumnExistsAsync(connection, tableName, columnName))
                    {
                        Utils.LogDebug($"Adding column {columnName} to {tableName}...");
                        await AddColumnToTableAsync(connection, tableName, columnDefinition);
                    }
                }
            }
        }

        private async Task<bool> TableExistsAsync(IDbConnection connection, string tableName)
        {
            DbCommand? command;
            string query;
            switch (dbType)
            {
                case DatabaseType.MySQL:
                    query =
                        $"SELECT COUNT(*) FROM information_schema.tables WHERE table_schema = '{connection.Database}' AND table_name = '{tableName}'";
                    command = new MySqlCommand(query, (MySqlConnection)connection);
                    break;
                case DatabaseType.PostgreSQL:
                    query =
                        $@"SELECT COUNT(*) FROM information_schema.tables WHERE table_schema = 'public' AND table_name = '""{tableName}""'";
                    command = new NpgsqlCommand(query, (NpgsqlConnection)connection);
                    break;
                case DatabaseType.SQLite:
                    query = $"SELECT COUNT(*) FROM sqlite_master WHERE type = 'table' AND name = '{tableName}'";
                    command = new SqliteCommand(query, (SqliteConnection)connection);
                    break;
                default:
                    command = null;
                    break;
            }

            using (command)
            {
                try
                {
                    int count = Convert.ToInt32(await command!.ExecuteScalarAsync());
                    return count > 0;
                }
                catch (Exception ex)
                {
                    Utils.LogError($"Error in TableExistsAsync: {ex.Message}");
                    return false;
                }
            }
        }

        private async Task<bool> ColumnExistsAsync(IDbConnection connection, string tableName, string columnName)
        {
            DbCommand? command;
            string query;
            switch (dbType)
            {
                case DatabaseType.MySQL:
                    query =
                        $"SELECT COUNT(*) FROM information_schema.columns WHERE table_schema = '{connection.Database}' AND table_name = '{tableName}' AND column_name = '{columnName}'";
                    command = new MySqlCommand(query, (MySqlConnection)connection);
                    break;
                case DatabaseType.PostgreSQL:
                    query =
                        $@"SELECT COUNT(*) FROM information_schema.columns WHERE table_schema = 'public' AND table_name = '{tableName}' AND column_name = '{columnName}'";
                    command = new NpgsqlCommand(query, (NpgsqlConnection)connection);
                    break;
                case DatabaseType.SQLite:
                    query = $"PRAGMA table_info({tableName})";
                    command = new SqliteCommand(query, (SqliteConnection)connection);
                    break;
                default:
                    command = null;
                    break;
            }

            using (command)
            {
                if (dbType == DatabaseType.SQLite)
                {
                    try
                    {
                        using (var reader = await command!.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                if (reader.GetString(1) == columnName) return true;
                            }

                            return false;
                        }
                    }
                    catch (Exception ex)
                    {
                        Utils.LogError($"Error in ColumnExistsAsync: {ex.Message}");
                        return false;
                    }
                }
                else
                {
                    try
                    {
                        int count = Convert.ToInt32(await command!.ExecuteScalarAsync());
                        return count > 0;
                    }
                    catch (Exception ex)
                    {
                        Utils.LogError($"Error in ColumnExistsAsync: {ex.Message}");
                        return false;
                    }
                }
            }
        }

        private async Task AddColumnToTableAsync(IDbConnection connection, string tableName, string columnDefinition)
        {
            DbCommand? command;
            string query;
            switch (dbType)
            {
                case DatabaseType.MySQL:
                    query = $"ALTER TABLE {tableName} ADD COLUMN {columnDefinition}";
                    command = new MySqlCommand(query, (MySqlConnection)connection);
                    break;
                case DatabaseType.PostgreSQL:
                    query = $@"ALTER TABLE ""{tableName}"" ADD ""{columnDefinition}""";
                    command = new NpgsqlCommand(query, (NpgsqlConnection)connection);
                    break;
                case DatabaseType.SQLite:
                    query = $"ALTER TABLE {tableName} ADD COLUMN {columnDefinition}";
                    command = new SqliteCommand(query, (SqliteConnection)connection);
                    break;
                default:
                    command = null;
                    break;
            }

            using (command)
            {
                try
                {
                    await command!.ExecuteNonQueryAsync();
                }
                catch (Exception ex)
                {
                    Utils.LogError($"Error in AddColumnToTableAsync: {ex.Message}");
                }
            }
        }

        private async Task CreatePlayerStatsTableAsync(IDbConnection connection)
        {
            DbCommand? command;
            string query;
            switch (dbType)
            {
                case DatabaseType.MySQL:
                    query = $@"CREATE TABLE IF NOT EXISTS {PlayerStatsTable} (
                                            SteamID VARCHAR(20),
                                            PlayerName VARCHAR(32) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci,
                                            TimesConnected INT,
                                            LastConnected INT,
                                            GlobalPoints INT,
                                            HideTimerHud BOOL,
                                            HideKeys BOOL,
                                            SoundsEnabled BOOL,
                                            PlayerFov INT,
                                            IsVip BOOL,
                                            BigGifID VARCHAR(16),
                                            PRIMARY KEY (SteamID)
                                        )";
                    command = new MySqlCommand(query, (MySqlConnection)connection);
                    break;
                case DatabaseType.PostgreSQL:
                    query = $@"CREATE TABLE IF NOT EXISTS ""{PlayerStatsTable}"" (
                                            ""SteamID"" VARCHAR(20) UNIQUE,
                                            ""PlayerName"" VARCHAR(32),
                                            ""TimesConnected"" INT,
                                            ""LastConnected"" INT,
                                            ""GlobalPoints"" INT,
                                            ""HideTimerHud"" BOOL,
                                            ""HideKeys"" BOOL,
                                            ""SoundsEnabled"" BOOL,
                                            ""PlayerFov"" INT,
                                            ""IsVip"" BOOL,
                                            ""BigGifID"" VARCHAR(16),
                                            PRIMARY KEY (""SteamID"")
                                        )";
                    command = new NpgsqlCommand(query, (NpgsqlConnection)connection);
                    break;
                case DatabaseType.SQLite:
                    query = $@"CREATE TABLE IF NOT EXISTS {PlayerStatsTable} (
                                            SteamID TEXT PRIMARY KEY,
                                            PlayerName TEXT,
                                            TimesConnected INTEGER,
                                            LastConnected INTEGER,
                                            GlobalPoints INTEGER,
                                            HideTimerHud INTEGER,
                                            HideKeys INTEGER,
                                            SoundsEnabled INTEGER,
                                            PlayerFov INTEGER,
                                            IsVip INTEGER,
                                            BigGifID TEXT
                                        )";
                    command = new SqliteCommand(query, (SqliteConnection)connection);
                    break;
                default:
                    command = null;
                    break;
            }

            using (command)
            {
                try
                {
                    await command!.ExecuteNonQueryAsync();
                }
                catch (Exception ex)
                {
                    Utils.LogError($"Error in CreatePlayerStatsTableAsync: {ex.Message}");
                }
            }
        }

        private async Task CreatePlayerStageTimesTableAsync(IDbConnection connection)
        {
            DbCommand? command;
            string query;
            switch (dbType)
            {
                case DatabaseType.MySQL:
                    query = @"CREATE TABLE IF NOT EXISTS PlayerStageTimes (
                        MapName VARCHAR(255),
                        SteamID VARCHAR(20),
                        PlayerName VARCHAR(32) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci,
                        Stage INT,
                        TimerTicks INT,
                        FormattedTime VARCHAR(255),
                        Velocity VARCHAR(255),
                        PRIMARY KEY (MapName, SteamID, Stage)
                    )";
                    command = new MySqlCommand(query, (MySqlConnection)connection);
                    break;
                case DatabaseType.PostgreSQL:
                    query = @"CREATE TABLE IF NOT EXISTS ""PlayerStageTimes"" (
                        ""MapName"" VARCHAR(255),
                        ""SteamID"" VARCHAR(20),
                        ""PlayerName"" VARCHAR(32),
                        ""Stage"" INT,
                        ""TimerTicks"" INT,
                        ""FormattedTime"" VARCHAR(255),
                        ""Velocity"" VARCHAR(255),
                        PRIMARY KEY (""MapName"", ""SteamID"", ""Stage"")
                    )";
                    command = new NpgsqlCommand(query, (NpgsqlConnection)connection);
                    break;
                case DatabaseType.SQLite:
                    query = @"CREATE TABLE IF NOT EXISTS PlayerStageTimes (
                        MapName TEXT,
                        SteamID TEXT,
                        PlayerName TEXT,
                        Stage INTEGER,
                        TimerTicks INTEGER,
                        FormattedTime TEXT,
                        Velocity TEXT,
                        PRIMARY KEY (MapName, SteamID, Stage)
                    )";
                    command = new SqliteCommand(query, (SqliteConnection)connection);
                    break;
                default:
                    command = null;
                    break;
            }

            using (command)
            {
                try
                {
                    await command!.ExecuteNonQueryAsync();
                }
                catch (Exception ex)
                {
                    Utils.LogError($"Error in CreatePlayerStageTimesTableAsync: {ex.Message}");
                }
            }
        }

        public async Task SavePlayerTimeToDatabase(CCSPlayerController? player, int timerTicks, string steamId,
            string playerName, int slot, int bonusX = 0, int style = 0, string mode = "")
        {
            Utils.LogDebug(
                $"Trying to save player {(bonusX != 0 ? $"bonus {bonusX} time" : "time")} to database for {playerName} {timerTicks}");
            try
            {
                if (!IsAllowedPlayer(player)) return;
                //if ((bonusX == 0 && !playerTimers[slot].IsTimerRunning) || (bonusX != 0 && !playerTimers[slot].IsBonusTimerRunning)) return;
                string currentMapNamee = bonusX == 0 ? currentMapName! : $"{currentMapName}_bonus{bonusX}";

                int timeNowUnix = (int)DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                // get player columns
                int dBtimesFinished = 0;
                int dBlastFinished = 0;
                int dBunixStamp = 0;
                int dBtimerTicks = 0;
                string dBFormattedTime;

                // store new value separatley
                int new_dBtimerTicks = 0;
                int playerPoints = 0;
                bool beatPB = false;

                using (var connection = await OpenConnectionAsync())
                {
                    await CreatePlayerRecordsTableAsync(connection);

                    string formattedTime = Utils.FormatTime(timerTicks);
                    string? selectQuery;
                    DbCommand? selectCommand;
                    switch (dbType)
                    {
                        case DatabaseType.MySQL:
                            selectQuery =
                                @"SELECT TimesFinished, LastFinished, FormattedTime, TimerTicks, UnixStamp FROM PlayerRecords WHERE MapName = @MapName AND SteamID = @SteamID AND Style = @Style AND Mode = @Mode";
                            selectCommand = new MySqlCommand(selectQuery, (MySqlConnection)connection);
                            break;
                        case DatabaseType.PostgreSQL:
                            selectQuery =
                                @"SELECT ""TimesFinished"", ""LastFinished"", ""FormattedTime"", ""TimerTicks"", ""UnixStamp"" FROM ""PlayerRecords"" WHERE ""MapName"" = @MapName AND ""SteamID"" = @SteamID AND ""Style"" = @Style AND ""Mode"" = @Mode";
                            selectCommand = new NpgsqlCommand(selectQuery, (NpgsqlConnection)connection);
                            break;
                        case DatabaseType.SQLite:
                            selectQuery =
                                @"SELECT TimesFinished, LastFinished, FormattedTime, TimerTicks, UnixStamp FROM PlayerRecords WHERE MapName = @MapName AND SteamID = @SteamID AND Style = @Style AND Mode = @Mode";
                            selectCommand = new SqliteCommand(selectQuery, (SqliteConnection)connection);
                            break;
                        default:
                            selectQuery = null;
                            selectCommand = null;
                            break;
                    }

                    // Check if the record already exists or has a higher timer value
                    selectCommand!.AddParameterWithValue("@MapName", currentMapNamee);
                    selectCommand!.AddParameterWithValue("@SteamID", steamId);
                    selectCommand!.AddParameterWithValue("@Style", style);
                    selectCommand!.AddParameterWithValue("@Mode", mode);

                    var row = await selectCommand!.ExecuteReaderAsync();

                    if (row.Read())
                    {
                        // get player columns
                        dBtimesFinished = row.GetInt32("TimesFinished");
                        dBlastFinished = row.GetInt32("LastFinished");
                        dBunixStamp = row.GetInt32("UnixStamp");
                        dBtimerTicks = row.GetInt32("TimerTicks");
                        dBFormattedTime = row.GetString("FormattedTime");

                        // Modify the stats
                        dBtimesFinished++;
                        dBlastFinished = timeNowUnix;
                        if (timerTicks < dBtimerTicks)
                        {
                            new_dBtimerTicks = timerTicks;
                            dBunixStamp = timeNowUnix;
                            dBFormattedTime = formattedTime;
                            playerPoints = timerTicks;
                            beatPB = true;
                        }
                        else
                        {
                            new_dBtimerTicks = dBtimerTicks;
                            beatPB = false;
                        }

                        await row.CloseAsync();
                        // Update or insert the record
                        string? upsertQuery;
                        DbCommand? upsertCommand;
                        switch (dbType)
                        {
                            case DatabaseType.MySQL:
                                upsertQuery = @"
                                                    INSERT INTO PlayerRecords 
                                                    (MapName, SteamID, PlayerName, TimerTicks, LastFinished, TimesFinished, FormattedTime, UnixStamp, Style, Mode)
                                                    VALUES 
                                                    (@MapName, @SteamID, @PlayerName, @TimerTicks, @LastFinished, @TimesFinished, @FormattedTime, @UnixStamp, @Style, @Mode)
                                                    ON DUPLICATE KEY UPDATE
                                                    MapName = VALUES(MapName),
                                                    PlayerName = VALUES(PlayerName),
                                                    TimerTicks = VALUES(TimerTicks),
                                                    LastFinished = VALUES(LastFinished),
                                                    TimesFinished = VALUES(TimesFinished),
                                                    FormattedTime = VALUES(FormattedTime),
                                                    UnixStamp = VALUES(UnixStamp),
                                                    Style = VALUES(Style),
                                                    Mode = VALUES(Mode);";
                                upsertCommand = new MySqlCommand(upsertQuery, (MySqlConnection)connection);
                                break;
                            case DatabaseType.PostgreSQL:
                                upsertQuery = @"
                                                    INSERT INTO ""PlayerRecords"" 
                                                    (""MapName"", ""SteamID"", ""PlayerName"", ""TimerTicks"", ""LastFinished"", ""TimesFinished"", ""FormattedTime"", ""UnixStamp"", ""Style"", ""Mode"")
                                                    VALUES 
                                                    (@MapName, @SteamID, @PlayerName, @TimerTicks, @LastFinished, @TimesFinished, @FormattedTime, @UnixStamp, @Style, @Mode)
                                                    ON CONFLICT (""MapName"", ""SteamID"", ""Style"", ""Mode"")
                                                    DO UPDATE SET
                                                    ""MapName"" = EXCLUDED.""MapName"",
                                                    ""PlayerName"" = EXCLUDED.""PlayerName"",
                                                    ""TimerTicks"" = EXCLUDED.""TimerTicks"",
                                                    ""LastFinished"" = EXCLUDED.""LastFinished"",
                                                    ""TimesFinished"" = EXCLUDED.""TimesFinished"",
                                                    ""FormattedTime"" = EXCLUDED.""FormattedTime"",
                                                    ""UnixStamp"" = EXCLUDED.""UnixStamp"",
                                                    ""Style"" = EXCLUDED.""Style"",
                                                    ""Mode"" = EXCLUDED.""Mode"";
                                                    ";
                                upsertCommand = new NpgsqlCommand(upsertQuery, (NpgsqlConnection)connection);
                                break;
                            case DatabaseType.SQLite:
                                upsertQuery = @"
                                                    INSERT INTO PlayerRecords 
                                                    (MapName, SteamID, PlayerName, TimerTicks, LastFinished, TimesFinished, FormattedTime, UnixStamp, Style, Mode)
                                                    VALUES 
                                                    (@MapName, @SteamID, @PlayerName, @TimerTicks, @LastFinished, @TimesFinished, @FormattedTime, @UnixStamp, @Style, @Mode)
                                                    ON CONFLICT (MapName, SteamID, Style, Mode)
                                                    DO UPDATE SET
                                                    MapName = excluded.MapName,
                                                    PlayerName = excluded.PlayerName,
                                                    TimerTicks = excluded.TimerTicks,
                                                    LastFinished = excluded.LastFinished,
                                                    TimesFinished = excluded.TimesFinished,
                                                    FormattedTime = excluded.FormattedTime,
                                                    UnixStamp = excluded.UnixStamp,
                                                    Style = excluded.Style,
                                                    Mode = excluded.Mode;
                                                    ";
                                upsertCommand = new SqliteCommand(upsertQuery, (SqliteConnection)connection);
                                break;
                            default:
                                upsertQuery = null;
                                upsertCommand = null;
                                break;
                        }

                        using (upsertCommand)
                        {
                            upsertCommand!.AddParameterWithValue("@MapName", currentMapNamee);
                            upsertCommand!.AddParameterWithValue("@PlayerName", playerName);
                            upsertCommand!.AddParameterWithValue("@TimesFinished", dBtimesFinished);
                            upsertCommand!.AddParameterWithValue("@LastFinished", dBlastFinished);
                            upsertCommand!.AddParameterWithValue("@TimerTicks", new_dBtimerTicks);
                            upsertCommand!.AddParameterWithValue("@FormattedTime", dBFormattedTime);
                            upsertCommand!.AddParameterWithValue("@UnixStamp", dBunixStamp);
                            upsertCommand!.AddParameterWithValue("@SteamID", steamId);
                            upsertCommand!.AddParameterWithValue("@Style", style);
                            upsertCommand!.AddParameterWithValue("@Mode", mode);

                            var prevSRData = await GetMapRecordSteamIDFromDatabase(bonusX, 0, style, mode);
                            int prevSRTimerTicks = prevSRData.Item3;
                            
                            await upsertCommand!.ExecuteNonQueryAsync();
                            Server.NextFrame(() =>
                                Utils.LogDebug(
                                    $"Saved player {(bonusX != 0 ? $"bonus {bonusX} time" : "time")} to database for {playerName} {timerTicks} {DateTimeOffset.UtcNow.ToUnixTimeSeconds()}"));
                            
                            if (IsAllowedPlayer(player))
                            {
                                Server.NextFrame(() => _ = Task.Run(async () => await PrintMapTimeToChat(player!,
                                    steamId, playerName, dBtimerTicks, timerTicks, bonusX, dBtimesFinished, style,
                                    prevSRTimerTicks, mode)));
                                await RankCommandHandler(player, steamId, slot, playerName, true, style, mode);
                            }

                            if (globalRanksEnabled)
                            {
                                var (oldPts, newPts) = await SavePlayerPoints(steamId, playerName, slot, timerTicks, dBtimerTicks, mode,
                                    beatPB,
                                    bonusX, style, dBtimesFinished);
                                
                                Server.NextFrame(() => Utils.PrintToChat(player, Localizer["gained_points",
                                    playerName, Convert.ToInt32(newPts - oldPts),
                                    newPts]));
                            }

                            if (enableReplays)
                            {
                                if (onlySRReplay && (prevSRTimerTicks == 0 || prevSRTimerTicks > timerTicks))
                                {
                                    if(useBinaryReplays)
                                        _ = Task.Run(async () => await DumpReplayToBinary(player!, steamId, slot, bonusX, playerTimers[slot].currentStyle, playerTimers[slot].Mode));
                                    else
                                        _ = Task.Run(async () => await DumpReplayToJson(player!, steamId, slot, bonusX, playerTimers[slot].currentStyle, playerTimers[slot].Mode));
                                }
                                else if (!onlySRReplay)
                                {
                                    if(useBinaryReplays)
                                        _ = Task.Run(async () => await DumpReplayToBinary(player!, steamId, slot, bonusX, playerTimers[slot].currentStyle, playerTimers[slot].Mode));
                                    else
                                        _ = Task.Run(async () => await DumpReplayToJson(player!, steamId, slot, bonusX, playerTimers[slot].currentStyle, playerTimers[slot].Mode));
                                }
                                
                            }

                            Server.NextFrame(async () =>
                            {
                                 if (globalDisabled)
                                    return;
                                 
                                 DateTimeOffset timeCreated = DateTimeOffset.UtcNow;
                                 int playerId = playerCache.PlayerID[slot];

                                 if (playerTimers[slot].Mode == "Custom")
                                    return;

                                 //first lets see if the new record beats global pb
                                 var beatGlobalPB = false;
                                 var prevPBTime = await GetPreviousPlayerRecordFromGlobal(playerId, playerTimers[slot].Mode,
                                    GetNamedStyle(style), bonusX);
                                 if (prevPBTime > Utils.TicksToDecimal(timerTicks) || prevPBTime == 0)
                                    beatGlobalPB = true;

                                 var record_payload = new GlobalRecord
                                 {
                                     player_id = playerId,
                                     server_id = serverCache.ServerID,
                                     map_id = mapCache.MapID,
                                     bonus = bonusX,
                                     mode = playerTimers[slot].Mode,
                                     style = GetNamedStyle(style),
                                     time = Utils.TicksToDecimal(timerTicks),
                                     created_on = timeCreated
                                 };

                                _ = Task.Run(async () =>
                                {
                                    await SubmitRecordAsync(record_payload);
                                    int recordID = await GetRecordIDAsync(playerId, timeCreated);
                                    if (beatGlobalPB && useBinaryReplays && style == 0)
                                    {
                                        var replay_payload = new ReplayData
                                        {
                                            record_id = recordID,
                                            map_id = mapCache.MapID,
                                            bonus = bonusX,
                                            mode = mode,
                                            hash = GetHash(),
                                            replay_data = GetReplayBinary(player!, player!.Slot)
                                        };

                                        await SubmitReplayAsync(replay_payload);
                                    }
                                }).ConfigureAwait(false);
                            });
                        }
                    }
                    else
                    {
                        Server.NextFrame(() => Utils.LogDebug($"No player record yet"));
                        await row.CloseAsync();

                        string? upsertQuery;
                        DbCommand? upsertCommand;
                        switch (dbType)
                        {
                            case DatabaseType.MySQL:
                                upsertQuery =
                                    @"REPLACE INTO PlayerRecords (MapName, SteamID, PlayerName, TimerTicks, LastFinished, TimesFinished, FormattedTime, UnixStamp, Style, Mode) VALUES (@MapName, @SteamID, @PlayerName, @TimerTicks, @LastFinished, @TimesFinished, @FormattedTime, @UnixStamp, @Style, @Mode)";
                                upsertCommand = new MySqlCommand(upsertQuery, (MySqlConnection)connection);
                                break;
                            case DatabaseType.PostgreSQL:
                                upsertQuery =
                                    @"INSERT INTO ""PlayerRecords"" (""MapName"", ""SteamID"", ""PlayerName"", ""TimerTicks"", ""LastFinished"", ""TimesFinished"", ""FormattedTime"", ""UnixStamp"", ""Style"", ""Mode"") VALUES (@MapName, @SteamID, @PlayerName, @TimerTicks, @LastFinished, @TimesFinished, @FormattedTime, @UnixStamp, @Style, @Mode)";
                                upsertCommand = new NpgsqlCommand(upsertQuery, (NpgsqlConnection)connection);
                                break;
                            case DatabaseType.SQLite:
                                upsertQuery =
                                    @"REPLACE INTO PlayerRecords (MapName, SteamID, PlayerName, TimerTicks, LastFinished, TimesFinished, FormattedTime, UnixStamp, Style, Mode) VALUES (@MapName, @SteamID, @PlayerName, @TimerTicks, @LastFinished, @TimesFinished, @FormattedTime, @UnixStamp, @Style, @Mode)";
                                upsertCommand = new SqliteCommand(upsertQuery, (SqliteConnection)connection);
                                break;
                            default:
                                upsertQuery = null;
                                upsertCommand = null;
                                break;
                        }

                        using (upsertCommand)
                        {
                            upsertCommand!.AddParameterWithValue("@MapName", currentMapNamee);
                            upsertCommand!.AddParameterWithValue("@PlayerName", playerName);
                            upsertCommand!.AddParameterWithValue("@TimesFinished", 1);
                            upsertCommand!.AddParameterWithValue("@LastFinished", timeNowUnix);
                            upsertCommand!.AddParameterWithValue("@TimerTicks", timerTicks);
                            upsertCommand!.AddParameterWithValue("@FormattedTime", formattedTime);
                            upsertCommand!.AddParameterWithValue("@UnixStamp", timeNowUnix);
                            upsertCommand!.AddParameterWithValue("@SteamID", steamId);
                            upsertCommand!.AddParameterWithValue("@Style", style);
                            upsertCommand!.AddParameterWithValue("@Mode", mode);
                            
                            var prevSRData = await GetMapRecordSteamIDFromDatabase(bonusX, 0, style, mode);
                            int prevSRTimerTicks = prevSRData.Item3;
                            
                            await upsertCommand!.ExecuteNonQueryAsync();

                            if (globalRanksEnabled)
                            {
                                var (oldPts, newPts) = await SavePlayerPoints(steamId, playerName, slot, timerTicks, dBtimerTicks, mode,
                                    beatPB,
                                    bonusX, style, dBtimesFinished);
                                
                                Server.NextFrame(() => Utils.PrintToChat(player, Localizer["gained_points",
                                    playerName, Convert.ToInt32(newPts - oldPts),
                                    newPts]));
                            }
                            
                            Server.NextFrame(() =>
                                Utils.LogDebug(
                                    $"Saved player {(bonusX != 0 ? $"bonus {bonusX} time" : "time")} to database for {playerName} {timerTicks} {DateTimeOffset.UtcNow.ToUnixTimeSeconds()}"));

                            if (IsAllowedPlayer(player))
                            {
                                await RankCommandHandler(player, steamId, slot, playerName, true, style, mode);
                                Server.NextFrame(() => _ = Task.Run(async () => await PrintMapTimeToChat(player!,
                                    steamId, playerName, dBtimerTicks, timerTicks, bonusX, 1, style, prevSRTimerTicks, mode)));
                            }

                            if (enableReplays)
                            {
                                if (onlySRReplay && (prevSRTimerTicks == 0 || prevSRTimerTicks > timerTicks))
                                {
                                    if(useBinaryReplays)
                                        _ = Task.Run(async () => await DumpReplayToBinary(player!, steamId, slot, bonusX, playerTimers[slot].currentStyle, playerTimers[slot].Mode));
                                    else
                                        _ = Task.Run(async () => await DumpReplayToJson(player!, steamId, slot, bonusX, playerTimers[slot].currentStyle, playerTimers[slot].Mode));
                                }
                                else if (!onlySRReplay)
                                {
                                    if(useBinaryReplays)
                                        _ = Task.Run(async () => await DumpReplayToBinary(player!, steamId, slot, bonusX, playerTimers[slot].currentStyle, playerTimers[slot].Mode));
                                    else
                                        _ = Task.Run(async () => await DumpReplayToJson(player!, steamId, slot, bonusX, playerTimers[slot].currentStyle, playerTimers[slot].Mode));
                                }
                                
                            }

                            Server.NextFrame(async () =>
                            {
                                if (globalDisabled)
                                    return;

                                DateTimeOffset timeCreated = DateTimeOffset.UtcNow;
                                int playerId = playerCache.PlayerID[slot];

                                if (playerTimers[slot].Mode == "Custom")
                                    return;

                                //first lets see if the new record beats global pb
                                var beatGlobalPB = false;
                                var prevPBTime = await GetPreviousPlayerRecordFromGlobal(playerId, playerTimers[slot].Mode,
                                    GetNamedStyle(style), bonusX);
                                if (prevPBTime > Utils.TicksToDecimal(timerTicks) || prevPBTime == 0)
                                    beatGlobalPB = true;

                                var record_payload = new GlobalRecord
                                {
                                    player_id = playerId,
                                    server_id = serverCache.ServerID,
                                    map_id = mapCache.MapID,
                                    bonus = bonusX,
                                    mode = playerTimers[slot].Mode,
                                    style = GetNamedStyle(style),
                                    time = Utils.TicksToDecimal(timerTicks),
                                    created_on = timeCreated
                                };

                                _ = Task.Run(async () =>
                                {
                                    await SubmitRecordAsync(record_payload);
                                    int recordID = await GetRecordIDAsync(playerId, timeCreated);
                                    if (beatGlobalPB && useBinaryReplays && style == 0)
                                    {
                                        var replay_payload = new ReplayData
                                        {
                                            record_id = recordID,
                                            map_id = mapCache.MapID,
                                            bonus = bonusX,
                                            mode = mode,
                                            hash = GetHash(),
                                            replay_data = GetReplayBinary(player!, player!.Slot)
                                        };

                                        await SubmitReplayAsync(replay_payload);
                                    }
                                }).ConfigureAwait(false);
                            });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Server.NextFrame(() =>
                    Utils.LogError(
                        $"Error saving player {(bonusX != 0 ? $"bonus {bonusX} time" : "time")} to database: {ex.Message}"));
            }
        }

        public async Task GetPlayerStats(CCSPlayerController? player, string steamId, string playerName, int slot,
            bool fromConnect)
        {
            Utils.LogDebug($"Trying to get player stats from database for {playerName}");
            try
            {
                if (player == null || !player.IsValid || player.IsBot) return;
                if (!(connectedPlayers.ContainsKey(slot) && playerTimers.ContainsKey(slot))) return;

                int timeNowUnix = (int)DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                // get player columns
                int timesConnected = 0;
                int lastConnected = 0;
                bool hideTimerHud = false;
                bool hideKeys = false;
                bool soundsEnabled = true;
                int playerFov = 0;
                bool isVip = false;
                string bigGif = "x";
                int playerPoints = 0;
                bool hideWeapon = false;
                bool hidePlayers = false;
                bool hideChatSpeed = false;
                string mode = "";

                using (var connection = await OpenConnectionAsync())
                {
                    await CreatePlayerStatsTableAsync(connection);

                    string? selectQuery;
                    DbCommand? selectCommand;
                    switch (dbType)
                    {
                        case DatabaseType.MySQL:
                            selectQuery =
                                $@"SELECT PlayerName, TimesConnected, LastConnected, HideTimerHud, HideKeys, SoundsEnabled, PlayerFov, IsVip, BigGifID, GlobalPoints, HideWeapon, HidePlayers, Mode, HideChatSpeed FROM {PlayerStatsTable} WHERE SteamID = @SteamID";
                            selectCommand = new MySqlCommand(selectQuery, (MySqlConnection)connection);
                            break;
                        case DatabaseType.PostgreSQL:
                            selectQuery =
                                $@"SELECT ""PlayerName"", ""TimesConnected"", ""LastConnected"", ""HideTimerHud"", ""HideKeys"", ""SoundsEnabled"", ""PlayerFov"", ""IsVip"", ""BigGifID"", ""GlobalPoints"", ""HideWeapon"", ""HidePlayers"", ""Mode"", ""HideChatSpeed"" FROM ""{PlayerStatsTable}"" WHERE ""SteamID"" = @SteamID";
                            selectCommand = new NpgsqlCommand(selectQuery, (NpgsqlConnection)connection);
                            break;
                        case DatabaseType.SQLite:
                            selectQuery =
                                $@"SELECT PlayerName, TimesConnected, LastConnected, HideTimerHud, HideKeys, SoundsEnabled, PlayerFov, IsVip, BigGifID, GlobalPoints, HideWeapon, HidePlayers, Mode, HideChatSpeed FROM {PlayerStatsTable} WHERE SteamID = @SteamID";
                            selectCommand = new SqliteCommand(selectQuery, (SqliteConnection)connection);
                            break;
                        default:
                            selectQuery = null;
                            selectCommand = null;
                            break;
                    }

                    using (selectCommand)
                    {
                        selectCommand!.AddParameterWithValue("@SteamID", steamId);

                        var row = await selectCommand!.ExecuteReaderAsync();

                        if (row.Read())
                        {
                            // get player columns
                            switch (dbType)
                            {
                                case DatabaseType.MySQL:
                                case DatabaseType.PostgreSQL:
                                    timesConnected = row.GetInt32("TimesConnected");
                                    hideTimerHud = row.GetBoolean("HideTimerHud");
                                    hideKeys = row.GetBoolean("HideKeys");
                                    hideWeapon = row.GetBoolean("HideWeapon");
                                    hidePlayers = row.GetBoolean("HidePlayers");
                                    hideChatSpeed = row.GetBoolean("HideChatSpeed");
                                    soundsEnabled = row.GetBoolean("SoundsEnabled");
                                    playerFov = row.GetInt32("PlayerFov");
                                    isVip = row.GetBoolean("IsVip");
                                    bigGif = row.GetString("BigGifID");
                                    mode = row.GetString("Mode");
                                    playerPoints = row.GetInt32("GlobalPoints");
                                    break;
                                case DatabaseType.SQLite:
                                    timesConnected = row.GetInt32("TimesConnected");
                                    hideTimerHud = row.GetSQLiteBool("HideTimerHud");
                                    hideKeys = row.GetSQLiteBool("HideKeys");
                                    hideWeapon = row.GetSQLiteBool("HideWeapon");
                                    hidePlayers = row.GetSQLiteBool("HidePlayers");
                                    hideChatSpeed = row.GetSQLiteBool("HideChatSpeed");
                                    soundsEnabled = row.GetSQLiteBool("SoundsEnabled");
                                    playerFov = row.GetInt32("PlayerFov");
                                    isVip = row.GetSQLiteBool("IsVip");
                                    bigGif = row.GetString("BigGifID");
                                    mode = row.GetString("Mode");
                                    playerPoints = row.GetInt32("GlobalPoints");
                                    break;
                            }

                            // Modify the stats
                            timesConnected++;
                            lastConnected = timeNowUnix;
                            Server.NextFrame(() =>
                            {
                                if (string.IsNullOrEmpty(mode))
                                    mode = GetModeName(defaultMode);
                                
                                if (playerTimers.TryGetValue(slot, out PlayerTimerInfo? value))
                                {
                                    value.HideTimerHud = hideTimerHud;
                                    value.HideKeys = hideKeys;
                                    value.HideWeapon = hideWeapon;
                                    value.HideChatSpeed = hideChatSpeed;
                                    value.HidePlayers = hidePlayers;
                                    value.SoundsEnabled = soundsEnabled;
                                    value.PlayerFov = playerFov;
                                    value.IsVip = isVip;
                                    value.VipBigGif = bigGif;
                                    value.Mode = mode;
                                    value.TimesConnected = timesConnected;
                                }
                                else
                                {
                                    Utils.LogError(
                                        $"Error getting player stats from database for {playerName}: player was not on the server anymore");
                                    return;
                                }
                            });

                            await row.CloseAsync();
                            // Update or insert the record

                            string? upsertQuery;
                            DbCommand? upsertCommand;
                            switch (dbType)
                            {
                                case DatabaseType.MySQL:
                                    upsertQuery =
                                        $@"REPLACE INTO {PlayerStatsTable} (PlayerName, SteamID, TimesConnected, LastConnected, HideTimerHud, HideKeys, SoundsEnabled, PlayerFov, IsVip, BigGifID, GlobalPoints, HideWeapon, HidePlayers, Mode, HideChatSpeed) 
                                                        VALUES (@PlayerName, @SteamID, @TimesConnected, @LastConnected, @HideTimerHud, @HideKeys, @SoundsEnabled, @PlayerFov, @IsVip, @BigGifID, @GlobalPoints, @HideWeapon, @HidePlayers, @Mode, @HideChatSpeed)";
                                    upsertCommand = new MySqlCommand(upsertQuery, (MySqlConnection)connection);
                                    break;
                                case DatabaseType.PostgreSQL:
                                    upsertQuery = $@"
                                                    INSERT INTO ""{PlayerStatsTable}"" 
                                                    (""PlayerName"", ""SteamID"", ""TimesConnected"", ""LastConnected"", ""HideTimerHud"", ""HideKeys"", ""SoundsEnabled"", ""PlayerFov"", ""IsVip"", ""BigGifID"", ""GlobalPoints"", ""HideWeapon"", ""HidePlayers"", ""Mode"", ""HideChatSpeed"")
                                                    VALUES 
                                                    (@PlayerName, @SteamID, @TimesConnected, @LastConnected, @HideTimerHud, @HideKeys, @SoundsEnabled, @PlayerFov, @IsVip, @BigGifID, @GlobalPoints, @HideWeapon, @HidePlayers, @Mode, @HideChatSpeed)
                                                    ON CONFLICT (""SteamID"")
                                                    DO UPDATE SET
                                                    ""PlayerName"" = EXCLUDED.""PlayerName"",
                                                    ""TimesConnected"" = EXCLUDED.""TimesConnected"",
                                                    ""LastConnected"" = EXCLUDED.""LastConnected"",
                                                    ""HideTimerHud"" = EXCLUDED.""HideTimerHud"",
                                                    ""HideKeys"" = EXCLUDED.""HideKeys"",
                                                    ""SoundsEnabled"" = EXCLUDED.""SoundsEnabled"",
                                                    ""PlayerFov"" = EXCLUDED.""PlayerFov"",
                                                    ""IsVip"" = EXCLUDED.""IsVip"",
                                                    ""BigGifID"" = EXCLUDED.""BigGifID"",
                                                    ""GlobalPoints"" = EXCLUDED.""GlobalPoints"",
                                                    ""HideWeapon"" = EXCLUDED.""HideWeapon"",
                                                    ""HidePlayers"" = EXCLUDED.""HidePlayers"",
                                                    ""Mode"" = EXCLUDED.""Mode"";
                                                    ";
                                    upsertCommand = new NpgsqlCommand(upsertQuery, (NpgsqlConnection)connection);
                                    break;
                                case DatabaseType.SQLite:
                                    upsertQuery =
                                        $@"REPLACE INTO {PlayerStatsTable} (PlayerName, SteamID, TimesConnected, LastConnected, HideTimerHud, HideKeys, SoundsEnabled, PlayerFov, IsVip, BigGifID, GlobalPoints, HideWeapon, HidePlayers, Mode, HideChatSpeed) 
                                                        VALUES (@PlayerName, @SteamID, @TimesConnected, @LastConnected, @HideTimerHud, @HideKeys, @SoundsEnabled, @PlayerFov, @IsVip, @BigGifID, @GlobalPoints, @HideWeapon, @HidePlayers, @Mode, @HideChatSpeed)";
                                    upsertCommand = new SqliteCommand(upsertQuery, (SqliteConnection)connection);
                                    break;
                                default:
                                    upsertQuery = null;
                                    upsertCommand = null;
                                    break;
                            }

                            using (upsertCommand)
                            {
                                upsertCommand!.AddParameterWithValue("@PlayerName", playerName);
                                upsertCommand!.AddParameterWithValue("@SteamID", steamId);
                                upsertCommand!.AddParameterWithValue("@TimesConnected", timesConnected);
                                upsertCommand!.AddParameterWithValue("@LastConnected", lastConnected);
                                upsertCommand!.AddParameterWithValue("@HideTimerHud", hideTimerHud);
                                upsertCommand!.AddParameterWithValue("@HideKeys", hideKeys);
                                upsertCommand!.AddParameterWithValue("@SoundsEnabled", soundsEnabled);
                                upsertCommand!.AddParameterWithValue("@PlayerFov", playerFov);
                                upsertCommand!.AddParameterWithValue("@IsVip", isVip);
                                upsertCommand!.AddParameterWithValue("@BigGifID", bigGif);
                                upsertCommand!.AddParameterWithValue("@GlobalPoints", playerPoints);
                                upsertCommand!.AddParameterWithValue("@HideWeapon", hideWeapon);
                                upsertCommand!.AddParameterWithValue("@HidePlayers", hidePlayers);
                                upsertCommand!.AddParameterWithValue("@HideChatSpeed", hideChatSpeed);
                                upsertCommand!.AddParameterWithValue("@Mode", mode);

                                await upsertCommand!.ExecuteNonQueryAsync();
                                Server.NextFrame(() =>
                                    Utils.LogDebug($"Got player stats from database for {playerName}"));
                                if (connectMsgEnabled)
                                    Server.NextFrame(() =>
                                        Utils.PrintToChatAll(
                                            $"{Localizer["connected_message", playerName, Utils.FormatOrdinal(timesConnected)]}"));
                            }
                        }
                        else
                        {
                            Server.NextFrame(() => Utils.LogDebug($"No player stats yet"));
                            await row.CloseAsync();

                            string? upsertQuery;
                            DbCommand? upsertCommand;
                            switch (dbType)
                            {
                                case DatabaseType.MySQL:
                                    upsertQuery =
                                        $@"REPLACE INTO {PlayerStatsTable} (PlayerName, SteamID, TimesConnected, LastConnected, HideTimerHud, HideKeys, SoundsEnabled, PlayerFov, IsVip, BigGifID, GlobalPoints, HideWeapon, HidePlayers, Mode, HideChatSpeed) VALUES (@PlayerName, @SteamID, @TimesConnected, @LastConnected, @HideTimerHud, @HideKeys, @SoundsEnabled, @PlayerFov, @IsVip, @BigGifID, @GlobalPoints, @HideWeapon, @HidePlayers, @Mode, @HideChatSpeed)";
                                    upsertCommand = new MySqlCommand(upsertQuery, (MySqlConnection)connection);
                                    break;
                                case DatabaseType.PostgreSQL:
                                    upsertQuery =
                                        $@"INSERT INTO ""{PlayerStatsTable}"" (""PlayerName"", ""SteamID"", ""TimesConnected"", ""LastConnected"", ""HideTimerHud"", ""HideKeys"", ""SoundsEnabled"", ""PlayerFov"", ""IsVip"", ""BigGifID"", ""GlobalPoints"", ""HideWeapon"", ""HidePlayers"", ""Mode"", ""HideChatSpeed"") VALUES (@PlayerName, @SteamID, @TimesConnected, @LastConnected, @HideTimerHud, @HideKeys, @SoundsEnabled, @PlayerFov, @IsVip, @BigGifID, @GlobalPoints, @HideWeapon, @HidePlayers, @Mode, @HideChatSpeed)";
                                    upsertCommand = new NpgsqlCommand(upsertQuery, (NpgsqlConnection)connection);
                                    break;
                                case DatabaseType.SQLite:
                                    upsertQuery =
                                        $@"REPLACE INTO {PlayerStatsTable} (PlayerName, SteamID, TimesConnected, LastConnected, HideTimerHud, HideKeys, SoundsEnabled, PlayerFov, IsVip, BigGifID, GlobalPoints, HideWeapon, HidePlayers, Mode, HideChatSpeed) VALUES (@PlayerName, @SteamID, @TimesConnected, @LastConnected, @HideTimerHud, @HideKeys, @SoundsEnabled, @PlayerFov, @IsVip, @BigGifID, @GlobalPoints, @HideWeapon, @HidePlayers, @Mode)";
                                    upsertCommand = new SqliteCommand(upsertQuery, (SqliteConnection)connection);
                                    break;
                                default:
                                    upsertQuery = null;
                                    upsertCommand = null;
                                    break;
                            }

                            using (upsertCommand)
                            {
                                upsertCommand!.AddParameterWithValue("@PlayerName", playerName);
                                upsertCommand!.AddParameterWithValue("@SteamID", steamId);
                                upsertCommand!.AddParameterWithValue("@TimesConnected", 1);
                                upsertCommand!.AddParameterWithValue("@LastConnected", timeNowUnix);
                                upsertCommand!.AddParameterWithValue("@HideTimerHud", false);
                                upsertCommand!.AddParameterWithValue("@HideKeys", false);
                                upsertCommand!.AddParameterWithValue("@HideChatSpeed", false);
                                upsertCommand!.AddParameterWithValue("@SoundsEnabled", soundsEnabledByDefault);
                                upsertCommand!.AddParameterWithValue("@PlayerFov", 0);
                                upsertCommand!.AddParameterWithValue("@IsVip", false);
                                upsertCommand!.AddParameterWithValue("@BigGifID", "x");
                                upsertCommand!.AddParameterWithValue("@GlobalPoints", 0);
                                upsertCommand!.AddParameterWithValue("@HideWeapon", false);
                                upsertCommand!.AddParameterWithValue("@HidePlayers", false);
                                upsertCommand!.AddParameterWithValue("@Mode", GetModeName(defaultMode));

                                await upsertCommand!.ExecuteNonQueryAsync();
                                Server.NextFrame(() =>
                                    Utils.LogDebug($"Got player stats from database for {playerName}"));
                                if (connectMsgEnabled)
                                    Server.NextFrame(() =>
                                        Utils.PrintToChatAll($"{Localizer["connected_message_first", playerName]}"));
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Server.NextFrame(() =>
                    Utils.LogError($"Error getting player stats from database for {playerName}: {ex}"));
            }
        }

        public async Task SavePlayerStageTimeToDatabase(CCSPlayerController? player, int timerTicks, int stage,
            string velocity, string steamId, string playerName, int slot, int bonusX = 0, int style = 0)
        {
            Utils.LogDebug(
                $"Trying to save player {(bonusX != 0 ? $"bonus {bonusX} stage {stage} time" : $"stage {stage} time")} to database for {playerName} {timerTicks}");
            try
            {
                if (player == null || !IsAllowedPlayer(player))
                    return;

                //if ((bonusX == 0 && !playerTimers[slot].IsTimerRunning) || (bonusX != 0 && !playerTimers[slot].IsBonusTimerRunning)) return;
                string currentMapNamee = bonusX == 0 ? currentMapName! : $"{currentMapName}_bonus{bonusX}";

                int timeNowUnix = (int)DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                // get player columns
                int dBtimerTicks = 0;
                string dBFormattedTime;

                // store new value separatley
                int new_dBtimerTicks = 0;
                int playerPoints = 0;

                using (var connection = await OpenConnectionAsync())
                {
                    string formattedTime = Utils.FormatTime(timerTicks);
                    string? selectQuery;
                    DbCommand? selectCommand;
                    switch (dbType)
                    {
                        case DatabaseType.MySQL:
                            selectQuery =
                                @"SELECT FormattedTime, TimerTicks FROM PlayerStageTimes WHERE MapName = @MapName AND SteamID = @SteamID AND Stage = @Stage";
                            selectCommand = new MySqlCommand(selectQuery, (MySqlConnection)connection);
                            break;
                        case DatabaseType.PostgreSQL:
                            selectQuery =
                                @"SELECT ""FormattedTime"", ""TimerTicks"" FROM ""PlayerStageTimes"" WHERE ""MapName"" = @MapName AND ""SteamID"" = @SteamID AND ""Stage"" = @Stage";
                            selectCommand = new NpgsqlCommand(selectQuery, (NpgsqlConnection)connection);
                            break;
                        case DatabaseType.SQLite:
                            selectQuery =
                                @"SELECT FormattedTime, TimerTicks FROM PlayerStageTimes WHERE MapName = @MapName AND SteamID = @SteamID AND Stage = @Stage";
                            selectCommand = new SqliteCommand(selectQuery, (SqliteConnection)connection);
                            break;
                        default:
                            selectQuery = null;
                            selectCommand = null;
                            break;
                    }

                    // Check if the record already exists or has a higher timer value
                    selectCommand!.AddParameterWithValue("@MapName", currentMapNamee);
                    selectCommand!.AddParameterWithValue("@SteamID", steamId);
                    selectCommand!.AddParameterWithValue("@Stage", stage);

                    var row = await selectCommand!.ExecuteReaderAsync();

                    if (row.Read())
                    {
                        // get player columns
                        dBtimerTicks = row.GetInt32("TimerTicks");
                        dBFormattedTime = row.GetString("FormattedTime");

                        // Modify the stats
                        if (timerTicks < dBtimerTicks)
                        {
                            new_dBtimerTicks = timerTicks;
                            dBFormattedTime = formattedTime;
                            playerPoints = timerTicks;
                            if (playerPoints < 32)
                            {
                                playerPoints = 320000;
                            }
                            //not saving replays for stage times
                            //if (enableReplays == true && enableDb) _ = Task.Run(async () => await DumpReplayToJson(player!, steamId, slot, bonusX, playerTimers[slot].currentStyle));
                        }
                        else
                        {
                            new_dBtimerTicks = dBtimerTicks;
                            playerPoints = 320000;
                        }

                        await row.CloseAsync();
                        // Update or insert the record
                        string? upsertQuery;
                        DbCommand? upsertCommand;
                        switch (dbType)
                        {
                            case DatabaseType.MySQL:
                                upsertQuery = @"
                                                    INSERT INTO PlayerStageTimes 
                                                    (MapName, SteamID, PlayerName, Stage, TimerTicks, FormattedTime, Velocity, Style, Mode)
                                                    VALUES 
                                                    (@MapName, @SteamID, @PlayerName, @Stage, @TimerTicks, @FormattedTime, @Velocity, @Style, @Mode)
                                                    ON DUPLICATE KEY UPDATE
                                                    MapName = VALUES(MapName),
                                                    PlayerName = VALUES(PlayerName),
                                                    Stage = VALUES(Stage),
                                                    TimerTicks = VALUES(TimerTicks),
                                                    FormattedTime = VALUES(FormattedTime),
                                                    Velocity = VALUES(Velocity),
                                                    Style = Values(Style),
                                                    Mode = VALUES(Mode);
                                                    ";
                                upsertCommand = new MySqlCommand(upsertQuery, (MySqlConnection)connection);
                                break;
                            case DatabaseType.PostgreSQL:
                                upsertQuery = @"
                                                    INSERT INTO ""PlayerStageTimes"" 
                                                    (""MapName"", ""SteamID"", ""PlayerName"", ""Stage"", ""TimerTicks"", ""FormattedTime"", ""Velocity"", ""Style"", ""Mode"")
                                                    VALUES 
                                                    (@MapName, @SteamID, @PlayerName, @Stage, @TimerTicks, @FormattedTime, @Velocity, @Style, @Mode)
                                                    ON CONFLICT (""MapName"", ""SteamID"", ""Stage"", ""Style"", ""Mode"")
                                                    DO UPDATE SET
                                                    ""MapName"" = EXCLUDED.""MapName"",
                                                    ""PlayerName"" = EXCLUDED.""PlayerName"",
                                                    ""Stage"" = EXCLUDED.""Stage"",
                                                    ""TimerTicks"" = EXCLUDED.""TimerTicks"",
                                                    ""FormattedTime"" = EXCLUDED.""FormattedTime"",
                                                    ""Velocity"" = EXCLUDED.""Velocity"",
                                                    ""Style"" = EXCLUDED.""Style"",
                                                    ""Mode"" = EXCLUDED.""Mode"";
                                                    ";
                                upsertCommand = new NpgsqlCommand(upsertQuery, (NpgsqlConnection)connection);
                                break;
                            case DatabaseType.SQLite:
                                upsertQuery = @"
                                                    INSERT INTO PlayerStageTimes 
                                                    (MapName, SteamID, PlayerName, TimerTicks, Stage, FormattedTime, Velocity, Style, Mode)
                                                    VALUES 
                                                    (@MapName, @SteamID, @PlayerName, @Stage, @TimerTicks, @FormattedTime, @Velocity, @Style, @Mode)
                                                    ON CONFLICT (MapName, SteamID, Stage, Style, Mode)
                                                    DO UPDATE SET
                                                    MapName = excluded.MapName,
                                                    PlayerName = excluded.PlayerName,
                                                    Stage = excluded.Stage,
                                                    TimerTicks = excluded.TimerTicks,
                                                    FormattedTime = excluded.FormattedTime,
                                                    Velocity = excluded.Velocity,
                                                    Style = excluded.Style,
                                                    Mode = excluded.Mode;
                                                    ";
                                upsertCommand = new SqliteCommand(upsertQuery, (SqliteConnection)connection);
                                break;
                            default:
                                upsertQuery = null;
                                upsertCommand = null;
                                break;
                        }

                        using (upsertCommand)
                        {
                            upsertCommand!.AddParameterWithValue("@MapName", currentMapNamee);
                            upsertCommand!.AddParameterWithValue("@PlayerName", playerName);
                            upsertCommand!.AddParameterWithValue("@TimerTicks", new_dBtimerTicks);
                            upsertCommand!.AddParameterWithValue("@FormattedTime", dBFormattedTime);
                            upsertCommand!.AddParameterWithValue("@SteamID", steamId);
                            upsertCommand!.AddParameterWithValue("@Stage", stage);
                            upsertCommand!.AddParameterWithValue("@Velocity", velocity);
                            upsertCommand!.AddParameterWithValue("@Style", style);
                            upsertCommand!.AddParameterWithValue("@Mode", playerTimers[slot].Mode);
                            //no points for stage times until points overhaul
                            //if (enableDb && globalRanksEnabled == true && ((dBtimesFinished <= maxGlobalFreePoints && globalRanksFreePointsEnabled == true) || beatPB)) await SavePlayerPoints(steamId, playerName, slot, playerPoints, dBtimerTicks, beatPB, bonusX, style);
                            //dont save stagetimes unless they complete map
                            //if ((stageTriggerCount != 0 || cpTriggerCount != 0) && bonusX == 0 && enableDb && timerTicks < dBtimerTicks) Server.NextFrame(() => _ = Task.Run(async () => await DumpPlayerStageTimesToJson(player, steamId, slot)));
                            var prevSRID = await GetStageRecordSteamIDFromDatabase(stage, style, playerTimers[player.Slot].Mode);
                            var prevSR = await GetPreviousPlayerStageRecordFromDatabase(player, prevSRID.Item1,
                                currentMapNamee, stage, prevSRID.Item2, bonusX);
                            await upsertCommand!.ExecuteNonQueryAsync();
                            Server.NextFrame(() =>
                                Utils.LogDebug(
                                    $"Saved player {(bonusX != 0 ? $"bonus {bonusX} stage {stage} time" : $"{stage} time")} to database for {playerName} {timerTicks} {DateTimeOffset.UtcNow.ToUnixTimeSeconds()}"));
                            if (IsAllowedPlayer(player) && enableStageTimes && enableStageSR)
                                Server.NextFrame(() => _ = Task.Run(async () => await PrintStageTimeToChat(player!,
                                    steamId, playerName, dBtimerTicks, timerTicks, stage, bonusX, prevSR)));
                        }
                    }
                    else
                    {
                        Server.NextFrame(() => Utils.LogDebug($"No player record yet"));
                        //dont save stagetimes unless they complete map
                        //if (enableReplays == true && usePostgres == true) _ = Task.Run(async () => await DumpReplayToJson(player!, steamId, slot, bonusX, playerTimers[slot].currentStyle));
                        await row.CloseAsync();

                        string? upsertQuery;
                        DbCommand? upsertCommand;
                        switch (dbType)
                        {
                            case DatabaseType.MySQL:
                                upsertQuery =
                                    @"REPLACE INTO PlayerStageTimes (MapName, SteamID, PlayerName, Stage, TimerTicks, FormattedTime, Velocity, Style, Mode) VALUES (@MapName, @SteamID, @PlayerName, @Stage, @TimerTicks, @FormattedTime, @Velocity, @Style, @Mode)";
                                upsertCommand = new MySqlCommand(upsertQuery, (MySqlConnection)connection);
                                break;
                            case DatabaseType.PostgreSQL:
                                upsertQuery =
                                    @"INSERT INTO ""PlayerStageTimes"" (""MapName"", ""SteamID"", ""PlayerName"", ""Stage"", ""TimerTicks"", ""FormattedTime"", ""Velocity"", ""Style"", ""Mode"") VALUES (@MapName, @SteamID, @PlayerName, @Stage, @TimerTicks, @FormattedTime, @Velocity, @Style, @Mode)";
                                upsertCommand = new NpgsqlCommand(upsertQuery, (NpgsqlConnection)connection);
                                break;
                            case DatabaseType.SQLite:
                                upsertQuery =
                                    @"REPLACE INTO PlayerStageTimes (MapName, SteamID, PlayerName, Stage, TimerTicks, FormattedTime, Velocity, Style, Mode) VALUES (@MapName, @SteamID, @PlayerName, @Stage, @TimerTicks, @FormattedTime, @Velocity, @Style, @Mode)";
                                upsertCommand = new SqliteCommand(upsertQuery, (SqliteConnection)connection);
                                break;
                            default:
                                upsertQuery = null;
                                upsertCommand = null;
                                break;
                        }

                        using (upsertCommand)
                        {
                            upsertCommand!.AddParameterWithValue("@MapName", currentMapNamee);
                            upsertCommand!.AddParameterWithValue("@PlayerName", playerName);
                            upsertCommand!.AddParameterWithValue("@TimerTicks", timerTicks);
                            upsertCommand!.AddParameterWithValue("@FormattedTime", formattedTime);
                            upsertCommand!.AddParameterWithValue("@SteamID", steamId);
                            upsertCommand!.AddParameterWithValue("@Stage", stage);
                            upsertCommand!.AddParameterWithValue("@Velocity", velocity);
                            upsertCommand!.AddParameterWithValue("@Style", style);
                            upsertCommand!.AddParameterWithValue("@Mode", playerTimers[slot].Mode);
                            var prevSRID = await GetStageRecordSteamIDFromDatabase(stage, style, playerTimers[player.Slot].Mode);
                            var prevSR = await GetPreviousPlayerStageRecordFromDatabase(player, prevSRID.Item1,
                                currentMapNamee, stage, prevSRID.Item2, bonusX);
                            await upsertCommand!.ExecuteNonQueryAsync();
                            //no points until points overhaul
                            //if (globalRanksEnabled == true) await SavePlayerPoints(steamId, playerName, slot, timerTicks, dBtimerTicks, beatPB, bonusX, style);
                            //dont save stagetimes unless they complete map
                            //if ((stageTriggerCount != 0 || cpTriggerCount != 0) && bonusX == 0) Server.NextFrame(() => _ = Task.Run(async () => await DumpPlayerStageTimesToJson(player, steamId, slot)));
                            Server.NextFrame(() =>
                                Utils.LogDebug(
                                    $"Saved player {(bonusX != 0 ? $"bonus {bonusX} stage {stage} time" : $"stage {stage} time")} to database for {playerName} {timerTicks} {DateTimeOffset.UtcNow.ToUnixTimeSeconds()}"));
                            if (IsAllowedPlayer(player) && enableStageTimes && enableStageSR)
                                Server.NextFrame(() => _ = Task.Run(async () => await PrintStageTimeToChat(player!,
                                    steamId, playerName, dBtimerTicks, timerTicks, stage, bonusX, prevSR)));
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Server.NextFrame(() =>
                    Utils.LogError(
                        $"Error saving player {(bonusX != 0 ? $"bonus {bonusX} time" : "time")} to database: {ex.Message}"));
            }
        }

        public async Task SetPlayerStats(CCSPlayerController? player, string steamId, string playerName, int slot)
        {
            Utils.LogDebug($"Trying to set player stats in database for {playerName}");
            try
            {
                if (!IsAllowedPlayer(player)) return;
                int timeNowUnix = (int)DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                // get player columns
                int timesConnected = 0;
                int lastConnected = 0;
                /*bool hideTimerHud = false;
                bool hideKeys = false;
                bool hideWeapon = false;
                bool hidePlayers = false;
                bool soundsEnabled = true;
                int playerFov = 0;*/
                bool isVip = false;
                string bigGif = "x";
                int playerPoints = 0;

                using (var connection = await OpenConnectionAsync())
                {
                    await CreatePlayerStatsTableAsync(connection);
                    string? selectQuery;
                    DbCommand? selectCommand;
                    switch (dbType)
                    {
                        case DatabaseType.MySQL:
                            selectQuery =
                                $"SELECT PlayerName, TimesConnected, IsVip, BigGifID, GlobalPoints FROM {PlayerStatsTable} WHERE SteamID = @SteamID";
                            selectCommand = new MySqlCommand(selectQuery, (MySqlConnection)connection);
                            break;
                        case DatabaseType.PostgreSQL:
                            selectQuery =
                                $@"SELECT ""PlayerName"", ""TimesConnected"", ""IsVip"", ""BigGifID"", ""GlobalPoints"" FROM ""{PlayerStatsTable}"" WHERE ""SteamID"" = @SteamID";
                            selectCommand = new NpgsqlCommand(selectQuery, (NpgsqlConnection)connection);
                            break;
                        case DatabaseType.SQLite:
                            selectQuery =
                                $"SELECT PlayerName, TimesConnected, IsVip, BigGifID, GlobalPoints FROM {PlayerStatsTable} WHERE SteamID = @SteamID";
                            selectCommand = new SqliteCommand(selectQuery, (SqliteConnection)connection);
                            break;
                        default:
                            selectQuery = null;
                            selectCommand = null;
                            break;
                    }

                    using (selectCommand)
                    {
                        selectCommand!.AddParameterWithValue("@SteamID", steamId);

                        var row = await selectCommand!.ExecuteReaderAsync();

                        if (row.Read())
                        {
                            // get player columns
                            switch (dbType)
                            {
                                case DatabaseType.MySQL:
                                case DatabaseType.PostgreSQL:
                                    timesConnected = row.GetInt32("TimesConnected");
                                    isVip = row.GetBoolean("IsVip");
                                    bigGif = row.GetString("BigGifID");
                                    playerPoints = row.GetInt32("GlobalPoints");
                                    break;
                                case DatabaseType.SQLite:
                                    timesConnected = row.GetInt32("TimesConnected");
                                    isVip = row.GetSQLiteBool("IsVip");
                                    bigGif = row.GetString("BigGifID");
                                    playerPoints = row.GetInt32("GlobalPoints");
                                    break;
                            }

                            await row.CloseAsync();
                            // Update or insert the record

                            string? upsertQuery;
                            DbCommand? upsertCommand;
                            switch (dbType)
                            {
                                case DatabaseType.MySQL:
                                    upsertQuery =
                                        $"REPLACE INTO {PlayerStatsTable} (PlayerName, SteamID, TimesConnected, LastConnected, HideTimerHud, HideKeys, SoundsEnabled, PlayerFov, IsVip, BigGifID, GlobalPoints, HideWeapon, HidePlayers, Mode, HideChatSpeed) VALUES (@PlayerName, @SteamID, @TimesConnected, @LastConnected, @HideTimerHud, @HideKeys, @SoundsEnabled, @PlayerFov, @IsVip, @BigGifID, @GlobalPoints, @HideWeapon, @HidePlayers, @Mode, @HideChatSpeed)";
                                    upsertCommand = new MySqlCommand(upsertQuery, (MySqlConnection)connection);
                                    break;
                                case DatabaseType.PostgreSQL:
                                    upsertQuery = $@"
                                                    INSERT INTO ""{PlayerStatsTable}"" 
                                                    (""PlayerName"", ""SteamID"", ""TimesConnected"", ""LastConnected"", ""HideTimerHud"", ""HideKeys"", ""SoundsEnabled"", ""PlayerFov"", ""IsVip"", ""BigGifID"", ""GlobalPoints"", ""HideWeapon"", ""HidePlayers"", ""Mode"", ""HideChatSpeed"")
                                                    VALUES 
                                                    (@PlayerName, @SteamID, @TimesConnected, @LastConnected, @HideTimerHud, @HideKeys, @SoundsEnabled, @PlayerFov, @IsVip, @BigGifID, @GlobalPoints, @HideWeapon, @HidePlayers, @Mode, @HideChatSpeed)
                                                    ON CONFLICT (""SteamID"")
                                                    DO UPDATE SET
                                                    ""PlayerName"" = EXCLUDED.""PlayerName"",
                                                    ""TimesConnected"" = EXCLUDED.""TimesConnected"",
                                                    ""LastConnected"" = EXCLUDED.""LastConnected"",
                                                    ""HideTimerHud"" = EXCLUDED.""HideTimerHud"",
                                                    ""HideKeys"" = EXCLUDED.""HideKeys"",
                                                    ""SoundsEnabled"" = EXCLUDED.""SoundsEnabled"",
                                                    ""PlayerFov"" = EXCLUDED.""PlayerFov"",
                                                    ""IsVip"" = EXCLUDED.""IsVip"",
                                                    ""BigGifID"" = EXCLUDED.""BigGifID"",
                                                    ""GlobalPoints"" = EXCLUDED.""GlobalPoints"",
                                                    ""HideWeapon"" = EXCLUDED.""HideWeapon"",
                                                    ""HidePlayers"" = EXCLUDED.""HidePlayers"",
                                                    ""Mode"" = EXCLUDED.""Mode"",
                                                    ""HideChatSpeed"" = EXCLUDED.""HideChatSpeed""
                                                    ";
                                    upsertCommand = new NpgsqlCommand(upsertQuery, (NpgsqlConnection)connection);
                                    break;
                                case DatabaseType.SQLite:
                                    upsertQuery =
                                        $"REPLACE INTO {PlayerStatsTable} (PlayerName, SteamID, TimesConnected, LastConnected, HideTimerHud, HideKeys, SoundsEnabled, PlayerFov, IsVip, BigGifID, GlobalPoints, HideWeapon, HidePlayers, Mode, HideChatSpeed) VALUES (@PlayerName, @SteamID, @TimesConnected, @LastConnected, @HideTimerHud, @HideKeys, @SoundsEnabled, @PlayerFov, @IsVip, @BigGifID, @GlobalPoints, @HideWeapon, @HidePlayers, @Mode, @HideChatSpeed)";
                                    upsertCommand = new SqliteCommand(upsertQuery, (SqliteConnection)connection);
                                    break;
                                default:
                                    upsertQuery = null;
                                    upsertCommand = null;
                                    break;
                            }

                            using (upsertCommand)
                            {
                                if (playerTimers.TryGetValue(slot, out PlayerTimerInfo? value))
                                {
                                    upsertCommand!.AddParameterWithValue("@PlayerName", playerName);
                                    upsertCommand!.AddParameterWithValue("@SteamID", steamId);
                                    upsertCommand!.AddParameterWithValue("@TimesConnected", timesConnected);
                                    upsertCommand!.AddParameterWithValue("@LastConnected", lastConnected);
                                    upsertCommand!.AddParameterWithValue("@HideTimerHud", value.HideTimerHud);
                                    upsertCommand!.AddParameterWithValue("@HideKeys", value.HideKeys);
                                    upsertCommand!.AddParameterWithValue("@HideWeapon", value.HideWeapon);
                                    upsertCommand!.AddParameterWithValue("@HidePlayers", value.HidePlayers);
                                    upsertCommand!.AddParameterWithValue("@HideChatSpeed", value.HideChatSpeed);
                                    upsertCommand!.AddParameterWithValue("@Mode", value.Mode);
                                    upsertCommand!.AddParameterWithValue("@SoundsEnabled", value.SoundsEnabled);
                                    upsertCommand!.AddParameterWithValue("@PlayerFov", value.PlayerFov);
                                    upsertCommand!.AddParameterWithValue("@IsVip", isVip);
                                    upsertCommand!.AddParameterWithValue("@BigGifID", bigGif);
                                    upsertCommand!.AddParameterWithValue("@GlobalPoints", playerPoints);

                                    await upsertCommand!.ExecuteNonQueryAsync();
                                    Server.NextFrame(() =>
                                        Utils.LogDebug($"Set player stats in database for {playerName}"));
                                }
                                else
                                {
                                    Utils.LogError(
                                        $"Error setting player stats in database for {playerName}: player was not on the server anymore");

                                    return;
                                }
                            }
                        }
                        else
                        {
                            Server.NextFrame(() => Utils.LogDebug($"No player stats yet"));
                            await row.CloseAsync();

                            string? upsertQuery;
                            DbCommand? upsertCommand;
                            switch (dbType)
                            {
                                case DatabaseType.MySQL:
                                    upsertQuery =
                                        $"REPLACE INTO {PlayerStatsTable} (PlayerName, SteamID, TimesConnected, LastConnected, HideTimerHud, HideKeys, SoundsEnabled, PlayerFov, IsVip, BigGifID, GlobalPoints, HideWeapon, HidePlayers, Mode, HideChatSpeed) VALUES (@PlayerName, @SteamID, @TimesConnected, @LastConnected, @HideTimerHud, @HideKeys, @SoundsEnabled, @PlayerFov, @IsVip, @BigGifID, @GlobalPoints, @HideWeapon, @HidePlayers, @Mode, @HideChatSpeed)";
                                    upsertCommand = new MySqlCommand(upsertQuery, (MySqlConnection)connection);
                                    break;
                                case DatabaseType.PostgreSQL:
                                    upsertQuery =
                                        $@"INSERT INTO ""{PlayerStatsTable}"" (""PlayerName"", ""SteamID"", ""TimesConnected"", ""LastConnected"", ""HideTimerHud"", ""HideKeys"", ""SoundsEnabled"", ""PlayerFov"", ""IsVip"", ""BigGifID"", ""GlobalPoints"", ""HideWeapon"", ""HidePlayers"", ""Mode"", ""HideChatSpeed"") VALUES (@PlayerName, @SteamID, @TimesConnected, @LastConnected, @HideTimerHud, @HideKeys, @SoundsEnabled, @PlayerFov, @IsVip, @BigGifID, @GlobalPoints, @HideWeapon, @HidePlayers, @Mode, @HideChatSpeed)";
                                    upsertCommand = new NpgsqlCommand(upsertQuery, (NpgsqlConnection)connection);
                                    break;
                                case DatabaseType.SQLite:
                                    upsertQuery =
                                        $"REPLACE INTO {PlayerStatsTable} (PlayerName, SteamID, TimesConnected, LastConnected, HideTimerHud, HideKeys, SoundsEnabled, PlayerFov, IsVip, BigGifID, GlobalPoints, HideWeapon, HidePlayers, Mode, HideChatSpeed) VALUES (@PlayerName, @SteamID, @TimesConnected, @LastConnected, @HideTimerHud, @HideKeys, @SoundsEnabled, @PlayerFov, @IsVip, @BigGifID, @GlobalPoints, @HideWeapon, @HidePlayers, @Mode, @HideChatSpeed)";
                                    upsertCommand = new SqliteCommand(upsertQuery, (SqliteConnection)connection);
                                    break;
                                default:
                                    upsertQuery = null;
                                    upsertCommand = null;
                                    break;
                            }

                            using (upsertCommand)
                            {
                                if (playerTimers.TryGetValue(slot, out PlayerTimerInfo? value))
                                {
                                    upsertCommand!.AddParameterWithValue("@PlayerName", playerName);
                                    upsertCommand!.AddParameterWithValue("@SteamID", steamId);
                                    upsertCommand!.AddParameterWithValue("@TimesConnected", 1);
                                    upsertCommand!.AddParameterWithValue("@LastConnected", timeNowUnix);
                                    upsertCommand!.AddParameterWithValue("@HideTimerHud",
                                        playerTimers[slot].HideTimerHud);
                                    upsertCommand!.AddParameterWithValue("@HideKeys", playerTimers[slot].HideKeys);
                                    upsertCommand!.AddParameterWithValue("@HideWeapon", playerTimers[slot].HideWeapon);
                                    upsertCommand!.AddParameterWithValue("@HidePlayers",
                                        playerTimers[slot].HidePlayers);
                                    upsertCommand!.AddParameterWithValue("@Mode", playerTimers[slot].Mode);
                                    upsertCommand!.AddParameterWithValue("@SoundsEnabled",
                                        playerTimers[slot].SoundsEnabled);
                                    upsertCommand!.AddParameterWithValue("@PlayerFov", playerTimers[slot].PlayerFov);
                                    upsertCommand!.AddParameterWithValue("@HideChatSpeed", playerTimers[slot].HideChatSpeed);
                                    upsertCommand!.AddParameterWithValue("@IsVip", false);
                                    upsertCommand!.AddParameterWithValue("@BigGifID", "x");
                                    upsertCommand!.AddParameterWithValue("@GlobalPoints", 0);

                                    await upsertCommand!.ExecuteNonQueryAsync();
                                    Server.NextFrame(() =>
                                        Utils.LogDebug($"Set player stats in database for {playerName}"));
                                }
                                else
                                {
                                    Utils.LogError(
                                        $"Error setting player stats in database for {playerName}: player was not on the server anymore");

                                    return;
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Server.NextFrame(() =>
                    Utils.LogError($"Error setting player stats in database for {playerName}: {ex}"));
            }
        }

        public (string, int) FixMapAndBonus(string mapName)
        {
            string pattern = @"_bonus(\d+)$";
            Match match = Regex.Match(mapName, pattern);

            if (match.Success)
            {
                int bonusNumber = int.Parse(match.Groups[1].Value);
                string fixedMapName = Regex.Replace(mapName, pattern, "");

                return (fixedMapName, bonusNumber);
            }

            // Unchanged if map name doesn't contain _bonusX from import
            return (mapName, 0);
        }

        public async Task<(int oldPoints, int newPoints)> SavePlayerPoints(string steamId, string playerName, int slot, int timerTicks, int oldTicks,
            string mode, bool beatPB = false, int bonusX = 0, int style = 0, int completions = 0, string mapname = "",
            bool import = false)
        {
            Utils.LogDebug($"Trying to set player points in database for {playerName}");
            try
            {
                if (mapname == "") mapname = currentMapName!;

                // If we're importing points, we need to fix mapname and bonusX
                if (bonusX == 0)
                    (mapname, bonusX) = FixMapAndBonus(mapname);

                int playerPoints = 0;
                using (var connection = await OpenConnectionAsync())
                {
                    await CreatePlayerStatsTableAsync(connection);

                    string? selectQuery;
                    DbCommand? selectCommand;
                    switch (dbType)
                    {
                        case DatabaseType.MySQL:
                            selectQuery =
                                $@"SELECT GlobalPoints FROM {PlayerStatsTable} WHERE SteamID = @SteamID";
                            selectCommand = new MySqlCommand(selectQuery, (MySqlConnection)connection);
                            break;
                        case DatabaseType.PostgreSQL:
                            selectQuery =
                                $@"SELECT ""GlobalPoints"" FROM ""{PlayerStatsTable}"" WHERE ""SteamID"" = @SteamID";
                            selectCommand = new NpgsqlCommand(selectQuery, (NpgsqlConnection)connection);
                            break;
                        case DatabaseType.SQLite:
                            selectQuery =
                                $@"SELECT GlobalPoints FROM {PlayerStatsTable} WHERE SteamID = @SteamID";
                            selectCommand = new SqliteCommand(selectQuery, (SqliteConnection)connection);
                            break;
                        default:
                            selectQuery = null;
                            selectCommand = null;
                            break;
                    }

                    using (selectCommand)
                    {
                        if (import)
                            selectCommand!.CommandTimeout = 120;

                        selectCommand!.AddParameterWithValue("@SteamID", steamId);

                        var row = await selectCommand!.ExecuteReaderAsync();

                        if (row.Read())
                        {
                            playerPoints = row.GetInt32("GlobalPoints");
                            
                            int newPoints = await CalculatePlayerPoints(steamId, playerName, timerTicks, oldTicks, mode,
                                beatPB, bonusX, style, completions, mapname) + playerPoints;

                            await row.CloseAsync();
                            // Update or insert the record

                            string? upsertQuery;
                            DbCommand? upsertCommand;
                            switch (dbType)
                            {
                                case DatabaseType.MySQL:
                                    upsertQuery =
                                        $@"UPDATE {PlayerStatsTable} SET GlobalPoints = @GlobalPoints WHERE SteamID = @SteamID";
                                    upsertCommand = new MySqlCommand(upsertQuery, (MySqlConnection)connection);
                                    break;
                                case DatabaseType.PostgreSQL:
                                    upsertQuery =
                                        $@"UPDATE ""{PlayerStatsTable}"" SET ""GlobalPoints"" = @GlobalPoints WHERE ""SteamID"" = @SteamID";
                                    upsertCommand = new NpgsqlCommand(upsertQuery, (NpgsqlConnection)connection);
                                    break;
                                case DatabaseType.SQLite:
                                    upsertQuery =
                                        $@"UPDATE {PlayerStatsTable} SET GlobalPoints = @GlobalPoints WHERE SteamID = @SteamID";
                                    upsertCommand = new SqliteCommand(upsertQuery, (SqliteConnection)connection);
                                    break;
                                default:
                                    upsertQuery = null;
                                    upsertCommand = null;
                                    break;
                            }

                            using (upsertCommand)
                            {
                                if (playerTimers.TryGetValue(slot, out PlayerTimerInfo? value) || slot == -1)
                                {
                                    upsertCommand!.AddParameterWithValue("@SteamID", steamId);
                                    upsertCommand!.AddParameterWithValue("@GlobalPoints", newPoints);

                                    await upsertCommand!.ExecuteNonQueryAsync();
                                    
                                    Server.NextFrame(() =>
                                        Utils.LogDebug(
                                            $"Set points in database for {playerName} from {playerPoints} to {newPoints}"));
                                }
                                else
                                {
                                    Utils.LogError(
                                        $"Error setting player points to database for {playerName}: player was not on the server anymore");
                                    return (0, 0);
                                }
                            }

                            return (playerPoints, newPoints);
                        }
                        // player will always have a stats column generated on connect
                    }
                }
            }
            catch (Exception ex)
            {
                Server.NextFrame(() =>
                    Utils.LogError($"Error getting player stats from database for {playerName}: {ex}"));
            }
            return (0, 0);
        }

        public async Task<int> CalculatePlayerPoints(string steamId, string playerName, int timerTicks, int oldTicks,
            string mode, bool beatPB = false, int bonusX = 0, int style = 0, int completions = 0, string mapname = "")
        {
            Utils.LogDebug($"Trying to calculate player points for {playerName}");
            try
            {
                if (mapname == "") mapname = currentMapName!;

                double newPoints;

                // First calculate basic map completion points based on tier
                newPoints = CalculateCompletion(false);

                // now grab sortedrecords for getting total map completes and top10
                var sortedRecords = new Dictionary<int, PlayerRecord>();
                sortedRecords = await GetSortedRecordsFromDatabase(0, bonusX, mapname, style, mode);

                // Then calculate max points based on **map total** times finished
                double maxPoints = await CalculateTier(sortedRecords.Count, mapname);

                int rank = 1;
                bool isTop10 = false;
                if (sortedRecords.Count == 0)
                {
                    newPoints += CalculateTop10(maxPoints, rank, false);
                    Utils.LogDebug($"First map entry, player {playerName} is rank #1");
                    isTop10 = true;
                }
                else
                {
                    foreach (var kvp in sortedRecords.Take(10))
                    {
                        if (kvp.Value.TimerTicks >= timerTicks)
                        {
                            newPoints += CalculateTop10(maxPoints, rank);
                            isTop10 = true;
                            Utils.LogDebug($"Player {playerName} is rank #{rank}");
                            break;
                        }

                        rank++;
                    }
                }

                // If not in top 10, calculate groups based on percentile
                if (!isTop10)
                {
                    newPoints += CalculateGroups(maxPoints,
                        await GetPlayerMapPercentile(steamId, playerName, mapname, bonusX, style, false,
                            timerTicks, mode), false);
                }

                // Apply style multiplier if enabled
                if (enableStylePoints)
                    newPoints *= GetStyleMultiplier(style);

                // Apply mode multiplier
                newPoints *= GetModeMultiplier(mode);

                // Apply bonus multiplier if bonus completion
                if (bonusX != 0)
                    newPoints *= globalPointsBonusMultiplier;

                // Hastily round the new points to prevent 123.4567890123456789 points
                newPoints = Math.Round(newPoints);

                // Zero out new points if style points are disabled and player is using styles
                if (!enableStylePoints && style != 0)
                    newPoints = 0;

                // 0 completions is an easy identifier for importpoints
                if (completions == 0)
                    return (int)newPoints;

                // Zero out new points if player has exceeded max completions and has not set a pb
                if (globalPointsMaxCompletions > 0 &&
                    await PlayerCompletions(steamId, bonusX, style, mode) > globalPointsMaxCompletions && !beatPB)
                    newPoints = 0;

                return (int)newPoints;
            }
            catch (Exception ex)
            {
                Server.NextFrame(() => Utils.LogError($"Error calculating player points for {playerName}: {ex}"));
            }

            return 0;
        }

        public async Task<int> CalculateGlobalPoints(int playerId, decimal time, int bonusX = 0,
            int style = 0, string mode = "")
        {
            Utils.LogDebug($"Trying to calculate global player points");
            try
            {
                double newPoints;

                // First calculate basic map completion points based on tier
                newPoints = CalculateCompletion(true);

                // now grab sortedrecords for getting total map completes and top10
                var sortedRecords = new Dictionary<int, GlobalRecord>();

                sortedRecords = await GetSortedRecordsFromGlobal(GetNamedStyle(style), mode, bonusX, 0);

                // Then calculate max points based on **map total** times finished
                double maxPoints = await CalculateTier(sortedRecords.Count, mapCache.MapName);

                int rank = 1;
                bool isTop10 = false;
                if (sortedRecords.Count == 0)
                {
                    newPoints += CalculateTop10(maxPoints, rank, true);
                    Utils.LogDebug($"First map entry, player is rank #1");
                    isTop10 = true;
                }
                else
                {
                    foreach (var kvp in sortedRecords.Take(10))
                    {
                        if (kvp.Value.time >= time)
                        {
                            newPoints += CalculateTop10(maxPoints, rank);
                            isTop10 = true;
                            Utils.LogDebug($"Player is rank #{rank}");
                            break;
                        }

                        rank++;
                    }
                }

                // If not in top 10, calculate groups based on percentile
                if (!isTop10)
                {
                    newPoints += CalculateGroups(maxPoints,
                        await GetPlayerGlobalMapPercentile(playerId, mode, GetNamedStyle(style), bonusX, time), true);
                }

                // Apply mode multiplier
                newPoints *= GetModeMultiplier(mode, true);

                // zero out style and bonus points
                if (style != 0)
                    newPoints = 0;
                if (bonusX != 0)
                    newPoints = 0;
                newPoints = Math.Round(newPoints);
                return (int)newPoints;
            }
            catch (Exception ex)
            {
                Server.NextFrame(() =>
                    Utils.LogError($"Error calculating player points: {ex}"));
            }

            return 0;
        }

        public async Task<int> PlayerCompletions(string steamId, int bonusX = 0, int style = 0, string mode = "")
        {
            try
            {
                //if ((bonusX == 0 && !playerTimers[slot].IsTimerRunning) || (bonusX != 0 && !playerTimers[slot].IsBonusTimerRunning)) return;
                string currentMapNamee = bonusX == 0 ? currentMapName! : $"{currentMapName}_bonus{bonusX}";

                using (var connection = await OpenConnectionAsync())
                {
                    await CreatePlayerRecordsTableAsync(connection);

                    string? selectQuery;
                    DbCommand? selectCommand;
                    switch (dbType)
                    {
                        case DatabaseType.MySQL:
                            selectQuery =
                                @"SELECT TimesFinished FROM PlayerRecords WHERE MapName = @MapName AND SteamID = @SteamID AND Style = @Style AND Mode = @Mode";
                            selectCommand = new MySqlCommand(selectQuery, (MySqlConnection)connection);
                            break;
                        case DatabaseType.PostgreSQL:
                            selectQuery =
                                @"SELECT ""TimesFinished"" FROM ""PlayerRecords"" WHERE ""MapName"" = @MapName AND ""SteamID"" = @SteamID AND ""Style"" = @Style AND ""Mode"" = @Mode";
                            selectCommand = new NpgsqlCommand(selectQuery, (NpgsqlConnection)connection);
                            break;
                        case DatabaseType.SQLite:
                            selectQuery =
                                @"SELECT TimesFinished FROM PlayerRecords WHERE MapName = @MapName AND SteamID = @SteamID AND Style = @Style AND Mode = @Mode";
                            selectCommand = new SqliteCommand(selectQuery, (SqliteConnection)connection);
                            break;
                        default:
                            selectQuery = null;
                            selectCommand = null;
                            break;
                    }

                    // Check if the record already exists or has a higher timer value
                    selectCommand!.AddParameterWithValue("@MapName", currentMapNamee);
                    selectCommand!.AddParameterWithValue("@SteamID", steamId);
                    selectCommand!.AddParameterWithValue("@Style", style);
                    selectCommand!.AddParameterWithValue("@Mode", mode);

                    var row = await selectCommand!.ExecuteReaderAsync();

                    if (row.Read())
                    {
                        return row.GetInt32("TimesFinished");
                    }
                }
            }
            catch (Exception ex)
            {
                Server.NextFrame(() =>
                    Utils.LogError($"Error getting player completions from database for id:{steamId}: {ex}"));
            }

            return 0;
        }

        public async Task PrintTop10PlayerPoints(CCSPlayerController player)
        {
            try
            {
                using (IDbConnection connection = await OpenConnectionAsync())
                {
                    try
                    {
                        string? query;
                        DbCommand? command;
                        switch (dbType)
                        {
                            case DatabaseType.MySQL:
                                query =
                                    $@"SELECT PlayerName, GlobalPoints FROM {PlayerStatsTable} ORDER BY GlobalPoints DESC LIMIT 10";
                                command = new MySqlCommand(query, (MySqlConnection)connection);
                                break;
                            case DatabaseType.PostgreSQL:
                                query =
                                    $@"SELECT ""PlayerName"", ""GlobalPoints"" FROM ""{PlayerStatsTable}"" ORDER BY ""GlobalPoints"" DESC LIMIT 10";
                                command = new NpgsqlCommand(query, (NpgsqlConnection)connection);
                                break;
                            case DatabaseType.SQLite:
                                query =
                                    $@"SELECT PlayerName, GlobalPoints FROM {PlayerStatsTable} ORDER BY GlobalPoints DESC LIMIT 10";
                                command = new SqliteCommand(query, (SqliteConnection)connection);
                                break;
                            default:
                                query = null;
                                command = null;
                                break;
                        }

                        using (command)
                        {
                            using (DbDataReader reader = await command!.ExecuteReaderAsync())
                            {
                                Server.NextFrame(() =>
                                {
                                    if (IsPlayerOrSpectator(player))
                                        Utils.PrintToChat(player, Localizer["top_10_points"]);
                                });

                                int rank = 0;

                                while (await reader.ReadAsync())
                                {
                                    string playerName = reader["PlayerName"].ToString()!;
                                    int points = Convert.ToInt32(reader["GlobalPoints"]);

                                    if (points >= minGlobalPointsForRank)
                                    {
                                        int currentRank = ++rank;
                                        Server.NextFrame(() =>
                                        {
                                            if (IsPlayerOrSpectator(player))
                                                Utils.PrintToChat(player,
                                                    Localizer["top_10_points_list", currentRank, playerName, points]);
                                        });
                                    }
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Server.NextFrame(() =>
                            Utils.LogError($"An error occurred in PrintTop10PlayerPoints inside using con: {ex}"));
                    }
                }
            }
            catch (Exception ex)
            {
                Server.NextFrame(() => Utils.LogError($"An error occurred in PrintTop10PlayerPoints: {ex}"));
            }
        }

        /*public async Task GetReplayVIPGif(string steamId, int slot)
        {
            Server.NextFrame(() => Utils.LogDebug($"Trying to get replay VIP Gif from database"));
            try
            {
                using (var connection = await OpenConnectionAsync())
                {
                    await CreatePlayerStatsTableAsync(connection);
                    string? selectQuery;
                    DbCommand? selectCommand;
                    switch (dbType)
                    {
                        case DatabaseType.MySQL:
                            selectQuery = selectQuery =
                                $"SELECT IsVip, BigGifID FROM {PlayerStatsTable} WHERE SteamID = @SteamID";
                            selectCommand = new MySqlCommand(selectQuery, (MySqlConnection)connection);
                            break;
                        case DatabaseType.PostgreSQL:
                            selectQuery =
                                $@"SELECT ""IsVip"", ""BigGifID"" FROM ""{PlayerStatsTable}"" WHERE ""SteamID"" = @SteamID";
                            selectCommand = new NpgsqlCommand(selectQuery, (NpgsqlConnection)connection);
                            break;
                        case DatabaseType.SQLite:
                            selectQuery = $"SELECT IsVip, BigGifID FROM {PlayerStatsTable} WHERE SteamID = @SteamID";
                            selectCommand = new SqliteCommand(selectQuery, (SqliteConnection)connection);
                            break;
                        default:
                            selectQuery = null;
                            selectCommand = null;
                            break;
                    }

                    using (selectCommand)
                    {
                        selectCommand!.AddParameterWithValue("@SteamID", steamId);

                        var row = await selectCommand!.ExecuteReaderAsync();

                        if (row.Read() && playerTimers.TryGetValue(slot, out PlayerTimerInfo? value))
                        {
                            // get player columns
                            bool isVip = false;
                            switch (dbType)
                            {
                                case DatabaseType.MySQL:
                                case DatabaseType.PostgreSQL:
                                    isVip = row.GetBoolean("IsVip");
                                    break;
                                case DatabaseType.SQLite:
                                    isVip = row.GetSQLiteBool("IsVip");
                                    break;
                            }

                            if (isVip)
                            {
                                Server.NextFrame(() => Utils.LogDebug($"Replay is VIP setting gif..."));
                                value.VipReplayGif =
                                    $"<br><img src='https://files.catbox.moe/{row.GetString("BigGifID")}.gif'><br>";
                            }
                            else
                            {
                                Server.NextFrame(() => Utils.LogDebug($"Replay is not VIP..."));
                                value.VipReplayGif = "x";
                            }

                            await row.CloseAsync();
                        }
                        else
                        {
                            await row.CloseAsync();
                            Server.NextFrame(() => Utils.LogDebug($"Replay is not VIP... goofy"));
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Server.NextFrame(() => Utils.LogError($"Error getting ReplayVIPGif from database: {ex}"));
            }
        }*/

        public async Task<(string, string, int)> GetMapRecordSteamIDFromDatabase(int bonusX = 0, int top10 = 0,
            int style = 0, string mode = "")
        {
            Utils.LogDebug($"Trying to get {(bonusX != 0 ? $"bonus {bonusX}" : "map")} record steamid from database");
            try
            {
                using (IDbConnection connection = await OpenConnectionAsync())
                {
                    await CreatePlayerRecordsTableAsync(connection);
                    string? selectQuery;
                    DbCommand? selectCommand;
                    if (top10 != 0)
                    {
                        switch (dbType)
                        {
                            case DatabaseType.MySQL:
                                // Get the top N records based on TimerTicks
                                selectQuery = "SELECT SteamID, PlayerName, TimerTicks " +
                                              "FROM PlayerRecords " +
                                              "WHERE MapName = @MapName " +
                                              "AND Style = @Style " +
                                              "AND Mode = @Mode " +
                                              "ORDER BY TimerTicks ASC " +
                                              $"LIMIT 1 OFFSET {top10 - 1};";
                                selectCommand = new MySqlCommand(selectQuery, (MySqlConnection)connection);
                                break;
                            case DatabaseType.PostgreSQL:
                                // Get the top N records based on TimerTicks
                                selectQuery = @"SELECT ""SteamID"", ""PlayerName"", ""TimerTicks"" " +
                                              @"FROM ""PlayerRecords"" " +
                                              @"WHERE ""MapName"" = @MapName " +
                                              @"AND ""Style"" = @Style " +
                                              @"AND ""Mode"" = @Mode " +
                                              @"ORDER BY ""TimerTicks"" ASC " +
                                              $"LIMIT 1 OFFSET {top10 - 1};";
                                selectCommand = new NpgsqlCommand(selectQuery, (NpgsqlConnection)connection);
                                break;
                            case DatabaseType.SQLite:
                                // Get the top N records based on TimerTicks
                                selectQuery = "SELECT SteamID, PlayerName, TimerTicks " +
                                              "FROM PlayerRecords " +
                                              "WHERE MapName = @MapName " +
                                              "AND Style = @Style " +
                                              "AND Mode = @Mode " +
                                              "ORDER BY TimerTicks ASC " +
                                              $"LIMIT 1 OFFSET {top10 - 1};";
                                selectCommand = new SqliteCommand(selectQuery, (SqliteConnection)connection);
                                break;
                            default:
                                selectQuery = null;
                                selectCommand = null;
                                break;
                        }
                    }
                    else
                    {
                        // Get the overall top player
                        switch (dbType)
                        {
                            case DatabaseType.MySQL:
                                selectQuery =
                                    $"SELECT SteamID, PlayerName, TimerTicks FROM PlayerRecords WHERE MapName = @MapName AND Style = @Style AND Mode = @Mode ORDER BY TimerTicks ASC LIMIT 1";
                                selectCommand = new MySqlCommand(selectQuery, (MySqlConnection)connection);
                                break;
                            case DatabaseType.PostgreSQL:
                                selectQuery =
                                    $@"SELECT ""SteamID"", ""PlayerName"", ""TimerTicks"" FROM ""PlayerRecords"" WHERE ""MapName"" = @MapName AND ""Style"" = @Style AND ""Mode"" = @Mode ORDER BY ""TimerTicks"" ASC LIMIT 1";
                                selectCommand = new NpgsqlCommand(selectQuery, (NpgsqlConnection)connection);
                                break;
                            case DatabaseType.SQLite:
                                selectQuery =
                                    $"SELECT SteamID, PlayerName, TimerTicks FROM PlayerRecords WHERE MapName = @MapName AND Style = @Style AND Mode = @Mode ORDER BY TimerTicks ASC LIMIT 1";
                                selectCommand = new SqliteCommand(selectQuery, (SqliteConnection)connection);
                                break;
                            default:
                                selectQuery = null;
                                selectCommand = null;
                                break;
                        }
                    }

                    using (selectCommand)
                    {
                        selectCommand!.AddParameterWithValue("@MapName",
                            bonusX == 0 ? currentMapName! : $"{currentMapName}_bonus{bonusX}");
                        selectCommand!.AddParameterWithValue("@Style", style);
                        selectCommand!.AddParameterWithValue("@Mode", mode);

                        var row = await selectCommand!.ExecuteReaderAsync();

                        if (row.Read())
                        {
                            string steamId64 = row.GetString("SteamID");
                            string playerName = row.GetString("PlayerName");
                            int timerTicks = row.GetInt32("TimerTicks");


                            await row.CloseAsync();

                            return (steamId64, playerName, timerTicks);
                        }
                        else
                        {
                            await row.CloseAsync();

                            return ("null", "null", 0);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Server.NextFrame(() =>
                    Utils.LogError($"Error getting GetMapRecordSteamIDFromDatabase from database: {ex}"));
                return ("null", "null", 0);
            }
        }

        public async Task<(string, string, string)> GetStageRecordSteamIDFromDatabase(int stage, int style, string mode, int bonusX = 0, int top10 = 0)
        {
            Utils.LogDebug(
                $"Trying to get {(bonusX != 0 ? $"bonus {bonusX} stage {stage}" : $"stage {stage}")} record steamid from database");
            try
            {
                using (IDbConnection connection = await OpenConnectionAsync())
                {
                    string? selectQuery;
                    DbCommand? selectCommand;
                    if (top10 != 0)
                    {
                        switch (dbType)
                        {
                            case DatabaseType.MySQL:
                                // Get the top N records based on TimerTicks
                                selectQuery = "SELECT SteamID, PlayerName, TimerTicks " +
                                              "FROM PlayerStageTimes " +
                                              "WHERE MapName = @MapName " +
                                              "AND Stage = @Stage " +
                                              "AND Style = @Style " +
                                              "AND Mode = @Mode " +
                                              "ORDER BY TimerTicks ASC " +
                                              $"LIMIT 1 OFFSET {top10 - 1};";
                                selectCommand = new MySqlCommand(selectQuery, (MySqlConnection)connection);
                                break;
                            case DatabaseType.PostgreSQL:
                                // Get the top N records based on TimerTicks
                                selectQuery = @"SELECT ""SteamID"", ""PlayerName"", ""TimerTicks"" " +
                                              @"FROM ""PlayerStageTimes"" " +
                                              @"WHERE ""MapName"" = @MapName " +
                                              @"AND ""Stage"" = @Stage " +
                                              @"AND ""Style"" = @Style " +
                                              @"AND ""Mode"" = @Mode " +
                                              @"ORDER BY ""TimerTicks"" ASC " +
                                              $"LIMIT 1 OFFSET {top10 - 1};";
                                selectCommand = new NpgsqlCommand(selectQuery, (NpgsqlConnection)connection);
                                break;
                            case DatabaseType.SQLite:
                                // Get the top N records based on TimerTicks
                                selectQuery = "SELECT SteamID, PlayerName, TimerTicks " +
                                              "FROM PlayerStageTimes " +
                                              "WHERE MapName = @MapName " +
                                              "AND Stage = @Stage " +
                                              "AND Style = @Style " +
                                              "AND Mode = @Mode " +
                                              "ORDER BY TimerTicks ASC " +
                                              $"LIMIT 1 OFFSET {top10 - 1};";
                                selectCommand = new SqliteCommand(selectQuery, (SqliteConnection)connection);
                                break;
                            default:
                                selectQuery = null;
                                selectCommand = null;
                                break;
                        }
                    }
                    else
                    {
                        // Get the overall top player
                        switch (dbType)
                        {
                            case DatabaseType.MySQL:
                                selectQuery =
                                    $"SELECT SteamID, PlayerName, TimerTicks FROM PlayerStageTimes WHERE MapName = @MapName AND Stage = @Stage AND Style = @Style AND Mode = @Mode ORDER BY TimerTicks ASC LIMIT 1";
                                selectCommand = new MySqlCommand(selectQuery, (MySqlConnection)connection);
                                break;
                            case DatabaseType.PostgreSQL:
                                selectQuery =
                                    $@"SELECT ""SteamID"", ""PlayerName"", ""TimerTicks"" FROM ""PlayerStageTimes"" WHERE ""MapName"" = @MapName AND ""Stage"" = @Stage AND ""Style"" = @Style AND ""Mode"" = @Mode ORDER BY ""TimerTicks"" ASC LIMIT 1";
                                selectCommand = new NpgsqlCommand(selectQuery, (NpgsqlConnection)connection);
                                break;
                            case DatabaseType.SQLite:
                                selectQuery =
                                    $"SELECT SteamID, PlayerName, TimerTicks FROM PlayerStageTimes WHERE MapName = @MapName AND Stage = @Stage AND Style = @Style AND Mode = @Mode ORDER BY TimerTicks ASC LIMIT 1";
                                selectCommand = new SqliteCommand(selectQuery, (SqliteConnection)connection);
                                break;
                            default:
                                selectQuery = null;
                                selectCommand = null;
                                break;
                        }
                    }

                    using (selectCommand)
                    {
                        selectCommand!.AddParameterWithValue("@MapName",
                            bonusX == 0 ? currentMapName! : $"{currentMapName}_bonus{bonusX}");
                        selectCommand!.AddParameterWithValue("@Stage", stage);
                        selectCommand!.AddParameterWithValue("@Mode", mode);
                        selectCommand!.AddParameterWithValue("@Style", style);

                        var row = await selectCommand!.ExecuteReaderAsync();

                        if (row.Read())
                        {
                            string steamId64 = row.GetString("SteamID");
                            string playerName = row.GetString("PlayerName");
                            string timerTicks = Utils.FormatTime(row.GetInt32("TimerTicks"));

                            await row.CloseAsync();

                            return (steamId64, playerName, timerTicks);
                        }
                        else
                        {
                            await row.CloseAsync();

                            return ("null", "null", "null");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Server.NextFrame(() =>
                    Utils.LogError($"Error getting GetStageRecordSteamIDFromDatabase from database: {ex}"));
                return ("null", "null", "null");
            }
        }

        public async Task<(int, string)> GetStageRecordFromDatabase(int stage, string steamId, int style, string mode, int bonusX = 0)
        {
            Utils.LogDebug(
                $"Trying to get {(bonusX != 0 ? $"bonus {bonusX} stage {stage}" : $"stage {stage}")} record steamid from database");
            try
            {
                using (IDbConnection connection = await OpenConnectionAsync())
                {
                    string? selectQuery;
                    DbCommand? selectCommand;
                    switch (dbType)
                    {
                        case DatabaseType.MySQL:
                            // Get the top N records based on TimerTicks
                            selectQuery = "SELECT Velocity, TimerTicks " +
                                          "FROM PlayerStageTimes " +
                                          "WHERE MapName = @MapName " +
                                          "AND Stage = @Stage " +
                                          "AND SteamID = @SteamID " +
                                          "AND Style = @Style " +
                                          "AND Mode = @Mode " +
                                          "ORDER BY TimerTicks ASC " +
                                          $"LIMIT 1;";
                            selectCommand = new MySqlCommand(selectQuery, (MySqlConnection)connection);
                            break;
                        case DatabaseType.PostgreSQL:
                            // Get the top N records based on TimerTicks
                            selectQuery = @"SELECT ""Velocity"", ""TimerTicks"" " +
                                          @"FROM ""PlayerStageTimes"" " +
                                          @"WHERE ""MapName"" = @MapName " +
                                          @"AND ""Stage"" = @Stage " +
                                          @"AND ""SteamID"" = @SteamID " +
                                          @"AND ""Style"" = @Style " +
                                          @"AND ""Mode"" = @Mode " +
                                          @"ORDER BY ""TimerTicks"" ASC " +
                                          $"LIMIT 1;";
                            selectCommand = new NpgsqlCommand(selectQuery, (NpgsqlConnection)connection);
                            break;
                        case DatabaseType.SQLite:
                            // Get the top N records based on TimerTicks
                            selectQuery = "SELECT Velocity, TimerTicks " +
                                          "FROM PlayerStageTimes " +
                                          "WHERE MapName = @MapName " +
                                          "AND Stage = @Stage " +
                                          "AND SteamID = @SteamID " +
                                          "AND Style = @Style " +
                                          "AND Mode = @Mode " +
                                          "ORDER BY TimerTicks ASC " +
                                          $"LIMIT 1;";
                            selectCommand = new SqliteCommand(selectQuery, (SqliteConnection)connection);
                            break;
                        default:
                            selectQuery = null;
                            selectCommand = null;
                            break;
                    }

                    using (selectCommand)
                    {
                        selectCommand!.AddParameterWithValue("@MapName",
                            bonusX == 0 ? currentMapName! : $"{currentMapName}_bonus{bonusX}");
                        selectCommand!.AddParameterWithValue("@Stage", stage);
                        selectCommand!.AddParameterWithValue("@SteamID", steamId);
                        selectCommand!.AddParameterWithValue("@Mode", mode);
                        selectCommand!.AddParameterWithValue("@Style", style);

                        var row = await selectCommand!.ExecuteReaderAsync();

                        if (row.Read())
                        {
                            int stageTime = row.GetInt32("TimerTicks");
                            string stageSpeed = row.GetString("Velocity");


                            await row.CloseAsync();

                            return (stageTime, stageSpeed);
                        }
                        else
                        {
                            await row.CloseAsync();

                            return (0, "null");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Server.NextFrame(() => Utils.LogError($"Error getting GetStageRecord from database: {ex}"));
                return (0, "null");
            }
        }

        public async Task<int> GetPreviousPlayerRecordFromDatabase(string steamId, string currentMapName,
            string playerName, int bonusX = 0, int style = 0, string mode = "")
        {
            Utils.LogDebug(
                $"Trying to get Previous {(bonusX != 0 ? $"bonus {bonusX} time" : "time")} from database for {playerName}");
            try
            {
                string currentMapNamee = bonusX == 0 ? currentMapName : $"{currentMapName}_bonus{bonusX}";

                using (IDbConnection connection = await OpenConnectionAsync())
                {
                    await CreatePlayerRecordsTableAsync(connection);
                    string? selectQuery;
                    DbCommand? selectCommand;

                    // Retrieve the TimerTicks value for the specified player on the current map
                    switch (dbType)
                    {
                        case DatabaseType.MySQL:
                            selectQuery =
                                "SELECT TimerTicks FROM PlayerRecords WHERE MapName = @MapName AND SteamID = @SteamID AND Style = @Style AND Mode = @Mode";
                            selectCommand = new MySqlCommand(selectQuery, (MySqlConnection)connection);
                            break;
                        case DatabaseType.PostgreSQL:
                            selectQuery =
                                @"SELECT ""TimerTicks"" FROM ""PlayerRecords"" WHERE ""MapName"" = @MapName AND ""SteamID"" = @SteamID AND ""Style"" = @Style AND ""Mode"" = @Mode";
                            selectCommand = new NpgsqlCommand(selectQuery, (NpgsqlConnection)connection);
                            break;
                        case DatabaseType.SQLite:
                            selectQuery =
                                "SELECT TimerTicks FROM PlayerRecords WHERE MapName = @MapName AND SteamID = @SteamID AND Style = @Style AND Mode = @Mode";
                            selectCommand = new SqliteCommand(selectQuery, (SqliteConnection)connection);
                            break;
                        default:
                            selectQuery = null;
                            selectCommand = null;
                            break;
                    }

                    using (selectCommand)
                    {
                        selectCommand!.AddParameterWithValue("@MapName", currentMapNamee);
                        selectCommand!.AddParameterWithValue("@SteamID", steamId);
                        selectCommand!.AddParameterWithValue("@Style", style);
                        selectCommand!.AddParameterWithValue("@Mode", mode);

                        var result = await selectCommand!.ExecuteScalarAsync();

                        // Check for DBNull
                        if (result != null && result != DBNull.Value)
                        {
                            Utils.LogDebug($"Got Previous Time from database for {playerName}");
                            return Convert.ToInt32(result);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Utils.LogError(
                    $"Error getting previous player {(bonusX != 0 ? $"bonus {bonusX} time" : "time")} from database: {ex.Message}");
            }

            return 0;
        }

        public async Task<int> GetPreviousPlayerStageRecordFromDatabase(CCSPlayerController? player, string steamId,
            string currentMapName, int stage, string playerName, int bonusX = 0)
        {
            Utils.LogDebug(
                $"Trying to get Previous {(bonusX != 0 ? $"bonus {bonusX} stage {stage} time" : $"stage {stage} time")} from database for {playerName}");
            try
            {
                if (!IsPlayerOrSpectator(player))
                {
                    return 0;
                }

                string currentMapNamee = bonusX == 0 ? currentMapName : $"{currentMapName}_bonus{bonusX}";

                using (IDbConnection connection = await OpenConnectionAsync())
                {
                    string? selectQuery;
                    DbCommand? selectCommand;

                    // Retrieve the TimerTicks value for the specified player on the current map
                    switch (dbType)
                    {
                        case DatabaseType.MySQL:
                            selectQuery =
                                "SELECT TimerTicks FROM PlayerStageTimes WHERE MapName = @MapName AND SteamID = @SteamID AND Stage = @Stage";
                            selectCommand = new MySqlCommand(selectQuery, (MySqlConnection)connection);
                            break;
                        case DatabaseType.PostgreSQL:
                            selectQuery =
                                @"SELECT ""TimerTicks"" FROM ""PlayerStageTimes"" WHERE ""MapName"" = @MapName AND ""SteamID"" = @SteamID AND ""Stage"" = @Stage";
                            selectCommand = new NpgsqlCommand(selectQuery, (NpgsqlConnection)connection);
                            break;
                        case DatabaseType.SQLite:
                            selectQuery =
                                "SELECT TimerTicks FROM PlayerStageTimes WHERE MapName = @MapName AND SteamID = @SteamID AND Stage = @Stage";
                            selectCommand = new SqliteCommand(selectQuery, (SqliteConnection)connection);
                            break;
                        default:
                            selectQuery = null;
                            selectCommand = null;
                            break;
                    }

                    using (selectCommand)
                    {
                        selectCommand!.AddParameterWithValue("@MapName", currentMapNamee);
                        selectCommand!.AddParameterWithValue("@SteamID", steamId);
                        selectCommand!.AddParameterWithValue("@Stage", stage);

                        var result = await selectCommand!.ExecuteScalarAsync();

                        // Check for DBNull
                        if (result != null && result != DBNull.Value)
                        {
                            Utils.LogDebug($"Got Previous stage {stage} Time from database for {playerName}");
                            return Convert.ToInt32(result);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Utils.LogError(
                    $"Error getting previous player {(bonusX != 0 ? $"bonus {bonusX} stage {stage} time" : $"stage {stage} time")} from database: {ex.Message}");
            }

            return 0;
        }

        public async Task<int> GetPlayerPointsFromDatabase(CCSPlayerController? player, string steamId,
            string playerName)
        {
            Utils.LogDebug("Trying GetPlayerPointsFromDatabase");
            int playerPoints = 0;

            try
            {
                if (!IsPlayerOrSpectator(player))
                {
                    return playerPoints;
                }

                using (var connection = await OpenConnectionAsync())
                {
                    await CreatePlayerStatsTableAsync(connection);
                    string? selectQuery;
                    DbCommand? selectCommand;
                    switch (dbType)
                    {
                        case DatabaseType.MySQL:
                            selectQuery = $"SELECT GlobalPoints FROM {PlayerStatsTable} WHERE SteamID = @SteamID";
                            selectCommand = new MySqlCommand(selectQuery, (MySqlConnection)connection);
                            break;
                        case DatabaseType.PostgreSQL:
                            selectQuery =
                                $@"SELECT ""GlobalPoints"" FROM ""{PlayerStatsTable}"" WHERE ""SteamID"" = @SteamID";
                            selectCommand = new NpgsqlCommand(selectQuery, (NpgsqlConnection)connection);
                            break;
                        case DatabaseType.SQLite:
                            selectQuery = $"SELECT GlobalPoints FROM {PlayerStatsTable} WHERE SteamID = @SteamID";
                            selectCommand = new SqliteCommand(selectQuery, (SqliteConnection)connection);
                            break;
                        default:
                            selectQuery = null;
                            selectCommand = null;
                            break;
                    }

                    using (selectCommand)
                    {
                        selectCommand!.AddParameterWithValue("@SteamID", steamId);

                        var result = await selectCommand!.ExecuteScalarAsync();

                        // Check for DBNull
                        if (result != null && result != DBNull.Value)
                        {
                            playerPoints = Convert.ToInt32(result);
                            Utils.LogDebug($"Got Player Points from database for {playerName} p: {playerPoints}");
                            return playerPoints;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Utils.LogError($"Error getting player points from database: {ex.Message}");
            }

            return playerPoints;
        }

        public async Task<Dictionary<int, PlayerRecord>> GetSortedRecordsFromDatabase(int limit = 0, int bonusX = 0,
            string mapName = "", int style = 0, string mode = "")
        {
            Utils.LogDebug($"Trying GetSortedRecords {(bonusX != 0 ? $"bonus {bonusX}" : "")} from database");
            using (var connection = await OpenConnectionAsync())
            {
                try
                {
                    string? currentMapNamee;
                    if (string.IsNullOrEmpty(mapName))
                        currentMapNamee = bonusX == 0 ? currentMapName! : $"{currentMapName}_bonus{bonusX}";
                    else
                        currentMapNamee = mapName;

                    await CreatePlayerRecordsTableAsync(connection);

                    // Retrieve and sort records for the current map
                    string? selectQuery;
                    DbCommand? selectCommand;
                    if (limit != 0)
                    {
                        switch (dbType)
                        {
                            case DatabaseType.MySQL:
                                selectQuery =
                                    $@"SELECT SteamID, PlayerName, TimerTicks FROM PlayerRecords WHERE MapName = @MapName AND Style = @Style AND Mode = @Mode ORDER BY TimerTicks ASC LIMIT {limit}";
                                selectCommand = new MySqlCommand(selectQuery, (MySqlConnection)connection);
                                break;
                            case DatabaseType.PostgreSQL:
                                selectQuery =
                                    $@"SELECT ""SteamID"", ""PlayerName"", ""TimerTicks"" FROM ""PlayerRecords"" WHERE ""MapName"" = @MapName AND ""Style"" = @Style AND ""Mode"" = @Mode ORDER BY ""TimerTicks"" ASC LIMIT {limit}";
                                selectCommand = new NpgsqlCommand(selectQuery, (NpgsqlConnection)connection);
                                break;
                            case DatabaseType.SQLite:
                                selectQuery =
                                    $@"SELECT SteamID, PlayerName, TimerTicks FROM PlayerRecords WHERE MapName = @MapName AND Style = @Style AND Mode = @Mode ORDER BY TimerTicks ASC LIMIT {limit}";
                                selectCommand = new SqliteCommand(selectQuery, (SqliteConnection)connection);
                                break;
                            default:
                                selectQuery = null;
                                selectCommand = null;
                                break;
                        }
                    }
                    else
                    {
                        switch (dbType)
                        {
                            case DatabaseType.MySQL:
                                selectQuery =
                                    @"SELECT SteamID, PlayerName, TimerTicks FROM PlayerRecords WHERE MapName = @MapName AND Style = @Style AND Mode = @Mode";
                                selectCommand = new MySqlCommand(selectQuery, (MySqlConnection)connection);
                                break;
                            case DatabaseType.PostgreSQL:
                                selectQuery =
                                    @"SELECT ""SteamID"", ""PlayerName"", ""TimerTicks"" FROM ""PlayerRecords"" WHERE ""MapName"" = @MapName AND ""Style"" = @Style AND ""Mode"" = @Mode";
                                selectCommand = new NpgsqlCommand(selectQuery, (NpgsqlConnection)connection);
                                break;
                            case DatabaseType.SQLite:
                                selectQuery =
                                    @"SELECT SteamID, PlayerName, TimerTicks FROM PlayerRecords WHERE MapName = @MapName AND Style = @Style AND Mode = @Mode";
                                selectCommand = new SqliteCommand(selectQuery, (SqliteConnection)connection);
                                break;
                            default:
                                selectQuery = null;
                                selectCommand = null;
                                break;
                        }
                    }

                    using (selectCommand)
                    {
                        selectCommand!.AddParameterWithValue("@MapName", currentMapNamee);
                        selectCommand!.AddParameterWithValue("@Style", style);
                        selectCommand!.AddParameterWithValue("@Mode", mode);
                        using (var reader = await selectCommand!.ExecuteReaderAsync())
                        {
                            var sortedRecords = new Dictionary<int, PlayerRecord>();
                            int record = 0;
                            while (await reader.ReadAsync())
                            {
                                string steamId = reader.GetString(0);
                                string playerName = reader.IsDBNull(1) ? "Unknown" : reader.GetString(1);
                                int timerTicks = reader.GetInt32(2);
                                sortedRecords.Add(record, new PlayerRecord
                                {
                                    SteamID = steamId,
                                    PlayerName = playerName,
                                    TimerTicks = timerTicks
                                });
                                record++;
                            }

                            // Sort the records by TimerTicks
                            sortedRecords = sortedRecords.OrderBy(record => record.Value.TimerTicks)
                                .ToDictionary(record => record.Key, record => record.Value);

                            Utils.LogDebug(
                                $"Got GetSortedRecords {(bonusX != 0 ? $"bonus {bonusX}" : "")} from database");

                            return sortedRecords;
                        }
                    }
                }
                catch (Exception ex)
                {
                    Utils.LogError($"Error getting sorted records from database: {ex.Message}");
                }
            }

            return [];
        }

        public async Task<Dictionary<int, PlayerRecord>> GetTopPlayersFromDatabase(int limit = 0, int bonusX = 0,
            string mapName = "", int style = 0, string mode = "", string type = "")
        {
            Utils.LogDebug($"Trying GetSortedRecords {(bonusX != 0 ? $"bonus {bonusX}" : "")} from database");
            using (var connection = await OpenConnectionAsync())
            {
                try
                {
                    string? currentMapNamee;
                    if (string.IsNullOrEmpty(mapName))
                        currentMapNamee = bonusX == 0 ? currentMapName! : $"{currentMapName}_bonus{bonusX}";
                    else
                        currentMapNamee = mapName;

                    await CreatePlayerRecordsTableAsync(connection);

                    // Retrieve and sort records for the current map
                    string? selectQuery;
                    DbCommand? selectCommand;
                    if (limit != 0)
                    {
                        switch (dbType)
                        {
                            case DatabaseType.MySQL:
                                selectQuery =
                                    $@"SELECT SteamID, PlayerName, TimerTicks, Completions FROM PlayerRecords WHERE MapName = @MapName AND Style = @Style AND Mode = @Mode ORDER BY TimerTicks ASC LIMIT {limit}";
                                selectCommand = new MySqlCommand(selectQuery, (MySqlConnection)connection);
                                break;
                            case DatabaseType.PostgreSQL:
                                selectQuery =
                                    $@"SELECT ""SteamID"", ""PlayerName"", ""TimerTicks"", ""Completions"" FROM ""PlayerRecords"" WHERE ""MapName"" = @MapName AND ""Style"" = @Style AND ""Mode"" = @Mode ORDER BY ""TimerTicks"" ASC LIMIT {limit}";
                                selectCommand = new NpgsqlCommand(selectQuery, (NpgsqlConnection)connection);
                                break;
                            case DatabaseType.SQLite:
                                selectQuery =
                                    $@"SELECT SteamID, PlayerName, TimerTicks, Completions FROM PlayerRecords WHERE MapName = @MapName AND Style = @Style AND Mode = @Mode ORDER BY TimerTicks ASC LIMIT {limit}";
                                selectCommand = new SqliteCommand(selectQuery, (SqliteConnection)connection);
                                break;
                            default:
                                selectQuery = null;
                                selectCommand = null;
                                break;
                        }
                    }
                    else
                    {
                        switch (dbType)
                        {
                            case DatabaseType.MySQL:
                                selectQuery =
                                    @"SELECT SteamID, PlayerName, TimerTicks, Completions FROM PlayerRecords WHERE MapName = @MapName AND Style = @Style AND Mode = @Mode";
                                selectCommand = new MySqlCommand(selectQuery, (MySqlConnection)connection);
                                break;
                            case DatabaseType.PostgreSQL:
                                selectQuery =
                                    @"SELECT ""SteamID"", ""PlayerName"", ""TimerTicks"", ""Completions"" FROM ""PlayerRecords"" WHERE ""MapName"" = @MapName AND ""Style"" = @Style AND ""Mode"" = @Mode";
                                selectCommand = new NpgsqlCommand(selectQuery, (NpgsqlConnection)connection);
                                break;
                            case DatabaseType.SQLite:
                                selectQuery =
                                    @"SELECT SteamID, PlayerName, TimerTicks, Completions FROM PlayerRecords WHERE MapName = @MapName AND Style = @Style AND Mode = @Mode";
                                selectCommand = new SqliteCommand(selectQuery, (SqliteConnection)connection);
                                break;
                            default:
                                selectQuery = null;
                                selectCommand = null;
                                break;
                        }
                    }

                    using (selectCommand)
                    {
                        selectCommand!.AddParameterWithValue("@MapName", currentMapNamee);
                        selectCommand!.AddParameterWithValue("@Style", style);
                        selectCommand!.AddParameterWithValue("@Mode", mode);
                        using (var reader = await selectCommand!.ExecuteReaderAsync())
                        {
                            var sortedRecords = new Dictionary<int, PlayerRecord>();
                            int record = 0;
                            while (await reader.ReadAsync())
                            {
                                string steamId = reader.GetString(0);
                                string playerName = reader.IsDBNull(1) ? "Unknown" : reader.GetString(1);
                                int timerTicks = reader.GetInt32(2);
                                int completions = reader.GetInt32(3);
                                sortedRecords.Add(record, new PlayerRecord
                                {
                                    SteamID = steamId,
                                    PlayerName = playerName,
                                    TimerTicks = timerTicks,
                                    Completions = completions
                                });
                                record++;
                            }

                            if (type == "Times")
                            {
                                sortedRecords = sortedRecords.OrderBy(record => record.Value.TimerTicks)
                                    .ToDictionary(record => record.Key, record => record.Value);
                            }
                            else
                            {
                                sortedRecords = sortedRecords.OrderByDescending(record => record.Value.Completions)
                                    .ToDictionary(record => record.Key, record => record.Value);
                            }

                            Utils.LogDebug(
                                $"Got GetTopPlayers {(bonusX != 0 ? $"bonus {bonusX}" : "")} from database");

                            return sortedRecords;
                        }
                    }
                }
                catch (Exception ex)
                {
                    Utils.LogError($"Error getting sorted records from database: {ex.Message}");
                }
            }

            return [];
        }

        public async Task<Dictionary<int, PlayerPoints>> GetTopPointsFromDatabase(int limit = 0)
        {
            using (var connection = await OpenConnectionAsync())
            {
                try
                {
                    string? selectQuery;
                    DbCommand? selectCommand;
                    switch (dbType)
                    {
                        case DatabaseType.MySQL:
                            selectQuery =
                                $@"WITH RankedPlayers AS (
                                            SELECT
                                                SteamID,
                                                PlayerName,
                                                GlobalPoints,
                                                DENSE_RANK() OVER (ORDER BY GlobalPoints DESC) AS playerPlace
                                            FROM {PlayerStatsTable})
                                        SELECT SteamID, PlayerName, GlobalPoints, playerPlace
                                        FROM RankedPlayers
                                        ORDER BY GlobalPoints DESC
                                        LIMIT @limit";
                            selectCommand = new MySqlCommand(selectQuery, (MySqlConnection)connection);
                            break;
                        case DatabaseType.PostgreSQL:
                            selectQuery =
                                $@"WITH RankedPlayers AS (
                                            SELECT
                                                ""SteamID"",
                                                ""PlayerName"",
                                                ""GlobalPoints"",
                                                DENSE_RANK() OVER (ORDER BY ""GlobalPoints"" DESC) AS playerPlace
                                            FROM ""{PlayerStatsTable}"")
                                        SELECT ""SteamID"", ""PlayerName"", ""GlobalPoints"", playerPlace
                                        FROM RankedPlayers
                                        ORDER BY ""GlobalPoints"" DESC
                                        LIMIT @limit";
                            selectCommand = new NpgsqlCommand(selectQuery, (NpgsqlConnection)connection);
                            break;
                        case DatabaseType.SQLite:
                            selectQuery =
                                $@"WITH RankedPlayers AS (
                                            SELECT
                                                SteamID,
                                                PlayerName,
                                                GlobalPoints,
                                                DENSE_RANK() OVER (ORDER BY GlobalPoints DESC) AS playerPlace
                                            FROM {PlayerStatsTable})
                                        SELECT SteamID, PlayerName, GlobalPoints, playerPlace
                                        FROM RankedPlayers
                                        ORDER BY GlobalPoints DESC
                                        LIMIT @limit";
                            selectCommand = new SqliteCommand(selectQuery, (SqliteConnection)connection);
                            break;
                        default:
                            selectQuery = null;
                            selectCommand = null;
                            break;
                    }

                    using (selectCommand)
                    {
                        selectCommand!.AddParameterWithValue("@limit", limit);
                        using (var reader = await selectCommand!.ExecuteReaderAsync())
                        {
                            var sortedPoints = new Dictionary<int, PlayerPoints>();
                            int playerNum = 0;
                            while (await reader.ReadAsync())
                            {
                                string steamId = reader.GetString(0);
                                string playerName = reader.IsDBNull(1) ? "Unknown" : reader.GetString(1);
                                int globalPoints = reader.GetInt32(2);
                                int playerPlace = reader.GetInt32(3);
                                sortedPoints.Add(playerNum, new PlayerPoints
                                {
                                    SteamID = steamId,
                                    PlayerName = playerName,
                                    GlobalPoints = globalPoints,
                                    Placement = playerPlace
                                });
                                playerNum++;
                            }
                            return sortedPoints.ToDictionary(points => points.Key, points => points.Value);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Utils.LogError($"Error getting top points from database: {ex.Message}");
                }
            }

            return [];
        }

        public async Task<List<PlayerRecord>> GetAllSortedRecordsFromDatabase(int limit = 0, int bonusX = 0,
            int style = 0, string mode = "")
        {
            Utils.LogDebug($"Trying GetSortedRecords {(bonusX != 0 ? $"bonus {bonusX}" : "")} from database");
            using (var connection = await OpenConnectionAsync())
            {
                try
                {
                    await CreatePlayerRecordsTableAsync(connection);

                    // Retrieve and sort records for the current map
                    string? selectQuery;
                    DbCommand? selectCommand;
                    if (limit != 0)
                    {
                        switch (dbType)
                        {
                            case DatabaseType.MySQL:
                                selectQuery =
                                    $@"SELECT SteamID, PlayerName, TimerTicks, MapName FROM PlayerRecords WHERE Style = @Style AND Mode = @Mode ORDER BY TimerTicks ASC LIMIT {limit}";
                                selectCommand = new MySqlCommand(selectQuery, (MySqlConnection)connection);
                                break;
                            case DatabaseType.PostgreSQL:
                                selectQuery =
                                    $@"SELECT ""SteamID"", ""PlayerName"", ""TimerTicks"", ""MapName"" FROM ""PlayerRecords"" WHERE ""Style"" = @Style AND ""Mode"" = @Mode ORDER BY ""TimerTicks"" ASC LIMIT {limit}";
                                selectCommand = new NpgsqlCommand(selectQuery, (NpgsqlConnection)connection);
                                break;
                            case DatabaseType.SQLite:
                                selectQuery =
                                    $@"SELECT SteamID, PlayerName, TimerTicks, MapName FROM PlayerRecords WHERE Style = @Style AND Mode = @Mode ORDER BY TimerTicks ASC LIMIT {limit}";
                                selectCommand = new SqliteCommand(selectQuery, (SqliteConnection)connection);
                                break;
                            default:
                                selectQuery = null;
                                selectCommand = null;
                                break;
                        }
                    }
                    else
                    {
                        switch (dbType)
                        {
                            case DatabaseType.MySQL:
                                selectQuery =
                                    @"SELECT SteamID, PlayerName, TimerTicks, MapName FROM PlayerRecords WHERE Style = @Style AND Mode = @Mode";
                                selectCommand = new MySqlCommand(selectQuery, (MySqlConnection)connection);
                                break;
                            case DatabaseType.PostgreSQL:
                                selectQuery =
                                    @"SELECT ""SteamID"", ""PlayerName"", ""TimerTicks"", ""MapName"" FROM ""PlayerRecords"" WHERE ""Style"" = @Style AND ""Mode"" = @Mode";
                                selectCommand = new NpgsqlCommand(selectQuery, (NpgsqlConnection)connection);
                                break;
                            case DatabaseType.SQLite:
                                selectQuery =
                                    @"SELECT SteamID, PlayerName, TimerTicks, MapName FROM PlayerRecords WHERE Style = @Style AND Mode = @Mode";
                                selectCommand = new SqliteCommand(selectQuery, (SqliteConnection)connection);
                                break;
                            default:
                                selectQuery = null;
                                selectCommand = null;
                                break;
                        }
                    }

                    using (selectCommand)
                    {
                        selectCommand!.AddParameterWithValue("@Style", style);
                        selectCommand!.AddParameterWithValue("@Mode", mode);
                        using (var reader = await selectCommand!.ExecuteReaderAsync())
                        {
                            Dictionary<string, List<PlayerRecord>> sortedRecords =
                                new Dictionary<string, List<PlayerRecord>>();
                            while (await reader.ReadAsync())
                            {
                                string steamId = reader.GetString(0);
                                string playerName = reader.IsDBNull(1) ? "Unknown" : reader.GetString(1);
                                int timerTicks = reader.GetInt32(2);
                                string mapname = reader.GetString(3);
                                if (!sortedRecords.ContainsKey(steamId))
                                {
                                    // If steamId doesn't exist, create a new list for the steamId
                                    sortedRecords[steamId] = new List<PlayerRecord>();
                                }

                                sortedRecords[steamId].Add(new PlayerRecord
                                {
                                    PlayerName = playerName,
                                    SteamID = steamId,
                                    TimerTicks = timerTicks,
                                    MapName = mapname
                                });
                            }

                            var sortedList = sortedRecords
                                .SelectMany(recordEntry => recordEntry.Value)
                                .OrderBy(record => record.TimerTicks)
                                .ToList();

                            Utils.LogDebug(
                                $"Got GetSortedRecords {(bonusX != 0 ? $"bonus {bonusX}" : "")} from database");

                            return sortedList;
                        }
                    }
                }
                catch (Exception ex)
                {
                    Utils.LogError($"Error getting all sorted records from database: {ex.Message}");
                }
            }

            return [];
        }

        public async Task<Dictionary<string, PlayerRecord>> GetSortedStageRecordsFromDatabase(int stage, int limit = 0,
            int bonusX = 0, string mapName = "", int style = 0, string mode = "")
        {
            Utils.LogDebug($"Trying GetSortedStageRecords {(bonusX != 0 ? $"bonus {bonusX}" : "")} from database");
            using (var connection = await OpenConnectionAsync())
            {
                try
                {
                    string? currentMapNamee;
                    if (string.IsNullOrEmpty(mapName))
                        currentMapNamee = bonusX == 0 ? currentMapName! : $"{currentMapName}_bonus{bonusX}";
                    else
                        currentMapNamee = mapName;

                    await CreatePlayerRecordsTableAsync(connection);

                    // Retrieve and sort records for the current map
                    string? selectQuery;
                    DbCommand? selectCommand;
                    if (limit != 0)
                    {
                        switch (dbType)
                        {
                            case DatabaseType.MySQL:
                                selectQuery =
                                    $@"SELECT SteamID, PlayerName, TimerTicks FROM PlayerStageTimes WHERE MapName = @MapName AND Stage = @Stage AND Style = @Style AND Mode = @Mode ORDER BY TimerTicks ASC LIMIT {limit}";
                                selectCommand = new MySqlCommand(selectQuery, (MySqlConnection)connection);
                                break;
                            case DatabaseType.PostgreSQL:
                                selectQuery =
                                    $@"SELECT ""SteamID"", ""PlayerName"", ""TimerTicks"" FROM ""PlayerStageTimes"" WHERE ""MapName"" = @MapName AND ""Stage"" = @Stage AND ""Style"" = @Style AND ""Mode"" = @Mode ORDER BY ""TimerTicks"" ASC LIMIT {limit}";
                                selectCommand = new NpgsqlCommand(selectQuery, (NpgsqlConnection)connection);
                                break;
                            case DatabaseType.SQLite:
                                selectQuery =
                                    $@"SELECT SteamID, PlayerName, TimerTicks FROM PlayerStageTimes WHERE MapName = @MapName AND Stage = @Stage AND Style = @Style AND Mode = @Mode ORDER BY TimerTicks ASC LIMIT {limit}";
                                selectCommand = new SqliteCommand(selectQuery, (SqliteConnection)connection);
                                break;
                            default:
                                selectQuery = null;
                                selectCommand = null;
                                break;
                        }
                    }
                    else
                    {
                        switch (dbType)
                        {
                            case DatabaseType.MySQL:
                                selectQuery =
                                    @"SELECT SteamID, PlayerName, TimerTicks FROM PlayerStageTimes WHERE MapName = @MapName AND Stage = @Stage AND Style = @Style AND Mode = @Mode";
                                selectCommand = new MySqlCommand(selectQuery, (MySqlConnection)connection);
                                break;
                            case DatabaseType.PostgreSQL:
                                selectQuery =
                                    @"SELECT ""SteamID"", ""PlayerName"", ""TimerTicks"" FROM ""PlayerStageTimes"" WHERE ""MapName"" = @MapName AND ""Stage"" = @Stage AND ""Style"" = @Style AND ""Mode"" = @Mode";
                                selectCommand = new NpgsqlCommand(selectQuery, (NpgsqlConnection)connection);
                                break;
                            case DatabaseType.SQLite:
                                selectQuery =
                                    @"SELECT SteamID, PlayerName, TimerTicks FROM PlayerStageTimes WHERE MapName = @MapName AND Stage = @Stage AND Style = @Style AND Mode = @Mode";
                                selectCommand = new SqliteCommand(selectQuery, (SqliteConnection)connection);
                                break;
                            default:
                                selectQuery = null;
                                selectCommand = null;
                                break;
                        }
                    }

                    using (selectCommand)
                    {
                        selectCommand!.AddParameterWithValue("@MapName", currentMapNamee);
                        selectCommand!.AddParameterWithValue("@Stage", stage);
                        selectCommand!.AddParameterWithValue("@Style", style);
                        selectCommand!.AddParameterWithValue("@Mode", mode);
                        using (var reader = await selectCommand!.ExecuteReaderAsync())
                        {
                            var sortedRecords = new Dictionary<string, PlayerRecord>();
                            while (await reader.ReadAsync())
                            {
                                string steamId = reader.GetString(0);
                                string playerName = reader.IsDBNull(1) ? "Unknown" : reader.GetString(1);
                                int timerTicks = reader.GetInt32(2);
                                sortedRecords.Add(steamId, new PlayerRecord
                                {
                                    PlayerName = playerName,
                                    TimerTicks = timerTicks
                                });
                            }

                            // Sort the records by TimerTicks
                            sortedRecords = sortedRecords.OrderBy(record => record.Value.TimerTicks)
                                .ToDictionary(record => record.Key, record => record.Value);

                            Utils.LogDebug(
                                $"Got GetSortedStageRecords {(bonusX != 0 ? $"bonus {bonusX}" : "")} from database");

                            return sortedRecords;
                        }
                    }
                }
                catch (Exception ex)
                {
                    Utils.LogError($"Error getting sorted stage records from database: {ex.Message}");
                }
            }

            return [];
        }

        public async Task<Dictionary<string, PlayerPoints>> GetSortedPointsFromDatabase()
        {
            Utils.LogDebug("Trying GetSortedPoints from database");
            using (var connection = await OpenConnectionAsync())
            {
                try
                {
                    await CreatePlayerStatsTableAsync(connection);
                    string? selectQuery;
                    DbCommand? selectCommand;
                    switch (dbType)
                    {
                        case DatabaseType.MySQL:
                            selectQuery = $@"SELECT SteamID, PlayerName, GlobalPoints FROM {PlayerStatsTable}";
                            selectCommand = new MySqlCommand(selectQuery, (MySqlConnection)connection);
                            break;
                        case DatabaseType.PostgreSQL:
                            selectQuery =
                                $@"SELECT ""SteamID"", ""PlayerName"", ""GlobalPoints"" FROM ""{PlayerStatsTable}""";
                            selectCommand = new NpgsqlCommand(selectQuery, (NpgsqlConnection)connection);
                            break;
                        case DatabaseType.SQLite:
                            selectQuery = $@"SELECT SteamID, PlayerName, GlobalPoints FROM {PlayerStatsTable}";
                            selectCommand = new SqliteCommand(selectQuery, (SqliteConnection)connection);
                            break;
                        default:
                            selectQuery = null;
                            selectCommand = null;
                            break;
                    }

                    using (selectCommand)
                    {
                        using (var reader = await selectCommand!.ExecuteReaderAsync())
                        {
                            var sortedPoints = new Dictionary<string, PlayerPoints>();
                            while (await reader.ReadAsync())
                            {
                                string steamId = reader.GetString(0);
                                string playerName = reader.IsDBNull(1) ? "Unknown" : reader.GetString(1);
                                int globalPoints = reader.GetInt32(2);

                                if (globalPoints >=
                                    minGlobalPointsForRank) // Only add if GlobalPoints is above or equal to minGlobalPointsForRank
                                {
                                    sortedPoints.Add(steamId, new PlayerPoints
                                    {
                                        PlayerName = playerName,
                                        GlobalPoints = globalPoints
                                    });
                                }
                            }

                            sortedPoints = sortedPoints.OrderByDescending(record => record.Value.GlobalPoints)
                                .ToDictionary(record => record.Key, record => record.Value);


                            return sortedPoints;
                        }
                    }
                }
                catch (Exception ex)
                {
                    Utils.LogError($"Error getting GetSortedPoints from database: {ex.Message}");
                }
            }

            return [];
        }

        [ConsoleCommand("css_importpoints", " ")]
        [RequiresPermissions("@css/root")]
        [CommandHelper(whoCanExecute: CommandUsage.CLIENT_AND_SERVER)]
        public void ImportPlayerPointsCommand(CCSPlayerController? player, CommandInfo command)
        {
            _ = Task.Run(ImportPlayerPoints);
        }

        public async Task ImportPlayerPoints()
        {
            try
            {
                Server.NextFrame(() => Utils.PrintToChatAll("Points import initialized"));
                var sortedRecords = await GetAllSortedRecordsFromDatabase(0, 0, 0, GetModeName(defaultMode));

                int batchSize = 10;
                for (int i = 0; i < sortedRecords.Count; i += batchSize)
                {
                    var batch = sortedRecords.Skip(i).Take(batchSize);
                    var tasks = batch.Select(record => SavePlayerPoints(record.SteamID!, record.PlayerName!, -1,
                        record.TimerTicks, 0, GetModeName(defaultMode), false, 0, 0, 0, record.MapName!, true));

                    await Task.WhenAll(tasks);
                }

                Server.NextFrame(() => Utils.PrintToChatAll("Points import completed"));
            }
            catch (Exception ex)
            {
                Utils.LogError($"Error ImportPlayerPoints to the database: {ex.Message}");
            }
        }

        [ConsoleCommand("css_resetpoints", " ")]
        [RequiresPermissions("@css/root")]
        [CommandHelper(whoCanExecute: CommandUsage.CLIENT_AND_SERVER)]
        public void ResetPlayerPointsCommand(CCSPlayerController? player, CommandInfo command)
        {
            _ = Task.Run(ResetPlayerPoints);
        }

        public async Task ResetPlayerPoints()
        {
            using (var connection = await OpenConnectionAsync())
            {
                try
                {
                    await CreatePlayerStatsTableAsync(connection);
                    string? updateQuery;
                    DbCommand? updateCommand;
                    switch (dbType)
                    {
                        case DatabaseType.MySQL:
                            updateQuery = $@"UPDATE {PlayerStatsTable} SET GlobalPoints = 0";
                            updateCommand = new MySqlCommand(updateQuery, (MySqlConnection)connection);
                            break;
                        case DatabaseType.PostgreSQL:
                            updateQuery = $@"UPDATE ""{PlayerStatsTable}"" SET ""GlobalPoints"" = 0";
                            updateCommand = new NpgsqlCommand(updateQuery, (NpgsqlConnection)connection);
                            break;
                        case DatabaseType.SQLite:
                            updateQuery = $@"UPDATE {PlayerStatsTable} SET GlobalPoints = 0";
                            updateCommand = new SqliteCommand(updateQuery, (SqliteConnection)connection);
                            break;
                        default:
                            updateQuery = null;
                            updateCommand = null;
                            break;
                    }

                    using (updateCommand)
                    {
                        await updateCommand!.ExecuteNonQueryAsync();
                    }
                }
                catch (Exception ex)
                {
                    Utils.LogError($"Error in ResetPlayerPoints: {ex.Message}");
                }
            }
        }

        [ConsoleCommand("css_jsontodatabase", " ")]
        [RequiresPermissions("@css/root")]
        [CommandHelper(whoCanExecute: CommandUsage.CLIENT_AND_SERVER)]
        public void AddJsonTimesToDatabaseCommand(CCSPlayerController? player, CommandInfo command)
        {
            _ = Task.Run(AddJsonTimesToDatabaseAsync);
        }

        public async Task AddJsonTimesToDatabaseAsync()
        {
            try
            {
                string recordsDirectoryNamee = "SharpTimer/PlayerRecords";
                string playerRecordsPathh = Path.Combine(gameDir!, "csgo", "cfg", recordsDirectoryNamee);

                if (!Directory.Exists(playerRecordsPathh))
                {
                    Utils.LogDebug($"Error: Directory not found at {playerRecordsPathh}");
                    return;
                }

                string connectionString = await GetConnectionStringFromConfigFile();
                IDbConnection? connection = null;
                switch (dbType)
                {
                    case DatabaseType.MySQL:
                        connection = new MySqlConnection(connectionString);
                        break;
                    case DatabaseType.PostgreSQL:
                        connection = new NpgsqlConnection(connectionString);
                        break;
                    case DatabaseType.SQLite:
                        connection = new SqliteConnection(connectionString);
                        break;
                    default:
                        Utils.LogError($"Error: Invalid database type.");
                        return;
                }

                using (connection)
                {
                    connection.Open();

                    // Check if the table exists, and create it if necessary
                    await CheckTablesAsync();

                    foreach (var filePath in Directory.EnumerateFiles(playerRecordsPathh, "*.json"))
                    {
                        string json = await File.ReadAllTextAsync(filePath);
                        var records = JsonSerializer.Deserialize<Dictionary<string, PlayerRecord>>(json);

                        if (records == null)
                        {
                            Utils.LogDebug($"Error: Failed to deserialize JSON data from {filePath}");
                            continue;
                        }

                        foreach (var recordEntry in records)
                        {
                            string steamId = recordEntry.Key;
                            PlayerRecord playerRecord = recordEntry.Value;

                            // Extract MapName from the filename (remove extension)
                            string mapName = Path.GetFileNameWithoutExtension(filePath);

                            // Check if the player is already in the database
                            string? insertOrUpdateQuery = null;
                            DbCommand? insertOrUpdateCommand = null;
                            switch (dbType)
                            {
                                case DatabaseType.MySQL:
                                    insertOrUpdateQuery =
                                        @"INSERT INTO PlayerRecords (SteamID, PlayerName, TimerTicks, FormattedTime, MapName, UnixStamp, TimesFinished, LastFinished, Style, Mode)
                                        VALUES (@SteamID, @PlayerName, @TimerTicks, @FormattedTime, @MapName, @UnixStamp, @TimesFinished, @LastFinished, @Style, @Mode)
                                        ON DUPLICATE KEY UPDATE
                                        TimerTicks = IF(@TimerTicks < TimerTicks, @TimerTicks, TimerTicks),
                                        FormattedTime = IF(@TimerTicks < TimerTicks, @FormattedTime, FormattedTime)";
                                    insertOrUpdateCommand =
                                        new MySqlCommand(insertOrUpdateQuery, (MySqlConnection)connection);
                                    break;
                                case DatabaseType.PostgreSQL:
                                    insertOrUpdateQuery =
                                        @"INSERT INTO ""PlayerRecords"" (""SteamID"", ""PlayerName"", ""TimerTicks"", ""FormattedTime"", ""MapName"", ""UnixStamp"", ""TimesFinished"", ""LastFinished"", ""Style"", ""Mode"")
                                        VALUES (@SteamID, @PlayerName, @TimerTicks, @FormattedTime, @MapName, @UnixStamp, @TimesFinished, @LastFinished, @Style, @Mode)
                                        ON CONFLICT (""MapName"", ""SteamID"", ""Style"", ""Mode"") DO UPDATE
                                        SET ""TimerTicks"" = CASE WHEN @TimerTicks < ""TimerTicks"" THEN @TimerTicks ELSE ""TimerTicks"" END,
                                        ""FormattedTime"" = CASE WHEN @TimerTicks < ""TimerTicks"" THEN @FormattedTime ELSE ""FormattedTime"" END";
                                    insertOrUpdateCommand = new NpgsqlCommand(insertOrUpdateQuery,
                                        (NpgsqlConnection)connection);
                                    break;
                                case DatabaseType.SQLite:
                                    insertOrUpdateQuery =
                                        @"INSERT INTO PlayerRecords (SteamID, PlayerName, TimerTicks, FormattedTime, MapName, UnixStamp, TimesFinished, LastFinished, Style, Mode)
                                        VALUES (@SteamID, @PlayerName, @TimerTicks, @FormattedTime, @MapName, @UnixStamp, @TimesFinished, @LastFinished, @Style, @Mode)
                                        ON CONFLICT (MapName, SteamID, Style, Mode) DO UPDATE
                                        SET TimerTicks = CASE WHEN @TimerTicks < TimerTicks THEN @TimerTicks ELSE TimerTicks END,
                                        FormattedTime = CASE WHEN @TimerTicks < TimerTicks THEN @FormattedTime ELSE FormattedTime END";
                                    insertOrUpdateCommand = new SqliteCommand(insertOrUpdateQuery,
                                        (SqliteConnection)connection);
                                    break;
                                default:
                                    insertOrUpdateQuery = null;
                                    break;
                            }

                            using (insertOrUpdateCommand)
                            {
                                insertOrUpdateCommand!.AddParameterWithValue("@SteamID", steamId);
                                insertOrUpdateCommand!.AddParameterWithValue("@PlayerName", playerRecord.PlayerName!);
                                insertOrUpdateCommand!.AddParameterWithValue("@TimerTicks", playerRecord.TimerTicks);
                                insertOrUpdateCommand!.AddParameterWithValue("@FormattedTime",
                                    Utils.FormatTime(playerRecord.TimerTicks));
                                insertOrUpdateCommand!.AddParameterWithValue("@MapName", mapName);
                                insertOrUpdateCommand!.AddParameterWithValue("@UnixStamp", 0);
                                insertOrUpdateCommand!.AddParameterWithValue("@TimesFinished", 0);
                                insertOrUpdateCommand!.AddParameterWithValue("@LastFinished", 0);
                                insertOrUpdateCommand!.AddParameterWithValue("@Style", 0);
                                insertOrUpdateCommand!.AddParameterWithValue("@Mode", "Standard");

                                await insertOrUpdateCommand!.ExecuteNonQueryAsync();
                            }
                        }

                        Utils.LogDebug(
                            $"JSON times from {Path.GetFileName(filePath)} successfully added to the database.");
                    }
                }
            }
            catch (Exception ex)
            {
                Utils.LogError($"Error adding JSON times to the database: {ex.Message}");
            }
        }
    }
}