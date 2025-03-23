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
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;

namespace SharpTimer
{
    public partial class SharpTimer
    {
        public void OnTimerStart(CCSPlayerController? player, int bonusX = 0)
        {
            if (!IsAllowedPlayer(player)) return;

            if (bonusX != 0)
            {
                if (useTriggers || useTriggersAndFakeZones) SharpTimerDebug($"Starting Bonus Timer for {player!.PlayerName}");
                playerTimers[player!.Slot].IsTimerRunning = false;
                playerTimers[player!.Slot].IsBonusTimerRunning = true;
            }
            else
            {
                if (useTriggers || useTriggersAndFakeZones) SharpTimerDebug($"Starting Timer for {player!.PlayerName}");
                playerTimers[player!.Slot].IsTimerRunning = true;
                playerTimers[player!.Slot].IsBonusTimerRunning = false;
            }

            playerCheckpoints.Remove(player!.Slot);
            playerTimers[player!.Slot].TimerTicks = 0;
            playerTimers[player!.Slot].StageTicks = 0;
            playerTimers[player.Slot].StageTimes!.Clear();
            playerTimers[player.Slot].StageVelos!.Clear();
            playerTimers[player!.Slot].BonusStage = bonusX;
            playerTimers[player!.Slot].BonusTimerTicks = 0;
            playerTimers[player.Slot].TotalSync = 0;
            playerTimers[player.Slot].GoodSync = 0;
            playerTimers[player.Slot].Sync = 0;

            playerTimers[player!.Slot].IsRecordingReplay = true;
        }

        public void OnTimerStop(CCSPlayerController? player)
        {

            var playerName = player!.PlayerName;
            var playerSlot = player.Slot;
            var steamID = player.SteamID.ToString();
            var playerTimer = playerTimers[playerSlot];
            var currentTicks = playerTimer.TimerTicks;

            if (!IsAllowedPlayer(player) || playerTimer.IsTimerRunning == false) return;

            if(currentTicks == 0)
            {
                PrintToChat(player, $"{ChatColors.LightRed}{Localizer["error_savingtime"]}");
                playerTimer.IsTimerRunning = false;
                playerTimer.IsRecordingReplay = false;
                return;
            }

            if (useStageTriggers == true && useCheckpointTriggers == true)
            {
                if (playerTimer.CurrentMapStage != stageTriggerCount && currentMapOverrideStageRequirement == true)
                {
                    PrintToChat(player, $"{ChatColors.LightRed}{Localizer["error_stagenotmatchfinalone"]}({stageTriggerCount})");
                    SharpTimerDebug($"Player current stage: {playerTimers[playerSlot].CurrentMapStage}; Final checkpoint: {stageTriggerCount}");
                    playerTimer.IsTimerRunning = false;
                    playerTimer.IsRecordingReplay = false;
                    return;
                }

                if (playerTimer.CurrentMapCheckpoint != cpTriggerCount && useCheckpointVerification)
                {
                    PrintToChat(player, $"{ChatColors.LightRed}{Localizer["error_checkpointnotmatchfinalone"]}({cpTriggerCount})");
                    SharpTimerDebug($"Player current checkpoint: {playerTimers[playerSlot].CurrentMapCheckpoint}; Final checkpoint: {cpTriggerCount}");
                    playerTimer.IsTimerRunning = false;
                    playerTimer.IsRecordingReplay = false;
                    return;
                }
            }

            if (useStageTriggers == true && useCheckpointTriggers == false)
            {
                if (playerTimer.CurrentMapStage != stageTriggerCount && currentMapOverrideStageRequirement == false)
                {
                    PrintToChat(player, $"{ChatColors.LightRed}{Localizer["error_stagenotmatchfinalone"]} ({playerTimer.CurrentMapStage}/{stageTriggerCount})");
                    SharpTimerDebug($"Player {player.PlayerName} ({player.SteamID}) tried to finish a map with a current stage of {playerTimer.CurrentMapStage}, but this map has {stageTriggerCount} stages. It is recommended that you review {Server.MapName} for exploits.");
                    playerTimer.IsTimerRunning = false;
                    playerTimer.IsRecordingReplay = false;
                    return;
                }
            }

            if (useStageTriggers == false && useCheckpointTriggers == true)
            {
                if (playerTimer.CurrentMapCheckpoint != cpTriggerCount && useCheckpointVerification)
                {
                    PrintToChat(player, $"{ChatColors.LightRed}{Localizer["error_checkpointnotmatchfinalone"]}({cpTriggerCount})");
                    SharpTimerDebug($"Player current checkpoint: {playerTimers[playerSlot].CurrentMapCheckpoint}; Final checkpoint: {cpTriggerCount}");
                    playerTimer.IsTimerRunning = false;
                    playerTimer.IsRecordingReplay = false;
                    return;
                }
            }

            if (useTriggers || useTriggersAndFakeZones) SharpTimerDebug($"Stopping Timer for {playerName}");

            if (!ignoreJSON) SavePlayerTime(player, currentTicks);
            if (enableDb) _ = Task.Run(async () => await SavePlayerTimeToDatabase(player, currentTicks, steamID, playerName, playerSlot, 0, playerTimer.currentStyle));

            //if (enableReplays == true) _ = Task.Run(async () => await DumpReplayToJson(player!, steamID, playerSlot));
            playerTimer.IsTimerRunning = false;
            playerTimer.IsRecordingReplay = false;

            if (!enableDb) _ = Task.Run(async () => await RankCommandHandler(player, steamID, playerSlot, playerName, true));
        }

        public void OnBonusTimerStop(CCSPlayerController? player, int bonusX)
        {
            if (!IsAllowedPlayer(player) || playerTimers[player!.Slot].IsBonusTimerRunning == false) return;

            var playerName = player.PlayerName;
            var playerSlot = player.Slot;
            var playerTimer = playerTimers[playerSlot];
            var steamID = player.SteamID.ToString();

            if (useTriggers || useTriggersAndFakeZones) SharpTimerDebug($"Stopping Bonus Timer for {playerName}");

            int currentTicks = playerTimers[player.Slot].BonusTimerTicks;

            if(currentTicks == 0)
            {
                PrintToChat(player, $"{ChatColors.LightRed}Error Saving Time: Player time is 0 ticks");
                playerTimer.IsTimerRunning = false;
                playerTimer.IsRecordingReplay = false;
                return;
            }

            if (!ignoreJSON) SavePlayerTime(player, currentTicks, bonusX);
            if (enableDb) _ = Task.Run(async () => await SavePlayerTimeToDatabase(player, currentTicks, steamID, playerName, playerSlot, bonusX, playerTimers[player.Slot].currentStyle));
            //if (enableReplays == true) _ = Task.Run(async () => await DumpReplayToJson(player!, steamID, playerSlot, bonusX));
            playerTimers[player.Slot].IsBonusTimerRunning = false;
            playerTimers[player.Slot].IsRecordingReplay = false;
        }

        public void SavePlayerTime(CCSPlayerController? player, int timerTicks, int bonusX = 0)
        {
            if (!IsAllowedPlayer(player)) return;
            var playerName = player!.PlayerName;
            var playerSlot = player!.Slot;
            var steamId = player.SteamID.ToString();
            if ((bonusX == 0 && playerTimers[playerSlot].IsTimerRunning == false) || (bonusX != 0 && playerTimers[playerSlot].IsBonusTimerRunning == false)) return;

            SharpTimerDebug($"Saving player {(bonusX != 0 ? $"bonus {bonusX} time" : "time")} of {timerTicks} ticks for {playerName} to json");
            string mapRecordsPath = Path.Combine(playerRecordsPath!, bonusX == 0 ? $"{currentMapName}.json" : $"{currentMapName}_bonus{bonusX}.json");

            Task.Run(async () =>
            {
                try
                {
                    using (JsonDocument? jsonDocument = await LoadJson(mapRecordsPath)!)
                    {
                        Dictionary<string, PlayerRecord> records;

                        if (jsonDocument != null)
                        {
                            string json = jsonDocument.RootElement.GetRawText();
                            records = JsonSerializer.Deserialize<Dictionary<string, PlayerRecord>>(json) ?? [];
                        }
                        else
                        {
                            records = [];
                        }

                        if (!records.ContainsKey(steamId) || records[steamId].TimerTicks > timerTicks)
                        {
                            if (!enableDb) await PrintMapTimeToChat(player, steamId, playerName, records.GetValueOrDefault(steamId)?.TimerTicks ?? 0, timerTicks, bonusX, 0, playerTimers[player.Slot].currentStyle);

                            records[steamId] = new PlayerRecord
                            {
                                PlayerName = playerName,
                                TimerTicks = timerTicks
                            };

                            string updatedJson = JsonSerializer.Serialize(records, jsonSerializerOptions);
                            File.WriteAllText(mapRecordsPath, updatedJson);

                            if ((stageTriggerCount != 0 || cpTriggerCount != 0) && bonusX == 0 && (!enableDb) && playerTimers[player.Slot].currentStyle == 0 && !ignoreJSON)
                            {
                                _ = Task.Run(async () => await DumpPlayerStageTimesToJson(player, steamId, playerSlot));
                            }
                            if (enableReplays == true && !enableDb)
                            {
                                _ = Task.Run(async () => await DumpReplayToJson(player!, steamId, playerSlot, bonusX, playerTimers[player.Slot].currentStyle));
                            }
                        }
                        else
                        {
                            if (!enableDb) await PrintMapTimeToChat(player, steamId, playerName, records[steamId].TimerTicks, timerTicks, bonusX, 0, playerTimers[player.Slot].currentStyle);
                        }
                    }

                }
                catch (Exception ex)
                {
                    SharpTimerError($"Error in SavePlayerTime: {ex.Message}");
                }
            });
        }

        private async Task HandlePlayerStageTimes(CCSPlayerController player, nint triggerHandle, int playerSlot, string playerSteamID, string playerName)
        {
            try
            {
                if (!IsAllowedPlayer(player))
                    return;

                SharpTimerDebug($"Player {playerName} has a stage trigger with handle {triggerHandle}");

                if (stageTriggers.TryGetValue(triggerHandle, out int stageTrigger))
                {
                    //var playerTimerTicks = playerTimers[playerSlot].TimerTicks; // store so its in sync with player
                    var playerStageTicks = playerTimers[playerSlot].StageTicks;
                    var formattedStageTicks = FormatTime(playerStageTicks);
                    var prevStage = stageTrigger - 1;

                    string currentSpeed = GetCurrentPlayerSpeed(player);

                    var (srSteamID, srPlayerName, srTime) = ("null", "null", "null");
                    if (playerTimers[playerSlot].CurrentMapStage == stageTrigger || playerTimers[playerSlot] == null) return;

                    (srSteamID, srPlayerName, srTime) = await GetStageRecordSteamIDFromDatabase(prevStage);

                    var (previousStageTime, previousStageSpeed) = await GetStageRecordFromDatabase(prevStage, playerSteamID);
                    var (srStageTime, srStageSpeed) = await GetStageRecordFromDatabase(prevStage, srSteamID);

                    Server.NextFrame(() =>
                    {
                        if (!IsAllowedPlayer(player)) return;
                        if (playerTimers.TryGetValue(playerSlot, out PlayerTimerInfo? playerTimer))
                        {

                            if (playerTimer.CurrentMapStage == stageTrigger || playerTimer == null) return;

                            //TO-DO: Add player setting to enabled/disable printing time comparisons to chat
                            if (previousStageTime != 0)
                            {
                                player.PrintToChat($" {Localizer["prefix"]} Entering Stage: {stageTrigger}");
                                player.PrintToChat(
                                    $" {Localizer["prefix"]} Time: {ChatColors.White}[{primaryChatColor}{formattedStageTicks}{ChatColors.White}] " +
                                    $"[{FormatTimeDifference(playerStageTicks, previousStageTime)}{ChatColors.White}]" +
                                    $" {(previousStageTime != srStageTime ? $"[SR {FormatTimeDifference(playerStageTicks, srStageTime)}{ChatColors.White}]" : "")}"
                                );

                                if (float.TryParse(currentSpeed, out float speed) && speed >= 100)
                                {
                                    player.PrintToChat(
                                        $" {Localizer["prefix"]} Speed: {ChatColors.White}[{primaryChatColor}{currentSpeed}u/s{ChatColors.White}]" +
                                        $" [{FormatSpeedDifferenceFromString(currentSpeed, previousStageSpeed)}u/s{ChatColors.White}]" +
                                        $" {(previousStageSpeed != srStageSpeed ? $"[SR {FormatSpeedDifferenceFromString(currentSpeed, srStageSpeed)}u/s{ChatColors.White}]" : "")}"
                                    );
                                }
                            }

                            if (playerTimer.StageVelos != null && playerTimer.StageTimes != null && playerTimer.IsTimerRunning == true && IsAllowedPlayer(player))
                            {
                                try
                                {
                                    playerTimer.StageTimes[stageTrigger] = playerStageTicks;
                                    playerTimer.StageVelos[stageTrigger] = $"{currentSpeed}";
                                    SharpTimerDebug($"Player {playerName} Entering stage {stageTrigger} Time {playerTimer.StageTimes[stageTrigger]}");
                                }
                                catch (Exception ex)
                                {
                                    SharpTimerError($"Error updating StageTimes dictionary: {ex.Message}");
                                    SharpTimerDebug($"Player {playerName} dictionary keys: {string.Join(", ", playerTimer.StageTimes.Keys)}");
                                }
                            }

                            playerTimer.CurrentMapStage++;
                            playerTimer.StageTicks = 0;
                        }
                    });
                    
                    if (playerTimers[player.Slot].currentStyle == 0)
                    {
                        await SavePlayerStageTimeToDatabase(player, playerStageTicks, prevStage, currentSpeed, playerSteamID, playerName, playerSlot);
                    }
                }
            }
            catch (Exception ex)
            {
                SharpTimerError($"Error in HandlePlayerStageTimes: {ex.Message}");
            }
        }

        private async Task HandlePlayerCheckpointTimes(CCSPlayerController player, nint triggerHandle, int playerSlot, string playerSteamID, string playerName)
        {
            try
            {
                if (!IsAllowedPlayer(player))
                    return;

                if (cpTriggers.TryGetValue(triggerHandle, out int cpTrigger))
                {
                    if (useStageTriggers == true) //use stagetime instead
                    {
                        playerTimers[playerSlot].CurrentMapCheckpoint++;
                        return;
                    }

                    SharpTimerDebug($"Player {playerName} has a checkpoint trigger with handle {triggerHandle}");
                    playerTimers[playerSlot].CurrentMapCheckpoint++;

                    var playerTimerTicks = playerTimers[playerSlot].TimerTicks; // store so its in sync with player

                    var (srSteamID, srPlayerName, srTime) = ("null", "null", "null");
                    if (playerTimers[playerSlot] == null) return;
                    if (enableDb)
                        (srSteamID, srPlayerName, srTime) = await GetMapRecordSteamIDFromDatabase();
                    else
                        (srSteamID, srPlayerName, srTime) = await GetMapRecordSteamID();

                    (srSteamID, srPlayerName, srTime) = await GetStageRecordSteamIDFromDatabase(cpTrigger);
                    var (previousCheckpointTime, previousCheckpointSpeed) = await GetStageRecordFromDatabase(cpTrigger, playerSteamID);
                    var (srCheckpointTime, srCheckpointSpeed) = await GetStageRecordFromDatabase(cpTrigger, srSteamID);

                    string currentSpeed = GetCurrentPlayerSpeed(player);

                    Server.NextFrame(() =>
                    {
                        if (!IsAllowedPlayer(player)) return;
                        if (playerTimers.TryGetValue(playerSlot, out PlayerTimerInfo? playerTimer))
                        {
                            if (playerTimer == null) return;

                            //TO-DO: Add player setting to enabled/disable printing time comparisons to chat
                            if (previousCheckpointTime != 0)
                            {
                                player.PrintToChat($" {Localizer["prefix"]} Checkpoint: {playerTimer.CurrentMapCheckpoint}");
                                player.PrintToChat(
                                    $" {Localizer["prefix"]} Time: {ChatColors.White}[{primaryChatColor}{FormatTime(playerTimerTicks)}{ChatColors.White}] " +
                                    $" [{FormatTimeDifference(playerTimerTicks, previousCheckpointTime)}{ChatColors.White}]" +
                                    $" {(previousCheckpointTime != srCheckpointTime ? $"[SR {FormatTimeDifference(playerTimerTicks, srCheckpointTime)}{ChatColors.White}]" : "")}");

                                if (float.TryParse(currentSpeed, out float speed) && speed >= 100)
                                {
                                    player.PrintToChat(
                                        $" {Localizer["prefix"]} Speed: {ChatColors.White}[{primaryChatColor}{currentSpeed}u/s{ChatColors.White}]" +
                                        $" [{FormatSpeedDifferenceFromString(currentSpeed, previousCheckpointSpeed)}u/s{ChatColors.White}]" +
                                        $" {(previousCheckpointSpeed != srCheckpointSpeed ? $"[SR {FormatSpeedDifferenceFromString(currentSpeed, srCheckpointSpeed)}u/s{ChatColors.White}]" : "")}");
                                }
                            }

                            if (playerTimer.StageVelos != null && playerTimer.StageTimes != null &&
                                playerTimer.IsTimerRunning == true && IsAllowedPlayer(player) && playerTimer.currentStyle == 0)
                            {
                                if (!playerTimer.StageTimes.ContainsKey(cpTrigger))
                                {
                                    SharpTimerDebug($"Player {playerName} cleared StageTimes before (cpTrigger)");
                                    playerTimer.StageTimes.Add(cpTrigger, playerTimerTicks);
                                    playerTimer.StageVelos.Add(cpTrigger, currentSpeed);
                                }
                                else
                                {
                                    try
                                    {
                                        playerTimer.StageTimes[cpTrigger] = playerTimerTicks;
                                        playerTimer.StageVelos[cpTrigger] = currentSpeed;
                                        SharpTimerDebug($"Player {playerName} Entering checkpoint {cpTrigger} Time {playerTimer.StageTimes[cpTrigger]}");
                                    }
                                    catch (Exception ex)
                                    {
                                        SharpTimerError($"Error updating StageTimes dictionary: {ex.Message}");
                                        SharpTimerDebug($"Player {playerName} dictionary keys: {string.Join(", ", playerTimer.StageTimes.Keys)}");
                                    }
                                }
                            }
                        }
                    });

                    if (playerTimers[playerSlot].currentStyle == 0)
                    {
                        await SavePlayerStageTimeToDatabase(player, playerTimerTicks, cpTrigger, currentSpeed, playerSteamID, playerName, playerSlot);
                    }
                }
            }
            catch (Exception ex)
            {
                SharpTimerError($"Error in HandlePlayerCheckpointTimes: {ex.Message}");
            }
        }

        private async Task HandlePlayerBonusCheckpointTimes(CCSPlayerController player, nint triggerHandle, int playerSlot, string playerSteamID, string playerName)
        {
            try
            {
                if (!IsAllowedPlayer(player))
                    return;

                if (bonusCheckpointTriggers.TryGetValue(triggerHandle, out int bonusCheckpointTrigger))
                {
                    if (useStageTriggers == true) //use stagetime instead
                    {
                        playerTimers[playerSlot].CurrentMapCheckpoint++;
                        return;
                    }

                    SharpTimerDebug($"Player {playerName} has a bonus checkpoint trigger with handle {triggerHandle}");

                    var playerTimerTicks = playerTimers[playerSlot].TimerTicks; // store so its in sync with player

                    var (srSteamID, srPlayerName, srTime) = ("null", "null", "null");
                    if (playerTimers[playerSlot].CurrentMapCheckpoint == bonusCheckpointTrigger || playerTimers[playerSlot] == null)
                        return;
                    if (enableDb)
                        (srSteamID, srPlayerName, srTime) = await GetMapRecordSteamIDFromDatabase();
                    else
                        (srSteamID, srPlayerName, srTime) = await GetMapRecordSteamID();

                    (srSteamID, srPlayerName, srTime) = await GetStageRecordSteamIDFromDatabase(bonusCheckpointTrigger);
                    var (previousCheckpointTime, previousCheckpointSpeed) = await GetStageRecordFromDatabase(bonusCheckpointTrigger, playerSteamID);
                    var (srCheckpointTime, srCheckpointSpeed) = await GetStageRecordFromDatabase(bonusCheckpointTrigger, srSteamID);

                    string currentStageSpeed = GetCurrentPlayerSpeed(player);

                    Server.NextFrame(() =>
                    {
                        if (!IsAllowedPlayer(player))
                            return;
                            
                        if (playerTimers.TryGetValue(playerSlot, out PlayerTimerInfo? playerTimer))
                        {
                            if (playerTimer == null)
                                return;

                            //TO-DO: Add player setting to enabled/disable printing time comparisons to chat
                            if (previousCheckpointTime != 0)
                            {
                                player.PrintToChat($" {Localizer["prefix"]} Bonus Checkpoint: {bonusCheckpointTrigger}");
                                player.PrintToChat(
                                    $" {Localizer["prefix"]} Time: {ChatColors.White}[{primaryChatColor}{FormatTime(playerTimerTicks)}{ChatColors.White}] " +
                                    $" [{FormatTimeDifference(playerTimerTicks, previousCheckpointTime)}{ChatColors.White}]" +
                                    $" {(previousCheckpointTime != srCheckpointTime ? $"[SR {FormatTimeDifference(playerTimerTicks, srCheckpointTime)}{ChatColors.White}]" : "")}");

                                if (float.TryParse(currentStageSpeed, out float speed) && speed >= 100)
                                {
                                    player.PrintToChat(
                                        $" {Localizer["prefix"]} Speed: {ChatColors.White}[{primaryChatColor}{currentStageSpeed}u/s{ChatColors.White}]" +
                                        $" [{FormatSpeedDifferenceFromString(currentStageSpeed, previousCheckpointSpeed)}u/s{ChatColors.White}]" +
                                        $" {(previousCheckpointSpeed != srCheckpointSpeed ? $"[SR {FormatSpeedDifferenceFromString(currentStageSpeed, srCheckpointSpeed)}u/s{ChatColors.White}]" : "")}");
                                }
                            }

                            if (playerTimer.StageVelos != null && playerTimer.StageTimes != null &&
                                playerTimer.IsTimerRunning == true && IsAllowedPlayer(player) && playerTimer.currentStyle == 0)
                            {
                                if (!playerTimer.StageTimes.ContainsKey(bonusCheckpointTrigger))
                                {
                                    SharpTimerDebug($"Player {playerName} cleared StageTimes before (bonus checkpoint)");
                                    playerTimer.StageTimes.Add(bonusCheckpointTrigger, playerTimerTicks);
                                    playerTimer.StageVelos.Add(bonusCheckpointTrigger, currentStageSpeed);
                                }
                                else
                                {
                                    try
                                    {
                                        playerTimer.StageTimes[bonusCheckpointTrigger] = playerTimerTicks;
                                        playerTimer.StageVelos[bonusCheckpointTrigger] = currentStageSpeed;
                                        SharpTimerDebug($"Player {playerName} Entering bonus checkpoint {bonusCheckpointTrigger} Time {playerTimer.StageTimes[bonusCheckpointTrigger]}");
                                    }
                                    catch (Exception ex)
                                    {
                                        SharpTimerError($"Error updating StageTimes dictionary: {ex.Message}");
                                        SharpTimerDebug($"Player {playerName} dictionary keys: {string.Join(", ", playerTimer.StageTimes.Keys)}");
                                    }
                                }
                            }
                            playerTimer.CurrentMapCheckpoint++;
                        }
                    });

                    if (playerTimers[playerSlot].currentStyle == 0)
                    {
                        await SavePlayerStageTimeToDatabase(player, playerTimerTicks, bonusCheckpointTrigger, currentStageSpeed, playerSteamID, playerName, playerSlot);
                    }
                }
            }
            catch (Exception ex)
            {
                SharpTimerError($"Error in HandlePlayerBonusCheckpointTimes: {ex.Message}");
            }
        }

        public async Task DumpPlayerStageTimesToJson(CCSPlayerController? player, string playerId, int playerSlot)
        {
            if (!IsAllowedPlayer(player)) return;

            string fileName = $"{currentMapName!.ToLower()}_stage_times.json";
            string playerStageRecordsPath = Path.Join(gameDir, "csgo", "cfg", "SharpTimer", "PlayerStageData", fileName);

            try
            {
                using (JsonDocument? jsonDocument = await LoadJson(playerStageRecordsPath)!)
                {
                    if (jsonDocument != null)
                    {
                        string jsonContent = jsonDocument.RootElement.GetRawText();

                        Dictionary<string, PlayerStageData> playerData;
                        if (!string.IsNullOrEmpty(jsonContent))
                        {
                            playerData = JsonSerializer.Deserialize<Dictionary<string, PlayerStageData>>(jsonContent)!;
                        }
                        else
                        {
                            playerData = [];
                        }

                        if (!playerData!.ContainsKey(playerId))
                        {
                            playerData[playerId] = new PlayerStageData();
                        }

                        if (playerTimers.TryGetValue(playerSlot, out PlayerTimerInfo? playerTimer))
                        {
                            playerData[playerId].StageTimes = playerTimer.StageTimes;
                            playerData[playerId].StageVelos = playerTimer.StageVelos;
                        }
                        else
                        {
                            SharpTimerError($"Error in DumpPlayerStageTimesToJson: playerTimers does not have the requested playerSlot");
                        }

                        string updatedJson = JsonSerializer.Serialize(playerData, jsonSerializerOptions);
                        await File.WriteAllTextAsync(playerStageRecordsPath, updatedJson);
                    }
                    else
                    {
                        Dictionary<string, PlayerStageData> playerData = [];

                        if (playerTimers.TryGetValue(playerSlot, out PlayerTimerInfo? playerTimer))
                        {
                            playerData[playerId] = new PlayerStageData
                            {
                                StageTimes = playerTimers[playerSlot].StageTimes,
                                StageVelos = playerTimers[playerSlot].StageVelos
                            };
                        }
                        else
                        {
                            SharpTimerError($"Error in DumpPlayerStageTimesToJson: playerTimers does not have the requested playerSlot");
                        }

                        string updatedJson = JsonSerializer.Serialize(playerData, jsonSerializerOptions);
                        await File.WriteAllTextAsync(playerStageRecordsPath, updatedJson);
                    }
                }
            }
            catch (Exception ex)
            {
                SharpTimerError($"Error in DumpPlayerStageTimesToJson: {ex.Message}");
            }
        }
    }
}