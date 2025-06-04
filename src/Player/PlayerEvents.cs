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

using CounterStrikeSharp.API;
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
                    connectedPlayers[slot] = new CCSPlayerController(player.Handle);
                    playerTimers[slot] = new PlayerTimerInfo();

                    if (playerTimers.TryGetValue(slot, out var playerTime))
                    {
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
                    }

                    if (isForBot == false)
                    {
                        string steamID = player.SteamID.ToString();

                        _ = Task.Run(async () => await IsPlayerATester(steamID, slot));

                        if (enableDb)
                            _ = Task.Run(async () => await GetPlayerStats(player, steamID, playerName, slot, true));

                        if (cmdJoinMsgEnabled == true)
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
                    if (connectedPlayers[slot] == null)
                        connectedPlayers.Remove(slot);

                    if (playerTimers[slot] == null)
                    {
                        playerTimers.Remove(slot);
                    }
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
                if (connectedPlayers.TryGetValue(player.Slot, out var connectedPlayer))
                {

                    connectedPlayers.Remove(player.Slot);

                    //schizo removing data from memory
                    playerTimers[player.Slot] = new PlayerTimerInfo();
                    playerTimers.Remove(player.Slot);

                    //schizo removing data from memory
                    playerCheckpoints[player.Slot] = new List<PlayerCheckpoint>();
                    playerCheckpoints.Remove(player.Slot);

                    specTargets.Remove(player.Pawn.Value!.EntityHandle.Index);

                    if (enableReplays)
                    {
                        //schizo removing data from memory
                        playerReplays[player.Slot] = new PlayerReplays();
                        playerReplays.Remove(player.Slot);
                    }

                    Utils.LogDebug($"Removed player {connectedPlayer.PlayerName} with UserID {connectedPlayer.UserId} from connectedPlayers.");
                    Utils.LogDebug($"Removed specTarget index {player.Pawn.Value.EntityHandle.Index} from specTargets.");
                    Utils.LogDebug($"Total players connected: {connectedPlayers.Count}");
                    Utils.LogDebug($"Total playerTimers: {playerTimers.Count}");
                    Utils.LogDebug($"Total specTargets: {specTargets.Count}");

                    if (connectMsgEnabled == true && isForBot == false)
                    {
                        Utils.PrintToChatAll(Localizer["disconnect_message", connectedPlayer.PlayerName]);
                    }
                }
            }
            catch (Exception ex)
            {
                Utils.LogError($"Error in OnPlayerDisconnect (probably replay bot related lolxd): {ex.Message}");
            }
        }
    }
}