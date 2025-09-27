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
using SharpTimerAPI.Events;

namespace SharpTimer
{
    public partial class SharpTimer
    {
        public void OnTimerStart(CCSPlayerController? player, int bonusX = 0)
        {
            if (!IsAllowedPlayer(player)) return;
            
            try
            {
                StEventSenderCapability.Get()?.TriggerEvent(new StartTimerEvent(player));
            }
            catch (Exception e)
            {
                Utils.LogError($"Couldn't trigger timer start event {e.Message}");
            }

            if (bonusX != 0)
            {
                if (useTriggers || useTriggersAndFakeZones) Utils.LogDebug($"Starting Bonus Timer for {player!.PlayerName}");
                playerTimers[player!.Slot].IsTimerRunning = false;
                playerTimers[player!.Slot].IsBonusTimerRunning = true;
            }
            else
            {
                if (useTriggers || useTriggersAndFakeZones) Utils.LogDebug($"Starting Timer for {player!.PlayerName}");
                playerTimers[player!.Slot].IsTimerRunning = true;
                playerTimers[player!.Slot].IsBonusTimerRunning = false;
            }

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

            if (printStartSpeedEnabled) PrintStartSpeed(player);
        }

        public void OnTimerStop(CCSPlayerController? player)
        {

            var playerName = player!.PlayerName;
            var slot = player.Slot;
            var steamID = player.SteamID.ToString();
            var playerTimer = playerTimers[slot];
            var currentTicks = playerTimer.TimerTicks;

            if (!IsAllowedPlayer(player) || playerTimer.IsTimerRunning == false) return;

            if(currentTicks == 0)
            {
                Utils.PrintToChat(player, $"{ChatColors.LightRed}{Localizer["error_savingtime"]}");
                playerTimer.IsTimerRunning = false;
                playerTimer.IsRecordingReplay = false;
                return;
            }

            if (useStageTriggers == true && useCheckpointTriggers == true)
            {
                if (playerTimer.CurrentMapStage != stageTriggerCount && currentMapOverrideStageRequirement == true)
                {
                    Utils.PrintToChat(player, $"{ChatColors.LightRed}{Localizer["error_stagenotmatchfinalone"]}({stageTriggerCount})");
                    Utils.LogDebug($"Player current stage: {playerTimers[slot].CurrentMapStage}; Final checkpoint: {stageTriggerCount}");
                    playerTimer.IsTimerRunning = false;
                    playerTimer.IsRecordingReplay = false;
                    return;
                }

                if (playerTimer.CurrentMapCheckpoint != cpTriggerCount && useCheckpointVerification)
                {
                    Utils.PrintToChat(player, $"{ChatColors.LightRed}{Localizer["error_checkpointnotmatchfinalone"]}({cpTriggerCount})");
                    Utils.LogDebug($"Player current checkpoint: {playerTimers[slot].CurrentMapCheckpoint}; Final checkpoint: {cpTriggerCount}");
                    playerTimer.IsTimerRunning = false;
                    playerTimer.IsRecordingReplay = false;
                    return;
                }
            }

            if (useStageTriggers == true && useCheckpointTriggers == false)
            {
                if (playerTimer.CurrentMapStage != stageTriggerCount && currentMapOverrideStageRequirement == false)
                {
                    Utils.PrintToChat(player, $"{ChatColors.LightRed}{Localizer["error_stagenotmatchfinalone"]} ({playerTimer.CurrentMapStage}/{stageTriggerCount})");
                    Utils.LogDebug($"Player {player.PlayerName} ({player.SteamID}) tried to finish a map with a current stage of {playerTimer.CurrentMapStage}, but this map has {stageTriggerCount} stages. It is recommended that you review {Server.MapName} for exploits.");
                    playerTimer.IsTimerRunning = false;
                    playerTimer.IsRecordingReplay = false;
                    return;
                }
            }

            if (useStageTriggers == false && useCheckpointTriggers == true)
            {
                if (playerTimer.CurrentMapCheckpoint != cpTriggerCount && useCheckpointVerification)
                {
                    Utils.PrintToChat(player, $"{ChatColors.LightRed}{Localizer["error_checkpointnotmatchfinalone"]}({cpTriggerCount})");
                    Utils.LogDebug($"Player current checkpoint: {playerTimers[slot].CurrentMapCheckpoint}; Final checkpoint: {cpTriggerCount}");
                    playerTimer.IsTimerRunning = false;
                    playerTimer.IsRecordingReplay = false;
                    return;
                }
            }

            if (useTriggers || useTriggersAndFakeZones) Utils.LogDebug($"Stopping Timer for {playerName}");

            if (enableDb) _ = Task.Run(async () => await SavePlayerTimeToDatabase(player, currentTicks, steamID, playerName, slot, 0, playerTimer.currentStyle, playerTimer.Mode));

            playerTimer.IsTimerRunning = false;
            playerTimer.IsRecordingReplay = false;

            if (!enableDb) _ = Task.Run(async () => await RankCommandHandler(player, steamID, slot, playerName, true));
        }

        public void OnBonusTimerStop(CCSPlayerController? player, int bonusX)
        {
            if (!IsAllowedPlayer(player) || playerTimers[player!.Slot].IsBonusTimerRunning == false)
                return;

            var playerName = player.PlayerName;
            var slot = player.Slot;
            var playerTimer = playerTimers[slot];
            var steamID = player.SteamID.ToString();

            if (useTriggers || useTriggersAndFakeZones) Utils.LogDebug($"Stopping Bonus Timer for {playerName}");

            int currentTicks = playerTimers[player.Slot].BonusTimerTicks;

            if(currentTicks == 0)
            {
                Utils.PrintToChat(player, $"{ChatColors.LightRed}Error Saving Time: Player time is 0 ticks");
                playerTimer.IsTimerRunning = false;
                playerTimer.IsRecordingReplay = false;
                return;
            }

            _ = Task.Run(async () => await SavePlayerTimeToDatabase(player, currentTicks, steamID, playerName, slot, bonusX, playerTimers[player.Slot].currentStyle, playerTimer.Mode));

            playerTimers[player.Slot].IsBonusTimerRunning = false;
            playerTimers[player.Slot].IsRecordingReplay = false;
        }

        private async Task HandlePlayerStageTimes(CCSPlayerController player, nint triggerHandle, int slot, string playerSteamID, string playerName, int style, string mode)
        {
            try
            {
                if (!IsAllowedPlayer(player))
                    return;

                Utils.LogDebug($"Player {playerName} has a stage trigger with handle {triggerHandle}");

                if (stageTriggers.TryGetValue(triggerHandle, out int stageTrigger))
                {
                    //var playerTimerTicks = playerTimers[slot].TimerTicks; // store so its in sync with player
                    var playerStageTicks = playerTimers[slot].StageTicks;
                    var formattedStageTicks = Utils.FormatTime(playerStageTicks);
                    var prevStage = stageTrigger - 1;

                    string currentSpeed = GetCurrentPlayerSpeed(player);

                    var (srSteamID, srPlayerName, srTime) = ("null", "null", "null");
                    if (playerTimers[slot] == null || playerTimers[slot].CurrentMapStage == stageTrigger) return;
                    
                    (srSteamID, srPlayerName, srTime) = await GetStageRecordSteamIDFromDatabase(prevStage, style, mode);

                    var (previousStageTime, previousStageSpeed) = await GetStageRecordFromDatabase(prevStage, playerSteamID, style, mode);
                    var (srStageTime, srStageSpeed) = await GetStageRecordFromDatabase(prevStage, srSteamID, style, mode);

                    Server.NextFrame(() =>
                    {
                        if (!IsAllowedPlayer(player)) return;
                        if (playerTimers.TryGetValue(slot, out PlayerTimerInfo? playerTimer))
                        {
                            if (playerTimer == null || playerTimer.CurrentMapStage == stageTrigger) return;
                            //TO-DO: Add player setting to enabled/disable printing time comparisons to chat
                            
                            if (previousStageTime != 0)
                            {
                                Utils.PrintToSpec(player, $"Entering Stage: {stageTrigger}");
                                Utils.PrintToSpec(player, $"Time: {ChatColors.White}[{primaryChatColor}{formattedStageTicks}{ChatColors.White}] " +
                                                                $" [{Utils.FormatTimeDifference(playerStageTicks, previousStageTime)}{ChatColors.White}]" +
                                                                $" {(previousStageTime != srStageTime && enableStageSR ? $"[SR {Utils.FormatTimeDifference(playerStageTicks, srStageTime)}{ChatColors.White}]" : "")}");

                                if (float.TryParse(currentSpeed, out float speed2) && speed2 >= 100) //workaround for staged maps with not telehops
                                    Utils.PrintToSpec(player, $"Speed: {ChatColors.White}[{primaryChatColor}{currentSpeed}u/s{ChatColors.White}]" +
                                                                    $" [{Utils.FormatSpeedDifferenceFromString(currentSpeed, previousStageSpeed)}u/s{ChatColors.White}]" +
                                                                    $" {(previousStageSpeed != srStageSpeed && enableStageSR ? $"[SR {Utils.FormatSpeedDifferenceFromString(currentSpeed, srStageSpeed)}u/s{ChatColors.White}]" : "")}");

                                if (!playerTimers[slot].HideChatSpeed)
                                {
                                    Utils.PrintToChat(player, $"Entering Stage: {stageTrigger}");
                                    Utils.PrintToChat(player, $"Time: {ChatColors.White}[{primaryChatColor}{formattedStageTicks}{ChatColors.White}] " +
                                                                    $" [{Utils.FormatTimeDifference(playerStageTicks, previousStageTime)}{ChatColors.White}]" +
                                                                    $" {(previousStageTime != srStageTime && enableStageSR ? $"[SR {Utils.FormatTimeDifference(playerStageTicks, srStageTime)}{ChatColors.White}]" : "")}");

                                    if (float.TryParse(currentSpeed, out float speed) && speed >= 100) //workaround for staged maps with not telehops
                                        Utils.PrintToChat(player, $"Speed: {ChatColors.White}[{primaryChatColor}{currentSpeed}u/s{ChatColors.White}]" +
                                                                        $" [{Utils.FormatSpeedDifferenceFromString(currentSpeed, previousStageSpeed)}u/s{ChatColors.White}]" +
                                                                        $" {(previousStageSpeed != srStageSpeed && enableStageSR ? $"[SR {Utils.FormatSpeedDifferenceFromString(currentSpeed, srStageSpeed)}u/s{ChatColors.White}]" : "")}");
                                }
                            }

                            if (playerTimer.StageVelos != null && playerTimer.StageTimes != null && playerTimer.IsTimerRunning == true && IsAllowedPlayer(player))
                            {
                                try
                                {
                                    playerTimer.StageTimes[stageTrigger] = playerStageTicks;
                                    playerTimer.StageVelos[stageTrigger] = $"{currentSpeed}";
                                    Utils.LogDebug($"Player {playerName} Entering stage {stageTrigger} Time {playerTimer.StageTimes[stageTrigger]}");
                                }
                                catch (Exception ex)
                                {
                                    Utils.LogError($"Error updating StageTimes dictionary: {ex.Message}");
                                    Utils.LogDebug($"Player {playerName} dictionary keys: {string.Join(", ", playerTimer.StageTimes.Keys)}");
                                }
                            }

                            playerTimer.CurrentMapStage++;
                            playerTimer.StageTicks = 0;
                        }
                    });
                    
                    if (playerTimers.TryGetValue(player.Slot, out var timer) && timer?.currentStyle == 0)
                    {
                        await SavePlayerStageTimeToDatabase(player, playerStageTicks, prevStage, currentSpeed, playerSteamID, playerName, slot);
                    }
                }
            }
            catch (Exception ex)
            {
                Utils.LogError($"Error in HandlePlayerStageTimes: {ex.Message}");
            }
        }

        private async Task HandlePlayerCheckpointTimes(CCSPlayerController player, nint triggerHandle, int slot, string playerSteamID, string playerName, int style, string mode)
        {
            try
            {
                if (!IsAllowedPlayer(player))
                    return;

                if (cpTriggers.TryGetValue(triggerHandle, out int cpTrigger))
                {
                    if (useStageTriggers == true) //use stagetime instead
                    {
                        playerTimers[slot].CurrentMapCheckpoint++;
                        return;
                    }

                    Utils.LogDebug($"Player {playerName} has a checkpoint trigger with handle {triggerHandle}");

                    playerTimers[slot].CurrentMapCheckpoint++;

                    var playerTimerTicks = playerTimers[slot].TimerTicks; // store so its in sync with player

                    var (srSteamID, srPlayerName, srTime) = ("null", "null", "null");
                    if (playerTimers[slot] == null) return;

                    (srSteamID, srPlayerName, srTime) = await GetStageRecordSteamIDFromDatabase(cpTrigger, style, mode);
                    var (previousStageTime, previousStageSpeed) = await GetStageRecordFromDatabase(cpTrigger, playerSteamID, style, mode);
                    var (srStageTime, srStageSpeed) = await GetStageRecordFromDatabase(cpTrigger, srSteamID, style, mode);

                    string currentStageSpeed = GetCurrentPlayerSpeed(player);

                    Server.NextFrame(() =>
                    {
                        if (!IsAllowedPlayer(player)) return;
                        if (playerTimers.TryGetValue(slot, out PlayerTimerInfo? playerTimer))
                        {
                            if (playerTimer == null) return;

                            //TO-DO: Add player setting to enabled/disable printing time comparisons to chat
                            if (previousStageTime != 0)
                            {
                                Utils.PrintToSpec(player, $"Checkpoint: {playerTimer.CurrentMapCheckpoint}");
                                Utils.PrintToSpec(player, $"Time: {ChatColors.White}[{primaryChatColor}{Utils.FormatTime(playerTimerTicks)}{ChatColors.White}] " +
                                                               $" [{Utils.FormatTimeDifference(playerTimerTicks, previousStageTime)}{ChatColors.White}]" +
                                                               $" {(previousStageTime != srStageTime ? $"[SR {Utils.FormatTimeDifference(playerTimerTicks, srStageTime)}{ChatColors.White}]" : "")}");

                                if (float.TryParse(currentStageSpeed, out float speed2) && speed2 >= 100) //workaround for staged maps with not telehops
                                    Utils.PrintToSpec(player, $"Speed: {ChatColors.White}[{primaryChatColor}{currentStageSpeed}u/s{ChatColors.White}]" +
                                                                   $" [{Utils.FormatSpeedDifferenceFromString(currentStageSpeed, previousStageSpeed)}u/s{ChatColors.White}]" +
                                                                   $" {(previousStageSpeed != srStageSpeed ? $"[SR {Utils.FormatSpeedDifferenceFromString(currentStageSpeed, srStageSpeed)}u/s{ChatColors.White}]" : "")}");

                                if (!playerTimers[slot].HideChatSpeed)
                                {
                                    Utils.PrintToChat(player, $"Checkpoint: {playerTimer.CurrentMapCheckpoint}");
                                    Utils.PrintToChat(player, $"Time: {ChatColors.White}[{primaryChatColor}{Utils.FormatTime(playerTimerTicks)}{ChatColors.White}] " +
                                                                $" [{Utils.FormatTimeDifference(playerTimerTicks, previousStageTime)}{ChatColors.White}]" +
                                                                $" {(previousStageTime != srStageTime ? $"[SR {Utils.FormatTimeDifference(playerTimerTicks, srStageTime)}{ChatColors.White}]" : "")}");

                                    if (float.TryParse(currentStageSpeed, out float speed) && speed >= 100) //workaround for staged maps with not telehops
                                        Utils.PrintToChat(player, $"Speed: {ChatColors.White}[{primaryChatColor}{currentStageSpeed}u/s{ChatColors.White}]" +
                                                                    $" [{Utils.FormatSpeedDifferenceFromString(currentStageSpeed, previousStageSpeed)}u/s{ChatColors.White}]" +
                                                                    $" {(previousStageSpeed != srStageSpeed ? $"[SR {Utils.FormatSpeedDifferenceFromString(currentStageSpeed, srStageSpeed)}u/s{ChatColors.White}]" : "")}");
                                }
                            }

                            if (playerTimer.StageVelos != null && playerTimer.StageTimes != null &&
                                playerTimer.IsTimerRunning == true && IsAllowedPlayer(player) && playerTimer.currentStyle == 0)
                            {
                                if (!playerTimer.StageTimes.ContainsKey(cpTrigger))
                                {
                                    Utils.LogDebug($"Player {playerName} cleared StageTimes before (cpTrigger)");
                                    playerTimer.StageTimes.Add(cpTrigger, playerTimerTicks);
                                    playerTimer.StageVelos.Add(cpTrigger, currentStageSpeed);
                                }
                                else
                                {
                                    try
                                    {
                                        playerTimer.StageTimes[cpTrigger] = playerTimerTicks;
                                        playerTimer.StageVelos[cpTrigger] = $"{currentStageSpeed}";
                                        Utils.LogDebug($"Player {playerName} Entering checkpoint {cpTrigger} Time {playerTimer.StageTimes[cpTrigger]}");
                                    }
                                    catch (Exception ex)
                                    {
                                        Utils.LogError($"Error updating StageTimes dictionary: {ex.Message}");
                                        Utils.LogDebug($"Player {playerName} dictionary keys: {string.Join(", ", playerTimer.StageTimes.Keys)}");
                                    }
                                }
                            }
                        }
                    });

                    if (playerTimers.TryGetValue(player.Slot, out var timer) && timer?.currentStyle == 0)
                    {
                        await SavePlayerStageTimeToDatabase(player, playerTimerTicks, cpTrigger, currentStageSpeed, playerSteamID, playerName, slot);
                    }
                }
            }
            catch (Exception ex)
            {
                Utils.LogError($"Error in HandlePlayerCheckpointTimes: {ex.Message}");
            }
        }

        private async Task HandlePlayerBonusCheckpointTimes(CCSPlayerController player, nint triggerHandle, int slot, string playerSteamID, string playerName, int style, string mode)
        {
            try
            {
                if (!IsAllowedPlayer(player))
                    return;

                if (bonusCheckpointTriggers.TryGetValue(triggerHandle, out int bonusCheckpointTrigger))
                {
                    if (useStageTriggers == true) //use stagetime instead
                    {
                        playerTimers[slot].CurrentMapCheckpoint++;
                        return;
                    }

                    Utils.LogDebug($"Player {playerName} has a checkpoint trigger with handle {triggerHandle}");

                    var playerTimerTicks = playerTimers[slot].TimerTicks; // store so its in sync with player

                    var (srSteamID, srPlayerName, srTime) = ("null", "null", "null");
                    
                    if (playerTimers[slot] == null || playerTimers[slot].CurrentMapCheckpoint == bonusCheckpointTrigger) return;

                    (srSteamID, srPlayerName, srTime) = await GetStageRecordSteamIDFromDatabase(bonusCheckpointTrigger, style, mode);
                    var (previousStageTime, previousStageSpeed) = await GetStageRecordFromDatabase(bonusCheckpointTrigger, playerSteamID, style, mode);
                    var (srStageTime, srStageSpeed) = await GetStageRecordFromDatabase(bonusCheckpointTrigger, srSteamID, style, mode);

                    string currentStageSpeed = GetCurrentPlayerSpeed(player);

                    Server.NextFrame(() =>
                    {
                        if (!IsAllowedPlayer(player))
                            return;
                            
                        if (playerTimers.TryGetValue(slot, out PlayerTimerInfo? playerTimer))
                        {
                            if (playerTimer == null)
                                return;

                            //TO-DO: Add player setting to enabled/disable printing time comparisons to chat
                            if (previousStageTime != 0)
                            {
                                Utils.PrintToSpec(player, $"Bonus Checkpoint: {bonusCheckpointTrigger}");
                                Utils.PrintToSpec(player, $"Time: {ChatColors.White}[{primaryChatColor}{Utils.FormatTime(playerTimerTicks)}{ChatColors.White}] " +
                                                               $" [{Utils.FormatTimeDifference(playerTimerTicks, previousStageTime)}{ChatColors.White}]" +
                                                               $" {(previousStageTime != srStageTime ? $"[SR {Utils.FormatTimeDifference(playerTimerTicks, srStageTime)}{ChatColors.White}]" : "")}");

                                if (float.TryParse(currentStageSpeed, out float speed2) && speed2 >= 100) //workaround for staged maps with not telehops
                                    Utils.PrintToSpec(player, $"Speed: {ChatColors.White}[{primaryChatColor}{currentStageSpeed}u/s{ChatColors.White}]" +
                                                                    $" [{Utils.FormatSpeedDifferenceFromString(currentStageSpeed, previousStageSpeed)}u/s{ChatColors.White}]" +
                                                                    $" {(previousStageSpeed != srStageSpeed ? $"[SR {Utils.FormatSpeedDifferenceFromString(currentStageSpeed, srStageSpeed)}u/s{ChatColors.White}]" : "")}");

                                if (!playerTimers[slot].HideChatSpeed)
                                {
                                    Utils.PrintToChat(player, $"Bonus Checkpoint: {bonusCheckpointTrigger}");
                                    Utils.PrintToChat(player, $"Time: {ChatColors.White}[{primaryChatColor}{Utils.FormatTime(playerTimerTicks)}{ChatColors.White}] " +
                                                                   $" [{Utils.FormatTimeDifference(playerTimerTicks, previousStageTime)}{ChatColors.White}]" +
                                                                   $" {(previousStageTime != srStageTime ? $"[SR {Utils.FormatTimeDifference(playerTimerTicks, srStageTime)}{ChatColors.White}]" : "")}");

                                    if (float.TryParse(currentStageSpeed, out float speed) && speed >= 100) //workaround for staged maps with not telehops
                                        Utils.PrintToChat(player, $"Speed: {ChatColors.White}[{primaryChatColor}{currentStageSpeed}u/s{ChatColors.White}]" +
                                                                       $" [{Utils.FormatSpeedDifferenceFromString(currentStageSpeed, previousStageSpeed)}u/s{ChatColors.White}]" +
                                                                       $" {(previousStageSpeed != srStageSpeed ? $"[SR {Utils.FormatSpeedDifferenceFromString(currentStageSpeed, srStageSpeed)}u/s{ChatColors.White}]" : "")}");
                                }
                            }

                            if (playerTimer.StageVelos != null && playerTimer.StageTimes != null &&
                                playerTimer.IsTimerRunning == true && IsAllowedPlayer(player) && playerTimer.currentStyle == 0)
                            {
                                if (!playerTimer.StageTimes.ContainsKey(bonusCheckpointTrigger))
                                {
                                    Utils.LogDebug($"Player {playerName} cleared StageTimes before (cpTrigger)");
                                    playerTimer.StageTimes.Add(bonusCheckpointTrigger, playerTimerTicks);
                                    playerTimer.StageVelos.Add(bonusCheckpointTrigger, currentStageSpeed);
                                }
                                else
                                {
                                    try
                                    {
                                        playerTimer.StageTimes[bonusCheckpointTrigger] = playerTimerTicks;
                                        playerTimer.StageVelos[bonusCheckpointTrigger] = $"{currentStageSpeed}";
                                        Utils.LogDebug($"Player {playerName} Entering checkpoint {bonusCheckpointTrigger} Time {playerTimer.StageTimes[bonusCheckpointTrigger]}");
                                    }
                                    catch (Exception ex)
                                    {
                                        Utils.LogError($"Error updating StageTimes dictionary: {ex.Message}");
                                        Utils.LogDebug($"Player {playerName} dictionary keys: {string.Join(", ", playerTimer.StageTimes.Keys)}");
                                    }
                                }
                            }
                            playerTimer.CurrentMapCheckpoint++;
                        }
                    });

                    if (playerTimers.TryGetValue(player.Slot, out var timer) && timer?.currentStyle == 0)
                    {
                        await SavePlayerStageTimeToDatabase(player, playerTimerTicks, bonusCheckpointTrigger, currentStageSpeed, playerSteamID, playerName, slot);
                    }
                }
            }
            catch (Exception ex)
            {
                Utils.LogError($"Error in HandlePlayerBonusCheckpointTimes: {ex.Message}");
            }
        }
    }
}