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
using CounterStrikeSharp.API.Modules.Memory;
using CounterStrikeSharp.API.Modules.Admin;
using FixVectorLeak;

namespace SharpTimer
{
    public partial class SharpTimer
    {
        [ConsoleCommand("css_dp_timers", "Prints playerTimers")]
        [CommandHelper(whoCanExecute: CommandUsage.SERVER_ONLY)]
        public void DeepPrintPlayerTimers(CCSPlayerController? player, CommandInfo command)
        {
            Utils.ConPrint("Printing Player Timers:");
            foreach (var kvp in playerTimers)
            {
                Utils.ConPrint($"Player Slot: {kvp.Key}");
                foreach (var prop in typeof(PlayerTimerInfo).GetProperties())
                {
                    var value = prop.GetValue(kvp.Value, null);
                    Utils.ConPrint($"  {prop.Name}: {value}");
                    if (value is Dictionary<int, int> intIntDictionary)
                    {
                        Utils.ConPrint($"    {prop.Name}:");
                        foreach (var entry in intIntDictionary)
                        {
                            Utils.ConPrint($"      {entry.Key}: {entry.Value}");
                        }
                    }
                    else if (value is Dictionary<int, string> intStringDictionary)
                    {
                        Utils.ConPrint($"    {prop.Name}:");
                        foreach (var entry in intStringDictionary)
                        {
                            Utils.ConPrint($"      {entry.Key}: {entry.Value}");
                        }
                    }
                }

                Utils.ConPrint(" ");
            }
            Utils.ConPrint("End of Player Timers");
        }

        [ConsoleCommand("css_replaypb", "Replay your last pb")]
        [CommandHelper(whoCanExecute: CommandUsage.CLIENT_ONLY)]
        public void ReplaySelfCommand(CCSPlayerController? player, CommandInfo command)
        {
            if (!IsAllowedPlayer(player) || enableReplays == false)
                return;

            var slot = player!.Slot;
            var steamID = player.SteamID.ToString();
            var playerName = player.PlayerName;

            QuietStopTimer(player);

            if (IsTimerBlocked(player))
                return;

            if (ReplayCheck(player))
                return;

            _ = Task.Run(async () => await ReplayHandler(player, slot, "self", steamID, playerName, 0, playerTimers[slot].currentStyle));
        }

        [ConsoleCommand("css_replay", "Replay server map record")]
        [ConsoleCommand("css_replaysr", "Replay server map record")]
        [CommandHelper(whoCanExecute: CommandUsage.CLIENT_ONLY)]
        public void ReplaySRCommand(CCSPlayerController? player, CommandInfo command)
        {
            if (!IsAllowedPlayer(player) || enableReplays == false)
                return;

            int slot = player!.Slot;

            QuietStopTimer(player!);

            if (IsTimerBlocked(player))
                return;

            if (ReplayCheck(player))
                return;

            Utils.PrintToChat(player, Localizer["available_replay_cmds"]);

            _ = Task.Run(async () => await ReplayHandler(player, slot, "1", "69", "unknown", 0, playerTimers[slot].currentStyle));
        }

        [ConsoleCommand("css_replaytop", "Replay a top 10 server map record")]
        [CommandHelper(minArgs: 1, usage: "[1-10]", whoCanExecute: CommandUsage.CLIENT_ONLY)]
        public void ReplayTop10SRCommand(CCSPlayerController? player, CommandInfo command)
        {
            if (!IsAllowedPlayer(player) || enableReplays == false)
                return;

            int slot = player!.Slot;

            QuietStopTimer(player);

            if (IsTimerBlocked(player))
                return;

            if (ReplayCheck(player))
                return;

            string arg = command.ArgByIndex(1);

            _ = Task.Run(async () => await ReplayHandler(player, slot, arg, "69", "unknown", 0, playerTimers[slot].currentStyle));
        }

        [ConsoleCommand("css_replaywr", "Replay a top 10 world record")]
        [CommandHelper(minArgs: 1, usage: "[1-10]", whoCanExecute: CommandUsage.CLIENT_ONLY)]
        public void ReplayTop10WRCommand(CCSPlayerController? player, CommandInfo command)
        {
            if (!IsAllowedPlayer(player) || enableReplays == false)
                return;

            int slot = player!.Slot;

            QuietStopTimer(player);

            if (IsTimerBlocked(player))
                return;

            if (ReplayCheck(player))
                return;

            string arg = command.ArgByIndex(1);

            _ = Task.Run(async () => await ReplayHandler(player, slot, arg, "69", "unknown", 0, playerTimers[slot].currentStyle, true));
        }

        [ConsoleCommand("css_gc", "Globalcheck")]
        [CommandHelper(whoCanExecute: CommandUsage.CLIENT_AND_SERVER)]
        public async void GlobalCheckCommand(CCSPlayerController? player, CommandInfo command)
        {
            if (player == null || !IsAllowedPlayer(player))
                return;

            if (apiKey == "")
            {
                Server.NextFrame(() => Utils.PrintToChat(player, $"[GC] {ChatColors.LightRed}Missing API Key!"));
                return;
            }
            
            var validKey = await CheckKeyAsync();
            if (!validKey)
                Server.NextFrame(() => Utils.PrintToChat(player, $"[GC] {ChatColors.LightRed}Invalid API Key!"));
            else
                Server.NextFrame(() => Utils.PrintToChat(player, $"[GC] {ChatColors.Green}Valid API Key"));

            var validHash = await CheckHashAsync();
            if (!validHash)
                Server.NextFrame(() => Utils.PrintToChat(player, $"[GC] {ChatColors.LightRed}Invalid ST build!"));
            else
                Server.NextFrame(() => Utils.PrintToChat(player, $"[GC] {ChatColors.Green}Valid ST build"));

            var validAddon = await CheckAddonAsync();
            if (!validAddon)
                Server.NextFrame(() => Utils.PrintToChat(player, $"[GC] {ChatColors.LightRed}Map is not verified!"));
            else
                Server.NextFrame(() => Utils.PrintToChat(player, $"[GC] {ChatColors.Green}Map is verified"));
            
            Server.NextFrame(() =>
            {
                var (globalCheck, maxVel, maxWish) = CheckCvarsAndMaxVelo();
                if (!globalCheck)
                    Utils.PrintToChat(player, $"[GC] {ChatColors.LightRed}Cvar Check Failed");
                else
                    Utils.PrintToChat(player, $"[GC] {ChatColors.Green}Cvar Check Passed");
            });

            if (!globalDisabled && validKey && validHash && validAddon)
                Server.NextFrame(() => Utils.PrintToChat(player, $"[GC] {ChatColors.Green}All checks passed!"));
            else
                Server.NextFrame(() => Utils.PrintToChat(player, $"[GC] {ChatColors.LightRed}Some checks failed"));
        }

        [ConsoleCommand("css_gethash", "GetHash")]
        [CommandHelper(whoCanExecute: CommandUsage.CLIENT_AND_SERVER)]
        [RequiresPermissions("@css/cheats")]
        public void GetHashCommand(CCSPlayerController? player, CommandInfo command)
        {
            if (!IsAllowedPlayer(player))
                return;

            var hash = GetHash();
            Server.NextFrame(() => player!.PrintToConsole($"ST HASH: {hash}"));
        }

        [ConsoleCommand("css_replayb", "Replay a top 10 server bonus record")]
        [ConsoleCommand("css_replaybonus", "Replay a top 10 server bonus record")]
        [CommandHelper(minArgs: 1, usage: "[1-10] [bonus stage]", whoCanExecute: CommandUsage.CLIENT_ONLY)]
        public void ReplayBonusCommand(CCSPlayerController? player, CommandInfo command)
        {
            if (!IsAllowedPlayer(player) || enableReplays == false)
                return;

            int slot = player!.Slot;

            QuietStopTimer(player);

            if (IsTimerBlocked(player))
                return;

            if (ReplayCheck(player))
                return;

            string arg = command.ArgByIndex(1);
            string arg2 = command.ArgByIndex(2);

            _ = Task.Run(async () => await ReplayHandler(player, slot, arg, "69", "unknown", Int16.Parse(arg2), playerTimers[slot].currentStyle));
        }

        [ConsoleCommand("css_replaybpb", "Replay your bonus pb")]
        [ConsoleCommand("css_replaybonuspb", "Replay your bonus pb")]
        [CommandHelper(minArgs: 1, usage: "[bonus stage]", whoCanExecute: CommandUsage.CLIENT_ONLY)]
        public void ReplayBonusPBCommand(CCSPlayerController? player, CommandInfo command)
        {
            if (!IsAllowedPlayer(player) || enableReplays == false)
                return;

            var slot = player!.Slot;
            var steamID = player.SteamID.ToString();
            var playerName = player.PlayerName;

            QuietStopTimer(player);

            if (IsTimerBlocked(player))
                return;

            if (ReplayCheck(player))
                return;

            string arg = command.ArgByIndex(1);
            int bonusX = Int16.Parse(arg);

            _ = Task.Run(async () => await ReplayHandler(player, slot, "self", steamID, playerName, bonusX, playerTimers[slot].currentStyle));
        }

        public async Task ReplayHandler(CCSPlayerController player, int slot, string arg = "1", string pbSteamID = "69", string playerName = "unknown", int bonusX = 0, int style = 0, bool wr = false)
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

            playerReplays.Remove(slot);
            playerReplays[slot] = new PlayerReplays();

            var (srSteamID, srPlayerName, srTime) = ("null", "null", "null");
            var (wrID, wrSteamID, wrPlayerName, wrTime) = (0, "null", "null", "null");

            if (!self)
            {
                if (enableDb)
                    (srSteamID, srPlayerName, srTime) = await GetMapRecordSteamIDFromDatabase(bonusX, top10, style);

                else
                    (srSteamID, srPlayerName, srTime) = await GetMapRecordSteamID(bonusX, top10);

                if (wr)
                {
                    var sortedRecords = await GetSortedRecordsFromGlobal(10, bonusX, currentMapName!, style);
                    wrID = sortedRecords[top10-1].RecordID;
                    wrSteamID = sortedRecords[top10-1].SteamID;
                    wrPlayerName = sortedRecords[top10-1].PlayerName;
                    wrTime = Utils.FormatTime(sortedRecords[top10-1].TimerTicks);
                }
            }

            if ((srSteamID == "null" || srPlayerName == "null" || srTime == "null") && !self)
            {
                Server.NextFrame(() => {
                    Utils.PrintToChat(player, Localizer["no_sr_replay"]);
                    RespawnPlayer(player);
                });
                return;
            }

            if (wr)
                await ReadReplayFromGlobal(player, wrID, style, bonusX);
            else
                await ReadReplayFromJson(player, !self ? srSteamID : pbSteamID, slot, bonusX, style);

            if (playerReplays[slot].replayFrames.Count == 0) return;

            if (!wr) await GetReplayVIPGif(!self ? srSteamID : pbSteamID, slot);

            playerTimers[slot].IsReplaying = !playerTimers[slot].IsReplaying;

            if (wr)
                playerTimers[slot].ReplayHUDString = $"{wrPlayerName} | {wrTime}";
            else
                playerTimers[slot].ReplayHUDString = !self ? $"{srPlayerName} | {srTime}" : $"{playerName} | {playerTimers[slot].CachedPB}";

            playerTimers[slot].IsTimerRunning = false;
            playerTimers[slot].TimerTicks = 0;
            playerTimers[slot].IsBonusTimerRunning = false;
            playerTimers[slot].BonusTimerTicks = 0;
            playerReplays[slot].CurrentPlaybackFrame = 0;

            if (stageTriggers.Count != 0) playerTimers[slot].StageTimes!.Clear(); //remove previous stage times if the map has stages
            if (stageTriggers.Count != 0) playerTimers[slot].StageVelos!.Clear(); //remove previous stage times if the map has stages

            if (IsAllowedPlayer(player))
            {
                if (wr)
                    Server.NextFrame(() => Utils.PrintToChat(player, Localizer["replaying_world_top", top10]));
                else if (!self)
                    Server.NextFrame(() => Utils.PrintToChat(player, Localizer["replaying_server_top", top10]));
                else
                    Server.NextFrame(() => Utils.PrintToChat(player, Localizer["replaying_pb"]));
            }
            else
                Utils.LogError($"Error in ReplayHandler: player not allowed or not on server anymore");
        }

        [ConsoleCommand("css_stop", "stops the current replay")]
        [ConsoleCommand("css_stopreplay", "stops the current replay")]
        [CommandHelper(whoCanExecute: CommandUsage.CLIENT_ONLY)]
        public void StopReplayCommand(CCSPlayerController? player, CommandInfo command)
        {
            if (!IsAllowedPlayer(player) || enableReplays == false)
                return;

            StopReplay(player);
        }

        public void StopReplay(CCSPlayerController? player)
        {
            var slot = player!.Slot;

            if (!playerTimers[slot].IsTimerBlocked || !playerTimers[slot].IsReplaying)
            {
                Utils.PrintToChat(player, Localizer["no_replay"]);
                return;
            }

            if (playerTimers[slot].IsReplaying)
            {
                Utils.PrintToChat(player, Localizer["ending_replay"]);
                playerTimers[slot].IsReplaying = false;

                if (player.PlayerPawn.Value!.MoveType != MoveType_t.MOVETYPE_WALK || player.PlayerPawn.Value.ActualMoveType != MoveType_t.MOVETYPE_WALK) SetMoveType(player, MoveType_t.MOVETYPE_WALK);
                    playerReplays.Remove(slot);

                playerReplays[slot] = new PlayerReplays();
                playerTimers[slot].IsTimerBlocked = false;
                playerTimers[slot].IsTimerRunning = false;
                playerTimers[slot].TimerTicks = 0;
                playerTimers[slot].IsBonusTimerRunning = false;
                playerTimers[slot].BonusTimerTicks = 0;
                playerReplays[slot].CurrentPlaybackFrame = 0;

                if (stageTriggers.Count != 0) playerTimers[slot].StageTimes!.Clear(); //remove previous stage times if the map has stages
                if (stageTriggers.Count != 0) playerTimers[slot].StageVelos!.Clear(); //remove previous stage times if the map has stages

                RespawnPlayer(player);
            }
        }

        [ConsoleCommand("css_help", "alias for !sthelp")]
        [ConsoleCommand("css_sthelp", "Prints all commands for the player")]
        [CommandHelper(whoCanExecute: CommandUsage.CLIENT_ONLY)]
        public void HelpCommand(CCSPlayerController? player, CommandInfo command)
        {
            if (!IsPlayerOrSpectator(player) || !helpEnabled)
                return;

            PrintAllEnabledCommands(player!);
        }

        [ConsoleCommand("css_spectate", "Moves you to Spectator or back to a team")]
        [ConsoleCommand("css_spec", "Moves you to Spectator or back to a team")]
        [CommandHelper(whoCanExecute: CommandUsage.CLIENT_ONLY)]
        public void SpecCommand(CCSPlayerController? player, CommandInfo command)
        {
            if (!IsAllowedPlayer(player))
                return;

            if (player!.Team == CsTeam.Spectator)
            {
                player.ChangeTeam(CsTeam.CounterTerrorist);
                player.Respawn();
                player.CommitSuicide(false, true);
                player.ChangeTeam(CsTeam.CounterTerrorist);
            }
            else if (player.Team != CsTeam.Spectator)
            {
                player.ChangeTeam(CsTeam.Spectator);
                Utils.PrintToChat(player, $"You have been moved to Spectator.");
            }
        }

        [ConsoleCommand("css_hud", "Draws/Hides The timer HUD")]
        [CommandHelper(whoCanExecute: CommandUsage.CLIENT_ONLY)]
        public void HUDSwitchCommand(CCSPlayerController? player, CommandInfo command)
        {
            if (!IsPlayerOrSpectator(player))
                return;

            var slot = player!.Slot;
            var playerName = player.PlayerName;
            var steamID = player.SteamID.ToString();

            Utils.LogDebug($"{playerName} calling css_hud...");

            if (CommandCooldown(player))
                return;

            playerTimers[slot].HideTimerHud = !playerTimers[slot].HideTimerHud;

            if (playerTimers[slot].HideTimerHud)
                Utils.PrintToChat(player, Localizer["hud_hidden"]);
            else
                Utils.PrintToChat(player, Localizer["hud_shown"]);

            Utils.LogDebug($"Hide Timer HUD set to: {playerTimers[slot].HideTimerHud} for {playerName}");

            if (enableDb)
                _ = Task.Run(async () => await SetPlayerStats(player, steamID, playerName, slot));
        }

        [ConsoleCommand("css_keys", "Draws/Hides HUD Keys")]
        [CommandHelper(whoCanExecute: CommandUsage.CLIENT_ONLY)]
        public void KeysSwitchCommand(CCSPlayerController? player, CommandInfo command)
        {
            if (!IsPlayerOrSpectator(player))
                return;

            var slot = player!.Slot;
            var playerName = player.PlayerName;
            var steamID = player.SteamID.ToString();

            Utils.LogDebug($"{playerName} calling css_keys...");

            if (CommandCooldown(player))
                return;

            playerTimers[slot].HideKeys = playerTimers[slot].HideKeys ? false : true;

            if (playerTimers[slot].HideKeys)
                Utils.PrintToChat(player, Localizer["keys_hidden"]);
            else
                Utils.PrintToChat(player, Localizer["keys_shown"]);

            Utils.LogDebug($"Hide Timer HUD set to: {playerTimers[slot].HideKeys} for {playerName}");

            if (enableDb)
                _ = Task.Run(async () => await SetPlayerStats(player, steamID, playerName, slot));
        }

        [ConsoleCommand("css_sounds", "Toggles Sounds")]
        [CommandHelper(whoCanExecute: CommandUsage.CLIENT_ONLY)]
        public void SoundsSwitchCommand(CCSPlayerController? player, CommandInfo command)
        {
            if (!IsPlayerOrSpectator(player))
                return;

            var slot = player!.Slot;
            var playerName = player.PlayerName;
            var steamID = player.SteamID.ToString();

            Utils.LogDebug($"{playerName} calling css_sounds...");

            if (CommandCooldown(player))
                return;

            playerTimers[slot].SoundsEnabled = playerTimers[slot].SoundsEnabled ? false : true;

            if (playerTimers[slot].SoundsEnabled)
                Utils.PrintToChat(player, Localizer["sounds_on"]);
            else
                Utils.PrintToChat(player, Localizer["sounds_off"]);

            Utils.LogDebug($"Timer Sounds set to: {playerTimers[slot].SoundsEnabled} for {playerName}");

            if (enableDb)
                _ = Task.Run(async () => await SetPlayerStats(player, steamID, playerName, slot));
        }

        [ConsoleCommand("css_hideweapon", "Toggles the player's weapon visibility")]
        [ConsoleCommand("css_hw", "Toggles the player's weapon visibility")]
        [CommandHelper(whoCanExecute: CommandUsage.CLIENT_ONLY)]
        public void ToggleWeaponCommand(CCSPlayerController? player, CommandInfo command)
        {
            if (!IsPlayerOrSpectator(player))
                return;

            var slot = player!.Slot;
            var playerName = player.PlayerName;
            var steamID = player.SteamID.ToString();

            playerTimers[slot].HideWeapon = !playerTimers[slot].HideWeapon;
            _ = Task.Run(async () => await SetPlayerStats(player, steamID, playerName, slot));
        }

        [ConsoleCommand("css_fov", "Sets the player's FOV")]
        [CommandHelper(minArgs: 1, usage: "[fov]")]
        public void FovCommand(CCSPlayerController? player, CommandInfo command)
        {
            if (!IsAllowedPlayer(player) || fovChangerEnabled == false)
                return;

            if (!Int32.TryParse(command.GetArg(1), out var desiredFov)) return;

            SetFov(player, desiredFov);
        }

        public void SetFov(CCSPlayerController? player, int desiredFov, bool noMySql = false)
        {
            player!.DesiredFOV = (uint)desiredFov;
            Utilities.SetStateChanged(player, "CBasePlayerController", "m_iDesiredFOV");

            var playerName = player.PlayerName;
            var slot = player.Slot;
            var steamID = player.SteamID.ToString();

            if (noMySql == false) playerTimers[slot].PlayerFov = desiredFov;
            if (enableDb)
                _ = Task.Run(async () => await SetPlayerStats(player, steamID, playerName, slot));
        }

        [ConsoleCommand("css_top", "Prints top players of this map")]
        [ConsoleCommand("css_mtop", "alias for !top")]
        [ConsoleCommand("css_maptop", "alias for !top")]
        [CommandHelper(whoCanExecute: CommandUsage.CLIENT_ONLY)]
        public void PrintTopRecords(CCSPlayerController? player, CommandInfo command)
        {
            if (!IsPlayerOrSpectator(player) || topEnabled == false)
                return;

            Utils.LogDebug($"{player!.PlayerName} calling css_top...");

            if (CommandCooldown(player))
                return;

            var mapName = command.ArgByIndex(1);

            Server.NextFrame(async () =>
            {
                await PrintTopRecordsHandler(player, player.PlayerName, 0, string.IsNullOrEmpty(mapName) ? "" : mapName, playerTimers[player.Slot].currentStyle);
            });
        }

        [ConsoleCommand("css_points", "Prints top points")]
        [ConsoleCommand("css_top10", "Prints top points")]
        [CommandHelper(whoCanExecute: CommandUsage.CLIENT_ONLY)]
        public void PrintTopPoints(CCSPlayerController? player, CommandInfo command)
        {
            if (!IsPlayerOrSpectator(player) || globalRanksEnabled == false)
                return;

            Utils.LogDebug($"{player!.PlayerName} calling css_points...");

            if (CommandCooldown(player))
                return;

            _ = Task.Run(async () => await PrintTop10PlayerPoints(player));
        }

        [ConsoleCommand("css_wr", "Prints world record for current map")]
        [CommandHelper(whoCanExecute: CommandUsage.CLIENT_ONLY)]
        public void PrintWR(CCSPlayerController? player, CommandInfo command)
        {
            if (!IsPlayerOrSpectator(player))
                return;

            Utils.LogDebug($"{player!.PlayerName} calling css_wr...");

            if (CommandCooldown(player))
                return;

            PrintWorldRecord(player);
        }

        [ConsoleCommand("css_gpoints", "Prints top global points")]
        [CommandHelper(whoCanExecute: CommandUsage.CLIENT_ONLY)]
        public void PrintTopGlobalPoints(CCSPlayerController? player, CommandInfo command)
        {
            if (!IsPlayerOrSpectator(player))
                return;

            Utils.LogDebug($"{player!.PlayerName} calling css_gpoints...");

            if (CommandCooldown(player))
                return;

            PrintGlobalPoints(player);
        }

        [ConsoleCommand("css_grank", "Prints personal global rank")]
        [CommandHelper(whoCanExecute: CommandUsage.CLIENT_ONLY)]
        public void PrintPlayerGlobalPoints(CCSPlayerController? player, CommandInfo command)
        {
            if (!IsPlayerOrSpectator(player))
                return;

            Utils.LogDebug($"{player!.PlayerName} calling css_grank...");

            if (CommandCooldown(player)) return;

            _ = Task.Run(async () => await PrintGlobalRankAsync(player));
        }

        [ConsoleCommand("css_topbonus", "Prints top players of this map bonus")]
        [ConsoleCommand("css_btop", "alias for !topbonus")]
        [CommandHelper(whoCanExecute: CommandUsage.CLIENT_ONLY)]
        public void PrintTopBonusRecords(CCSPlayerController? player, CommandInfo command)
        {
            if (!IsPlayerOrSpectator(player) || topEnabled == false)
                return;

            Utils.LogDebug($"{player!.PlayerName} calling css_topbonus...");

            if (CommandCooldown(player))
                return;

            if (!int.TryParse(command.ArgString, out int bonusX))
            {
                Utils.LogDebug("css_topbonus conversion failed. The input string is not a valid integer.");

                Utils.PrintToChat(player, Localizer["invalid_bonus_stage"]);
                return;
            }

            Server.NextFrame(async () => await PrintTopRecordsHandler(player, player.PlayerName, bonusX));
        }

        public async Task PrintTopRecordsHandler(CCSPlayerController? player, string playerName, int bonusX = 0, string mapName = "", int style = 0)
        {
            if (!IsPlayerOrSpectator(player) || topEnabled == false)
                return;

            Utils.LogDebug($"Handling !top for {playerName}");

            string? currentMapNamee;
            if (string.IsNullOrEmpty(mapName))
                currentMapNamee = bonusX == 0 ? currentMapName! : $"{currentMapName}_bonus{bonusX}";
            else
                currentMapNamee = mapName;

            var sortedRecords = await GetSortedRecordsFromDatabase(10, bonusX, mapName, style);

            Server.NextFrame(() =>
            {
                if (!IsPlayerOrSpectator(player))
                    return;

                if (sortedRecords.Count == 0)
                {
                    if (bonusX != 0)
                        Utils.PrintToChat(player!, Localizer["no_records_available_bonus", bonusX, currentMapNamee]);
                    else
                        Utils.PrintToChat(player!, Localizer["no_records_available", currentMapNamee]);
                    return;
                }

                List<string> printStatements;

                if (bonusX != 0)
                    printStatements = [$" {Localizer["top10_records_bonus", GetNamedStyle(style), bonusX, currentMapNamee]}"];
                else
                    printStatements = [$" {Localizer["top10_records", GetNamedStyle(style), currentMapNamee]}"];

                int rank = 1;

                foreach (var kvp in sortedRecords.Take(10))
                {
                    string _playerName = kvp.Value.PlayerName!;
                    int timerTicks = kvp.Value.TimerTicks;

                    bool showReplays = false;
                    if (enableReplays == true)
                        showReplays = Task.Run(() => CheckSRReplay(kvp.Value.SteamID!, bonusX)).Result;

                    string replayIndicator = enableReplays ? (showReplays ? $"{ChatColors.Red}◉" : "") : "";

                    printStatements.Add($" {Localizer["records_map", rank, _playerName, replayIndicator, Utils.FormatTime(timerTicks)]}");
                    rank++;
                }

                foreach (var statement in printStatements)
                    Utils.PrintToChat(player!, statement);
            });
        }

        [ConsoleCommand("css_rank", "Tells you your rank on this map")]
        [CommandHelper(whoCanExecute: CommandUsage.CLIENT_ONLY)]
        public void RankCommand(CCSPlayerController? player, CommandInfo command)
        {
            if (!IsPlayerOrSpectator(player) || rankEnabled == false)
                return;

            var slot = player!.Slot;
            var playerName = player.PlayerName;
            var steamID = player.SteamID.ToString();

            Utils.LogDebug($"{playerName} calling css_rank...");

            if (CommandCooldown(player))
                return;

            _ = Task.Run(async () => await RankCommandHandler(player, steamID, slot, playerName, false, playerTimers[slot].currentStyle));
        }

        public async Task RankCommandHandler(CCSPlayerController? player, string steamId, int slot, string playerName, bool sendRankToHUD = false, int style = 0)
        {
            if (player!.IsBot || player.SteamID.ToString() == "0")
                return;

            try
            {
                if (!IsPlayerOrSpectator(player))
                {
                    Utils.LogError($"Error in RankCommandHandler: Player not allowed or not on server anymore");
                    return;
                }

                //Utils.LogDebug($"Handling !rank for {playerName}...");

                string ranking, rankIcon, mapPlacement, serverPoints = "", serverPlacement = "";
                bool useGlobalRanks = enableDb && globalRanksEnabled;

                ranking = useGlobalRanks ? await GetPlayerServerPlacement(player, steamId, playerName) : await GetPlayerMapPlacementWithTotal(player, steamId, playerName, false, false, 0, style);
                rankIcon = useGlobalRanks ? await GetPlayerServerPlacement(player, steamId, playerName, true) : await GetPlayerMapPlacementWithTotal(player, steamId, playerName, true, false, 0, style);
                mapPlacement = await GetPlayerMapPlacementWithTotal(player, steamId, playerName, false, true, 0, style);

                foreach (var bonusRespawnPose in bonusRespawnPoses)
                {
                    var bonusNumber = bonusRespawnPose.Key;
                    var bonusPbTicks = enableDb ? await GetPreviousPlayerRecordFromDatabase(steamId, currentMapName!, playerName, bonusNumber, style) : await GetPreviousPlayerRecord(steamId, bonusNumber);

                    /// Skip this bonus since the player doesn't have a saved time
                    if (bonusPbTicks <= 0) continue;

                    var bonusPlacement = await GetPlayerMapPlacementWithTotal(player, steamId, playerName, false, true, bonusNumber, style);

                    Utils.LogDebug($"Adding bonus info for Bonus {bonusNumber}");
                    Utils.LogDebug($"PbTicks: {bonusPbTicks}");
                    Utils.LogDebug($"Placement: {bonusPlacement}");

                    playerTimers[slot].CachedBonusInfo[bonusNumber] = new PlayerBonusPlacementInfo()
                    {
                        PbTicks = bonusPbTicks,
                        Placement = bonusPlacement
                    };
                }

                if (useGlobalRanks)
                {
                    serverPoints = await GetPlayerServerPlacement(player, steamId, playerName, false, false, true);
                    serverPlacement = await GetPlayerServerPlacement(player, steamId, playerName, false, true, false);
                }

                int pbTicks = enableDb ? await GetPreviousPlayerRecordFromDatabase(steamId, currentMapName!, playerName, 0, style) : await GetPreviousPlayerRecord(steamId, 0);

                Server.NextFrame(() =>
                {
                    if (!IsPlayerOrSpectator(player)) return;
                    playerTimers[slot].RankHUDIcon = $"{(!string.IsNullOrEmpty(rankIcon) ? $" {rankIcon}" : "")}";
                    playerTimers[slot].CachedPB = $"{(pbTicks != 0 ? $" {Utils.FormatTime(pbTicks)}" : "")}";
                    playerTimers[slot].CachedRank = ranking;
                    playerTimers[slot].CachedMapPlacement = mapPlacement;

                    if (displayScoreboardTags) AddRankTagToPlayer(player!, ranking);
                });

                if (!sendRankToHUD)
                {
                    Server.NextFrame(() =>
                    {
                        if (!IsPlayerOrSpectator(player))
                            return;

                        string rankMessage = Localizer["current_rank", GetRankColorForChat(player!), ranking];

                        if (useGlobalRanks)
                            rankMessage += Localizer["current_rank_points", serverPoints, serverPlacement];

                        Utils.PrintToChat(player!, rankMessage);

                        if (pbTicks != 0)
                            Utils.PrintToChat(player!, Localizer["current_pb", currentMapName!, Utils.FormatTime(pbTicks), mapPlacement]);
                        
                        if (playerTimers[slot].CachedBonusInfo.Any())
                        {
                            foreach (var bonusPb in playerTimers[slot].CachedBonusInfo.OrderBy(x => x.Key))
                                Utils.PrintToChat(player!, $"{Localizer["current_bonus_pb", bonusPb.Key!, Utils.FormatTime(bonusPb.Value.PbTicks), bonusPb.Value.Placement!]}");
                        }
                    });
                }
            }
            catch (Exception ex)
            {
                Utils.LogError($"Error in RankCommandHandler: {ex}");
            }
        }

        [ConsoleCommand("css_sr", "Tells you the Server record on this map")]
        [CommandHelper(whoCanExecute: CommandUsage.CLIENT_ONLY)]
        public void SRCommand(CCSPlayerController? player, CommandInfo command)
        {
            if (!IsPlayerOrSpectator(player) || rankEnabled == false)
                return;

            var playerName = player!.PlayerName;

            Utils.LogDebug($"{playerName} calling css_sr...");

            if (CommandCooldown(player))
                return;

            _ = Task.Run(async () => await SRCommandHandler(player, playerName));
        }

        public async Task SRCommandHandler(CCSPlayerController? player, string _playerName)
        {
            if (!IsPlayerOrSpectator(player) || rankEnabled == false)
                return;

            Utils.LogDebug($"Handling !sr for {_playerName}...");
            
            var sortedRecords = await GetSortedRecordsFromDatabase();

            if (sortedRecords.Count == 0)
                return;

            Server.NextFrame(() =>
            {
                Utils.PrintToChat(player!, Localizer["current_sr", currentMapName!]);
            });

            foreach (var kvp in sortedRecords.Take(1))
            {
                string playerName = kvp.Value.PlayerName!;
                int timerTicks = kvp.Value.TimerTicks;
                Server.NextFrame(() =>
                {
                    Utils.PrintToChat(player!, Localizer["current_sr_player", playerName!, Utils.FormatTime(timerTicks)]);
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
                if (!IsAllowedPlayer(player) || respawnEnabled == false)
                    return;

                var slot = player!.Slot;
                var playerName = player.PlayerName;
                var steamID = player.SteamID.ToString();

                Utils.LogDebug($"{playerName} calling css_rb...");

                if (CommandCooldown(player))
                    return;

                if (ReplayCheck(player))
                    return;

                //defaults to !b 1 without any args
                if (command.ArgString == null || command.ArgString == "")
                {
                    if (bonusRespawnPoses[1] != null)
                    {
                        if (bonusRespawnAngs.TryGetValue(1, out QAngle_t? bonusAng) && bonusAng != null)
                        {
                            player.PlayerPawn.Value!.Teleport(bonusRespawnPoses[1]!, bonusRespawnAngs[1]!);
                        }
                        else
                        {
                            player.PlayerPawn.Value!.Teleport(bonusRespawnPoses[1]!, player.PlayerPawn.Value?.EyeAngles.ToQAngle_t());
                        }
                        Utils.LogDebug($"{player.PlayerName} css_rb {1} to {bonusRespawnPoses[1]}");
                    }
                    else
                    {
                        Utils.PrintToChat(player, Localizer["no_respawnpos_bonus_index", 1]);
                    }
                    Server.NextFrame(() =>
                    {
                        playerTimers[slot].IsTimerRunning = false;
                        playerTimers[slot].TimerTicks = 0;
                        playerTimers[slot].IsBonusTimerRunning = false;
                        playerTimers[slot].BonusTimerTicks = 0;
                    });
                    return;
                }

                if (!int.TryParse(command.ArgString, out int bonusX))
                {
                    Utils.LogDebug("css_rb conversion failed. The input string is not a valid integer.");
                    Utils.PrintToChat(player, Localizer["no_respawnpos_bonus_rb"]);
                    return;
                }

                // Remove checkpoints for the current player
                if (!playerTimers[slot].IsTimerBlocked)
                {
                    playerCheckpoints.Remove(slot);
                }

                if (bonusRespawnPoses[bonusX] != null)
                {
                    if (bonusRespawnAngs.TryGetValue(bonusX, out QAngle_t? bonusAng) && bonusAng != null)
                        player.PlayerPawn.Value!.Teleport(bonusRespawnPoses[bonusX]!, bonusRespawnAngs[bonusX]!, new Vector_t(0, 0, 0));
                    else
                        player.PlayerPawn.Value!.Teleport(bonusRespawnPoses[bonusX]!, player.PlayerPawn.Value?.EyeAngles.ToQAngle_t(), new Vector_t(0, 0, 0));

                    Utils.LogDebug($"{player.PlayerName} css_rb {bonusX} to {bonusRespawnPoses[bonusX]}");
                }
                else
                    Utils.PrintToChat(player, Localizer["no_respawnpos_bonus"]);

                Server.NextFrame(() =>
                {
                    playerTimers[slot].IsTimerRunning = false;
                    playerTimers[slot].TimerTicks = 0;
                    playerTimers[slot].IsBonusTimerRunning = false;
                    playerTimers[slot].BonusTimerTicks = 0;
                    playerTimers[slot].IsTimerBlocked = false;
                });

                PlaySound(player, respawnSound);
            }
            catch (Exception ex)
            {
                Utils.LogError($"Exception in RespawnBonusPlayer: {ex.Message}");
            }
        }

        [ConsoleCommand("css_startpos", "Saves a custom respawn point within the start trigger")]
        [ConsoleCommand("css_setresp", "Saves a custom respawn point within the start trigger")]
        [ConsoleCommand("css_ssp", "Saves a custom respawn point within the start trigger")]
        [CommandHelper(whoCanExecute: CommandUsage.CLIENT_ONLY)]
        public void SetRespawnCommand(CCSPlayerController? player, CommandInfo command)
        {
            if (!IsAllowedPlayer(player) || respawnEnabled == false)
                return;

            var slot = player!.Slot;
            var playerName = player.PlayerName;
            var steamID = player.SteamID.ToString();

            Utils.LogDebug($"{playerName} calling css_startpos...");

            if (ReplayCheck(player))
                return;

            if (useTriggers == false)
            {
                Utils.PrintToChat(player, Localizer["map_using_manual_zones"]);
                return;
            }

            // Get the player's current position and rotation
            Vector_t currentPosition = player.Pawn.Value!.CBodyComponent?.SceneNode?.AbsOrigin.ToVector_t() ?? new Vector_t(0, 0, 0);
            QAngle_t currentRotation = player.PlayerPawn.Value!.EyeAngles.ToQAngle_t();

            if (useTriggers == true)
            {
                if (Utils.IsVectorInsideBox(currentPosition + new Vector_t(0, 0, 10), currentMapStartTriggerMaxs.GetValueOrDefault(), currentMapStartTriggerMins.GetValueOrDefault()))
                {
                    // Convert position and rotation to strings
                    string positionString = $"{currentPosition.X} {currentPosition.Y} {currentPosition.Z}";
                    string rotationString = $"{currentRotation.X} {currentRotation.Y} {currentRotation.Z}";

                    playerTimers[slot].SetRespawnPos = positionString;
                    playerTimers[slot].SetRespawnAng = rotationString;
                    Utils.PrintToChat(player, Localizer["saved_custom_respawnpos"]);
                }
                else
                    Utils.PrintToChat(player, Localizer["not_inside_startzone"]);
            }
            else
            {
                if (Utils.IsVectorInsideBox(currentPosition + new Vector_t(0, 0, 10), currentMapStartC1, currentMapStartC2))
                {
                    // Convert position and rotation to strings
                    string positionString = $"{currentPosition.X} {currentPosition.Y} {currentPosition.Z}";
                    string rotationString = $"{currentRotation.X} {currentRotation.Y} {currentRotation.Z}";

                    playerTimers[slot].SetRespawnPos = positionString;
                    playerTimers[slot].SetRespawnAng = rotationString;
                    Utils.PrintToChat(player, Localizer["saved_custom_respawnpos"]);
                }
                else
                    Utils.PrintToChat(player, Localizer["not_inside_startzone"]);
            }
        }

        [ConsoleCommand("css_stage", "Teleports you to a stage")]
        [CommandHelper(whoCanExecute: CommandUsage.CLIENT_ONLY)]
        public void TPtoStagePlayer(CCSPlayerController? player, CommandInfo command)
        {
            try
            {
                if (!IsAllowedPlayer(player) || respawnEnabled == false)
                    return;

                var slot = player!.Slot;
                var playerName = player.PlayerName;

                Utils.LogDebug($"{playerName} calling css_stage...");

                if (CommandCooldown(player))
                    return;

                if (ReplayCheck(player))
                    return;

                QuietStopTimer(player);

                if (IsTimerBlocked(player))
                    return;

                if (!int.TryParse(command.ArgString, out int stageX))
                {
                    Utils.LogDebug("css_stage conversion failed. The input string is not a valid integer.");
                    Utils.PrintToChat(player, Localizer["stages_enter_valid"]);
                    return;
                }

                if (useStageTriggers == false)
                {
                    Utils.LogDebug("css_stage failed useStages is false.");
                    Utils.PrintToChat(player, Localizer["stages_unavalible"]);
                    return;
                }

                // Remove checkpoints for the current player
                if (!playerTimers[slot].IsTimerBlocked)
                {
                    playerCheckpoints.Remove(slot);
                }

                if (stageTriggerPoses.TryGetValue(stageX, out Vector_t? stagePos) && stagePos != null)
                {
                    player.PlayerPawn.Value!.Teleport(stagePos, stageTriggerAngs[stageX] ?? player.PlayerPawn.Value?.EyeAngles.ToQAngle_t(), new Vector_t(0, 0, 0));
                    Utils.LogDebug($"{player.PlayerName} css_stage {stageX} to {stagePos}");
                }
                else
                {
                    Utils.PrintToChat(player, Localizer["stages_unavalible_respawnpos", stageX]);
                }

                Server.NextFrame(() =>
                {
                    playerTimers[slot].IsTimerRunning = false;
                    playerTimers[slot].TimerTicks = 0;
                    playerTimers[slot].IsBonusTimerRunning = false;
                    playerTimers[slot].BonusTimerTicks = 0;
                    playerTimers[slot].IsTimerBlocked = false;
                });

                PlaySound(player, respawnSound);
            }
            catch (Exception ex)
            {
                Utils.LogError($"Exception in TPtoStagePlayer: {ex.Message}");
            }
        }

        [ConsoleCommand("css_r", "Teleports you to start")]
        [CommandHelper(whoCanExecute: CommandUsage.CLIENT_ONLY)]
        public void RespawnPlayerCommand(CCSPlayerController? player, CommandInfo command)
        {
            if (!IsAllowedPlayer(player) || respawnEnabled == false)
                return;

            var slot = player!.Slot;
            var playerName = player.PlayerName;

            Utils.LogDebug($"{playerName} calling css_r...");

            if (CommandCooldown(player)) 
                return;

            if (playerTimers[slot].IsReplaying)
            {
                Utils.PrintToChat(player, Localizer["ending_replay"]);
                playerTimers[slot].IsReplaying = false;

                if (player.PlayerPawn.Value!.MoveType != MoveType_t.MOVETYPE_WALK || player.PlayerPawn.Value.ActualMoveType == MoveType_t.MOVETYPE_WALK) SetMoveType(player, MoveType_t.MOVETYPE_WALK);
                    playerReplays.Remove(slot);

                playerReplays[slot] = new PlayerReplays();
                playerTimers[slot].IsTimerBlocked = false;
                playerTimers[slot].IsTimerRunning = false;
                playerTimers[slot].TimerTicks = 0;
                playerTimers[slot].StageTicks = 0;
                playerTimers[slot].IsBonusTimerRunning = false;
                playerTimers[slot].BonusTimerTicks = 0;
                playerReplays[slot].CurrentPlaybackFrame = 0;

                if (stageTriggers.Count != 0) playerTimers[slot].StageTimes!.Clear(); //remove previous stage times if the map has stages
                if (stageTriggers.Count != 0) playerTimers[slot].StageVelos!.Clear(); //remove previous stage times if the map has stages

                RespawnPlayer(player);
            }

            else RespawnPlayer(player);
        }

        [ConsoleCommand("css_end", "Teleports you to end")]
        [CommandHelper(whoCanExecute: CommandUsage.CLIENT_ONLY)]
        public void EndPlayerCommand(CCSPlayerController? player, CommandInfo command)
        {
            if (!IsAllowedPlayer(player) || respawnEndEnabled == false)
                return;

            var slot = player!.Slot;
            var playerName = player.PlayerName;

            Utils.LogDebug($"{playerName} calling css_end...");

            if (CommandCooldown(player))
                return;

            playerTimers[slot].IsTimerRunning = false;
            playerTimers[slot].TimerTicks = 0;
            playerTimers[slot].IsBonusTimerRunning = false;
            playerTimers[slot].BonusTimerTicks = 0;

            Server.NextFrame(() => RespawnPlayer(player, true));
        }

        [ConsoleCommand("css_noclip", "Noclip")]
        [ConsoleCommand("css_nc", "Noclip")]
        [CommandHelper(whoCanExecute: CommandUsage.CLIENT_ONLY)]
        public void NoclipCommand(CCSPlayerController? player, CommandInfo command)
        {
            if (player == null || !player.PawnIsAlive || (enableNoclip == false && !AdminManager.PlayerHasPermissions(player, "@css/cheats")))
                return;

            var slot = player!.Slot;
            var playerName = player.PlayerName;
            var pawn = player.Pawn.Value!;

            Utils.LogDebug($"{playerName} calling css_noclip...");

            if (CommandCooldown(player))
                return;

            if (IsTimerBlocked(player))
                return;

            playerTimers[slot].IsTimerRunning = false;
            playerTimers[slot].TimerTicks = 0;
            playerTimers[slot].IsBonusTimerRunning = false;
            playerTimers[slot].BonusTimerTicks = 0;

            if (playerTimers[slot].IsNoclip)
            {
                pawn.MoveType = MoveType_t.MOVETYPE_WALK;
                Schema.SetSchemaValue(pawn.Handle, "CBaseEntity", "m_nActualMoveType", 2); // walk
                Utilities.SetStateChanged(pawn, "CBaseEntity", "m_MoveType");
                playerTimers[slot].IsNoclip = false;
            }
            else
            {
                pawn.MoveType = MoveType_t.MOVETYPE_NOCLIP;
                Schema.SetSchemaValue(pawn.Handle, "CBaseEntity", "m_nActualMoveType", 8); // noclip
                Utilities.SetStateChanged(pawn, "CBaseEntity", "m_MoveType");
                playerTimers[slot].IsNoclip = true;
            }
        }

        [ConsoleCommand("css_styles", "Styles command")]
        [ConsoleCommand("css_style", "Styles command")]
        [CommandHelper(whoCanExecute: CommandUsage.CLIENT_ONLY)]
        public void StyleCommand(CCSPlayerController? player, CommandInfo command)
        {
            if (!IsAllowedPlayer(player))
                return;

            var slot = player!.Slot;
            var playerName = player.PlayerName;

            if (ReplayCheck(player))
                return;

            if (!isLinux && !enableDb)
            {
                Utils.PrintToChat(player, Localizer["styles_not_supported"]);
                return;
            }
            if (!enableStyles)
            {
                Utils.PrintToChat(player, Localizer["styles_disabled"]);
                return;
            }

            if (!player!.PlayerPawn.Value!.AbsVelocity.IsZero())
            {
                Utils.PrintToChat(player, Localizer["styles_moving"]);
                return;
            }

            Utils.LogDebug($"{playerName} calling css_style...");

            if (CommandCooldown(player))
                return;

            playerTimers[slot].IsTimerRunning = false;
            playerTimers[slot].TimerTicks = 0;
            playerTimers[slot].IsBonusTimerRunning = false;
            playerTimers[slot].BonusTimerTicks = 0;

            var desiredStyle = command.GetArg(1);

            if (command.ArgByIndex(1) == "")
            {
                for (int i = 0; i < 13; i++) //runs 13 times for the 13 styles
                {
                    Utils.PrintToChat(player, Localizer["styles_list", i, GetNamedStyle(i)]);
                }
                Utils.PrintToChat(player, Localizer["style_example"]);
                return;
            }

            if (Int32.TryParse(command.GetArg(1), out var desiredStyleInt))
            {
                switch (desiredStyleInt)
                {
                    case 0:
                    case 1:
                    case 2:
                    case 3:
                    case 4:
                    case 5:
                    case 6:
                    case 7:
                    case 8:
                    case 9:
                    case 10:
                    case 11:
                    case 12:
                        setStyle(player, desiredStyleInt);
                        Utils.PrintToChat(player, Localizer["style_set", GetNamedStyle(desiredStyleInt)]);
                        break;
                    default:
                        Utils.PrintToChat(player, Localizer["style_not_found", desiredStyleInt]);
                        break;
                }
                RespawnPlayer(player);
            }
            else
            {
                string styleLowerCase = desiredStyle.ToLower();
                switch (styleLowerCase)
                {
                    case "default":
                    case "normal":
                    case "nrm":
                        setStyle(player, 0);
                        Utils.PrintToChat(player, Localizer["style_set", GetNamedStyle(0)]);
                        break;
                    case "lowgravity":
                    case "lowgrav":
                    case "lg":
                        setStyle(player, 1);
                        Utils.PrintToChat(player, Localizer["style_set", GetNamedStyle(1)]);
                        break;
                    case "sideways":
                    case "sw":
                        setStyle(player, 2);
                        Utils.PrintToChat(player, Localizer["style_set", GetNamedStyle(2)]);
                        break;
                    case "wonly":
                    case "onlyw":
                        setStyle(player, 3);
                        Utils.PrintToChat(player, Localizer["style_set", GetNamedStyle(3)]);
                        break;
                    case "400vel":
                        setStyle(player, 4);
                        Utils.PrintToChat(player, Localizer["style_set", GetNamedStyle(4)]);
                        break;
                    case "highgravity":
                    case "highgrav":
                    case "hg":
                        setStyle(player, 5);
                        Utils.PrintToChat(player, Localizer["style_set", GetNamedStyle(5)]);
                        break;
                    case "aonly":
                    case "onlya":
                        setStyle(player, 6);
                        Utils.PrintToChat(player, Localizer["style_set", GetNamedStyle(6)]);
                        break;
                    case "donly":
                    case "onlyd":
                        setStyle(player, 7);
                        Utils.PrintToChat(player, Localizer["style_set", GetNamedStyle(7)]);
                        break;
                    case "sonly":
                    case "onlys":
                        setStyle(player, 8);
                        Utils.PrintToChat(player, Localizer["style_set", GetNamedStyle(8)]);
                        break;
                    case "halfsideways":
                    case "hsw":
                        setStyle(player, 9);
                        Utils.PrintToChat(player, Localizer["style_set", GetNamedStyle(9)]);
                        break;
                    case "fastforward":
                    case "ff":
                        setStyle(player, 10);
                        Utils.PrintToChat(player, Localizer["style_set", GetNamedStyle(10)]);
                        break;
                    case "parachute":
                    case "para":
                        setStyle(player, 11);
                        Utils.PrintToChat(player, Localizer["style_set", GetNamedStyle(11)]);
                        break;
                    case "tas":
                        setStyle(player, 12);
                        Utils.PrintToChat(player, Localizer["style_set", GetNamedStyle(12)]);
                        break;
                    default:
                        Utils.PrintToChat(player, Localizer["style_not_found", styleLowerCase]);
                        break;
                }
                RespawnPlayer(player);
            }
        }

        [ConsoleCommand("css_ranks", "Ranks command")]
        [CommandHelper(whoCanExecute: CommandUsage.CLIENT_ONLY)]
        public void RanksCommand(CCSPlayerController? player, CommandInfo command)
        {
            if (!IsAllowedPlayer(player))
                return;

            if (CommandCooldown(player!))
                return;

            if (!rankEnabled)
            {
                Utils.PrintToChat(player!, "This server has not enabled ranks");
                return;
            }

            var rankList = string.Join($"{ChatColors.White}, ", 
                rankDataList
                    .Where(rank => rank.Title != UnrankedTitle)
                    .OrderByDescending(rank => rank.Percent)
                    .Select(rank => Utils.ReplaceVars($"{rank.Color}{rank.Title.Replace("[", "").Replace("]", "")}"))
            );

            Utils.PrintToChat(player!, $"{rankList}");
        }

        public void RespawnPlayer(CCSPlayerController player, bool toEnd = false)
        {
            try
            {
                var slot = player.Slot;

                // Remove checkpoints for the current player
                if (!playerTimers[slot].IsTimerBlocked)
                    playerCheckpoints.Remove(slot);

                if (stageTriggerCount != 0 || cpTriggerCount != 0)//remove previous stage times and checkpoints if the map has stages or checkpoints
                {
                    playerTimers[slot].StageTimes!.Clear();
                    playerTimers[slot].CurrentMapCheckpoint = 0;
                }

                if (toEnd == false)
                {
                    if (currentRespawnPos != null && playerTimers[slot].SetRespawnPos == null)
                    {
                        if (currentRespawnAng != null)
                            player.PlayerPawn.Value!.Teleport(currentRespawnPos, currentRespawnAng);
                        else
                            player.PlayerPawn.Value!.Teleport(currentRespawnPos, player.PlayerPawn.Value?.EyeAngles.ToQAngle_t());

                        Utils.LogDebug($"{player.PlayerName} css_r to {currentRespawnPos}");
                    }
                    else
                    {
                        if (playerTimers[slot].SetRespawnPos != null && playerTimers[slot].SetRespawnAng != null)
                            player.PlayerPawn.Value!.Teleport(Utils.ParseVector_t(playerTimers[slot].SetRespawnPos!), Utils.ParseQAngle_t(playerTimers[slot].SetRespawnAng!));
                        else
                            Utils.PrintToChat(player, Localizer["no_respawnpos"]);
                    }
                }
                else
                {
                    if (currentEndPos != null)
                        player.PlayerPawn.Value!.Teleport(currentEndPos, player.PlayerPawn.Value?.EyeAngles.ToQAngle_t(), new Vector_t(0, 0, 0));
                    else
                        Utils.PrintToChat(player, Localizer["no_endpos"]);
                }

                Server.NextFrame(() =>
                {
                    playerTimers[slot].IsTimerRunning = false;
                    playerTimers[slot].TimerTicks = 0;
                    playerTimers[slot].StageTicks = 0;
                    playerTimers[slot].IsBonusTimerRunning = false;
                    playerTimers[slot].BonusTimerTicks = 0;
                    playerTimers[slot].IsTimerBlocked = false;
                });

                PlaySound(player, respawnSound);
            }
            catch (Exception ex)
            {
                Utils.LogError($"Exception in RespawnPlayer: {ex.Message}");
            }
        }

        [ConsoleCommand("css_rs", "Teleport player to start of stage.")]
        [CommandHelper(whoCanExecute: CommandUsage.CLIENT_ONLY)]
        public void RestartCurrentStageCmd(CCSPlayerController? player, CommandInfo command)
        {
            if (!IsAllowedPlayer(player))
                return;

            var slot = player!.Slot;
            var playerName = player.PlayerName;

            Utils.LogDebug($"{playerName} calling css_rs...");

            if (CommandCooldown(player))
                return;

            if (stageTriggerCount == 0)
            {
                if (enableRsOnLinear) {
                    player.ExecuteClientCommandFromServer("css_r");
                    return;
                }
                Utils.PrintToChat(player, Localizer["map_no_stages"]);
                return;
            }

            if (!playerTimers.TryGetValue(slot, out PlayerTimerInfo? playerTimer) || playerTimer.CurrentMapStage == 0)
            {
                Utils.PrintToChat(player, Localizer["error_occured"]);
                Utils.LogDebug("Failed to get playerTimer or playerTimer.CurrentMapStage == 0.");
                return;
            }

            int currStage = playerTimer.CurrentMapStage;

            try
            {
                if (stageTriggerPoses.TryGetValue(currStage, out Vector_t? stagePos) && stagePos != null)
                {
                    player.PlayerPawn.Value!.Teleport(stagePos, stageTriggerAngs[currStage] ?? player.PlayerPawn.Value?.EyeAngles.ToQAngle_t(), new Vector_t(0, 0, 0));

                    Utils.LogDebug($"{playerName} css_rs");
                }
                else
                    Utils.PrintToChat(player, Localizer["stages_unavalible_respawnpos"]);
            }
            catch (Exception ex)
            {
                Utils.LogError($"Exception in RestartCurrentStage: {ex.Message}");
            }
        }

        [ConsoleCommand("css_timer", "Stops your timer")]
        [CommandHelper(whoCanExecute: CommandUsage.CLIENT_ONLY)]
        public void ForceStopTimer(CCSPlayerController? player, CommandInfo command)
        {
            if (!IsAllowedPlayer(player))
                return;

            var slot = player!.Slot;
            var playerName = player.PlayerName;

            Utils.LogDebug($"{playerName} calling css_timer...");

            if (CommandCooldown(player))
                return;

            if (ReplayCheck(player))
                return;

            // Remove checkpoints for the current player
            playerCheckpoints.Remove(slot);

            playerTimers[slot].IsTimerBlocked = !playerTimers[slot].IsTimerBlocked;
            playerTimers[slot].IsRecordingReplay = false;


            if (playerTimers[slot].IsTimerBlocked)
                Utils.PrintToChat(player, Localizer["timer_disabled"]);
            else
                Utils.PrintToChat(player, Localizer["timer_enabled"]);

            playerTimers[slot].IsTimerRunning = false;
            playerTimers[slot].TimerTicks = 0;
            playerTimers[slot].IsBonusTimerRunning = false;
            playerTimers[slot].BonusTimerTicks = 0;

            if (stageTriggers.Count != 0) playerTimers[slot].StageTimes!.Clear(); //remove previous stage times if the map has stages
            if (stageTriggers.Count != 0) playerTimers[slot].StageVelos!.Clear(); //remove previous stage times if the map has stages

            // fix timer toggle bug
            if (!playerTimers[slot].IsTimerBlocked)
            {
                Vector_t playerPos = player.Pawn?.Value!.CBodyComponent?.SceneNode!.AbsOrigin.ToVector_t() ?? new();
                bool isInsideStartBox = Utils.IsVectorInsideBox(playerPos, currentMapStartC1, currentMapStartC2);
                playerTimers[slot].inStartzone = isInsideStartBox; // Only set to true if player is actually in the start zone
            }
            else playerTimers[slot].inStartzone = false;

            PlaySound(player, timerSound);
            Utils.LogDebug($"{player.PlayerName} css_timer to {playerTimers[slot].IsTimerBlocked}");
        }

        public void QuietStopTimer(CCSPlayerController player)
        {
            var slot = player!.Slot;

            // Remove checkpoints for the current player
            if (playerCheckpoints.ContainsKey(slot))
                playerCheckpoints.Remove(slot);

            playerTimers[slot].IsTimerBlocked = true;
            playerTimers[slot].IsRecordingReplay = false;
            playerTimers[slot].IsTimerRunning = false;
            playerTimers[slot].TimerTicks = 0;
            playerTimers[slot].IsBonusTimerRunning = false;
            playerTimers[slot].BonusTimerTicks = 0;

            if (stageTriggers.Count != 0) playerTimers[slot].StageTimes!.Clear(); //remove previous stage times if the map has stages
            if (stageTriggers.Count != 0) playerTimers[slot].StageVelos!.Clear(); //remove previous stage times if the map has stages

            PlaySound(player, timerSound);
        }

        [ConsoleCommand("css_stver", "Prints SharpTimer Version")]
        [CommandHelper(whoCanExecute: CommandUsage.CLIENT_AND_SERVER)]
        public void STVerCommand(CCSPlayerController? player, CommandInfo command)
        {
            if (player == null || !IsPlayerOrSpectator(player))
            {
                Utils.ConPrint($"This server is running SharpTimer v{ModuleVersion}");
                Utils.ConPrint($"OS: {RuntimeInformation.OSDescription}");
                Utils.ConPrint($"Runtime: {RuntimeInformation.RuntimeIdentifier}");
                return;
            }

            if (CommandCooldown(player))
                return;

            Utils.PrintToChat(player, Localizer["info_version", ModuleVersion]);
            Utils.PrintToChat(player, Localizer["info_os", RuntimeInformation.OSDescription]);
            Utils.PrintToChat(player, Localizer["info_runtime", RuntimeInformation.RuntimeIdentifier]);
        }

        [ConsoleCommand("css_hide", "Hides players")]
        [CommandHelper(whoCanExecute: CommandUsage.CLIENT_AND_SERVER)]
        public void HideCommand(CCSPlayerController? player, CommandInfo command)
        {
            if (!IsAllowedPlayer(player))
                return;

            var slot = player!.Slot;
            var playerName = player.PlayerName;
            var steamID = player.SteamID.ToString();

            if (CommandCooldown(player))
                return;

            bool hidingPlayers = !playerTimers[slot].HidePlayers;

            playerTimers[slot].HidePlayers = hidingPlayers;
            
            _ = Task.Run(async () => await SetPlayerStats(player, steamID, playerName, slot));
            
            if (hidingPlayers)
                Utils.PrintToChat(player, $"Hide: {ChatColors.Green}Enabled");
            else
                Utils.PrintToChat(player, $"Hide: {ChatColors.LightRed}Disabled");
        }
        /*
        [ConsoleCommand("css_mode", "Changes mode")]
        [CommandHelper(minArgs: 1, usage: "[mode]", whoCanExecute: CommandUsage.CLIENT_ONLY)]
        public void ModeCommand(CCSPlayerController? player, CommandInfo command)
        {
            if (!IsAllowedPlayer(player) || goToEnabled == false) return;
            Utils.LogDebug($"{playerName} calling css_mode...");

            if (CommandCooldown(player))
                return;

            if (ReplayCheck(player))
                return;

            if (IsTimerBlocked(player))
                return;

            playerTimers[slot].TicksSinceLastCmd = 0;

            string mode = command.GetArg(1).ToLower();
            switch(mode)
            {
                case "classic":
                    playerTimers[slot].Mode = PlayerTimerInfo.CurrentMode.Classic;
                    SetModeClassic(player);
                    break;
                case "arcade":
                    playerTimers[slot].Mode = PlayerTimerInfo.CurrentMode.Arcade;
                    SetModeArcade(player);
                    break;
                default:
                    playerTimers[slot].Mode = PlayerTimerInfo.CurrentMode.Classic;
                    SetModeClassic(player);
                    break;
                
            }
            Server.NextFrame(() => Utils.PrintToChat(player, $"Mode changed to: {playerTimers[slot].Mode.ToString()}"));
        }
        */
        [ConsoleCommand("css_goto", "Teleports you to a player")]
        [CommandHelper(minArgs: 1, usage: "[name]", whoCanExecute: CommandUsage.CLIENT_ONLY)]
        public void GoToPlayer(CCSPlayerController? player, CommandInfo command)
        {
            if (!IsAllowedPlayer(player) || goToEnabled == false)
                return;

            var slot = player!.Slot;
            var playerName = player.PlayerName;

            Utils.LogDebug($"{playerName} calling css_goto...");

            if (CommandCooldown(player))
                return;

            if (ReplayCheck(player))
                return;

            if (IsTimerBlocked(player))
                return;

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
                Utils.PrintToChat(player, Localizer["goto_player_not_found"]);
                return;
            }

            if (!playerTimers[slot].IsTimerBlocked)
                playerCheckpoints.Remove(slot);

            playerTimers[slot].IsTimerRunning = false;
            playerTimers[slot].TimerTicks = 0;

            PlaySound(player, respawnSound);

            if (foundPlayer != null && playerTimers[slot].IsTimerBlocked)
            {
                Utils.PrintToChat(player, Localizer["goto_player", foundPlayer.PlayerName]);

                if (player != null && IsAllowedPlayer(foundPlayer) && playerTimers[slot].IsTimerBlocked)
                {
                    player.PlayerPawn.Value!.Teleport(foundPlayer.Pawn.Value!.CBodyComponent?.SceneNode?.AbsOrigin.ToVector_t(),
                        foundPlayer.PlayerPawn.Value!.EyeAngles.ToQAngle_t(), new Vector_t(0, 0, 0));

                    Utils.LogDebug($"{player.PlayerName} css_goto to {foundPlayer.Pawn.Value.CBodyComponent?.SceneNode?.AbsOrigin.ToVector_t()}");
                }
            }
            else
                Utils.PrintToChat(player, Localizer["goto_player_not_found"]);
        }

        [ConsoleCommand("css_cp", "Sets a checkpoint")]
        [ConsoleCommand("css_saveloc", "alias for !cp")]
        [CommandHelper(whoCanExecute: CommandUsage.CLIENT_ONLY)]
        public void SetPlayerCPCommand(CCSPlayerController? player, CommandInfo command)
        {
            if (!IsAllowedPlayer(player) || cpEnabled == false)
                return;

            var slot = player!.Slot;
            var playerName = player.PlayerName;

            Utils.LogDebug($"{playerName} calling css_cp...");

            if (ReplayCheck(player))
                return;

            SetPlayerCP(player, command, slot);
        }

        public void SetPlayerCP(CCSPlayerController? player, CommandInfo command, int slot)
        {
            if (((PlayerFlags)player!.Pawn.Value!.Flags & PlayerFlags.FL_ONGROUND) != PlayerFlags.FL_ONGROUND && removeCpRestrictEnabled == false)
            {
                Utils.PrintToChat(player, Localizer["cant_use_checkpoint_in_air", (currentMapName!.Contains("surf_") ? "loc" : "checkpoint")]);
                PlaySound(player, cpSoundError);
                return;
            }

            if (!CanCheckpoint(player))
                return;
            
            if (playerTimers[slot].currentStyle == 12)
                playerTimers[slot].PrevTimerTicks.Add(playerTimers[slot].TimerTicks);

            // Get the player's current position and rotation
            Vector_t? currentPosition = player.Pawn.Value.CBodyComponent?.SceneNode?.AbsOrigin.ToVector_t();
            Vector_t currentSpeed = player.PlayerPawn.Value!.AbsVelocity.ToVector_t();
            QAngle_t currentRotation = player.PlayerPawn.Value.EyeAngles.ToQAngle_t();

            // Convert position and rotation to strings
            string positionString = $"{currentPosition.GetValueOrDefault().X} {currentPosition.GetValueOrDefault().Y} {currentPosition.GetValueOrDefault().Z}";
            string rotationString = $"{currentRotation.X} {currentRotation.Y} {currentRotation.Z}";
            string speedString = $"{currentSpeed.X} {currentSpeed.Y} {currentSpeed.Z}";

            // Add the current position and rotation strings to the player's checkpoint list
            if (!playerCheckpoints.ContainsKey(slot))
            {
                playerCheckpoints[slot] = [];
            }

            playerCheckpoints[slot].Add(new PlayerCheckpoint
            {
                PositionString = positionString,
                RotationString = rotationString,
                SpeedString = speedString
            });

            // Get the count of checkpoints for this player
            int checkpointCount = playerCheckpoints[slot].Count;
            playerTimers[slot].CheckpointIndex = checkpointCount - 1;

            // Print the chat message with the checkpoint count
            Utils.PrintToChat(player, Localizer["checkpoint_set", (currentMapName!.Contains("surf_") ? "loc" : "checkpoint"), checkpointCount]);
            PlaySound(player, cpSound);
            Utils.LogDebug($"{player.PlayerName} css_cp to {checkpointCount} {positionString} {rotationString} {speedString}");
        }

        [ConsoleCommand("css_tp", "Tp to the most recent checkpoint")]
        [ConsoleCommand("css_loadloc", "alias for !tp")]
        [CommandHelper(whoCanExecute: CommandUsage.CLIENT_ONLY)]
        public void TpPlayerCPCommand(CCSPlayerController? player, CommandInfo command)
        {
            if (!IsAllowedPlayer(player) || cpEnabled == false)
                return;

            var slot = player!.Slot;
            var playerName = player.PlayerName;

            Utils.LogDebug($"{playerName} calling css_tp...");

            if (ReplayCheck(player))
                return;

            TpPlayerCP(player, command, slot);
        }

        public void TpPlayerCP(CCSPlayerController player, CommandInfo command, int slot)
        {
            if (ReplayCheck(player))
                return;

            if (!CanCheckpoint(player))
                return;
            
            if(playerTimers[slot].currentStyle == 12)
                playerTimers[slot].TimerTicks = playerTimers[slot].PrevTimerTicks[playerTimers[slot].CheckpointIndex];

            // Check if the player has any checkpoints
            if (!playerCheckpoints.ContainsKey(slot) || playerCheckpoints[slot].Count == 0)
            {
                Utils.PrintToChat(player, Localizer["no_checkpoint_set", currentMapName!.Contains("surf_") ? "loc" : "checkpoint"]);
                PlaySound(player, cpSoundError);
                return;
            }

            // Get the most recent checkpoint from the player's list
            PlayerCheckpoint lastCheckpoint = playerCheckpoints[slot][playerTimers[slot].CheckpointIndex];

            // Convert position and rotation strings to Vector_t and QAngle_t
            Vector_t position = Utils.ParseVector_t(lastCheckpoint.PositionString ?? "0 0 0");
            QAngle_t rotation = Utils.ParseQAngle_t(lastCheckpoint.RotationString ?? "0 0 0");
            Vector_t speed = Utils.ParseVector_t(lastCheckpoint.SpeedString ?? "0 0 0");

            // Teleport the player to the most recent checkpoint, including the saved rotation
            if (removeCpRestrictEnabled == true)
            {
                player.PlayerPawn.Value!.Teleport(position, rotation, speed);
            }
            else
            {
                player.PlayerPawn.Value!.Teleport(position, rotation, new Vector_t(0, 0, 0));
            }

            // Play a sound or provide feedback to the player
            PlaySound(player, tpSound);
            Utils.PrintToChat(player, Localizer["used_recent_checkpoint", (currentMapName!.Contains("surf_") ? "loc" : "checkpoint")]);
            Utils.LogDebug($"{player.PlayerName} css_tp to {position} {rotation} {speed}");
        }

        [ConsoleCommand("css_prevcp", "Tp to the previous checkpoint")]
        [ConsoleCommand("css_prevloc", "alias for !prevcp")]
        [CommandHelper(whoCanExecute: CommandUsage.CLIENT_ONLY)]
        public void TpPreviousCP(CCSPlayerController? player, CommandInfo command)
        {
            if (!IsAllowedPlayer(player) || cpEnabled == false)
                return;

            var slot = player!.Slot;
            var playerName = player.PlayerName;

            Utils.LogDebug($"{playerName} calling css_prevcp...");

            if (ReplayCheck(player))
                return;

            if (!CanCheckpoint(player))
                return;

            if (!playerCheckpoints.TryGetValue(slot, out List<PlayerCheckpoint>? checkpoints) || checkpoints.Count == 0)
            {
                Utils.PrintToChat(player, Localizer["no_checkpoint_set", (currentMapName!.Contains("surf_") ? "loc" : "checkpoint")]);
                return;
            }

            int index = playerTimers.TryGetValue(slot, out var timer) ? timer.CheckpointIndex : 0;

            if (checkpoints.Count == 1)
            {
                TpPlayerCP(player, command, slot);
            }
            else
            {
                // Calculate the index of the previous checkpoint, circling back if necessary
                index = (index - 1 + checkpoints.Count) % checkpoints.Count;

                PlayerCheckpoint previousCheckpoint = checkpoints[index];
                

                // Update the player's checkpoint index and timer ticks
                playerTimers[slot].CheckpointIndex = index;
                if(playerTimers[slot].currentStyle == 12)
                    playerTimers[slot].TimerTicks = playerTimers[slot].PrevTimerTicks[playerTimers[slot].CheckpointIndex];

                // Convert position and rotation strings to Vector_t and QAngle_t
                Vector_t position = Utils.ParseVector_t(previousCheckpoint.PositionString ?? "0 0 0");
                QAngle_t rotation = Utils.ParseQAngle_t(previousCheckpoint.RotationString ?? "0 0 0");
                Vector_t speed = Utils.ParseVector_t(previousCheckpoint.SpeedString ?? "0 0 0");

                // Teleport the player to the previous checkpoint, including the saved rotation
                player.PlayerPawn.Value!.Teleport(position, rotation, speed);

                // Play a sound or provide feedback to the player
                PlaySound(player, tpSound);
                Utils.PrintToChat(player, Localizer["used_previous_checkpoint", (currentMapName!.Contains("surf_") ? "loc" : "checkpoint")]);
                Utils.LogDebug($"{player.PlayerName} css_prevcp to {position} {rotation}");
            }
        }

        [ConsoleCommand("css_nextcp", "Tp to the next checkpoint")]
        [ConsoleCommand("css_nextloc", "alias for !nextcp")]
        [CommandHelper(whoCanExecute: CommandUsage.CLIENT_ONLY)]
        public void TpNextCP(CCSPlayerController? player, CommandInfo command)
        {
            if (!IsAllowedPlayer(player) || cpEnabled == false)
                return;

            var slot = player!.Slot;
            var playerName = player.PlayerName;

            Utils.LogDebug($"{playerName} calling css_nextcp...");

            if (ReplayCheck(player))
                return;

            if (!CanCheckpoint(player))
                return;

            if (!playerCheckpoints.TryGetValue(slot, out List<PlayerCheckpoint>? checkpoints) || checkpoints.Count == 0)
            {
                Utils.PrintToChat(player, Localizer["no_checkpoint_set", currentMapName!.Contains("surf_") ? "loc" : "checkpoint"]);
                return;
            }

            int index = playerTimers.TryGetValue(slot, out var timer) ? timer.CheckpointIndex : 0;

            if (checkpoints.Count == 1)
            {
                TpPlayerCP(player, command, slot);
            }
            else
            {
                // Calculate the index of the next checkpoint, circling back if necessary
                index = (index + 1) % checkpoints.Count;

                PlayerCheckpoint nextCheckpoint = checkpoints[index];

                // Update the player's checkpoint index and timer ticks
                playerTimers[slot].CheckpointIndex = index;
                if(playerTimers[slot].currentStyle == 12)
                    playerTimers[slot].TimerTicks = playerTimers[slot].PrevTimerTicks[playerTimers[slot].CheckpointIndex];

                // Convert position and rotation strings to Vector_t and QAngle_t
                Vector_t position = Utils.ParseVector_t(nextCheckpoint.PositionString ?? "0 0 0");
                QAngle_t rotation = Utils.ParseQAngle_t(nextCheckpoint.RotationString ?? "0 0 0");
                Vector_t speed = Utils.ParseVector_t(nextCheckpoint.SpeedString ?? "0 0 0");

                // Teleport the player to the next checkpoint, including the saved rotation
                player.PlayerPawn.Value!.Teleport(position, rotation, speed);

                // Play a sound or provide feedback to the player
                PlaySound(player, tpSound);
                Utils.PrintToChat(player, Localizer["used_checkpoint", currentMapName!.Contains("surf_") ? "loc" : "checkpoint"]);
                Utils.LogDebug($"{playerName} css_nextcp to {position} {rotation}");
            }
        }
    }
}