using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Modules.Timers;
using FixVectorLeak;
using System.Text.Json;

namespace SharpTimer;

public partial class SharpTimer
{
    private void OnMapStartHandler(string mapName)
    {
        try
        {
            Server.NextFrame(() =>
            {
                Utils.LogDebug("OnMapStart:");
                Utils.LogDebug("Executing SharpTimer/config");
                Server.ExecuteCommand("sv_autoexec_mapname_cfg 0");
                Server.ExecuteCommand($"execifexists SharpTimer/config.cfg");

                //delay custom_exec so it executes after map exec
                Utils.LogDebug("Creating custom_exec 1sec delay");
                var custom_exec_delay = AddTimer(1.0f, () =>
                {
                    Utils.LogDebug("Re-Executing SharpTimer/custom_exec");
                    Server.ExecuteCommand("execifexists SharpTimer/custom_exec.cfg");

                    if (execCustomMapCFG == true)
                    {
                        string MapExecFile = Utils.GetClosestMapCFGMatch();
                        if (!string.IsNullOrEmpty(MapExecFile))
                            Server.ExecuteCommand($"execifexists SharpTimer/MapData/MapExecs/{MapExecFile}");
                        else
                            Utils.LogError("MapExec Error: file name returned null");
                    }
                });

                if (removeCrouchFatigueEnabled == true) Server.ExecuteCommand("sv_timebetweenducks 0");

                //bonusRespawnPoses.Clear();
                bonusRespawnAngs.Clear();

                cpTriggers.Clear();         // make sure old data is flushed in case new map uses fake zones
                cpTriggerCount = 0;
                bonusCheckpointTriggers.Clear();
                stageTriggers.Clear();
                stageTriggerAngs.Clear();
                stageTriggerPoses.Clear();

                Utils.KillServerCommandEnts();
                globalDisabled = false;

                if (!sqlCheck)
                {
                    if (useMySQL)
                    {
                        string mysqlConfigFileName = "SharpTimer/mysqlConfig.json";
                        mySQLpath = Path.Join(gameDir + "/csgo/cfg", mysqlConfigFileName);
                        Utils.LogDebug($"Set mySQLpath to {mySQLpath}");
                        dbType = DatabaseType.MySQL;
                        dbPath = mySQLpath;
                        enableDb = true;
                    }
                    else if (usePostgres)
                    {
                        string postgresConfigFileName = "SharpTimer/postgresConfig.json";
                        postgresPath = Path.Join(gameDir + "/csgo/cfg", postgresConfigFileName);
                        Utils.LogDebug($"Set postgresPath to {postgresPath}");
                        dbType = DatabaseType.PostgreSQL;
                        dbPath = postgresPath;
                        enableDb = true;
                    }
                    else
                    {
                        Utils.LogDebug($"No db set, defaulting to SQLite");
                        dbPath = Path.Join(gameDir + "/csgo/cfg", "SharpTimer/database.db");
                        dbType = DatabaseType.SQLite;
                        enableDb = true;
                    }
                    using (var connection = OpenConnection())
                    {
                        _ = CheckTablesAsync();
                        ExecuteMigrations(connection);
                    }
                    sqlCheck = true;
                }

                if (Directory.Exists($"{gameDir}/addons/StripperCS2/maps/{Server.MapName}"))
                {
                    globalDisabled = true;
                    Utils.LogError("StripperCS2 detected for current map; disabling globalapi");
                }

                if (currentMapOverrideDisableTelehop!.Length > 0)
                {
                    globalDisabled = true;
                    Utils.LogError("OverrideDisableTelehop detected for current map; disabling globalapi");
                }

                _ = Task.Run(async () => await CacheWorldRecords());
                AddTimer(globalCacheInterval, async () => await CacheWorldRecords(), TimerFlags.REPEAT);

                _ = Task.Run(async () => await CacheGlobalPoints());
                AddTimer(globalCacheInterval, async () => await CacheGlobalPoints(), TimerFlags.REPEAT);
            });
        }
        catch (Exception ex)
        {
            Utils.LogError($"In OnMapStartHandler: {ex}");
        }
    }

    private void LoadMapData(string mapName)
    {
        try
        {
            currentMapName = mapName;
            currentAddonID = GetAddonID();
            totalBonuses = new int[11];
            bonusRespawnPoses.Clear();
            bonusRespawnAngs.Clear();
            string recordsFileName = $"SharpTimer/PlayerRecords/";
            playerRecordsPath = Path.Join(gameDir + "/csgo/cfg", recordsFileName);

            string mysqlConfigFileName = "SharpTimer/mysqlConfig.json";
            mySQLpath = Path.Join(gameDir + "/csgo/cfg", mysqlConfigFileName);

            string postgresConfigFileName = "SharpTimer/postgresConfig.json";
            postgresPath = Path.Join(gameDir + "/csgo/cfg", postgresConfigFileName);

            string discordConfigFileName = "SharpTimer/discordConfig.json";
            string discordCFGpath = Path.Join(gameDir + "/csgo/cfg", discordConfigFileName);

            string ranksFileName = $"SharpTimer/ranks.json";
            string ranksPath = Path.Join(gameDir + "/csgo/cfg", ranksFileName);

            string mapdataFileName = $"SharpTimer/MapData/{currentMapName}.json";
            string mapdataPath = Path.Join(gameDir + "/csgo/cfg", mapdataFileName);

            string bonusdataPath = $"{gameDir}/csgo/cfg/SharpTimer/MapData/";
            string[] files = Directory.GetFiles(bonusdataPath);

            string[] bonusdataFileNames = new string[11];
            string[] bonusdataPaths = new string[11];

            foreach (string file in files)
            {
                if (file.Contains($"{currentMapName}_bonus1"))
                {
                    totalBonuses[1] = 1;
                    bonusdataFileNames[1] = $"/SharpTimer/MapData/{currentMapName}_bonus1.json";
                    bonusdataPaths[1] = Path.Join(gameDir + "/csgo/cfg", bonusdataFileNames[1]);
                }
                else if (file.Contains($"{currentMapName}_bonus2"))
                {
                    totalBonuses[2] = 2;
                    bonusdataFileNames[2] = $"/SharpTimer/MapData/{currentMapName}_bonus2.json";
                    bonusdataPaths[2] = Path.Join(gameDir + "/csgo/cfg", bonusdataFileNames[2]);
                }
                else if (file.Contains($"{currentMapName}_bonus3"))
                {
                    totalBonuses[3] = 3;
                    bonusdataFileNames[3] = $"/SharpTimer/MapData/{currentMapName}_bonus3.json";
                    bonusdataPaths[3] = Path.Join(gameDir + "/csgo/cfg", bonusdataFileNames[3]);
                }
                else if (file.Contains($"{currentMapName}_bonus4"))
                {
                    totalBonuses[4] = 4;
                    bonusdataFileNames[4] = $"/SharpTimer/MapData/{currentMapName}_bonus4.json";
                    bonusdataPaths[4] = Path.Join(gameDir + "/csgo/cfg", bonusdataFileNames[4]);
                }
                else if (file.Contains($"{currentMapName}_bonus5"))
                {
                    totalBonuses[5] = 5;
                    bonusdataFileNames[5] = $"/SharpTimer/MapData/{currentMapName}_bonus5.json";
                    bonusdataPaths[5] = Path.Join(gameDir + "/csgo/cfg", bonusdataFileNames[5]);
                }
                else if (file.Contains($"{currentMapName}_bonus6"))
                {
                    totalBonuses[6] = 6;
                    bonusdataFileNames[6] = $"/SharpTimer/MapData/{currentMapName}_bonus6.json";
                    bonusdataPaths[6] = Path.Join(gameDir + "/csgo/cfg", bonusdataFileNames[6]);
                }
                else if (file.Contains($"{currentMapName}_bonus7"))
                {
                    totalBonuses[7] = 7;
                    bonusdataFileNames[7] = $"/SharpTimer/MapData/{currentMapName}_bonus7.json";
                    bonusdataPaths[7] = Path.Join(gameDir + "/csgo/cfg", bonusdataFileNames[7]);
                }
                else if (file.Contains($"{currentMapName}_bonus8"))
                {
                    totalBonuses[8] = 8;
                    bonusdataFileNames[8] = $"/SharpTimer/MapData/{currentMapName}_bonus8.json";
                    bonusdataPaths[8] = Path.Join(gameDir + "/csgo/cfg", bonusdataFileNames[8]);
                }
                else if (file.Contains($"{currentMapName}_bonus9"))
                {
                    totalBonuses[9] = 9;
                    bonusdataFileNames[9] = $"/SharpTimer/MapData/{currentMapName}_bonus9.json";
                    bonusdataPaths[9] = Path.Join(gameDir + "/csgo/cfg", bonusdataFileNames[9]);
                }
                else if (file.Contains($"{currentMapName}_bonus10"))
                {
                    totalBonuses[10] = 10;
                    bonusdataFileNames[10] = $"/SharpTimer/MapData/{currentMapName}_bonus10.json";
                    bonusdataPaths[10] = Path.Join(gameDir + "/csgo/cfg", bonusdataFileNames[10]);
                }
            }
            Server.ExecuteCommand($"execifexists SharpTimer/config.cfg");
            Utils.LogDebug("Re-Executing custom_exec with 1sec delay...");
            var custom_exec_delay = AddTimer(1.0f, () =>
            {
                Utils.LogDebug("Re-Executing SharpTimer/custom_exec");
                Server.ExecuteCommand("execifexists SharpTimer/custom_exec.cfg");

                if (execCustomMapCFG == true)
                {
                    string MapExecFile = Utils.GetClosestMapCFGMatch();
                    if (!string.IsNullOrEmpty(MapExecFile))
                        Server.ExecuteCommand($"execifexists SharpTimer/MapData/MapExecs/{MapExecFile}");
                    else
                        Utils.LogError("MapExec Error: file name returned null");
                }
            });

            if (adServerRecordEnabled == true) ADtimerServerRecord();
            if (adMessagesEnabled == true) ADtimerMessages();

            if (enableReplays && enableSRreplayBot)
            {
                replayBotController = null;
                Server.ExecuteCommand("bot_quota_mode fill");
                Server.ExecuteCommand("bot_quota 1");
                Server.ExecuteCommand("bot_chatter off");
                Server.ExecuteCommand("bot_controllable 0");
                Server.ExecuteCommand("bot_join_after_player 0");
                Server.ExecuteCommand("bot_kick");
            }

            entityCache = new EntityCache();
            UpdateEntityCache();

            ClearMapData();

            _ = Task.Run(Utils.GetMapInfo);

            _ = Task.Run(async () => await GetDiscordWebhookURLFromConfigFile(discordCFGpath));

            primaryChatColor = Utils.ParseColorToSymbol(primaryHUDcolor);

            using JsonDocument? ranksJsonDocument = Utils.LoadJsonOnMainThread(ranksPath);
            if (ranksJsonDocument != null)
            {
                Utils.LogDebug($"Ranks json found!");
                JsonElement root = ranksJsonDocument.RootElement;

                rankDataList.Clear();

                foreach (var property in root.EnumerateObject())
                {
                    JsonElement rankElement = property.Value;
                    RankData rankData = new RankData
                    {
                        Title = rankElement.TryGetProperty("title", out JsonElement titleElement) ? titleElement.GetString()! : string.Empty,
                        Percent = rankElement.TryGetProperty("percent", out var percentElement) ? percentElement.GetDouble() : 0,
                        Placement = rankElement.TryGetProperty("placement", out var placementElement) ? placementElement.GetInt32() : 0,
                        Color = rankElement.TryGetProperty("color", out JsonElement colorElement) ? colorElement.GetString()! : string.Empty,
                        Icon = rankElement.TryGetProperty("icon", out JsonElement iconElement) ? iconElement.GetString()! : string.Empty,
                    };

                    rankDataList.Add(rankData);

                    if (property.Name.ToLower() == "unranked")
                    {
                        UnrankedTitle = rankElement.GetProperty("title").GetString()!;
                        UnrankedColor = rankElement.GetProperty("color").GetString()!;
                        UnrankedIcon = rankElement.GetProperty("icon").GetString()!;
                    }
                }

                rankDataList = rankDataList
                    .OrderBy(r => r.Placement > 0 ? 0 : 1)  // Placement > 0 should come first
                    .ThenBy(r => r.Placement)               // sort Placement (low to high)
                    .ThenBy(r => r.Percent)                 // sort Percent (low to high)
                    .ToList();

                foreach (var xd in rankDataList)
                {
                    Utils.LogDebug(xd.Title + xd.Percent + xd.Placement);
                }
            }
            else
            {
                Utils.LogError($"Ranks json was null");
            }

            Utils.LogDebug($"Trying to find Map data json for map: {currentMapName}!");
            //Bonus fake zone check
            foreach (int bonus in totalBonuses)
            {
                if (bonus == 0) { }
                else
                {
                    using JsonDocument? bonusJsonDocument = Utils.LoadJsonOnMainThread(bonusdataPaths[bonus]);
                    if (bonusJsonDocument != null)
                    {
                        var mapInfo = JsonSerializer.Deserialize<MapInfo>(bonusJsonDocument.RootElement.GetRawText(), jsonSerializerOptions);
                        Utils.LogDebug($"Map data json found for map: {currentMapName}, bonus {bonus}!");

                        if (!string.IsNullOrEmpty(mapInfo!.BonusStartC1) && !string.IsNullOrEmpty(mapInfo.BonusStartC2) && !string.IsNullOrEmpty(mapInfo.BonusEndC1) && !string.IsNullOrEmpty(mapInfo.BonusEndC2) && !string.IsNullOrEmpty(mapInfo.BonusRespawnPos))
                        {
                            useTriggers = false;
                            if (FindEndTriggerPos() != null)
                                useTriggersAndFakeZones = true;
                            Utils.LogDebug($"useTriggers: {useTriggers}!");
                            currentBonusStartC1[bonus] = Utils.ParseVector_t(mapInfo.BonusStartC1);
                            currentBonusStartC2[bonus] = Utils.ParseVector_t(mapInfo.BonusStartC2);
                            currentBonusEndC1[bonus] = Utils.ParseVector_t(mapInfo.BonusEndC1);
                            currentBonusEndC2[bonus] = Utils.ParseVector_t(mapInfo.BonusEndC2);
                            currentBonusEndPos[bonus] = Utils.CalculateMiddleVector_t(currentBonusEndC1[bonus]!.Value, currentBonusEndC2[bonus]!.Value);
                            bonusRespawnPoses[bonus] = Utils.ParseVector_t(mapInfo.BonusRespawnPos);
                            Utils.LogDebug($"Found Fake Bonus {bonus} Trigger Corners: START {currentBonusStartC1[bonus]}, {currentBonusStartC2[bonus]} | END {currentBonusEndC1[bonus]}, {currentBonusEndC2[bonus]}");

                            // Disable global for lackluster maps
                            globalDisabled = true;
                        }
                        if (currentBonusStartC1[bonus] != null && currentBonusStartC2[bonus] != null && currentBonusEndC1[bonus] != null && currentBonusEndC2[bonus] != null)
                        {
                            Utils.DrawWireframe3D(currentBonusStartC1[bonus]!.Value, currentBonusStartC2[bonus]!.Value, startBeamColor);
                            Utils.DrawWireframe3D(currentBonusEndC1[bonus]!.Value, currentBonusEndC2[bonus]!.Value, endBeamColor);
                        }
                    }
                }
            }
            //Main fake zone check
            using JsonDocument? jsonDocument = Utils.LoadJsonOnMainThread(mapdataPath);
            if (jsonDocument != null)
            {
                var mapInfo = JsonSerializer.Deserialize<MapInfo>(jsonDocument.RootElement.GetRawText());
                Utils.LogDebug($"Map data json found for map: {currentMapName}!");

                if (!string.IsNullOrEmpty(mapInfo!.MapStartC1) && !string.IsNullOrEmpty(mapInfo.MapStartC2) && !string.IsNullOrEmpty(mapInfo.MapEndC1) && !string.IsNullOrEmpty(mapInfo.MapEndC2))
                {
                    useTriggers = false;
                    Utils.LogDebug($"useTriggers: {useTriggers}!");
                    currentMapStartC1 = Utils.ParseVector_t(mapInfo.MapStartC1);
                    currentMapStartC2 = Utils.ParseVector_t(mapInfo.MapStartC2);
                    currentMapEndC1 = Utils.ParseVector_t(mapInfo.MapEndC1);
                    currentMapEndC2 = Utils.ParseVector_t(mapInfo.MapEndC2);
                    currentEndPos = Utils.CalculateMiddleVector_t(currentMapEndC1!.Value, currentMapEndC2!.Value);
                    Utils.LogDebug($"Found Fake Trigger Corners: START {currentMapStartC1}, {currentMapStartC2} | END {currentMapEndC1}, {currentMapEndC2}");

                    // Disable global for lackluster maps
                    globalDisabled = true;
                }

                if (!string.IsNullOrEmpty(mapInfo.MapStartTrigger) && !string.IsNullOrEmpty(mapInfo.MapEndTrigger))
                {
                    currentMapStartTrigger = mapInfo.MapStartTrigger;
                    currentMapEndTrigger = mapInfo.MapEndTrigger;
                    useTriggers = true;
                    Utils.LogDebug($"Found Trigger Names: START {currentMapStartTrigger} | END {currentMapEndTrigger}");
                }

                if (!string.IsNullOrEmpty(mapInfo.RespawnPos))
                {
                    currentRespawnPos = Utils.ParseVector_t(mapInfo.RespawnPos);
                    Utils.LogDebug($"Found RespawnPos: {currentRespawnPos}");
                }
                else
                {
                    (currentRespawnPos, currentRespawnAng) = FindStartTriggerPos();
                    currentEndPos = FindEndTriggerPos();
                    FindBonusStartTriggerPos();
                    Utils.LogDebug($"RespawnPos not found, trying to hook trigger pos instead");
                    if (currentRespawnPos == null)
                    {
                        Utils.LogError($"Hooking Trigger RespawnPos Failed!");
                    }
                    else
                    {
                        Utils.LogDebug($"Hooking Trigger RespawnPos Success! {currentRespawnPos}");
                    }
                }

                if (mapInfo.OverrideDisableTelehop != null && mapInfo.OverrideDisableTelehop.Length != 0)
                {
                    try
                    {
                        currentMapOverrideDisableTelehop = mapInfo.OverrideDisableTelehop
                            .Split(',')
                            .Select(trigger => trigger.Trim())
                            .ToArray();

                        Utils.LogDebug($"Overriding OverrideDisableTelehop...");
                    }
                    catch (FormatException)
                    {
                        Utils.LogError("Invalid string format for OverrideDisableTelehop... Example: 's1_end, s2_end, s3_end, s4_end, s5_end, s6_end, s7_end, s8_end'");
                    }
                }
                else
                {
                    currentMapOverrideStageRequirement = false;
                }

                if (mapInfo.OverrideMaxSpeedLimit != null && mapInfo.OverrideMaxSpeedLimit.Length != 0)
                {
                    try
                    {
                        Utils.LogDebug($"Overriding MaxSpeedLimit...");
                        currentMapOverrideMaxSpeedLimit = mapInfo.OverrideMaxSpeedLimit
                            .Split(',')
                            .Select(trigger => trigger.Trim())
                            .ToArray();

                        foreach (var trigger in currentMapOverrideMaxSpeedLimit)
                        {
                            Utils.LogDebug($"OverrideMaxSpeedLimit for trigger: {trigger}");
                        }

                    }
                    catch (Exception ex)
                    {
                        Utils.LogError($"Error parsing OverrideMaxSpeedLimit array: {ex.Message}");
                    }
                }
                else
                {
                    currentMapOverrideMaxSpeedLimit = [];
                }

                if (!string.IsNullOrEmpty(mapInfo.OverrideStageRequirement))
                {
                    try
                    {
                        currentMapOverrideStageRequirement = bool.Parse(mapInfo.OverrideStageRequirement);
                        Utils.LogDebug($"Overriding StageRequirement...");
                    }
                    catch (FormatException)
                    {
                        Utils.LogError("Invalid boolean string format for OverrideStageRequirement");
                    }
                }
                else
                {
                    currentMapOverrideStageRequirement = false;
                }

                if (!string.IsNullOrEmpty(mapInfo.GlobalPointsMultiplier))
                {
                    try
                    {
                        globalPointsMultiplier = float.Parse(mapInfo.GlobalPointsMultiplier);
                        Utils.LogDebug($"Set global points multiplier to x{globalPointsMultiplier}");
                    }
                    catch (FormatException)
                    {
                        Utils.LogError("Invalid float string format for GlobalPointsMultiplier");
                    }
                }

                if (!string.IsNullOrEmpty(mapInfo.MapTier))
                {
                    AddTimer(10.0f, () => //making sure this happens after remote_data is fetched due to github being slow sometimes
                    {
                        try
                        {
                            currentMapTier = int.Parse(mapInfo.MapTier);
                            Utils.LogDebug($"Overriding MapTier to {currentMapTier}");
                        }
                        catch (FormatException)
                        {
                            Utils.LogError("Invalid int string format for MapTier");
                        }
                    });
                }

                if (!string.IsNullOrEmpty(mapInfo.MapType))
                {
                    AddTimer(10.0f, () => //making sure this happens after remote_data is fetched due to github being slow sometimes
                    {
                        try
                        {
                            currentMapType = mapInfo.MapType;
                            Utils.LogDebug($"Overriding MapType to {currentMapType}");
                        }
                        catch (FormatException)
                        {
                            Utils.LogError("Invalid string format for MapType");
                        }
                    });
                }

                if (useTriggers == false && currentMapStartC1 != null && currentMapStartC2 != null && currentMapEndC1 != null && currentMapEndC2 != null)
                {
                    Utils.DrawWireframe3D(currentMapStartC1.GetValueOrDefault(), currentMapStartC2.GetValueOrDefault(), startBeamColor);
                    Utils.DrawWireframe3D(currentMapEndC1.GetValueOrDefault(), currentMapEndC2.GetValueOrDefault(), endBeamColor);
                }
                else
                {
                    var (startRight, startLeft, endRight, endLeft) = FindTriggerBounds();

                    if (startRight == null || startLeft == null || endRight == null || endLeft == null) return;

                    Utils.DrawWireframe3D(startRight.GetValueOrDefault(), startLeft.GetValueOrDefault(), startBeamColor);
                    Utils.DrawWireframe3D(endRight.GetValueOrDefault(), endLeft.GetValueOrDefault(), endBeamColor);
                }

                if (useTriggers == true || useTriggersAndFakeZones == true)
                {
                    FindStageTriggers();
                    FindCheckpointTriggers();
                    FindBonusCheckpointTriggers();
                }

                Utils.KillServerCommandEnts();
            }
            else
            {
                Utils.ConPrint($"Map data json not found for map: {currentMapName}!");
                Utils.LogDebug($"Trying to hook Triggers supported by default!");

                Utils.KillServerCommandEnts();

                (currentRespawnPos, currentRespawnAng) = FindStartTriggerPos();
                currentEndPos = FindEndTriggerPos();
                FindBonusStartTriggerPos();
                FindStageTriggers();
                FindCheckpointTriggers();
                FindBonusCheckpointTriggers();

                if (currentRespawnPos == null)
                    Utils.LogError($"Hooking Trigger RespawnPos Failed!");
                else
                    Utils.LogDebug($"Hooking Trigger RespawnPos Success! {currentRespawnPos}");

                if (useTriggers == false && currentMapStartC1 != null && currentMapStartC2 != null && currentMapEndC1 != null && currentMapEndC2 != null && useTriggersAndFakeZones == false)
                {
                    Utils.DrawWireframe3D(currentMapStartC1.GetValueOrDefault(), currentMapStartC2.GetValueOrDefault(), startBeamColor);
                    Utils.DrawWireframe3D(currentMapEndC1.GetValueOrDefault(), currentMapEndC2.GetValueOrDefault(), endBeamColor);
                }
                else
                {
                    var (startRight, startLeft, endRight, endLeft) = FindTriggerBounds();

                    if (startRight == null || startLeft == null || endRight == null || endLeft == null) return;

                    Utils.DrawWireframe3D(startRight.GetValueOrDefault(), startLeft.GetValueOrDefault(), startBeamColor);
                    Utils.DrawWireframe3D(endRight.GetValueOrDefault(), endLeft.GetValueOrDefault(), endBeamColor);
                }

                useTriggers = true;
            }

            Utils.LogDebug($"useTriggers: {useTriggers}!");
        }
        catch (Exception ex)
        {
            Utils.LogError($"Error in LoadMapData: {ex.Message}");
        }
    }

    public void ClearMapData()
    {
        cpTriggers.Clear();
        cpTriggerCount = 0;
        bonusCheckpointTriggers.Clear();
        stageTriggers.Clear();
        stageTriggerAngs.Clear();
        stageTriggerPoses.Clear();

        stageTriggerCount = 0;
        useStageTriggers = false;

        useTriggers = true;
        useTriggersAndFakeZones = false;

        currentMapStartC1 = new Vector_t(nint.Zero);
        currentMapStartC2 = new Vector_t(nint.Zero);
        currentMapEndC1 = new Vector_t(nint.Zero);
        currentMapEndC2 = new Vector_t(nint.Zero);

        currentRespawnPos = null;
        currentRespawnAng = null;

        currentEndPos = null;

        currentBonusStartC1 = new Vector_t?[10];
        currentBonusStartC2 = new Vector_t?[10];
        currentBonusEndC1 = new Vector_t?[10];
        currentBonusEndC2 = new Vector_t?[10];

        // totalBonuses = new int[10];
        currentMapStartTriggerMaxs = null;
        currentMapStartTriggerMins = null;

        currentMapTier = null; //making sure previous map tier and type are wiped
        currentMapType = null;
        currentMapOverrideDisableTelehop = []; //making sure previous map overrides are reset
        currentMapOverrideMaxSpeedLimit = [];
        currentMapOverrideStageRequirement = false;

        globalPointsMultiplier = 1.0f;
    }

    public async Task<(string, string, string)> GetMapRecordSteamID(int bonusX = 0, int top10 = 0)
    {
        string mapRecordsPath = Path.Combine(playerRecordsPath!, bonusX == 0 ? $"{currentMapName}.json" : $"{currentMapName}_bonus{bonusX}.json");

        Dictionary<string, PlayerRecord> records;

        try
        {
            using (JsonDocument? jsonDocument = await Utils.LoadJson(mapRecordsPath)!)
            {
                if (jsonDocument != null)
                {
                    records = JsonSerializer.Deserialize<Dictionary<string, PlayerRecord>>(jsonDocument.RootElement.GetRawText()) ?? [];
                }
                else
                {
                    records = [];
                }
            }
        }
        catch (Exception ex)
        {
            Utils.LogError($"Error in GetSortedRecords: {ex.Message}");
            records = [];
        }

        string steamId64 = "null";
        string playerName = "null";
        string timerTicks = "null";

        if (top10 != 0 && top10 <= records.Count)
        {
            var sortedRecords = records.OrderBy(x => x.Value.TimerTicks).ToList();
            var record = sortedRecords[top10 - 1];
            steamId64 = record.Key;
            playerName = record.Value.PlayerName!;
            timerTicks = Utils.FormatTime(record.Value.TimerTicks);
        }
        else
        {
            var minTimerTicksRecord = records.OrderBy(x => x.Value.TimerTicks).FirstOrDefault();
            if (minTimerTicksRecord.Key != null)
            {
                steamId64 = minTimerTicksRecord.Key;
                playerName = minTimerTicksRecord.Value.PlayerName!;
                timerTicks = Utils.FormatTime(minTimerTicksRecord.Value.TimerTicks);
            }
        }

        return (steamId64, playerName, timerTicks);
    }

    private void ADtimerServerRecord()
    {
        if (isADServerRecordTimerRunning) return;

        var timer = AddTimer(adServerRecordTimer, () =>
        {
            Task.Run(async () =>
            {
                Utils.LogDebug($"Getting Server Record AD using database");
                var sortedRecords = await GetSortedRecordsFromDatabase(100);

                Utils.LogDebug($"Running Server Record AD...");

                if (sortedRecords.Count == 0)
                {
                    Utils.LogDebug($"No Server Records for this map yet!");
                    return;
                }

                Server.NextFrame(() => Utils.PrintToChatAll(Localizer["current_sr", currentMapName!]));

                var serverRecord = sortedRecords.FirstOrDefault();
                string playerName = serverRecord.Value.PlayerName!; // Get the player name from the dictionary value
                int timerTicks = serverRecord.Value.TimerTicks; // Get the timer ticks from the dictionary value
                Server.NextFrame(() => Utils.PrintToChatAll(Localizer["current_sr_player", playerName, Utils.FormatTime(timerTicks)]));

                SortedCachedRecords = sortedRecords;
            });
        }, TimerFlags.REPEAT);

        isADServerRecordTimerRunning = true;
    }

    private void ADtimerMessages()
    {
        if (isADMessagesTimerRunning) return;

        var timer = AddTimer(adMessagesTimer, () =>
        {
            List<string> allAdMessages = [.. GetAdMessages()];

            var customAdMessageFilePath = Path.Join(gameDir + "/csgo/cfg/SharpTimer/admessages.txt");
            if (File.Exists(customAdMessageFilePath))
            {
                string[] customAdMessages = File.ReadAllLines(customAdMessageFilePath, System.Text.Encoding.UTF8);
                var nonEmptyCustomAds = customAdMessages.Where(ad => !string.IsNullOrEmpty(ad) && !ad.TrimStart().StartsWith("//")).ToList();

                allAdMessages.AddRange(nonEmptyCustomAds);
            }

            Server.NextFrame(() => Utils.PrintToChatAll($"{Utils.ReplaceVars(allAdMessages[new Random().Next(allAdMessages.Count)])}"));
        }, TimerFlags.REPEAT);

        isADMessagesTimerRunning = true;
    }

    private List<string> GetAdMessages()
    {
        var adMessages = new List<string>()
        {
            $"{Localizer["prefix"]} {Localizer["ad_see_all_commands"]}",
            $"{(enableReplays ? $"{Localizer["prefix"]} {Localizer["ad_replay_pb"]}" : "")}",
            $"{(enableReplays ? $"{Localizer["prefix"]} {Localizer["ad_replay_sr"]}" : "")}",
            $"{(enableReplays ? $"{Localizer["prefix"]} {Localizer["ad_replay_top"]}" : "")}",
            $"{(enableReplays ? $"{Localizer["prefix"]} {Localizer["ad_replay_bonus"]}" : "")}",
            $"{(enableReplays ? $"{Localizer["prefix"]} {Localizer["ad_replay_bonus_pb"]}" : "")}",
            $"{(globalRanksEnabled ? $"{Localizer["prefix"]} {Localizer["ad_points"]}" : "")}",
            $"{(respawnEnabled ? $"{Localizer["prefix"]} {Localizer["ad_respawn"]}" : "")}",
            $"{(respawnEnabled ? $"{Localizer["prefix"]} {Localizer["ad_start_pos"]}" : "")}",
            $"{(topEnabled ? $"{Localizer["prefix"]} {Localizer["ad_top"]}" : "")}",
            $"{(rankEnabled ? $"{Localizer["prefix"]} {Localizer["ad_rank"]}" : "")}",
            $"{(cpEnabled ? $"{Localizer["prefix"]} {(currentMapName!.Contains("surf_") ? $"{Localizer["ad_save_loc"]}" : $"{Localizer["ad_cp"]}")}" : "")}",
            $"{(cpEnabled ? $"{Localizer["prefix"]} {(currentMapName!.Contains("surf_") ? $"{Localizer["ad_load_loc"]}" : $"{Localizer["ad_tp"]}")}" : "")}",
            $"{(goToEnabled ? $"{Localizer["prefix"]} {Localizer["ad_goto"]}" : "")}",
            $"{(fovChangerEnabled ? $"{Localizer["prefix"]} {Localizer["ad_fov"]}" : "")}",
            $"{Localizer["prefix"]} {Localizer["ad_sounds"]}",
            $"{Localizer["prefix"]} {Localizer["ad_hud"]}",
            $"{Localizer["prefix"]} {Localizer["ad_keys"]}",
            $"{(enableStyles ? $"{Localizer["prefix"]} {Localizer["ad_styles"]}" : "")}",
        };

        return adMessages;
    }

}