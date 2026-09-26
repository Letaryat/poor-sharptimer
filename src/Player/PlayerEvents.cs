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

using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Admin;

namespace SharpTimer
{
    public partial class SharpTimer
    {
        private void OnPlayerConnect(CCSPlayerController? player, bool isForBot = false)
        {
            try
            {
                if (player == null)
                {
                    Utils.LogError("Player object is null.");
                    return;
                }

                if (player.PlayerPawn == null)
                {
                    Utils.LogError("PlayerPawn is null.");
                    return;
                }

                if (player.PlayerPawn.Value!.MovementServices == null)
                {
                    Utils.LogError("MovementServices is null.");
                    return;
                }

                int slot = player.Slot;
                string playerName = player.PlayerName;

                try
                {
                    connectedPlayers.Remove(slot);
                    playerTimers.Remove(slot);
                    playerReplays.Remove(slot);
                    
                    connectedPlayers[slot] = new CCSPlayerController(player.Handle);
                    var playerTime = new PlayerTimerInfo();
                    playerTimers[slot] = playerTime;
                    
                    if (enableReplays) 
                        playerReplays[slot] = new PlayerReplays();

                    if (AdminManager.PlayerHasPermissions(player, "@css/root")) 
                        playerTime.ZoneToolWire = new Dictionary<int, CBeam>();

                    playerTime.MovementService = new CCSPlayer_MovementServices(player.PlayerPawn.Value.MovementServices!.Handle); 
                    playerTime.StageTimes = new Dictionary<int, int>();
                    playerTime.StageVelos = new Dictionary<int, string>();
                    playerTime.CurrentMapStage = 0;
                    playerTime.CurrentMapCheckpoint = 0;
                    playerTime.IsRecordingReplay = false;
                    playerTime.SetRespawnPos = null;
                    playerTime.SetRespawnAng = null;
                    playerTime.SoundsEnabled = soundsEnabledByDefault;

                    SetNormalStyle(player);
                    

                    if (isForBot == false)
                    {
                        string steamID = player.SteamID.ToString();
                        
                        _ = Task.Run(async () =>
                        {
                            await GetPlayerStats(player, steamID, playerName, slot, true);
                        });

                        if (cmdJoinMsgEnabled)
                            PrintAllEnabledCommands(player);

                        if (connectMsgEnabled == true && !enableDb)
                            Utils.PrintToChatAll(Localizer["connect_message", player.PlayerName]);
                    }

                    Utils.LogDebug($"Added player {player.PlayerName} with UserID {player.UserId} to connectedPlayers");
                    Utils.LogDebug($"Total players connected: {connectedPlayers.Count}");
                    Utils.LogDebug($"Total playerTimers: {playerTimers.Count}");
                    Utils.LogDebug($"Total playerReplays: {playerReplays.Count}");
                }
                finally
                {
                    if (connectedPlayers.TryGetValue(slot, out var playerController) && playerController == null)
                        connectedPlayers.Remove(slot);

                    if (playerTimers.TryGetValue(slot, out var playerTimer) && playerTimer == null)
                        playerTimers.Remove(slot);
                }
            }
            catch (Exception ex)
            {
                Utils.LogError($"Error in OnPlayerConnect: {ex.Message}");
            }
        }

        private void OnPlayerDisconnect(CCSPlayerController? player, bool isForBot = false)
        {
            if (player == null) return;

            try
            {
                int slot = player.Slot;
                bool wasTracked = connectedPlayers.ContainsKey(slot);
                string playerName = player.IsValid ? player.PlayerName : $"slot {slot}";

                RemovePlayerState(slot);

                if (wasTracked && connectMsgEnabled == true && isForBot == false)
                    Utils.PrintToChatAll(Localizer["disconnect_message", playerName]);
            }
            catch (Exception ex)
            {
                Utils.LogError($"Error in OnPlayerDisconnect: {ex.Message}");
            }
        }

        private void OnClientDisconnectHandler(int slot)
        {
            RemovePlayerState(slot);
        }

        private void RemovePlayerState(int slot)
        {
            try
            {
                bool wasTracked = connectedPlayers.Remove(slot);
                playerTimers.Remove(slot);
                playerCheckpoints.Remove(slot);
                playerReplays.Remove(slot);
                connectedAFKPlayers.Remove(slot);
                playerCache.PlayerID.Remove(slot);

                List<uint>? staleKeys = null;
                foreach (var kv in specTargets)
                {
                    if (kv.Value == null || kv.Value.Slot == slot)
                        (staleKeys ??= []).Add(kv.Key);
                }

                if (staleKeys != null)
                {
                    foreach (var key in staleKeys)
                        specTargets.Remove(key);
                }

                if (replayBotSlot == slot)
                    replayBotSlot = -1;

                if (wasTracked)
                {
                    Utils.LogDebug($"Removed slot {slot} from connectedPlayers.");
                    Utils.LogDebug($"Total players connected: {connectedPlayers.Count}");
                    Utils.LogDebug($"Total playerTimers: {playerTimers.Count}");
                    Utils.LogDebug($"Total specTargets: {specTargets.Count}");
                }
            }
            catch (Exception ex)
            {
                Utils.LogError($"Error in RemovePlayerState for slot {slot}: {ex.Message}");
            }
        }

        private void OnMapEndHandler()
        {
            connectedPlayers.Clear();
            playerTimers.Clear();
            playerCheckpoints.Clear();
            playerReplays.Clear();
            connectedAFKPlayers.Clear();
            specTargets.Clear();
            playerCache.PlayerID.Clear();
            replayBotSlot = -1;

            Utils.LogDebug("OnMapEnd: cleared per-player state.");
        }
    }
}