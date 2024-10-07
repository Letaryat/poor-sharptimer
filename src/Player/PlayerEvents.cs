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

using System.Drawing;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Admin;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Utils;

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
                    SharpTimerError("Player object is null.");
                    return;
                }

                if (player.PlayerPawn == null)
                {
                    SharpTimerError("PlayerPawn is null.");
                    return;
                }

                if (player.PlayerPawn.Value!.MovementServices == null)
                {
                    SharpTimerError("MovementServices is null.");
                    return;
                }

                int playerSlot = player.Slot;
                string steamID = player.SteamID.ToString();
                string playerName = player.PlayerName;

                try
                {
                    connectedPlayers[playerSlot] = new CCSPlayerController(player.Handle);
                    playerTimers[playerSlot] = new PlayerTimerInfo();
                    playerJumpStats[playerSlot] = new PlayerJumpStats();
                    if (enableReplays) playerReplays[playerSlot] = new PlayerReplays();
                    playerTimers[playerSlot].MovementService = new CCSPlayer_MovementServices(player.PlayerPawn.Value.MovementServices!.Handle);
                    playerTimers[playerSlot].StageTimes = new Dictionary<int, int>();
                    playerTimers[playerSlot].StageVelos = new Dictionary<int, string>();
                    if (AdminManager.PlayerHasPermissions(player, "@css/root")) playerTimers[playerSlot].ZoneToolWire = new Dictionary<int, CBeam>();
                    playerTimers[playerSlot].CurrentMapStage = 0;
                    playerTimers[playerSlot].CurrentMapCheckpoint = 0;
                    SetNormalStyle(player);
                    playerTimers[playerSlot].IsRecordingReplay = false;
                    playerTimers[playerSlot].SetRespawnPos = null;
                    playerTimers[playerSlot].SetRespawnAng = null;
                    playerTimers[playerSlot].SoundsEnabled = soundsEnabledByDefault;

                    if (isForBot == false) _ = Task.Run(async () => await IsPlayerATester(steamID, playerSlot));

                    //PlayerSettings
                    if (enableDb) _ = Task.Run(async () => await GetPlayerStats(player, steamID, playerName, playerSlot, true));

                    if (connectMsgEnabled == true && !enableDb) PrintToChatAll(Localizer["connect_message", player.PlayerName]);
                    if (cmdJoinMsgEnabled == true) PrintAllEnabledCommands(player);

                    SharpTimerDebug($"Added player {player.PlayerName} with UserID {player.UserId} to connectedPlayers");
                    SharpTimerDebug($"Total players connected: {connectedPlayers.Count}");
                    SharpTimerDebug($"Total playerTimers: {playerTimers.Count}");
                    SharpTimerDebug($"Total playerReplays: {playerReplays.Count}");

                    if (removeLegsEnabled == true)
                    {
                        player.PlayerPawn.Value.Render = Color.FromArgb(254, 254, 254, 254);
                        Utilities.SetStateChanged(player.PlayerPawn.Value, "CBaseModelEntity", "m_clrRender");
                    }
                }
                finally
                {
                    if (connectedPlayers[playerSlot] == null)
                    {
                        connectedPlayers.Remove(playerSlot);
                    }

                    if (playerTimers[playerSlot] == null)
                    {
                        playerTimers.Remove(playerSlot);
                    }
                }
            }
            catch (Exception ex)
            {
                SharpTimerError($"Error in OnPlayerConnect: {ex.Message}");
            }
        }

        private void OnPlayerSpawn(CCSPlayerController? player)
        {
            //just.. dont ask.
            AddTimer(0f, () =>
            {
                if (spawnOnRespawnPos == true && currentRespawnPos != null)
                    player!.PlayerPawn.Value!.Teleport(currentRespawnPos!, null, null);
            });
        }

        private void OnPlayerDisconnect(CCSPlayerController? player, bool isForBot = false)
        {
            if (player == null) return;

            try
            {
                if (isForBot == true && connectedReplayBots.TryGetValue(player.Slot, out var connectedReplayBot))
                {
                    connectedReplayBots.Remove(player.Slot);
                    SharpTimerDebug($"Removed bot {connectedReplayBot.PlayerName} with UserID {connectedReplayBot.UserId} from connectedReplayBots.");
                }
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

                    playerTimers[player.Slot].TotalSync = 0;
                    playerTimers[player.Slot].GoodSync = 0;
                    playerTimers[player.Slot].Sync = 0;
                    playerTimers[player.Slot].Rotation = new List<QAngle>();

                    if (enableReplays)
                    {
                        //schizo removing data from memory
                        playerReplays[player.Slot] = new PlayerReplays();
                        playerReplays.Remove(player.Slot);
                    }

                    SharpTimerDebug($"Removed player {connectedPlayer.PlayerName} with UserID {connectedPlayer.UserId} from connectedPlayers.");
                    SharpTimerDebug($"Removed specTarget index {player.Pawn.Value.EntityHandle.Index} from specTargets.");
                    SharpTimerDebug($"Total players connected: {connectedPlayers.Count}");
                    SharpTimerDebug($"Total playerTimers: {playerTimers.Count}");
                    SharpTimerDebug($"Total specTargets: {specTargets.Count}");

                    if (connectMsgEnabled == true && isForBot == false)
                    {
                        PrintToChatAll(Localizer["disconnect_message", connectedPlayer.PlayerName]);
                    }
                }
            }
            catch (Exception ex)
            {
                SharpTimerError($"Error in OnPlayerDisconnect (probably replay bot related lolxd): {ex.Message}");
            }
        }

        private HookResult OnPlayerChat(CCSPlayerController? player, CommandInfo message)
        {
            if (displayChatTags == false)
                return HookResult.Continue;

            string msg;

            if (player == null || !player.IsValid || player.IsBot || string.IsNullOrEmpty(message.GetArg(1)))
                return HookResult.Handled;
            else
                msg = message.GetArg(1);

            if (msg.Length > 0 && (msg[0] == '!' || msg[0] == '/' || msg[0] == '.'))
                return HookResult.Continue;
            else
            {
                string rankColor = GetRankColorForChat(player);

                if (playerTimers.TryGetValue(player.Slot, out PlayerTimerInfo? value))
                {
                    string deadText = player.PawnIsAlive ? "" : $"{ChatColors.Grey}*DEAD* ";
                    string vipText = (value.IsVip ? $"{ChatColors.Magenta}{customVIPTag} " : "");
                    if (player.Team == CsTeam.Terrorist)
                        Server.PrintToChatAll($" {deadText}{vipText}{rankColor}{value.CachedRank} {ChatColors.ForTeam(CsTeam.Terrorist)}{player.PlayerName} {ChatColors.Default}: {msg}");
                    if (player.Team == CsTeam.CounterTerrorist)
                        Server.PrintToChatAll($" {deadText}{vipText}{rankColor}{value.CachedRank} {ChatColors.ForTeam(CsTeam.CounterTerrorist)}{player.PlayerName} {ChatColors.Default}: {msg}");
                    if (player.Team == CsTeam.Spectator)
                        Server.PrintToChatAll($" {ChatColors.Grey}*SPEC* {ChatColors.ForTeam(CsTeam.Spectator)}{player.PlayerName} {ChatColors.Default}: {msg}");
                    if (player.Team == CsTeam.None)
                        Server.PrintToChatAll($" {ChatColors.ForTeam(CsTeam.None)}{player.PlayerName} {ChatColors.Default}: {msg}");
                }

                return HookResult.Handled;
            }

        }
    }
}