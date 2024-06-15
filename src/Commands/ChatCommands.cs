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

using System.Runtime.InteropServices;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes.Registration;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Utils;


namespace SharpTimer
{
    public partial class SharpTimer
    {
        [ConsoleCommand("css_dp_timers", "Prints playerTimers")]
        [CommandHelper(whoCanExecute: CommandUsage.SERVER_ONLY)]
        public void DeepPrintPlayerTimers(CCSPlayerController? player, CommandInfo command)
        {
            SharpTimerConPrint("Printing Player Timers:");
            foreach (var kvp in playerTimers)
            {
                SharpTimerConPrint($"PlayerSlot: {kvp.Key}");
                foreach (var prop in typeof(PlayerTimerInfo).GetProperties())
                {
                    var value = prop.GetValue(kvp.Value, null);
                    SharpTimerConPrint($"  {prop.Name}: {value}");
                    if (value is Dictionary<int, int> intIntDictionary)
                    {
                        SharpTimerConPrint($"    {prop.Name}:");
                        foreach (var entry in intIntDictionary)
                        {
                            SharpTimerConPrint($"      {entry.Key}: {entry.Value}");
                        }
                    }
                    else if (value is Dictionary<int, string> intStringDictionary)
                    {
                        SharpTimerConPrint($"    {prop.Name}:");
                        foreach (var entry in intStringDictionary)
                        {
                            SharpTimerConPrint($"      {entry.Key}: {entry.Value}");
                        }
                    }
                }

                SharpTimerConPrint(" ");
            }
            SharpTimerConPrint("End of Player Timers");
        }

        [ConsoleCommand("css_replay", "Replay command")]
        [CommandHelper(whoCanExecute: CommandUsage.CLIENT_ONLY)]
        public void ReplayCommand(CCSPlayerController? player, CommandInfo command)
        {
            if (!IsAllowedPlayer(player) || enableReplays == false) return;

            var playerSlot = player!.Slot;
            var steamID = player.SteamID.ToString();
            var playerName = player.PlayerName;

            if (!playerTimers[playerSlot].IsTimerBlocked)
            {
                player.PrintToChat(msgPrefix + $" Please stop your timer using {primaryChatColor}!timer{ChatColors.White} first!");
                return;
            }

            if (playerTimers[playerSlot].IsReplaying)
            {
                player.PrintToChat(msgPrefix + $" Please end your current replay first {primaryChatColor}!stop or {primaryChatColor}!stopreplay");
                return;
            }

            player.PrintToChat(msgPrefix + $" Available replay cmds: {primaryChatColor}!replaypb{ChatColors.Default} | {primaryChatColor}!replaytop <1-10>{ChatColors.Default} | {primaryChatColor}!replaysr{ChatColors.Default}");
            player.PrintToChat(msgPrefix + $" Replaying the Server Map Record, type {primaryChatColor}!stop or {primaryChatColor}!stopreplay {ChatColors.White}to exit the replay");

            _ = Task.Run(async () => await ReplayHandler(player, playerSlot, "1"));
        }

        [ConsoleCommand("css_replaypb", "Replay your last pb")]
        [CommandHelper(whoCanExecute: CommandUsage.CLIENT_ONLY)]
        public void ReplaySelfCommand(CCSPlayerController? player, CommandInfo command)
        {
            if (!IsAllowedPlayer(player) || enableReplays == false) return;

            var playerSlot = player!.Slot;
            var steamID = player.SteamID.ToString();
            var playerName = player.PlayerName;

            if (!playerTimers[playerSlot].IsTimerBlocked)
            {
                player.PrintToChat(msgPrefix + $" Please stop your timer using {primaryChatColor}!timer{ChatColors.White} first!");
                return;
            }

            if (playerTimers[playerSlot].IsReplaying)
            {
                player.PrintToChat(msgPrefix + $" Please end your current replay first {primaryChatColor}!stop or {primaryChatColor}!stopreplay");
                return;
            }

            _ = Task.Run(async () => await ReplayHandler(player, playerSlot, "self", steamID, playerName));
        }

        [ConsoleCommand("css_replaysr", "Replay server map record")]
        [CommandHelper(whoCanExecute: CommandUsage.CLIENT_ONLY)]
        public void ReplaySRCommand(CCSPlayerController? player, CommandInfo command)
        {
            if (!IsAllowedPlayer(player) || enableReplays == false) return;

            var playerSlot = player!.Slot;

            if (!playerTimers[playerSlot].IsTimerBlocked)
            {
                player.PrintToChat(msgPrefix + $" Please stop your timer using {primaryChatColor}!timer{ChatColors.White} first!");
                return;
            }

            if (playerTimers[playerSlot].IsReplaying)
            {
                player.PrintToChat(msgPrefix + $" Please end your current replay first {primaryChatColor}!stop or {primaryChatColor}!stopreplay");
                return;
            }

            _ = Task.Run(async () => await ReplayHandler(player, playerSlot));
        }

        [ConsoleCommand("css_replaytop", "Replay a top 10 server map record")]
        [CommandHelper(minArgs: 1, usage: "[1-10]", whoCanExecute: CommandUsage.CLIENT_ONLY)]
        public void ReplayTop10SRCommand(CCSPlayerController? player, CommandInfo command)
        {
            if (!IsAllowedPlayer(player) || enableReplays == false) return;

            var playerSlot = player!.Slot;

            if (!playerTimers[playerSlot].IsTimerBlocked)
            {
                player.PrintToChat(msgPrefix + $" Please stop your timer using {primaryChatColor}!timer{ChatColors.White} first!");
                return;
            }

            if (playerTimers[playerSlot].IsReplaying)
            {
                player.PrintToChat(msgPrefix + $" Please end your current replay first {primaryChatColor}!stop or {primaryChatColor}!stopreplay");
                return;
            }

            string arg = command.ArgByIndex(1);

            _ = Task.Run(async () => await ReplayHandler(player, playerSlot, arg));
        }

        [ConsoleCommand("css_replayb", "Replay a top 10 server bonus record")]
        [ConsoleCommand("css_replaybonus", "Replay a top 10 server bonus record")]
        [CommandHelper(minArgs: 1, usage: "[1-10] [bonus stage]", whoCanExecute: CommandUsage.CLIENT_ONLY)]
        public void ReplayBonusCommand(CCSPlayerController? player, CommandInfo command)
        {
            if (!IsAllowedPlayer(player) || enableReplays == false) return;

            var playerSlot = player!.Slot;

            if (!playerTimers[playerSlot].IsTimerBlocked)
            {
                player.PrintToChat(msgPrefix + $" Please stop your timer using {primaryChatColor}!timer{ChatColors.White} first!");
                return;
            }

            if (playerTimers[playerSlot].IsReplaying)
            {
                player.PrintToChat(msgPrefix + $" Please end your current replay first {primaryChatColor}!stop or {primaryChatColor}!stopreplay");
                return;
            }

            string arg = command.ArgByIndex(1);
            string arg2 = command.ArgByIndex(2);

            _ = Task.Run(async () => await ReplayHandler(player, playerSlot, arg, "69", "unknown", Int16.Parse(arg2)));
        }

        [ConsoleCommand("css_replaybpb", "Replay your bonus pb")]
        [ConsoleCommand("css_replaybonuspb", "Replay your bonus pb")]
        [CommandHelper(minArgs: 1, usage: "[bonus stage]", whoCanExecute: CommandUsage.CLIENT_ONLY)]
        public void ReplayBonusPBCommand(CCSPlayerController? player, CommandInfo command)
        {
            if (!IsAllowedPlayer(player) || enableReplays == false) return;

            var playerSlot = player!.Slot;
            var steamID = player.SteamID.ToString();
            var playerName = player.PlayerName;

            if (!playerTimers[playerSlot].IsTimerBlocked)
            {
                player.PrintToChat(msgPrefix + $" Please stop your timer using {primaryChatColor}!timer{ChatColors.White} first!");
                return;
            }

            if (playerTimers[playerSlot].IsReplaying)
            {
                player.PrintToChat(msgPrefix + $" Please end your current replay first {primaryChatColor}!stop or {primaryChatColor}!stopreplay");
                return;
            }

            string arg = command.ArgByIndex(1);
            int bonusX = Int16.Parse(arg);

            _ = Task.Run(async () => await ReplayHandler(player, playerSlot, "self", steamID, playerName, bonusX));
        }

        public async Task ReplayHandler(CCSPlayerController player, int playerSlot, string arg = "1", string pbSteamID = "69", string playerName = "unknown", int bonusX = 0)
        {
            bool self = false;

            int top10 = 1;
            if (arg != "self" && (!int.TryParse(arg, out top10) || top10 <= 0 || top10 > 10))
            {
                top10 = 1;
            }
            else if (arg == "self")
            {
                self = true;
            }

            playerReplays.Remove(playerSlot);
            playerReplays[playerSlot] = new PlayerReplays();

            var (srSteamID, srPlayerName, srTime) = ("null", "null", "null");

            if (!self)
            {
                if (useMySQL || usePostgres)
                {
                    (srSteamID, srPlayerName, srTime) = await GetMapRecordSteamIDFromDatabase(bonusX, top10);
                }
                else
                {
                    (srSteamID, srPlayerName, srTime) = await GetMapRecordSteamID(bonusX, top10);
                }
            }


            if ((srSteamID == "null" || srPlayerName == "null" || srTime == "null") && !self)
            {
                Server.NextFrame(() => player.PrintToChat(msgPrefix + $"No Server Record to replay!"));
                return;
            }

            await ReadReplayFromJson(player, !self ? srSteamID : pbSteamID, playerSlot, bonusX);

            if (playerReplays[playerSlot].replayFrames.Count == 0) return;

            if (useMySQL || usePostgres) await GetReplayVIPGif(!self ? srSteamID : pbSteamID, playerSlot);

            playerTimers[playerSlot].IsReplaying = !playerTimers[playerSlot].IsReplaying;
            playerTimers[playerSlot].ReplayHUDString = !self ? $"{srPlayerName} | {srTime}" : $"{playerName} | {playerTimers[playerSlot].CachedPB}";

            playerTimers[playerSlot].IsTimerRunning = false;
            playerTimers[playerSlot].TimerTicks = 0;
            playerTimers[playerSlot].IsBonusTimerRunning = false;
            playerTimers[playerSlot].BonusTimerTicks = 0;
            playerReplays[playerSlot].CurrentPlaybackFrame = 0;
            if (stageTriggers.Count != 0) playerTimers[playerSlot].StageTimes!.Clear(); //remove previous stage times if the map has stages
            if (stageTriggers.Count != 0) playerTimers[playerSlot].StageVelos!.Clear(); //remove previous stage times if the map has stages

            if (IsAllowedPlayer(player))
            {
                Server.NextFrame(() => player.PrintToChat(msgPrefix + $" Replaying {(!self ? "the Server Top" + top10 : "your PB")}, type {primaryChatColor}!stop or {primaryChatColor}!stopreplay {ChatColors.White}to exit the replay"));
            }
            else
            {
                SharpTimerError($"Error in ReplayHandler: player not allowed or not on server anymore");
            }
        }

        [ConsoleCommand("css_stop", "stops the current replay")]
        [ConsoleCommand("css_stopreplay", "stops the current replay")]
        [CommandHelper(whoCanExecute: CommandUsage.CLIENT_ONLY)]
        public void StopReplayCommand(CCSPlayerController? player, CommandInfo command)
        {
            if (!IsAllowedPlayer(player) || enableReplays == false) return;

            var playerSlot = player!.Slot;

            if (!playerTimers[playerSlot].IsTimerBlocked || !playerTimers[playerSlot].IsReplaying)
            {
                player.PrintToChat(msgPrefix + $" No Replay playing currently");
                return;
            }

            if (playerTimers[playerSlot].IsReplaying)
            {
                player.PrintToChat(msgPrefix + $" Ending Replay!");
                playerTimers[playerSlot].IsReplaying = false;
                if (player.PlayerPawn.Value!.MoveType != MoveType_t.MOVETYPE_WALK || player.PlayerPawn.Value.ActualMoveType == MoveType_t.MOVETYPE_WALK) SetMoveType(player, MoveType_t.MOVETYPE_WALK);
                RespawnPlayerCommand(player, command);
                playerReplays.Remove(playerSlot);
                playerReplays[playerSlot] = new PlayerReplays();
                playerTimers[playerSlot].IsTimerRunning = false;
                playerTimers[playerSlot].TimerTicks = 0;
                playerTimers[playerSlot].IsBonusTimerRunning = false;
                playerTimers[playerSlot].BonusTimerTicks = 0;
                playerReplays[playerSlot].CurrentPlaybackFrame = 0;
                if (stageTriggers.Count != 0) playerTimers[playerSlot].StageTimes!.Clear(); //remove previous stage times if the map has stages
                if (stageTriggers.Count != 0) playerTimers[playerSlot].StageVelos!.Clear(); //remove previous stage times if the map has stages
            }
        }

        [ConsoleCommand("css_help", "alias for !sthelp")]
        [ConsoleCommand("css_sthelp", "Prints all commands for the player")]
        [CommandHelper(whoCanExecute: CommandUsage.CLIENT_ONLY)]
        public void HelpCommand(CCSPlayerController? player, CommandInfo command)
        {
            if (!IsAllowedPlayer(player) || !helpEnabled)
            {
                if (!IsAllowedSpectator(player))
                    return;
            }

            PrintAllEnabledCommands(player!);
        }

        /* [ConsoleCommand("css_spec", "Moves you to Spectator")]
        [CommandHelper(whoCanExecute: CommandUsage.CLIENT_ONLY)]
        public void SpecCommand(CCSPlayerController? player, CommandInfo command)
        {
            if ((CsTeam)player.TeamNum == CsTeam.Spectator)
            {
                player.ChangeTeam(CsTeam.CounterTerrorist);
                player.PrintToChat(msgPrefix + $"Moving you to CT");
            }
            else
            {
                player.ChangeTeam(CsTeam.Spectator);
                player.PrintToChat(msgPrefix + $"Moving you to Spectator");
            }
        } */

        [ConsoleCommand("css_hud", "Draws/Hides The timer HUD")]
        [CommandHelper(whoCanExecute: CommandUsage.CLIENT_ONLY)]
        public void HUDSwitchCommand(CCSPlayerController? player, CommandInfo command)
        {
            if (!IsAllowedPlayer(player))
            {
                if (!IsAllowedSpectator(player))
                    return;
            }

            var playerName = player!.PlayerName;
            var playerSlot = player.Slot;
            var steamID = player.SteamID.ToString();

            SharpTimerDebug($"{playerName} calling css_hud...");

            if (playerTimers[playerSlot].TicksSinceLastCmd < cmdCooldown)
            {
                player.PrintToChat(msgPrefix + $" Command is on cooldown. Chill...");
                return;
            }

            playerTimers[playerSlot].TicksSinceLastCmd = 0;

            playerTimers[playerSlot].HideTimerHud = !playerTimers[playerSlot].HideTimerHud;

            player.PrintToChat(msgPrefix + $" Hud is now: {(playerTimers[playerSlot].HideTimerHud ? $"{ChatColors.Red} Hidden" : $"{ChatColors.Green} Shown")}");
            SharpTimerDebug($"Hide Timer HUD set to: {playerTimers[playerSlot].HideTimerHud} for {playerName}");

            if (useMySQL || usePostgres)
            {
                _ = Task.Run(async () => await SetPlayerStats(player, steamID, playerName, playerSlot));
            }
        }

        [ConsoleCommand("css_keys", "Draws/Hides HUD Keys")]
        [CommandHelper(whoCanExecute: CommandUsage.CLIENT_ONLY)]
        public void KeysSwitchCommand(CCSPlayerController? player, CommandInfo command)
        {
            if (!IsAllowedPlayer(player))
            {
                if (!IsAllowedSpectator(player))
                    return;
            }

            var playerName = player!.PlayerName;
            var playerSlot = player.Slot;
            var steamID = player.SteamID.ToString();

            SharpTimerDebug($"{playerName} calling css_keys...");

            if (playerTimers[playerSlot].TicksSinceLastCmd < cmdCooldown)
            {
                player.PrintToChat(msgPrefix + $" Command is on cooldown. Chill...");
                return;
            }

            playerTimers[playerSlot].TicksSinceLastCmd = 0;

            playerTimers[playerSlot].HideKeys = playerTimers[playerSlot].HideKeys ? false : true;

            player.PrintToChat(msgPrefix + $" Keys are now: {(playerTimers[playerSlot].HideKeys ? $"{ChatColors.Red} Hidden" : $"{ChatColors.Green} Shown")}");
            SharpTimerDebug($"Hide Timer HUD set to: {playerTimers[playerSlot].HideKeys} for {playerName}");

            if (useMySQL || usePostgres)
            {
                _ = Task.Run(async () => await SetPlayerStats(player, steamID, playerName, playerSlot));
            }

        }

        [ConsoleCommand("css_sounds", "Toggles Sounds")]
        [CommandHelper(whoCanExecute: CommandUsage.CLIENT_ONLY)]
        public void SoundsSwitchCommand(CCSPlayerController? player, CommandInfo command)
        {
            if (!IsAllowedPlayer(player))
            {
                if (!IsAllowedSpectator(player))
                    return;
            }

            var playerName = player!.PlayerName;
            var playerSlot = player.Slot;
            var steamID = player.SteamID.ToString();

            SharpTimerDebug($"{playerName} calling css_sounds...");

            if (playerTimers[playerSlot].TicksSinceLastCmd < cmdCooldown)
            {
                player.PrintToChat(msgPrefix + $" Command is on cooldown. Chill...");
                return;
            }

            playerTimers[playerSlot].TicksSinceLastCmd = 0;

            playerTimers[playerSlot].SoundsEnabled = playerTimers[playerSlot].SoundsEnabled ? false : true;

            player.PrintToChat(msgPrefix + $"Sounds are now:{(playerTimers[playerSlot].SoundsEnabled ? $"{ChatColors.Green} ON" : $"{ChatColors.Red} OFF")}");
            SharpTimerDebug($"Timer Sounds set to: {playerTimers[playerSlot].SoundsEnabled} for {playerName}");

            if (useMySQL || usePostgres)
            {
                _ = Task.Run(async () => await SetPlayerStats(player, steamID, playerName, playerSlot));
            }
        }

        [ConsoleCommand("css_jumpstats", "Toggles JumpStats")]
        [CommandHelper(whoCanExecute: CommandUsage.CLIENT_ONLY)]
        public void JSSwitchCommand(CCSPlayerController? player, CommandInfo command)
        {
            if (!IsAllowedPlayer(player) || jumpStatsEnabled == false)
            {
                if (!IsAllowedSpectator(player))
                    return;
            }

            var playerName = player!.PlayerName;
            var playerSlot = player.Slot;
            var steamID = player.SteamID.ToString();

            SharpTimerDebug($"{playerName} calling css_jumpstats...");

            if (playerTimers[playerSlot].TicksSinceLastCmd < cmdCooldown)
            {
                player.PrintToChat(msgPrefix + $" Command is on cooldown. Chill...");
                return;
            }

            playerTimers[playerSlot].TicksSinceLastCmd = 0;

            playerTimers[playerSlot].HideJumpStats = playerTimers[playerSlot].HideJumpStats ? false : true;

            player.PrintToChat(msgPrefix + $"Jump Stats are now:{(playerTimers[playerSlot].HideJumpStats ? $"{ChatColors.Red} Hidden" : $"{ChatColors.Green} Shown")}");
            SharpTimerDebug($"Hide Jump Stats set to: {playerTimers[playerSlot].HideJumpStats} for {playerName}");

            if (useMySQL || usePostgres)
            {
                _ = Task.Run(async () => await SetPlayerStats(player, steamID, playerName, playerSlot));
            }

        }

        [ConsoleCommand("css_hideweapon", "Hides the players weapon")]
        [CommandHelper(whoCanExecute: CommandUsage.CLIENT_ONLY)]
        public void HideWeaponCommand(CCSPlayerController? player, CommandInfo command)
        {
            if (!IsAllowedPlayer(player)) return;

            HideWeapon(player);
        }
        public void HideWeapon(CCSPlayerController? player)
        {
            if (!player.PlayerPawn!.Value.WeaponServices.ActiveWeapon.IsValid)
            {
                player.GiveNamedItem("weapon_usp_silencer");
                player.GiveNamedItem("weapon_knife");
            }
            else
            {
                player.RemoveWeapons();
            }
        }
        [ConsoleCommand("css_fov", "Sets the player's FOV")]
        [CommandHelper(minArgs: 1, usage: "[fov]")]
        public void FovCommand(CCSPlayerController? player, CommandInfo command)
        {
            if (!IsAllowedPlayer(player) || fovChangerEnabled == false) return;

            if (!Int32.TryParse(command.GetArg(1), out var desiredFov)) return;

            SetFov(player, desiredFov);
        }

        public void SetFov(CCSPlayerController? player, int desiredFov, bool noMySql = false)
        {
            player!.DesiredFOV = (uint)desiredFov;
            Utilities.SetStateChanged(player, "CBasePlayerController", "m_iDesiredFOV");

            var playerName = player.PlayerName;
            var playerSlot = player.Slot;
            var steamID = player.SteamID.ToString();

            if (noMySql == false) playerTimers[player.Slot].PlayerFov = desiredFov;
            if (useMySQL == true && noMySql == false)
            {
                _ = Task.Run(async () => await SetPlayerStats(player, steamID, playerName, playerSlot));
            }
            if (usePostgres == true && noMySql == false)
            {
                _ = Task.Run(async () => await SetPlayerStats(player, steamID, playerName, playerSlot));
            }
        }

        [ConsoleCommand("css_top", "Prints top players of this map")]
        [ConsoleCommand("css_mtop", "alias for !top")]
        [CommandHelper(whoCanExecute: CommandUsage.CLIENT_ONLY)]
        public void PrintTopRecords(CCSPlayerController? player, CommandInfo command)
        {
            if (!IsAllowedPlayer(player) || topEnabled == false)
            {
                if (!IsAllowedSpectator(player))
                    return;
            }

            var playerName = player!.PlayerName;

            SharpTimerDebug($"{playerName} calling css_top...");

            if (playerTimers[player.Slot].TicksSinceLastCmd < cmdCooldown)
            {
                player.PrintToChat(msgPrefix + $" Command is on cooldown. Chill...");
                return;
            }

            playerTimers[player.Slot].TicksSinceLastCmd = 0;

            var mapName = command.ArgByIndex(1);

            _ = Task.Run(async () => await PrintTopRecordsHandler(player, playerName, 0, string.IsNullOrEmpty(mapName) ? "" : mapName));
        }

        [ConsoleCommand("css_points", "Prints top points")]
        [CommandHelper(whoCanExecute: CommandUsage.CLIENT_ONLY)]
        public void PrintTopPoints(CCSPlayerController? player, CommandInfo command)
        {
            if (!IsAllowedPlayer(player) || globalRanksEnabled == false)
            {
                if (!IsAllowedSpectator(player))
                    return;
            }

            var playerName = player!.PlayerName;

            SharpTimerDebug($"{playerName} calling css_points...");

            if (playerTimers[player.Slot].TicksSinceLastCmd < cmdCooldown)
            {
                player.PrintToChat(msgPrefix + $" Command is on cooldown. Chill...");
                return;
            }

            playerTimers[player.Slot].TicksSinceLastCmd = 0;

            _ = Task.Run(async () => await PrintTop10PlayerPoints(player));
        }

        [ConsoleCommand("css_topbonus", "Prints top players of this map bonus")]
        [ConsoleCommand("css_btop", "alias for !topbonus")]
        [CommandHelper(whoCanExecute: CommandUsage.CLIENT_ONLY)]
        public void PrintTopBonusRecords(CCSPlayerController? player, CommandInfo command)
        {
            if (!IsAllowedPlayer(player) || topEnabled == false)
            {
                if (!IsAllowedSpectator(player))
                    return;
            }

            var playerName = player!.PlayerName;

            SharpTimerDebug($"{playerName} calling css_topbonus...");

            if (playerTimers[player.Slot].TicksSinceLastCmd < cmdCooldown)
            {
                player.PrintToChat(msgPrefix + $" Command is on cooldown. Chill...");
                return;
            }

            playerTimers[player.Slot].TicksSinceLastCmd = 0;

            if (!int.TryParse(command.ArgString, out int bonusX))
            {
                SharpTimerDebug("css_topbonus conversion failed. The input string is not a valid integer.");
                player.PrintToChat(msgPrefix + $" Please enter a valid Bonus stage i.e: {primaryChatColor}!topbonus 1");
                return;
            }

            _ = Task.Run(async () => await PrintTopRecordsHandler(player, playerName, bonusX));
        }

        public async Task PrintTopRecordsHandler(CCSPlayerController? player, string playerName, int bonusX = 0, string mapName = "")
        {
            if (!IsAllowedPlayer(player) || topEnabled == false) return;
            SharpTimerDebug($"Handling !top for {playerName}");

            string? currentMapNamee;
            if (string.IsNullOrEmpty(mapName))
                currentMapNamee = bonusX == 0 ? currentMapName! : $"{currentMapName}_bonus{bonusX}";
            else
                currentMapNamee = mapName;

            Dictionary<string, PlayerRecord> sortedRecords;
            if (useMySQL || usePostgres)
            {
                sortedRecords = await GetSortedRecordsFromDatabase(10, bonusX, mapName);
            }
            else
            {
                sortedRecords = await GetSortedRecords(bonusX, mapName);
            }

            if (sortedRecords.Count == 0)
            {
                Server.NextFrame(() =>
                {
                    if (IsAllowedPlayer(player)) player!.PrintToChat(msgPrefix + $" No records available for{(bonusX != 0 ? $" Bonus {bonusX} on" : "")} {currentMapNamee}.");
                });
                return;
            }

            List<string> printStatements = [$"{msgPrefix} Top 10 Records for{(bonusX != 0 ? $" Bonus {bonusX} on" : "")} {currentMapNamee}:"];

            int rank = 1;

            foreach (var kvp in sortedRecords.Take(10))
            {
                string _playerName = kvp.Value.PlayerName!;
                int timerTicks = kvp.Value.TimerTicks;

                bool showReplays = false;
                if (enableReplays == true) showReplays = await CheckSRReplay(kvp.Key, bonusX);

                printStatements.Add($"{msgPrefix} #{rank}: {primaryChatColor}{_playerName} {ChatColors.White}- {(enableReplays ? $"{(showReplays ? $" {ChatColors.Red}◉" : "")}" : "")}{primaryChatColor}{FormatTime(timerTicks)}");
                rank++;
            }

            Server.NextFrame(() =>
            {
                if (IsAllowedPlayer(player))
                {
                    foreach (var statement in printStatements)
                    {
                        player!.PrintToChat(statement);
                    }
                }
            });
        }

        [ConsoleCommand("css_rank", "Tells you your rank on this map")]
        [CommandHelper(whoCanExecute: CommandUsage.CLIENT_ONLY)]
        public void RankCommand(CCSPlayerController? player, CommandInfo command)
        {
            if (!IsAllowedPlayer(player) || rankEnabled == false)
            {
                if (!IsAllowedSpectator(player))
                    return;
            }

            var playerName = player!.PlayerName;
            var playerSlot = player.Slot;
            var steamID = player.SteamID.ToString();

            SharpTimerDebug($"{playerName} calling css_rank...");

            if (playerTimers[player.Slot].TicksSinceLastCmd < cmdCooldown)
            {
                player.PrintToChat(msgPrefix + $" Command is on cooldown. Chill...");
                return;
            }

            _ = Task.Run(async () => await RankCommandHandler(player, steamID, playerSlot, playerName));
        }

        public async Task RankCommandHandler(CCSPlayerController? player, string steamId, int playerSlot, string playerName, bool sendRankToHUD = false)
        {
            try
            {
                if (!IsAllowedPlayer(player))
                {
                    SharpTimerError($"Error in RankCommandHandler: Player not allowed or not on server anymore");
                    return;
                }

                //SharpTimerDebug($"Handling !rank for {playerName}...");

                string ranking, rankIcon, mapPlacement, serverPoints = "", serverPlacement = "";
                bool useGlobalRanks = (useMySQL || usePostgres) && globalRanksEnabled;

                ranking = useGlobalRanks ? await GetPlayerServerPlacement(player, steamId, playerName) : await GetPlayerMapPlacementWithTotal(player, steamId, playerName);
                rankIcon = useGlobalRanks ? await GetPlayerServerPlacement(player, steamId, playerName, true) : await GetPlayerMapPlacementWithTotal(player, steamId, playerName, true);
                mapPlacement = await GetPlayerMapPlacementWithTotal(player, steamId, playerName, false, true);

                if (useGlobalRanks)
                {
                    serverPoints = await GetPlayerServerPlacement(player, steamId, playerName, false, false, true);
                    serverPlacement = await GetPlayerServerPlacement(player, steamId, playerName, false, true, false);
                }

                int pbTicks = (useMySQL || usePostgres) ? await GetPreviousPlayerRecordFromDatabase(player, steamId, currentMapName!, playerName) : await GetPreviousPlayerRecord(player, steamId);

                Server.NextFrame(() =>
                {
                    if (!IsAllowedPlayer(player)) return;
                    playerTimers[playerSlot].RankHUDIcon = $"{(!string.IsNullOrEmpty(rankIcon) ? $" {rankIcon}" : "")}";
                    playerTimers[playerSlot].CachedPB = $"{(pbTicks != 0 ? $" {FormatTime(pbTicks)}" : "")}";
                    playerTimers[playerSlot].CachedRank = ranking;
                    playerTimers[playerSlot].CachedMapPlacement = mapPlacement;

                    if (displayScoreboardTags) AddScoreboardTagToPlayer(player!, ranking);
                });

                if (!sendRankToHUD)
                {
                    Server.NextFrame(() =>
                    {
                        if (!IsAllowedPlayer(player)) return;
                        string rankMessage = $"{msgPrefix} You are currently {primaryChatColor}{ranking}";
                        if (useGlobalRanks)
                        {
                            rankMessage += $" {ChatColors.Default}({primaryChatColor}{serverPoints}{ChatColors.Default}) [{primaryChatColor}{serverPlacement}{ChatColors.Default}]";
                        }
                        player!.PrintToChat(rankMessage);
                        if (pbTicks != 0)
                        {
                            player.PrintToChat($"{msgPrefix} Your current PB on {primaryChatColor}{currentMapName}{ChatColors.Default}: {primaryChatColor}{FormatTime(pbTicks)}{ChatColors.Default} [{primaryChatColor}{mapPlacement}{ChatColors.Default}]");
                        }
                    });
                }
            }
            catch (Exception ex)
            {
                SharpTimerError($"Error in RankCommandHandler: {ex}");
            }
        }

        [ConsoleCommand("css_sr", "Tells you the Server record on this map")]
        [CommandHelper(whoCanExecute: CommandUsage.CLIENT_ONLY)]
        public void SRCommand(CCSPlayerController? player, CommandInfo command)
        {
            if (!IsAllowedPlayer(player) || rankEnabled == false)
            {
                if (!IsAllowedSpectator(player))
                    return;
            }

            var playerName = player!.PlayerName;

            SharpTimerDebug($"{playerName} calling css_sr...");

            if (playerTimers[player.Slot].TicksSinceLastCmd < cmdCooldown)
            {
                player.PrintToChat(msgPrefix + $" Command is on cooldown. Chill...");
                return;
            }

            _ = Task.Run(async () => await SRCommandHandler(player, playerName));
        }

        public async Task SRCommandHandler(CCSPlayerController? player, string _playerName)
        {
            if (!IsAllowedPlayer(player) || rankEnabled == false) return;
            SharpTimerDebug($"Handling !sr for {_playerName}...");
            Dictionary<string, PlayerRecord> sortedRecords;
            if (!useMySQL && !usePostgres)
            {
                sortedRecords = await GetSortedRecords();
            }
            else
            {
                sortedRecords = await GetSortedRecordsFromDatabase();
            }

            if (sortedRecords.Count == 0)
            {
                return;
            }

            Server.NextFrame(() =>
            {
                if (!IsAllowedPlayer(player)) return;
                player!.PrintToChat($"{msgPrefix} Current Server Record on {primaryChatColor}{currentMapName}{ChatColors.White}: ");
            });

            foreach (var kvp in sortedRecords.Take(1))
            {
                string playerName = kvp.Value.PlayerName!;
                int timerTicks = kvp.Value.TimerTicks;
                Server.NextFrame(() =>
                {
                    if (!IsAllowedPlayer(player)) return;
                    player!.PrintToChat(msgPrefix + $" {primaryChatColor}{playerName} {ChatColors.White}- {primaryChatColor}{FormatTime(timerTicks)}");
                });
            }
        }

        [ConsoleCommand("css_rb", "Teleports you to Bonus start")]
        [ConsoleCommand("css_b", "alias for !rb")]
        [CommandHelper(whoCanExecute: CommandUsage.CLIENT_ONLY)]
        public void RespawnBonusPlayer(CCSPlayerController? player, CommandInfo command)
        {
            try
            {
                if (!IsAllowedPlayer(player) || respawnEnabled == false) return;
                SharpTimerDebug($"{player!.PlayerName} calling css_rb...");

                if (playerTimers[player.Slot].TicksSinceLastCmd < cmdCooldown)
                {
                    player.PrintToChat(msgPrefix + $" Command is on cooldown. Chill...");
                    return;
                }

                if (playerTimers[player.Slot].IsReplaying)
                {
                    player.PrintToChat(msgPrefix + $" Please end your current replay first {primaryChatColor}!stop or {primaryChatColor}!stopreplay");
                    return;
                }

                playerTimers[player.Slot].TicksSinceLastCmd = 0;

                if (!int.TryParse(command.ArgString, out int bonusX))
                {
                    SharpTimerDebug("css_rb conversion failed. The input string is not a valid integer.");
                    player.PrintToChat(msgPrefix + $" Please enter a valid Bonus stage i.e: {primaryChatColor}!rb <index>");
                    return;
                }

                // Remove checkpoints for the current player
                if (!playerTimers[player.Slot].IsTimerBlocked)
                {
                    playerCheckpoints.Remove(player.Slot);
                }

                if (jumpStatsEnabled) InvalidateJS(player.Slot);

                if (bonusRespawnPoses[bonusX] != null)
                {
                    if (bonusRespawnAngs.TryGetValue(bonusX, out QAngle? bonusAng) && bonusAng != null)
                    {
                        player.PlayerPawn.Value!.Teleport(bonusRespawnPoses[bonusX]!, bonusRespawnAngs[bonusX]!, new Vector(0, 0, 0));
                    }
                    else
                    {
                        player.PlayerPawn.Value!.Teleport(bonusRespawnPoses[bonusX]!, new QAngle(player.PlayerPawn.Value.EyeAngles.X, player.PlayerPawn.Value.EyeAngles.Y, player.PlayerPawn.Value.EyeAngles.Z) ?? new QAngle(0, 0, 0), new Vector(0, 0, 0));
                    }
                    SharpTimerDebug($"{player.PlayerName} css_rb {bonusX} to {bonusRespawnPoses[bonusX]}");
                }
                else
                {
                    player.PrintToChat(msgPrefix + $" {ChatColors.LightRed} No RespawnBonusPos with index {bonusX} found for current map!");
                }

                Server.NextFrame(() =>
                {
                    playerTimers[player.Slot].IsTimerRunning = false;
                    playerTimers[player.Slot].TimerTicks = 0;
                    playerTimers[player.Slot].IsBonusTimerRunning = false;
                    playerTimers[player.Slot].BonusTimerTicks = 0;
                });

                if (playerTimers[player.Slot].SoundsEnabled != false) player.ExecuteClientCommand($"play {respawnSound}");
            }
            catch (Exception ex)
            {
                SharpTimerError($"Exception in RespawnBonusPlayer: {ex.Message}");
            }
        }

        [ConsoleCommand("css_startpos", "Saves a custom respawn point within the start trigger")]
        [ConsoleCommand("css_setresp", "Saves a custom respawn point within the start trigger")]
        [CommandHelper(whoCanExecute: CommandUsage.CLIENT_ONLY)]
        public void SetRespawnCommand(CCSPlayerController? player, CommandInfo command)
        {
            if (!IsAllowedPlayer(player) || respawnEnabled == false) return;

            SharpTimerDebug($"{player!.PlayerName} calling css_rank...");

            if (playerTimers[player.Slot].TicksSinceLastCmd < cmdCooldown)
            {
                player.PrintToChat(msgPrefix + $" Command is on cooldown. Chill...");
                return;
            }

            if (playerTimers[player.Slot].IsReplaying)
            {
                player.PrintToChat(msgPrefix + $" Please end your current replay first {primaryChatColor}!stop or {primaryChatColor}!stopreplay");
                return;
            }

            if (useTriggers == false)
            {
                player.PrintToChat(msgPrefix + $" Current Map is using manual zones");
                return;
            }

            // Get the player's current position and rotation
            Vector currentPosition = player.Pawn.Value!.CBodyComponent?.SceneNode?.AbsOrigin ?? new Vector(0, 0, 0);
            QAngle currentRotation = player.PlayerPawn.Value!.EyeAngles ?? new QAngle(0, 0, 0);

            if (useTriggers == true)
            {
                if (IsVectorInsideBox(currentPosition + new Vector(0, 0, 10), currentMapStartTriggerMaxs!, currentMapStartTriggerMins!))
                {
                    // Convert position and rotation to strings
                    string positionString = $"{currentPosition.X} {currentPosition.Y} {currentPosition.Z}";
                    string rotationString = $"{currentRotation.X} {currentRotation.Y} {currentRotation.Z}";

                    playerTimers[player.Slot].SetRespawnPos = positionString;
                    playerTimers[player.Slot].SetRespawnAng = rotationString;
                    player.PrintToChat(msgPrefix + $" Saved custom Start Zone RespawnPos!");
                }
                else
                {
                    player.PrintToChat(msgPrefix + $" You are not inside the Start Zone!");
                }
            }
            else
            {
                if (IsVectorInsideBox(currentPosition + new Vector(0, 0, 10), currentMapStartC1, currentMapStartC2))
                {
                    // Convert position and rotation to strings
                    string positionString = $"{currentPosition.X} {currentPosition.Y} {currentPosition.Z}";
                    string rotationString = $"{currentRotation.X} {currentRotation.Y} {currentRotation.Z}";

                    playerTimers[player.Slot].SetRespawnPos = positionString;
                    playerTimers[player.Slot].SetRespawnAng = rotationString;
                    player.PrintToChat(msgPrefix + $" Saved custom Start Zone RespawnPos!");
                }
                else
                {
                    player.PrintToChat(msgPrefix + $" You are not inside the Start Zone!");
                }
            }
        }

        [ConsoleCommand("css_stage", "Teleports you to a stage")]
        [CommandHelper(whoCanExecute: CommandUsage.CLIENT_ONLY)]
        public void TPtoStagePlayer(CCSPlayerController? player, CommandInfo command)
        {
            try
            {
                if (!IsAllowedPlayer(player) || respawnEnabled == false) return;
                SharpTimerDebug($"{player!.PlayerName} calling css_stage...");

                if (playerTimers[player.Slot].TicksSinceLastCmd < cmdCooldown)
                {
                    player.PrintToChat(msgPrefix + $" Command is on cooldown. Chill...");
                    return;
                }

                if (playerTimers[player.Slot].IsReplaying)
                {
                    player.PrintToChat(msgPrefix + $" Please end your current replay first {primaryChatColor}!stop or {primaryChatColor}!stopreplay");
                    return;
                }

                playerTimers[player.Slot].TicksSinceLastCmd = 0;

                if (playerTimers[player.Slot].IsTimerBlocked == false)
                {
                    SharpTimerDebug($"css_stage failed. Player {player.PlayerName} had timer running.");
                    player.PrintToChat(msgPrefix + $" Please stop your timer first using: {primaryChatColor}!timer");
                    return;
                }

                if (!int.TryParse(command.ArgString, out int stageX))
                {
                    SharpTimerDebug("css_stage conversion failed. The input string is not a valid integer.");
                    player.PrintToChat(msgPrefix + $" Please enter a valid stage i.e: {primaryChatColor}!stage <index>");
                    return;
                }

                if (useStageTriggers == false)
                {
                    SharpTimerDebug("css_stage failed useStages is false.");
                    player.PrintToChat(msgPrefix + $" Stages unavalible");
                    return;
                }

                // Remove checkpoints for the current player
                if (!playerTimers[player.Slot].IsTimerBlocked)
                {
                    playerCheckpoints.Remove(player.Slot);
                }

                if (jumpStatsEnabled) InvalidateJS(player.Slot);

                if (stageTriggerPoses.TryGetValue(stageX, out Vector? stagePos) && stagePos != null)
                {
                    player.PlayerPawn.Value!.Teleport(stagePos, stageTriggerAngs[stageX] ?? player.PlayerPawn.Value.EyeAngles, new Vector(0, 0, 0));
                    SharpTimerDebug($"{player.PlayerName} css_stage {stageX} to {stagePos}");
                }
                else
                {
                    player.PrintToChat(msgPrefix + $" {ChatColors.LightRed} No RespawnStagePos with index {stageX} found for current map!");
                }

                Server.NextFrame(() =>
                {
                    playerTimers[player.Slot].IsTimerRunning = false;
                    playerTimers[player.Slot].TimerTicks = 0;
                    playerTimers[player.Slot].IsBonusTimerRunning = false;
                    playerTimers[player.Slot].BonusTimerTicks = 0;
                });

                if (playerTimers[player.Slot].SoundsEnabled != false) player.ExecuteClientCommand($"play {respawnSound}");
            }
            catch (Exception ex)
            {
                SharpTimerError($"Exception in TPtoStagePlayer: {ex.Message}");
            }
        }

        [ConsoleCommand("css_r", "Teleports you to start")]
        [CommandHelper(whoCanExecute: CommandUsage.CLIENT_ONLY)]
        public void RespawnPlayerCommand(CCSPlayerController? player, CommandInfo command)
        {
            if (!IsAllowedPlayer(player) || respawnEnabled == false) return;
            SharpTimerDebug($"{player!.PlayerName} calling css_r...");

            if (playerTimers[player.Slot].TicksSinceLastCmd < cmdCooldown)
            {
                player.PrintToChat(msgPrefix + $" Command is on cooldown. Chill...");
                return;
            }

            if (playerTimers[player.Slot].IsReplaying)
            {
                player.PrintToChat(msgPrefix + $" Please end your current replay first {primaryChatColor}!stop or {primaryChatColor}!stopreplay");
                return;
            }

            playerTimers[player.Slot].TicksSinceLastCmd = 0;

            RespawnPlayer(player);
        }

        [ConsoleCommand("css_end", "Teleports you to end")]
        [CommandHelper(whoCanExecute: CommandUsage.CLIENT_ONLY)]
        public void EndPlayerCommand(CCSPlayerController? player, CommandInfo command)
        {
            if (!IsAllowedPlayer(player) || respawnEndEnabled == false) return;
            SharpTimerDebug($"{player!.PlayerName} calling css_end...");

            if (playerTimers[player.Slot].TicksSinceLastCmd < cmdCooldown)
            {
                player.PrintToChat(msgPrefix + $" Command is on cooldown. Chill...");
                return;
            }

            if (playerTimers[player.Slot].IsReplaying)
            {
                player.PrintToChat(msgPrefix + $" Please end your current replay first {primaryChatColor}!stop or {primaryChatColor}!stopreplay");
                return;
            }

            playerTimers[player.Slot].TicksSinceLastCmd = 0;
            playerTimers[player.Slot].IsTimerRunning = false;
            playerTimers[player.Slot].TimerTicks = 0;
            playerTimers[player.Slot].IsBonusTimerRunning = false;
            playerTimers[player.Slot].BonusTimerTicks = 0;

            Server.NextFrame(() => RespawnPlayer(player, true));
        }

        public void RespawnPlayer(CCSPlayerController? player, bool toEnd = false)
        {
            try
            {
                // Remove checkpoints for the current player
                if (!playerTimers[player!.Slot].IsTimerBlocked)
                {
                    playerCheckpoints.Remove(player.Slot);
                }

                if (jumpStatsEnabled) InvalidateJS(player.Slot);

                if (stageTriggerCount != 0 || cpTriggerCount != 0)//remove previous stage times if the map has stages
                {
                    playerTimers[player.Slot].StageTimes!.Clear();
                }

                if (toEnd == false)
                {
                    if (currentRespawnPos != null && playerTimers[player.Slot].SetRespawnPos == null)
                    {
                        if (currentRespawnAng != null)
                        {
                            player.PlayerPawn.Value!.Teleport(currentRespawnPos, currentRespawnAng, new Vector(0, 0, 0));
                        }
                        else
                        {
                            player.PlayerPawn.Value!.Teleport(currentRespawnPos, player.PlayerPawn.Value.EyeAngles ?? new QAngle(0, 0, 0), new Vector(0, 0, 0));
                        }
                        SharpTimerDebug($"{player.PlayerName} css_r to {currentRespawnPos}");
                    }
                    else
                    {
                        if (playerTimers[player.Slot].SetRespawnPos != null && playerTimers[player.Slot].SetRespawnAng != null)
                        {
                            player.PlayerPawn.Value!.Teleport(ParseVector(playerTimers[player.Slot].SetRespawnPos!), ParseQAngle(playerTimers[player.Slot].SetRespawnAng!), new Vector(0, 0, 0));
                        }
                        else
                        {
                            player.PrintToChat(msgPrefix + $" {ChatColors.LightRed} No RespawnPos found for current map!");
                        }
                    }
                }
                else
                {
                    if (currentEndPos != null)
                    {
                        player.PlayerPawn.Value!.Teleport(currentEndPos, player.PlayerPawn.Value.EyeAngles ?? new QAngle(0, 0, 0), new Vector(0, 0, 0));
                    }
                    else
                    {
                        player.PrintToChat(msgPrefix + $" {ChatColors.LightRed} No EndPos found for current map!");
                    }
                }

                Server.NextFrame(() =>
                {
                    playerTimers[player.Slot].IsTimerRunning = false;
                    playerTimers[player.Slot].TimerTicks = 0;
                    playerTimers[player.Slot].IsBonusTimerRunning = false;
                    playerTimers[player.Slot].BonusTimerTicks = 0;
                });
                if (playerTimers[player.Slot].SoundsEnabled != false) player.ExecuteClientCommand($"play {respawnSound}");
            }
            catch (Exception ex)
            {
                SharpTimerError($"Exception in RespawnPlayer: {ex.Message}");
            }
        }

        [ConsoleCommand("css_rs", "Teleport player to start of stage.")]
        [CommandHelper(whoCanExecute: CommandUsage.CLIENT_ONLY)]
        public void RestartCurrentStageCmd(CCSPlayerController? player, CommandInfo command)
        {
            if (!IsAllowedPlayer(player)) return;

            if (playerTimers[player!.Slot].TicksSinceLastCmd < cmdCooldown)
            {
                player.PrintToChat(msgPrefix + $" Command is on cooldown. Chill...");
                return;
            }

            SharpTimerDebug($"{player.PlayerName} calling css_rs...");

            if (stageTriggerCount == 0)
            {
                player.PrintToChat(msgPrefix + $" {ChatColors.LightRed} Current map has no stages!");
                return;
            }

            if (!playerTimers.TryGetValue(player.Slot, out PlayerTimerInfo? playerTimer) || playerTimer.CurrentMapStage == 0)
            {
                player.PrintToChat(msgPrefix + $" {ChatColors.LightRed} Error occured.");
                SharpTimerDebug("Failed to get playerTimer or playerTimer.CurrentMapStage == 0.");
                return;
            }

            int currStage = playerTimer.CurrentMapStage;

            try
            {
                playerTimers[player.Slot].TicksSinceLastCmd = 0;

                if (stageTriggerPoses.TryGetValue(currStage, out Vector? stagePos) && stagePos != null)
                {
                    if (jumpStatsEnabled) InvalidateJS(player.Slot);
                    player.PlayerPawn.Value!.Teleport(stagePos, stageTriggerAngs[currStage] ?? player.PlayerPawn.Value.EyeAngles, new Vector(0, 0, 0));
                    SharpTimerDebug($"{player.PlayerName} css_rs {player.PlayerName}");
                }
                else
                {
                    player.PrintToChat(msgPrefix + $" {ChatColors.LightRed} No RespawnStagePos with index {currStage} found for current map!");
                }
            }
            catch (Exception ex)
            {
                SharpTimerError($"Exception in RestartCurrentStage: {ex.Message}");
            }
        }

        [ConsoleCommand("css_timer", "Stops your timer")]
        [CommandHelper(whoCanExecute: CommandUsage.CLIENT_ONLY)]
        public void ForceStopTimer(CCSPlayerController? player, CommandInfo command)
        {
            if (!IsAllowedPlayer(player)) return;
            SharpTimerDebug($"{player!.PlayerName} calling css_timer...");

            if (playerTimers[player.Slot].TicksSinceLastCmd < cmdCooldown)
            {
                player.PrintToChat(msgPrefix + $" Command is on cooldown. Chill...");
                return;
            }

            if (playerTimers[player.Slot].IsReplaying)
            {
                player.PrintToChat(msgPrefix + $" Please end your current replay first {primaryChatColor}!stop or {primaryChatColor}!stopreplay");
                return;
            }

            playerTimers[player.Slot].TicksSinceLastCmd = 0;

            // Remove checkpoints for the current player
            playerCheckpoints.Remove(player.Slot);

            playerTimers[player.Slot].IsTimerBlocked = playerTimers[player.Slot].IsTimerBlocked ? false : true;
            playerTimers[player.Slot].IsRecordingReplay = false;
            player.PrintToChat(msgPrefix + $" Timer: {(playerTimers[player.Slot].IsTimerBlocked ? $"{ChatColors.Red} Disabled" : $"{ChatColors.Green} Enabled")}");
            playerTimers[player.Slot].IsTimerRunning = false;
            playerTimers[player.Slot].TimerTicks = 0;
            playerTimers[player.Slot].IsBonusTimerRunning = false;
            playerTimers[player.Slot].BonusTimerTicks = 0;

            if (stageTriggers.Count != 0) playerTimers[player.Slot].StageTimes!.Clear(); //remove previous stage times if the map has stages
            if (stageTriggers.Count != 0) playerTimers[player.Slot].StageVelos!.Clear(); //remove previous stage times if the map has stages
            if (playerTimers[player.Slot].SoundsEnabled != false) player.ExecuteClientCommand($"play {beepSound}");
            SharpTimerDebug($"{player.PlayerName} css_timer to {playerTimers[player.Slot].IsTimerBlocked}");
        }

        [ConsoleCommand("css_stver", "Prints SharpTimer Version")]
        [CommandHelper(whoCanExecute: CommandUsage.CLIENT_AND_SERVER)]
        public void STVerCommand(CCSPlayerController? player, CommandInfo command)
        {
            if (!IsAllowedPlayer(player))
            {
                SharpTimerConPrint($"This server is running SharpTimer v{ModuleVersion}");
                SharpTimerConPrint($"OS: {RuntimeInformation.OSDescription}");
                SharpTimerConPrint($"Runtime: {RuntimeInformation.RuntimeIdentifier}");
                return;
            }

            if (playerTimers[player!.Slot].TicksSinceLastCmd < cmdCooldown)
            {
                player.PrintToChat(msgPrefix + $" Command is on cooldown. Chill...");
                return;
            }

            playerTimers[player.Slot].TicksSinceLastCmd = 0;

            player.PrintToChat($"This server is running SharpTimer v{ModuleVersion}");
            player.PrintToChat($"OS: {RuntimeInformation.OSDescription}");
            player.PrintToChat($"Runtime: {RuntimeInformation.RuntimeIdentifier}");
        }

        [ConsoleCommand("css_goto", "Teleports you to a player")]
        [CommandHelper(minArgs: 1, usage: "[name]", whoCanExecute: CommandUsage.CLIENT_ONLY)]
        public void GoToPlayer(CCSPlayerController? player, CommandInfo command)
        {
            if (!IsAllowedPlayer(player) || goToEnabled == false) return;
            SharpTimerDebug($"{player!.PlayerName} calling css_goto...");

            if (playerTimers[player.Slot].TicksSinceLastCmd < cmdCooldown)
            {
                player.PrintToChat(msgPrefix + $" Command is on cooldown. Chill...");
                return;
            }

            if (playerTimers[player.Slot].IsReplaying)
            {
                player.PrintToChat(msgPrefix + $" Please end your current replay first {primaryChatColor}!stop or {primaryChatColor}!stopreplay");
                return;
            }

            if (!playerTimers[player.Slot].IsTimerBlocked)
            {
                player.PrintToChat(msgPrefix + $" Please stop your timer using {primaryChatColor}!timer{ChatColors.White} first!");
                return;
            }

            playerTimers[player.Slot].TicksSinceLastCmd = 0;

            var name = command.GetArg(1);
            bool isPlayerFound = false;
            CCSPlayerController foundPlayer = null!;


            foreach (var playerEntry in connectedPlayers.Values)
            {
                if (playerEntry.PlayerName == name)
                {
                    foundPlayer = playerEntry;
                    isPlayerFound = true;
                    break;
                }
            }

            if (!isPlayerFound)
            {
                player.PrintToChat(msgPrefix + $"{ChatColors.LightRed} Player name not found! If the name contains spaces please try {primaryChatColor}!goto 'some name'");
                return;
            }


            if (!playerTimers[player.Slot].IsTimerBlocked)
            {
                playerCheckpoints.Remove(player.Slot);
            }

            playerTimers[player.Slot].IsTimerRunning = false;
            playerTimers[player.Slot].TimerTicks = 0;

            if (playerTimers[player.Slot].SoundsEnabled != false)
                player.ExecuteClientCommand($"play {respawnSound}");

            if (foundPlayer != null && playerTimers[player.Slot].IsTimerBlocked)
            {
                player.PrintToChat(msgPrefix + $"Teleporting to {primaryChatColor}{foundPlayer.PlayerName}");

                if (player != null && IsAllowedPlayer(foundPlayer) && playerTimers[player.Slot].IsTimerBlocked)
                {
                    if (jumpStatsEnabled) InvalidateJS(player.Slot);
                    player.PlayerPawn.Value!.Teleport(foundPlayer.Pawn.Value!.CBodyComponent?.SceneNode?.AbsOrigin ?? new Vector(0, 0, 0),
                        foundPlayer.PlayerPawn.Value!.EyeAngles ?? new QAngle(0, 0, 0), new Vector(0, 0, 0));
                    SharpTimerDebug($"{player.PlayerName} css_goto to {foundPlayer.Pawn.Value.CBodyComponent?.SceneNode?.AbsOrigin ?? new Vector(0, 0, 0)}");
                }
            }
            else
            {
                player.PrintToChat(msgPrefix + $"{ChatColors.LightRed} Player name not found! If the name contains spaces please try {primaryChatColor}!goto 'some name'");
            }
        }

        [ConsoleCommand("css_cp", "Sets a checkpoint")]
        [ConsoleCommand("css_saveloc", "alias for !cp")]
        [CommandHelper(whoCanExecute: CommandUsage.CLIENT_ONLY)]
        public void SetPlayerCPCommand(CCSPlayerController? player, CommandInfo command)
        {
            if (!IsAllowedPlayer(player) || cpEnabled == false) return;
            SharpTimerDebug($"{player!.PlayerName} calling css_cp...");

            if (playerTimers[player.Slot].TicksSinceLastCmd < cmdCooldown)
            {
                player.PrintToChat(msgPrefix + $" Command is on cooldown. Chill...");
                return;
            }

            if (playerTimers[player.Slot].IsReplaying)
            {
                player.PrintToChat(msgPrefix + $" Please end your current replay first {primaryChatColor}!stop or {primaryChatColor}!stopreplay");
                return;
            }

            SetPlayerCP(player, command);
        }

        public void SetPlayerCP(CCSPlayerController? player, CommandInfo command)
        {
            if (((PlayerFlags)player!.Pawn.Value!.Flags & PlayerFlags.FL_ONGROUND) != PlayerFlags.FL_ONGROUND && removeCpRestrictEnabled == false)
            {
                player.PrintToChat(msgPrefix + $"{ChatColors.LightRed}Cant set {(currentMapName!.Contains("surf_") ? "loc" : "checkpoint")} while in air");
                if (playerTimers[player.Slot].SoundsEnabled != false) player.ExecuteClientCommand($"play {cpSoundAir}");
                return;
            }

            if (cpOnlyWhenTimerStopped == true && playerTimers[player.Slot].IsTimerBlocked == false)
            {
                player.PrintToChat(msgPrefix + $"{ChatColors.LightRed}Cant set {(currentMapName!.Contains("surf_") ? "loc" : "checkpoint")} while timer is on, use {ChatColors.White}!timer");
                if (playerTimers[player.Slot].SoundsEnabled != false) player.ExecuteClientCommand($"play {cpSoundAir}");
                return;
            }

            playerTimers[player.Slot].TicksSinceLastCmd = 0;

            // Get the player's current position and rotation
            Vector currentPosition = player.Pawn.Value.CBodyComponent?.SceneNode?.AbsOrigin ?? new Vector(0, 0, 0);
            Vector currentSpeed = player.PlayerPawn.Value!.AbsVelocity ?? new Vector(0, 0, 0);
            QAngle currentRotation = player.PlayerPawn.Value.EyeAngles ?? new QAngle(0, 0, 0);

            // Convert position and rotation to strings
            string positionString = $"{currentPosition.X} {currentPosition.Y} {currentPosition.Z}";
            string rotationString = $"{currentRotation.X} {currentRotation.Y} {currentRotation.Z}";
            string speedString = $"{currentSpeed.X} {currentSpeed.Y} {currentSpeed.Z}";

            // Add the current position and rotation strings to the player's checkpoint list
            if (!playerCheckpoints.ContainsKey(player.Slot))
            {
                playerCheckpoints[player.Slot] = [];
            }

            playerCheckpoints[player.Slot].Add(new PlayerCheckpoint
            {
                PositionString = positionString,
                RotationString = rotationString,
                SpeedString = speedString
            });

            // Get the count of checkpoints for this player
            int checkpointCount = playerCheckpoints[player.Slot].Count;

            // Print the chat message with the checkpoint count
            player.PrintToChat(msgPrefix + $"{(currentMapName!.Contains("surf_") ? "Loc" : "Checkpoint")} set! {primaryChatColor}#{checkpointCount}");
            if (playerTimers[player.Slot].SoundsEnabled != false) player.ExecuteClientCommand($"play {cpSound}");
            SharpTimerDebug($"{player.PlayerName} css_cp to {checkpointCount} {positionString} {rotationString} {speedString}");
        }

        [ConsoleCommand("css_tp", "Tp to the most recent checkpoint")]
        [ConsoleCommand("css_loadloc", "alias for !tp")]
        [CommandHelper(whoCanExecute: CommandUsage.CLIENT_ONLY)]
        public void TpPlayerCPCommand(CCSPlayerController? player, CommandInfo command)
        {
            if (!IsAllowedPlayer(player) || cpEnabled == false) return;
            SharpTimerDebug($"{player!.PlayerName} calling css_tp...");

            if (playerTimers[player.Slot].TicksSinceLastCmd < cmdCooldown)
            {
                player.PrintToChat(msgPrefix + $" Command is on cooldown. Chill...");
                return;
            }

            if (playerTimers[player.Slot].IsReplaying)
            {
                player.PrintToChat(msgPrefix + $" Please end your current replay first {primaryChatColor}!stop or {primaryChatColor}!stopreplay");
                return;
            }

            TpPlayerCP(player, command);
        }

        public void TpPlayerCP(CCSPlayerController? player, CommandInfo command)
        {
            if (playerTimers[player!.Slot].IsReplaying)
            {
                player.PrintToChat(msgPrefix + $" Please end your current replay first {primaryChatColor}!stop or {primaryChatColor}!stopreplay");
                return;
            }

            if (cpOnlyWhenTimerStopped == true && playerTimers[player.Slot].IsTimerBlocked == false)
            {
                player.PrintToChat(msgPrefix + $"{ChatColors.LightRed}Cant use {(currentMapName!.Contains("surf_") ? "loc" : "checkpoint")} while timer is on, use {ChatColors.White}!timer");
                if (playerTimers[player.Slot].SoundsEnabled != false) player.ExecuteClientCommand($"play {cpSoundAir}");
                return;
            }

            playerTimers[player.Slot].TicksSinceLastCmd = 0;

            // Check if the player has any checkpoints
            if (!playerCheckpoints.ContainsKey(player.Slot) || playerCheckpoints[player.Slot].Count == 0)
            {
                player.PrintToChat(msgPrefix + $"No {(currentMapName!.Contains("surf_") ? "loc" : "checkpoint")} set!");
                return;
            }

            if (jumpStatsEnabled) InvalidateJS(player.Slot);

            // Get the most recent checkpoint from the player's list
            PlayerCheckpoint lastCheckpoint = playerCheckpoints[player.Slot].Last();

            // Convert position and rotation strings to Vector and QAngle
            Vector position = ParseVector(lastCheckpoint.PositionString ?? "0 0 0");
            QAngle rotation = ParseQAngle(lastCheckpoint.RotationString ?? "0 0 0");
            Vector speed = ParseVector(lastCheckpoint.SpeedString ?? "0 0 0");

            // Teleport the player to the most recent checkpoint, including the saved rotation
            if (removeCpRestrictEnabled == true)
            {
                player.PlayerPawn.Value!.Teleport(position, rotation, speed);
            }
            else
            {
                player.PlayerPawn.Value!.Teleport(position, rotation, new Vector(0, 0, 0));
            }

            // Play a sound or provide feedback to the player
            if (playerTimers[player.Slot].SoundsEnabled != false) player.ExecuteClientCommand($"play {tpSound}");
            player.PrintToChat(msgPrefix + $"Teleported to most recent {(currentMapName!.Contains("surf_") ? "loc" : "checkpoint")}!");
            SharpTimerDebug($"{player.PlayerName} css_tp to {position} {rotation} {speed}");
        }

        [ConsoleCommand("css_prevcp", "Tp to the previous checkpoint")]
        [ConsoleCommand("css_prevloc", "alias for !prevcp")]
        [CommandHelper(whoCanExecute: CommandUsage.CLIENT_ONLY)]
        public void TpPreviousCP(CCSPlayerController? player, CommandInfo command)
        {
            if (!IsAllowedPlayer(player) || cpEnabled == false) return;
            SharpTimerDebug($"{player!.PlayerName} calling css_prevcp...");

            if (playerTimers[player.Slot].TicksSinceLastCmd < cmdCooldown)
            {
                player.PrintToChat(msgPrefix + $" Command is on cooldown. Chill...");
                return;
            }

            if (playerTimers[player.Slot].IsReplaying)
            {
                player.PrintToChat(msgPrefix + $" Please end your current replay first {primaryChatColor}!stop or {primaryChatColor}!stopreplay");
                return;
            }

            if (cpOnlyWhenTimerStopped == true && playerTimers[player.Slot].IsTimerBlocked == false)
            {
                player.PrintToChat(msgPrefix + $"{ChatColors.LightRed}Cant use {(currentMapName!.Contains("surf_") ? "loc" : "checkpoint")} while timer is on, use {ChatColors.White}!timer");
                if (playerTimers[player.Slot].SoundsEnabled != false) player.ExecuteClientCommand($"play {cpSoundAir}");
                return;
            }

            playerTimers[player.Slot].TicksSinceLastCmd = 0;

            if (!playerCheckpoints.TryGetValue(player.Slot, out List<PlayerCheckpoint>? checkpoints) || checkpoints.Count == 0)
            {
                player.PrintToChat(msgPrefix + $"No {(currentMapName!.Contains("surf_") ? "loc" : "checkpoint")} set!");
                return;
            }

            int index = playerTimers.TryGetValue(player.Slot, out var timer) ? timer.CheckpointIndex : 0;

            if (checkpoints.Count == 1)
            {
                TpPlayerCP(player, command);
            }
            else
            {
                if (jumpStatsEnabled) InvalidateJS(player.Slot);
                // Calculate the index of the previous checkpoint, circling back if necessary
                index = (index - 1 + checkpoints.Count) % checkpoints.Count;

                PlayerCheckpoint previousCheckpoint = checkpoints[index];

                // Update the player's checkpoint index
                playerTimers[player.Slot].CheckpointIndex = index;

                // Convert position and rotation strings to Vector and QAngle
                Vector position = ParseVector(previousCheckpoint.PositionString ?? "0 0 0");
                QAngle rotation = ParseQAngle(previousCheckpoint.RotationString ?? "0 0 0");

                // Teleport the player to the previous checkpoint, including the saved rotation
                player.PlayerPawn.Value!.Teleport(position, rotation, new Vector(0, 0, 0));
                // Play a sound or provide feedback to the player
                if (playerTimers[player.Slot].SoundsEnabled != false) player.ExecuteClientCommand($"play {tpSound}");
                player.PrintToChat(msgPrefix + $"Teleported to the previous {(currentMapName!.Contains("surf_") ? "loc" : "checkpoint")}!");
                SharpTimerDebug($"{player.PlayerName} css_prevcp to {position} {rotation}");
            }
        }

        [ConsoleCommand("css_nextcp", "Tp to the next checkpoint")]
        [ConsoleCommand("css_nextloc", "alias for !nextcp")]
        [CommandHelper(whoCanExecute: CommandUsage.CLIENT_ONLY)]
        public void TpNextCP(CCSPlayerController? player, CommandInfo command)
        {
            if (!IsAllowedPlayer(player) || cpEnabled == false) return;
            SharpTimerDebug($"{player!.PlayerName} calling css_nextcp...");

            if (playerTimers[player.Slot].TicksSinceLastCmd < cmdCooldown)
            {
                player.PrintToChat(msgPrefix + $" Command is on cooldown. Chill...");
                return;
            }

            if (playerTimers[player.Slot].IsReplaying)
            {
                player.PrintToChat(msgPrefix + $" Please end your current replay first {primaryChatColor}!stop or {primaryChatColor}!stopreplay");
                return;
            }

            if (cpOnlyWhenTimerStopped == true && playerTimers[player.Slot].IsTimerBlocked == false)
            {
                player.PrintToChat(msgPrefix + $"{ChatColors.LightRed}Cant use {(currentMapName!.Contains("surf_") ? "loc" : "checkpoint")} while timer is on, use {ChatColors.White}!timer");
                if (playerTimers[player.Slot].SoundsEnabled != false) player.ExecuteClientCommand($"play {cpSoundAir}");
                return;
            }

            playerTimers[player.Slot].TicksSinceLastCmd = 0;

            if (!playerCheckpoints.TryGetValue(player.Slot, out List<PlayerCheckpoint>? checkpoints) || checkpoints.Count == 0)
            {
                player.PrintToChat(msgPrefix + $"No {(currentMapName!.Contains("surf_") ? "loc" : "checkpoint")} set!");
                return;
            }

            int index = playerTimers.TryGetValue(player.Slot, out var timer) ? timer.CheckpointIndex : 0;

            if (checkpoints.Count == 1)
            {
                TpPlayerCP(player, command);
            }
            else
            {
                if (jumpStatsEnabled) InvalidateJS(player.Slot);
                // Calculate the index of the next checkpoint, circling back if necessary
                index = (index + 1) % checkpoints.Count;

                PlayerCheckpoint nextCheckpoint = checkpoints[index];

                // Update the player's checkpoint index
                playerTimers[player.Slot].CheckpointIndex = index;

                // Convert position and rotation strings to Vector and QAngle
                Vector position = ParseVector(nextCheckpoint.PositionString ?? "0 0 0");
                QAngle rotation = ParseQAngle(nextCheckpoint.RotationString ?? "0 0 0");

                // Teleport the player to the next checkpoint, including the saved rotation
                player.PlayerPawn.Value!.Teleport(position, rotation, new Vector(0, 0, 0));

                // Play a sound or provide feedback to the player
                if (playerTimers[player.Slot].SoundsEnabled != false) player.ExecuteClientCommand($"play {tpSound}");
                player.PrintToChat(msgPrefix + $"Teleported to the next {(currentMapName!.Contains("surf_") ? "loc" : "checkpoint")}!");
                SharpTimerDebug($"{player.PlayerName} css_nextcp to {position} {rotation}");
            }
        }
    }
}
