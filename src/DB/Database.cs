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
using System.Data.Common;
using System.Diagnostics;
using SharpTimer.Database;

namespace SharpTimer.Database
{
    public enum DatabaseType
    {
        MySQL,
        PostgreSQL
    }

    public abstract class Database : SharpTimer
    {
        protected string dbPath;
        protected DatabaseType dbType;
        protected Database(string dbPath, DatabaseType dbType)
        {
            this.dbPath = dbPath;
            this.dbType = dbType;
        }

        public async Task<IDbConnection> OpenConnectionAsync(string dbPath)
        {
            IDbConnection connection = null;
            switch (dbType)
            {
                case DatabaseType.MySQL:
                    connection = new MySqlConnection(await GetConnectionStringFromConfigFile());
                    await (connection as MySqlConnection).OpenAsync();
                    break;
                case DatabaseType.PostgreSQL:
                    connection = new NpgsqlConnection(await GetConnectionStringFromConfigFile());
                    await (connection as NpgsqlConnection).OpenAsync();
                    break;
            }
            if (connection.State != ConnectionState.Open)
            {
                useMySQL = false;
                usePostgres = false;
            }
            return connection;
        }

        private async Task<string> GetConnectionStringFromConfigFile()
        {
            try
            {
                using (JsonDocument? jsonConfig = await LoadJson(dbPath)!)
                {
                    if (jsonConfig != null)
                    {
                        JsonElement root = jsonConfig.RootElement;

                        string host = root.TryGetProperty("Host", out var hostProperty) ? hostProperty.GetString()! : "localhost";
                        string database = root.TryGetProperty("Database", out var databaseProperty) ? databaseProperty.GetString()! : "database";
                        string username = root.TryGetProperty("Username", out var usernameProperty) ? usernameProperty.GetString()! : "root";
                        string password = root.TryGetProperty("Password", out var passwordProperty) ? passwordProperty.GetString()! : "root";
                        int port = root.TryGetProperty("Port", out var portProperty) ? portProperty.GetInt32()! : 3306;

                        if (dbType.Equals(DatabaseType.MySQL))
                        {
                            int timeout = root.TryGetProperty("Timeout", out var timeoutProperty) ? timeoutProperty.GetInt32()! : 30;
                            return $"Server={host};Database={database};User ID={username};Password={password};Port={port};CharSet=utf8mb4;Connection Timeout={timeout};";
                        }
                        else if (dbType.Equals(DatabaseType.PostgreSQL))
                        {
                            return $"Server={host};Database={database};User ID={username};Password={password};Port={port};SslMode=Disable";
                        }
                        else
                        {
                            SharpTimerError($"Database type not supported");
                        }
                    }
                    else
                    {
                        SharpTimerError($"Database json was null");
                    }
                }
            }
            catch (Exception ex)
            {
                SharpTimerError($"Error in GetConnectionString: {ex.Message}");
            }
            return "Server=localhost;Database=database;User ID=root;Password=root;Port=3306;";
        }

        private async Task CheckTablesAsync()
        {
            string[] playerRecords;
            string[] playerStats;

            switch (dbType)
            {
                case DatabaseType.MySQL:
                    playerRecords = [       "MapName VARCHAR(255) DEFAULT ''",
                                                    "SteamID VARCHAR(20) DEFAULT ''",
                                                    "PlayerName VARCHAR(32) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci DEFAULT ''",
                                                    "TimerTicks INT DEFAULT 0",
                                                    "FormattedTime VARCHAR(255) DEFAULT ''",
                                                    "UnixStamp INT DEFAULT 0",
                                                    "LastFinished INT DEFAULT 0",
                                                    "TimesFinished INT DEFAULT 0",
                                                    "Style INT DEFAULT 0"
                                                ];
                    playerStats = [         "SteamID VARCHAR(20) DEFAULT ''",
                                                    "PlayerName VARCHAR(32) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci DEFAULT ''",
                                                    "TimesConnected INT DEFAULT 0",
                                                    "LastConnected INT DEFAULT 0",
                                                    "GlobalPoints INT DEFAULT 0",
                                                    "HideTimerHud BOOL DEFAULT false",
                                                    "HideKeys BOOL DEFAULT false",
                                                    "HideJS BOOL DEFAULT false",
                                                    "SoundsEnabled BOOL DEFAULT false",
                                                    "PlayerFov INT DEFAULT 0",
                                                    "IsVip BOOL DEFAULT false",
                                                    "BigGifID VARCHAR(16) DEFAULT 'x'"
                                                ];
                    break;
                case DatabaseType.PostgreSQL:
                    playerRecords = [       @"""MapName"" VARCHAR(255) DEFAULT ''",
                                                    @"""SteamID"" VARCHAR(20) DEFAULT ''",
                                                    @"""PlayerName"" VARCHAR(32) DEFAULT ''",
                                                    @"""TimerTicks"" INT DEFAULT 0",
                                                    @"""FormattedTime"" VARCHAR(255) DEFAULT ''",
                                                    @"""UnixStamp"" INT DEFAULT 0",
                                                    @"""LastFinished"" INT DEFAULT 0",
                                                    @"""TimesFinished"" INT DEFAULT 0",
                                                    @"""Style"" INT DEFAULT 0"
                                                ];
                    playerStats = [         @"""SteamID"" VARCHAR(20) DEFAULT ''",
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
                    break;
                default:
                    playerRecords = null;
                    playerStats = null;
                    SharpTimerError($"Database type not supported");
                    break;
            }
            using (var connection = await OpenConnectionAsync(await GetConnectionStringFromConfigFile()))
            {
                try
                {
                    // Check PlayerRecords
                    SharpTimerDebug($"Checking PlayerRecords Table...");
                    await CreatePlayerRecordsTableAsync(connection);
                    await UpdateTableColumnsAsync(connection, "PlayerRecords", playerRecords);
                    await AddConstraintsToRecordsTableAsync(connection, "PlayerRecords");

                    // Check PlayerStats
                    SharpTimerDebug($"Checking PlayerStats Table...");
                    await CreatePlayerStatsTableAsync(connection);
                    await UpdateTableColumnsAsync(connection, "PlayerStats", playerStats);
                }
                catch (Exception ex)
                {
                    SharpTimerError($"Error in CheckTablesAsync: {ex}");
                }
            }
        }

        private async Task CreatePlayerRecordsTableAsync(IDbConnection connection)
        {
            DbCommand createTableCommand;
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
                default:
                    createTableCommand = null;
                    break;
            }
            using (createTableCommand)
            {
                try
                {
                    await createTableCommand.ExecuteNonQueryAsync();
                }
                catch (Exception ex)
                {
                    SharpTimerError($"Error in CreatePlayerRecordsTableAsync: {ex.Message}");
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
                        SharpTimerDebug($"Adding column {columnName} to {tableName}...");
                        await AddColumnToTableAsync(connection, tableName, columnDefinition);
                    }
                }
            }
        }

        private async Task<bool> TableExistsAsync(IDbConnection connection, string tableName)
        {
            DbCommand command;
            string query;
            switch (dbType)
            {
                case DatabaseType.MySQL:
                    query = $"SELECT COUNT(*) FROM information_schema.tables WHERE table_schema = '{connection.Database}' AND table_name = '{tableName}'";
                    command = new MySqlCommand(query, (MySqlConnection)connection);
                    break;
                case DatabaseType.PostgreSQL:
                    query = $@"SELECT COUNT(*) FROM information_schema.tables WHERE table_schema = 'public' AND table_name = '""{tableName}""'";
                    command = new NpgsqlCommand(query, (NpgsqlConnection)connection);
                    break;
                default:
                    command = null;
                    break;
            }
            using (command)
            {
                try
                {
                    int count = Convert.ToInt32(await command.ExecuteScalarAsync());
                    return count > 0;
                }
                catch (Exception ex)
                {
                    SharpTimerError($"Error in TableExistsAsync: {ex.Message}");
                    return false;
                }
            }
        }

        private async Task<bool> ColumnExistsAsync(IDbConnection connection, string tableName, string columnName)
        {
            DbCommand command;
            string query;
            switch (dbType)
            {
                case DatabaseType.MySQL:
                    query = $"SELECT COUNT(*) FROM information_schema.columns WHERE table_schema = '{connection.Database}' AND table_name = '{tableName}' AND column_name = '{columnName}'";
                    command = new MySqlCommand(query, (MySqlConnection)connection);
                    break;
                case DatabaseType.PostgreSQL:
                    query = query = $@"SELECT COUNT(*) FROM information_schema.columns WHERE table_schema = 'public' AND table_name = '{tableName}' AND column_name = '{columnName}'";
                    command = new NpgsqlCommand(query, (NpgsqlConnection)connection);
                    break;
                default:
                    command = null;
                    break;
            }
            using (command)
            {
                try
                {
                    int count = Convert.ToInt32(await command.ExecuteScalarAsync());
                    return count > 0;
                }
                catch (Exception ex)
                {
                    SharpTimerError($"Error in ColumnExistsAsync: {ex.Message}");
                    return false;
                }
            }
        }

        private async Task AddColumnToTableAsync(IDbConnection connection, string tableName, string columnDefinition)
        {
            DbCommand command;
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
                default:
                    command = null;
                    break;
            }
            using (command)
            {
                try
                {
                    await command.ExecuteNonQueryAsync();
                }
                catch (Exception ex)
                {
                    SharpTimerError($"Error in AddColumnToTableAsync: {ex.Message}");
                }
            }
        }

        private async Task AddConstraintsToRecordsTableAsync(IDbConnection connection, string tableName)
        {
            DbCommand dropCommand;
            DbCommand command;
            string dropQuery;
            string query;
            switch (dbType)
            {
                case DatabaseType.MySQL:
                    dropQuery = $"ALTER TABLE {tableName} DROP PRIMARY KEY";
                    query = $"ALTER TABLE {tableName} ADD CONSTRAINT pk_Records PRIMARY KEY (MapName, SteamID, Style)";
                    dropCommand = new MySqlCommand(dropQuery, (MySqlConnection)connection);
                    command = new MySqlCommand(query, (MySqlConnection)connection);
                    break;
                case DatabaseType.PostgreSQL:
                    dropQuery = $"ALTER TABLE {tableName} DROP CONSTRAINT pk_Records";
                    query = $@"ALTER TABLE ""{tableName}"" ADD CONSTRAINT pk_Records PRIMARY KEY (""MapName"", ""SteamID"", ""Style"")";
                    dropCommand = new NpgsqlCommand(dropQuery, (NpgsqlConnection)connection);
                    command = new NpgsqlCommand(query, (NpgsqlConnection)connection);
                    break;
                default:
                    dropCommand = null;
                    command = null;
                    break;
            }
            using (dropCommand)
            {
                try
                {
                    await command.ExecuteNonQueryAsync();
                }
                catch (Exception ex)
                {
                    SharpTimerError($"Error removing previous constraints: {ex.Message}");
                }
            }
            using (command)
            {
                try
                {
                    await command.ExecuteNonQueryAsync();
                }
                catch (Exception ex)
                {
                    SharpTimerError($"Error setting constraints: {ex.Message}");
                }
            }
        }

        private async Task CreatePlayerStatsTableAsync(IDbConnection connection)
        {
            DbCommand command;
            string query;
            switch (dbType)
            {
                case DatabaseType.MySQL:
                    query = @"CREATE TABLE IF NOT EXISTS PlayerStats (
                                            SteamID VARCHAR(20),
                                            PlayerName VARCHAR(32) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci,
                                            TimesConnected INT,
                                            LastConnected INT,
                                            GlobalPoints INT,
                                            HideTimerHud BOOL,
                                            HideKeys BOOL,
                                            HideJS BOOL,
                                            SoundsEnabled BOOL,
                                            PlayerFov INT,
                                            IsVip BOOL,
                                            BigGifID VARCHAR(16),
                                            PRIMARY KEY (SteamID)
                                        )";
                    command = new MySqlCommand(query, (MySqlConnection)connection);
                    break;
                case DatabaseType.PostgreSQL:
                    query = @"CREATE TABLE IF NOT EXISTS ""PlayerStats"" (
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
                    command = new NpgsqlCommand(query, (NpgsqlConnection)connection);
                    break;
                default:
                    command = null;
                    break;
            }
            using (command)
            {
                try
                {
                    await command.ExecuteNonQueryAsync();
                }
                catch (Exception ex)
                {
                    SharpTimerError($"Error in CreatePlayerStatsTableAsync: {ex.Message}");
                }
            }
        }

        public async Task SavePlayerTimeToDatabase(CCSPlayerController? player, int timerTicks, string steamId, string playerName, int playerSlot, int bonusX = 0, int style = 0)
        {
            if (usePostgres)
            {
                SharpTimerDebug($"Trying to save player {(bonusX != 0 ? $"bonus {bonusX} time" : "time")} to Postgres for {playerName} {timerTicks}");
                try
                {
                    if (!IsAllowedPlayer(player)) return;
                    //if ((bonusX == 0 && !playerTimers[playerSlot].IsTimerRunning) || (bonusX != 0 && !playerTimers[playerSlot].IsBonusTimerRunning)) return;
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

                    using (var connection = await OpenConnectionAsync(dbPath))
                    {
                        await CreatePlayerRecordsTableAsync(connection);

                        string formattedTime = FormatTime(timerTicks);
                        string selectQuery;
                        DbCommand selectCommand;
                        switch (dbType)
                        {
                            case DatabaseType.MySQL:
                                selectQuery = @"SELECT TimesFinished, LastFinished, FormattedTime, TimerTicks, UnixStamp FROM PlayerRecords WHERE MapName = @MapName AND SteamID = @SteamID AND Style = @Style";
                                selectCommand = new MySqlCommand(selectQuery, (MySqlConnection)connection);
                                break;
                            case DatabaseType.PostgreSQL:
                                selectQuery = @"SELECT ""TimesFinished"", ""LastFinished"", ""FormattedTime"", ""TimerTicks"", ""UnixStamp"" FROM ""PlayerRecords"" WHERE ""MapName"" = @MapName AND ""SteamID"" = @SteamID AND ""Style"" = @Style";
                                selectCommand = new NpgsqlCommand(selectQuery, (NpgsqlConnection)connection);
                                break;
                            default:
                                selectQuery = null;
                                selectCommand = null;
                                break;
                        }
                        // Check if the record already exists or has a higher timer value
                        selectCommand.AddParameterWithValue("@MapName", currentMapNamee);
                        selectCommand.AddParameterWithValue("@SteamID", steamId);
                        selectCommand.AddParameterWithValue("@SteamID", style);

                        var row = await selectCommand.ExecuteReaderAsync();

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
                                if (playerPoints < 32)
                                {
                                    beatPB = false;
                                    playerPoints = 320000;
                                }
                                if (enableReplays == true && usePostgres == true) _ = Task.Run(async () => await DumpReplayToJson(player!, steamId, playerSlot, bonusX, playerTimers[playerSlot].currentStyle));
                            }
                            else
                            {
                                new_dBtimerTicks = dBtimerTicks;
                                beatPB = false;
                                playerPoints = 320000;
                            }

                            await row.CloseAsync();
                            // Update or insert the record
                            string upsertQuery;
                            DbCommand upsertCommand;
                            switch (dbType)
                            {
                                case DatabaseType.MySQL:
                                    upsertQuery = @"
                                                    INSERT INTO PlayerRecords 
                                                    (MapName, SteamID, PlayerName, TimerTicks, LastFinished, TimesFinished, FormattedTime, UnixStamp, Style)
                                                    VALUES 
                                                    (@MapName, @SteamID, @PlayerName, @TimerTicks, @LastFinished, @TimesFinished, @FormattedTime, @UnixStamp, @Style)
                                                    ON DUPLICATE KEY UPDATE
                                                    MapName = VALUES(MapName),
                                                    PlayerName = VALUES(PlayerName),
                                                    TimerTicks = VALUES(TimerTicks),
                                                    LastFinished = VALUES(LastFinished),
                                                    TimesFinished = VALUES(TimesFinished),
                                                    FormattedTime = VALUES(FormattedTime),
                                                    UnixStamp = VALUES(UnixStamp),
                                                    Style = VALUES(Style);
                                                    ";
                                    upsertCommand = new MySqlCommand(upsertQuery, (MySqlConnection)connection);
                                    break;
                                case DatabaseType.PostgreSQL:
                                    upsertQuery = @"
                                                    INSERT INTO ""PlayerRecords"" 
                                                    (""MapName"", ""SteamID"", ""PlayerName"", ""TimerTicks"", ""LastFinished"", ""TimesFinished"", ""FormattedTime"", ""UnixStamp"", ""Style"")
                                                    VALUES 
                                                    (@MapName, @SteamID, @PlayerName, @TimerTicks, @LastFinished, @TimesFinished, @FormattedTime, @UnixStamp, @Style)
                                                    ON CONFLICT (""MapName"", ""SteamID"", ""Style"")
                                                    DO UPDATE SET
                                                    ""MapName"" = EXCLUDED.""MapName"",
                                                    ""PlayerName"" = EXCLUDED.""PlayerName"",
                                                    ""TimerTicks"" = EXCLUDED.""TimerTicks"",
                                                    ""LastFinished"" = EXCLUDED.""LastFinished"",
                                                    ""TimesFinished"" = EXCLUDED.""TimesFinished"",
                                                    ""FormattedTime"" = EXCLUDED.""FormattedTime"",
                                                    ""UnixStamp"" = EXCLUDED.""UnixStamp"",
                                                    ""Style"" = EXCLUDED.""Style"";
                                                    ";
                                    upsertCommand = new NpgsqlCommand(upsertQuery, (NpgsqlConnection)connection);
                                    break;
                                default:
                                    upsertQuery = null;
                                    upsertCommand = null;
                                    break;
                            }
                            using (upsertCommand)
                            {
                                upsertCommand.AddParameterWithValue("@MapName", currentMapNamee);
                                upsertCommand.AddParameterWithValue("@PlayerName", playerName);
                                upsertCommand.AddParameterWithValue("@TimesFinished", dBtimesFinished);
                                upsertCommand.AddParameterWithValue("@LastFinished", dBlastFinished);
                                upsertCommand.AddParameterWithValue("@TimerTicks", new_dBtimerTicks);
                                upsertCommand.AddParameterWithValue("@FormattedTime", dBFormattedTime);
                                upsertCommand.AddParameterWithValue("@UnixStamp", dBunixStamp);
                                upsertCommand.AddParameterWithValue("@SteamID", steamId);
                                upsertCommand.AddParameterWithValue("@Style", style);
                                if (usePostgres == true && globalRanksEnabled == true && ((dBtimesFinished <= maxGlobalFreePoints && globalRanksFreePointsEnabled == true) || beatPB)) await SavePlayerPoints(steamId, playerName, playerSlot, playerPoints, dBtimerTicks, beatPB, bonusX, style);
                                if ((stageTriggerCount != 0 || cpTriggerCount != 0) && bonusX == 0 && useMySQL == true && timerTicks < dBtimerTicks) Server.NextFrame(() => _ = Task.Run(async () => await DumpPlayerStageTimesToJson(player, steamId, playerSlot)));
                                await upsertCommand.ExecuteNonQueryAsync();
                                Server.NextFrame(() => SharpTimerDebug($"Saved player {(bonusX != 0 ? $"bonus {bonusX} time" : "time")} to Postgres for {playerName} {timerTicks} {DateTimeOffset.UtcNow.ToUnixTimeSeconds()}"));
                                if (usePostgres == true && IsAllowedPlayer(player)) await RankCommandHandler(player, steamId, playerSlot, playerName, true, style);
                                if (IsAllowedPlayer(player)) Server.NextFrame(() => _ = Task.Run(async () => await PrintMapTimeToChat(player!, steamId, playerName, dBtimerTicks, timerTicks, bonusX, dBtimesFinished, style)));
                            }

                        }
                        else
                        {
                            Server.NextFrame(() => SharpTimerDebug($"No player record yet"));
                            if (enableReplays == true && usePostgres == true) _ = Task.Run(async () => await DumpReplayToJson(player!, steamId, playerSlot, bonusX, playerTimers[playerSlot].currentStyle));
                            await row.CloseAsync();

                            string upsertQuery;
                            DbCommand upsertCommand;
                            switch (dbType)
                            {
                                case DatabaseType.MySQL:
                                    upsertQuery = @"REPLACE INTO PlayerRecords (MapName, SteamID, PlayerName, TimerTicks, LastFinished, TimesFinished, FormattedTime, UnixStamp, Style) VALUES (@MapName, @SteamID, @PlayerName, @TimerTicks, @LastFinished, @TimesFinished, @FormattedTime, @UnixStamp, @Style)";
                                    upsertCommand = new MySqlCommand(upsertQuery, (MySqlConnection)connection);
                                    break;
                                case DatabaseType.PostgreSQL:
                                    upsertQuery = @"INSERT INTO ""PlayerRecords"" (""MapName"", ""SteamID"", ""PlayerName"", ""TimerTicks"", ""LastFinished"", ""TimesFinished"", ""FormattedTime"", ""UnixStamp"", ""Style"") VALUES (@MapName, @SteamID, @PlayerName, @TimerTicks, @LastFinished, @TimesFinished, @FormattedTime, @UnixStamp, @Style)";
                                    upsertCommand = new NpgsqlCommand(upsertQuery, (NpgsqlConnection)connection);
                                    break;
                                default:
                                    upsertQuery = null;
                                    upsertCommand = null;
                                    break;
                            }

                            using (upsertCommand)
                            {
                                upsertCommand.AddParameterWithValue("@MapName", currentMapNamee);
                                upsertCommand.AddParameterWithValue("@PlayerName", playerName);
                                upsertCommand.AddParameterWithValue("@TimesFinished", 1);
                                upsertCommand.AddParameterWithValue("@LastFinished", timeNowUnix);
                                upsertCommand.AddParameterWithValue("@TimerTicks", timerTicks);
                                upsertCommand.AddParameterWithValue("@FormattedTime", formattedTime);
                                upsertCommand.AddParameterWithValue("@UnixStamp", timeNowUnix);
                                upsertCommand.AddParameterWithValue("@SteamID", steamId);
                                upsertCommand.AddParameterWithValue("@Style", style);
                                await upsertCommand.ExecuteNonQueryAsync();
                                if (globalRanksEnabled == true) await SavePlayerPoints(steamId, playerName, playerSlot, timerTicks, dBtimerTicks, beatPB, bonusX, style);
                                if ((stageTriggerCount != 0 || cpTriggerCount != 0) && bonusX == 0) Server.NextFrame(() => _ = Task.Run(async () => await DumpPlayerStageTimesToJson(player, steamId, playerSlot)));
                                Server.NextFrame(() => SharpTimerDebug($"Saved player {(bonusX != 0 ? $"bonus {bonusX} time" : "time")} to Postgres for {playerName} {timerTicks} {DateTimeOffset.UtcNow.ToUnixTimeSeconds()}"));
                                if (IsAllowedPlayer(player)) await RankCommandHandler(player, steamId, playerSlot, playerName, true, style);
                                if (IsAllowedPlayer(player)) Server.NextFrame(() => _ = Task.Run(async () => await PrintMapTimeToChat(player!, steamId, playerName, dBtimerTicks, timerTicks, bonusX, 1, style)));
                            }

                        }
                    }
                }
                catch (Exception ex)
                {
                    Server.NextFrame(() => SharpTimerError($"Error saving player {(bonusX != 0 ? $"bonus {bonusX} time" : "time")} to Postgres: {ex.Message}"));
                }
            }
        }
    }
}