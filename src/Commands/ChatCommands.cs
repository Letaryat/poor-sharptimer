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
using System.Drawing;


namespace SharpTimer
{
    public partial class SharpTimer
    {
        public bool CommandCooldown(CCSPlayerController? player)
        {
            if (playerTimers[player!.Slot].TicksSinceLastCmd < cmdCooldown)
            {
                player.PrintToChat($" {Localizer["prefix"]} {Localizer["command_cooldown"]}");
                return true;
            }
            return false;
        }

        public bool IsTimerBlocked(CCSPlayerController? player)
        {
            if (!playerTimers[player!.Slot].IsTimerBlocked)
            {
                player.PrintToChat($" {Localizer["prefix"]} {Localizer["stop_using_timer"]}");
                return true;
            }
            return false;
        }

        public bool ReplayCheck(CCSPlayerController? player)
        {
            if (playerTimers[player!.Slot].IsReplaying)
            {
                player.PrintToChat($" {Localizer["prefix"]} {Localizer["end_your_replay"]}");
                return true;
            }
            return false;
        }

        public bool CanCheckpoint(CCSPlayerController? player)
        {
            if (cpOnlyWhenTimerStopped == true && playerTimers[player!.Slot].IsTimerBlocked == false)
            {
                player.PrintToChat($" {Localizer["prefix"]} {Localizer["cant_use_checkpoint", (currentMapName!.Contains("surf_") ? "loc" : "checkpoint")]}");
                if (playerTimers[player.Slot].SoundsEnabled != false) player.ExecuteClientCommand($"play {cpSoundAir}");
                return true;
            }
            return false;
        }

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

            QuietStopTimer(player);

            if (IsTimerBlocked(player))
                return;

            if (ReplayCheck(player))
                return;

            player.PrintToChat($" {Localizer["prefix"]} {Localizer["available_replay_cmds"]}");
            player.PrintToChat($" {Localizer["prefix"]} {Localizer["replaying_server_record"]}");

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

            QuietStopTimer(player);

            if (IsTimerBlocked(player))
                return;

            if (ReplayCheck(player))
                return;

            _ = Task.Run(async () => await ReplayHandler(player, playerSlot, "self", steamID, playerName, 0, playerTimers[playerSlot].currentStyle));
        }

        [ConsoleCommand("css_replaysr", "Replay server map record")]
        [CommandHelper(whoCanExecute: CommandUsage.CLIENT_ONLY)]
        public void ReplaySRCommand(CCSPlayerController? player, CommandInfo command)
        {
            if (!IsAllowedPlayer(player) || enableReplays == false) return;

            var playerSlot = player!.Slot;

            QuietStopTimer(player);

            if (IsTimerBlocked(player))
                return;

            if (ReplayCheck(player))
                return;

            _ = Task.Run(async () => await ReplayHandler(player, playerSlot, "1", "69", "unknown", 0, playerTimers[playerSlot].currentStyle));
        }

        [ConsoleCommand("css_replaytop", "Replay a top 10 server map record")]
        [CommandHelper(minArgs: 1, usage: "[1-10]", whoCanExecute: CommandUsage.CLIENT_ONLY)]
        public void ReplayTop10SRCommand(CCSPlayerController? player, CommandInfo command)
        {
            if (!IsAllowedPlayer(player) || enableReplays == false) return;

            var playerSlot = player!.Slot;

            QuietStopTimer(player);

            if (IsTimerBlocked(player))
                return;

            if (ReplayCheck(player))
                return;

            string arg = command.ArgByIndex(1);

            _ = Task.Run(async () => await ReplayHandler(player, playerSlot, arg, "69", "unknown", 0, playerTimers[playerSlot].currentStyle));
        }

        [ConsoleCommand("css_replayb", "Replay a top 10 server bonus record")]
        [ConsoleCommand("css_replaybonus", "Replay a top 10 server bonus record")]
        [CommandHelper(minArgs: 1, usage: "[1-10] [bonus stage]", whoCanExecute: CommandUsage.CLIENT_ONLY)]
        public void ReplayBonusCommand(CCSPlayerController? player, CommandInfo command)
        {
            if (!IsAllowedPlayer(player) || enableReplays == false) return;

            var playerSlot = player!.Slot;

            QuietStopTimer(player);

            if (IsTimerBlocked(player))
                return;

            if (ReplayCheck(player))
                return;

            string arg = command.ArgByIndex(1);
            string arg2 = command.ArgByIndex(2);

            _ = Task.Run(async () => await ReplayHandler(player, playerSlot, arg, "69", "unknown", Int16.Parse(arg2), playerTimers[playerSlot].currentStyle));
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

            QuietStopTimer(player);

            if (IsTimerBlocked(player))
                return;

            if (ReplayCheck(player))
                return;

            string arg = command.ArgByIndex(1);
            int bonusX = Int16.Parse(arg);

            _ = Task.Run(async () => await ReplayHandler(player, playerSlot, "self", steamID, playerName, bonusX, playerTimers[playerSlot].currentStyle));
        }

        public async Task ReplayHandler(CCSPlayerController player, int playerSlot, string arg = "1", string pbSteamID = "69", string playerName = "unknown", int bonusX = 0, int style = 0)
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
                    (srSteamID, srPlayerName, srTime) = await GetMapRecordSteamIDFromDatabase(bonusX, top10, style);
                }
                else
                {
                    (srSteamID, srPlayerName, srTime) = await GetMapRecordSteamID(bonusX, top10);
                }
            }


            if ((srSteamID == "null" || srPlayerName == "null" || srTime == "null") && !self)
            {
                Server.NextFrame(() => {
                    player.PrintToChat($" {Localizer["prefix"]} {Localizer["no_sr_replay"]}");
                    RespawnPlayer(player);
                }); 
                return;
            }

            await ReadReplayFromJson(player, !self ? srSteamID : pbSteamID, playerSlot, bonusX, style);

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
                if (!self)
                    Server.NextFrame(() => player.PrintToChat($" {Localizer["prefix"]} {Localizer["replaying_server_top", top10]}"));
                else
                    Server.NextFrame(() => player.PrintToChat($" {Localizer["prefix"]} {Localizer["replaying_pb"]}"));
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
                player.PrintToChat($" {Localizer["prefix"]} {Localizer["no_replay"]}");
                return;
            }

            if (playerTimers[playerSlot].IsReplaying)
            {
                player.PrintToChat($" {Localizer["prefix"]} {Localizer["ending_replay"]}");
                playerTimers[playerSlot].IsReplaying = false;
                if (player.PlayerPawn.Value!.MoveType != MoveType_t.MOVETYPE_WALK || player.PlayerPawn.Value.ActualMoveType == MoveType_t.MOVETYPE_WALK) SetMoveType(player, MoveType_t.MOVETYPE_WALK);
                playerReplays.Remove(playerSlot);
                playerReplays[playerSlot] = new PlayerReplays();
                playerTimers[playerSlot].IsTimerBlocked = false;
                playerTimers[playerSlot].IsTimerRunning = false;
                playerTimers[playerSlot].TimerTicks = 0;
                playerTimers[playerSlot].IsBonusTimerRunning = false;
                playerTimers[playerSlot].BonusTimerTicks = 0;
                playerReplays[playerSlot].CurrentPlaybackFrame = 0;
                if (stageTriggers.Count != 0) playerTimers[playerSlot].StageTimes!.Clear(); //remove previous stage times if the map has stages
                if (stageTriggers.Count != 0) playerTimers[playerSlot].StageVelos!.Clear(); //remove previous stage times if the map has stages
                RespawnPlayerCommand(player, command);
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

            if (CommandCooldown(player))
                return;

            playerTimers[playerSlot].TicksSinceLastCmd = 0;

            playerTimers[playerSlot].HideTimerHud = !playerTimers[playerSlot].HideTimerHud;

            if (playerTimers[playerSlot].HideTimerHud)
                player.PrintToChat($" {Localizer["prefix"]} {Localizer["hud_hidden"]}");
            else
                player.PrintToChat($" {Localizer["prefix"]} {Localizer["hud_shown"]}");

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

            if (CommandCooldown(player))

            playerTimers[playerSlot].TicksSinceLastCmd = 0;

            playerTimers[playerSlot].HideKeys = playerTimers[playerSlot].HideKeys ? false : true;

            if (playerTimers[playerSlot].HideKeys)
                player.PrintToChat($" {Localizer["prefix"]} {Localizer["keys_hidden"]}");
            else
                player.PrintToChat($" {Localizer["prefix"]} {Localizer["keys_shown"]}");

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

            if (CommandCooldown(player))
                return;

            playerTimers[playerSlot].TicksSinceLastCmd = 0;

            playerTimers[playerSlot].SoundsEnabled = playerTimers[playerSlot].SoundsEnabled ? false : true;

            if (playerTimers[playerSlot].SoundsEnabled)
                player.PrintToChat($" {Localizer["prefix"]} {Localizer["sounds_on"]}");
            else
                player.PrintToChat($" {Localizer["prefix"]} {Localizer["sounds_off"]}");

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

            if (CommandCooldown(player))
                return;

            playerTimers[playerSlot].TicksSinceLastCmd = 0;

            playerTimers[playerSlot].HideJumpStats = playerTimers[playerSlot].HideJumpStats ? false : true;

            if (playerTimers[playerSlot].HideJumpStats)
                player.PrintToChat($" {Localizer["prefix"]} {Localizer["jumpstats_hidden"]}");
            else
                player.PrintToChat($" {Localizer["prefix"]} {Localizer["jumpstats_shown"]}");

            SharpTimerDebug($"Hide Jump Stats set to: {playerTimers[playerSlot].HideJumpStats} for {playerName}");

            if (useMySQL || usePostgres)
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

            if (CommandCooldown(player))
                return;

            playerTimers[player.Slot].TicksSinceLastCmd = 0;

            var mapName = command.ArgByIndex(1);

            _ = Task.Run(async () => await PrintTopRecordsHandler(player, playerName, 0, string.IsNullOrEmpty(mapName) ? "" : mapName, playerTimers[player.Slot].currentStyle));
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

            if (CommandCooldown(player))
                return;

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

            if (CommandCooldown(player))
                return;

            playerTimers[player.Slot].TicksSinceLastCmd = 0;

            if (!int.TryParse(command.ArgString, out int bonusX))
            {
                SharpTimerDebug("css_topbonus conversion failed. The input string is not a valid integer.");

                player.PrintToChat($" {Localizer["prefix"]} {Localizer["invalid_bonus_stage"]}");
                return;
            }

            _ = Task.Run(async () => await PrintTopRecordsHandler(player, playerName, bonusX));
        }

        public async Task PrintTopRecordsHandler(CCSPlayerController? player, string playerName, int bonusX = 0, string mapName = "", int style = 0)
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
                sortedRecords = await GetSortedRecordsFromDatabase(10, bonusX, mapName, style);
            }
            else
            {
                sortedRecords = await GetSortedRecords(bonusX, mapName);
            }

            if (sortedRecords.Count == 0)
            {
                Server.NextFrame(() =>
                {
                    if (IsAllowedPlayer(player))
                    {
                        if (bonusX != 0)
                            player!.PrintToChat($" {Localizer["prefix"]} {Localizer["no_records_available_bonus", bonusX, currentMapNamee]}");
                        else
                            player!.PrintToChat($" {Localizer["prefix"]} {Localizer["no_records_available", currentMapNamee]}");
                    }
                });
                return;
            }

            List<string> printStatements;

            if (bonusX != 0)
                printStatements = [$" {Localizer["prefix"]} {Localizer["top10_records_bonus", GetNamedStyle(style), bonusX, currentMapNamee]}"];
            else
                printStatements = [$" {Localizer["prefix"]} {Localizer["top10_records", GetNamedStyle(style), currentMapNamee]}"];

            int rank = 1;

            foreach (var kvp in sortedRecords.Take(10))
            {
                string _playerName = kvp.Value.PlayerName!;
                int timerTicks = kvp.Value.TimerTicks;

                bool showReplays = false;
                if (enableReplays == true) showReplays = await CheckSRReplay(kvp.Key, bonusX);

                string replayIndicator = enableReplays ? (showReplays ? $"{ChatColors.Red}◉" : "") : "";

                printStatements.Add($" {Localizer["prefix"]} {Localizer["records_map", rank, _playerName, replayIndicator, FormatTime(timerTicks)]}");
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

            if (CommandCooldown(player))
                return;

            _ = Task.Run(async () => await RankCommandHandler(player, steamID, playerSlot, playerName));
        }

        public async Task RankCommandHandler(CCSPlayerController? player, string steamId, int playerSlot, string playerName, bool sendRankToHUD = false, int style = 0)
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

                ranking = useGlobalRanks ? await GetPlayerServerPlacement(player, steamId, playerName) : await GetPlayerMapPlacementWithTotal(player, steamId, playerName, false, false, 0, style);
                rankIcon = useGlobalRanks ? await GetPlayerServerPlacement(player, steamId, playerName, true) : await GetPlayerMapPlacementWithTotal(player, steamId, playerName, true, false, 0, style);
                mapPlacement = await GetPlayerMapPlacementWithTotal(player, steamId, playerName, false, true, style);

                if (useGlobalRanks)
                {
                    serverPoints = await GetPlayerServerPlacement(player, steamId, playerName, false, false, true);
                    serverPlacement = await GetPlayerServerPlacement(player, steamId, playerName, false, true, false);
                }

                int pbTicks = (useMySQL || usePostgres) ? await GetPreviousPlayerRecordFromDatabase(player, steamId, currentMapName!, playerName, 0, style) : await GetPreviousPlayerRecord(player, steamId, 0, style);

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
                        string rankMessage = $" {Localizer["prefix"]} {Localizer["current_rank", GetRankColorForChat(player!), ranking]}";
                        if (useGlobalRanks)
                        {
                            rankMessage += $" {Localizer["current_rank_points", serverPoints, serverPlacement]}";
                        }
                        player!.PrintToChat(rankMessage);
                        if (pbTicks != 0)
                        {
                            player.PrintToChat($"{Localizer["prefix"]} {Localizer["current_pb", currentMapName!, FormatTime(pbTicks), mapPlacement]}");
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

            if (CommandCooldown(player))
                return;

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
                player!.PrintToChat($" {Localizer["prefix"]} {Localizer["current_sr", currentMapName!]}");
            });

            foreach (var kvp in sortedRecords.Take(1))
            {
                string playerName = kvp.Value.PlayerName!;
                int timerTicks = kvp.Value.TimerTicks;
                Server.NextFrame(() =>
                {
                    if (!IsAllowedPlayer(player)) return;
                    player!.PrintToChat($" {Localizer["prefix"]} {Localizer["current_sr_player", playerName!, FormatTime(timerTicks)]}");
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

                if (CommandCooldown(player))
                    return;

                if (ReplayCheck(player))
                    return;

                playerTimers[player.Slot].TicksSinceLastCmd = 0;

                //defaults to !b 1 without any args
                if(command.ArgString == null || command.ArgString == "")
                {
                    if (bonusRespawnPoses[1] != null)
                    {
                        if (bonusRespawnAngs.TryGetValue(1, out QAngle? bonusAng) && bonusAng != null)
                        {
                            player.PlayerPawn.Value!.Teleport(bonusRespawnPoses[1]!, bonusRespawnAngs[1]!, new Vector(0, 0, 0));
                        }
                        else
                        {
                            player.PlayerPawn.Value!.Teleport(bonusRespawnPoses[1]!, new QAngle(player.PlayerPawn.Value.EyeAngles.X, player.PlayerPawn.Value.EyeAngles.Y, player.PlayerPawn.Value.EyeAngles.Z) ?? new QAngle(0, 0, 0), new Vector(0, 0, 0));
                        }
                        SharpTimerDebug($"{player.PlayerName} css_rb {1} to {bonusRespawnPoses[1]}");
                    }
                    else
                    {
                        player.PrintToChat($" {Localizer["prefix"]} {Localizer["no_respawnpos_bonus_index", 1]}");
                    }
                    Server.NextFrame(() =>
                    {
                        playerTimers[player.Slot].IsTimerRunning = false;
                        playerTimers[player.Slot].TimerTicks = 0;
                        playerTimers[player.Slot].IsBonusTimerRunning = false;
                        playerTimers[player.Slot].BonusTimerTicks = 0;
                    });
                    return;
                }

                if (!int.TryParse(command.ArgString, out int bonusX))
                {
                    SharpTimerDebug("css_rb conversion failed. The input string is not a valid integer.");
                    player.PrintToChat($" {Localizer["prefix"]} {Localizer["no_respawnpos_bonus_rb"]}");
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
                    player.PrintToChat($" {Localizer["prefix"]} {Localizer["no_respawnpos_bonus"]}");
                }

                Server.NextFrame(() =>
                {
                    playerTimers[player.Slot].IsTimerRunning = false;
                    playerTimers[player.Slot].TimerTicks = 0;
                    playerTimers[player.Slot].IsBonusTimerRunning = false;
                    playerTimers[player.Slot].BonusTimerTicks = 0;
                    playerTimers[player.Slot].IsTimerBlocked = false;
                });

                if (playerTimers[player.Slot].SoundsEnabled != false) player.ExecuteClientCommand($"play {respawnSound}");
            }
            catch (Exception ex)
            {
                SharpTimerError($"Exception in RespawnBonusPlayer: {ex.Message}");
            }
        }

        [ConsoleCommand("css_startpos", "Saves a custom respawn point within the start trigger")]
        [ConsoleCommand("css_ssp", "Saves a custom respawn point within the start trigger")]
        [ConsoleCommand("css_setresp", "Saves a custom respawn point within the start trigger")]
        [CommandHelper(whoCanExecute: CommandUsage.CLIENT_ONLY)]
        public void SetRespawnCommand(CCSPlayerController? player, CommandInfo command)
        {
            if (!IsAllowedPlayer(player) || respawnEnabled == false) return;

            SharpTimerDebug($"{player!.PlayerName} calling css_rank...");

            if (CommandCooldown(player))
                return;

            if (ReplayCheck(player))
                return;

            if (useTriggers == false)
            {
                player.PrintToChat($" {Localizer["prefix"]} {Localizer["map_using_manual_zones"]}");
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
                    player.PrintToChat($" {Localizer["prefix"]} {Localizer["saved_custom_respawnpos"]}");
                }
                else
                {
                    player.PrintToChat($" {Localizer["prefix"]} {Localizer["not_inside_startzone"]}");
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
                    player.PrintToChat($" {Localizer["prefix"]} {Localizer["saved_custom_respawnpos"]}");
                }
                else
                {
                    player.PrintToChat($" {Localizer["prefix"]} {Localizer["not_inside_startzone"]}");
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

                if (CommandCooldown(player))
                    return;

                if (ReplayCheck(player))
                    return;

                playerTimers[player.Slot].TicksSinceLastCmd = 0;

                if (IsTimerBlocked(player))
                    return;

                if (!int.TryParse(command.ArgString, out int stageX))
                {
                    SharpTimerDebug("css_stage conversion failed. The input string is not a valid integer.");
                    player.PrintToChat($" {Localizer["prefix"]} {Localizer["stages_enter_valid"]}");
                    return;
                }

                if (useStageTriggers == false)
                {
                    SharpTimerDebug("css_stage failed useStages is false.");
                    player.PrintToChat($" {Localizer["prefix"]} {Localizer["stages_unavalible"]}");
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
                    player.PrintToChat($" {Localizer["prefix"]} {Localizer["stages_unavalible_respawnpos", stageX]}");
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

            if (CommandCooldown(player))
                return;

            if (playerTimers[player.Slot].IsReplaying)
            {
                player.PrintToChat($" {Localizer["prefix"]} {Localizer["ending_replay"]}");
                playerTimers[player.Slot].IsReplaying = false;
                if (player.PlayerPawn.Value!.MoveType != MoveType_t.MOVETYPE_WALK || player.PlayerPawn.Value.ActualMoveType == MoveType_t.MOVETYPE_WALK) SetMoveType(player, MoveType_t.MOVETYPE_WALK);
                playerReplays.Remove(player.Slot);
                playerReplays[player.Slot] = new PlayerReplays();
                playerTimers[player.Slot].IsTimerBlocked = false;
                playerTimers[player.Slot].IsTimerRunning = false;
                playerTimers[player.Slot].TimerTicks = 0;
                playerTimers[player.Slot].IsBonusTimerRunning = false;
                playerTimers[player.Slot].BonusTimerTicks = 0;
                playerReplays[player.Slot].CurrentPlaybackFrame = 0;
                if (stageTriggers.Count != 0) playerTimers[player.Slot].StageTimes!.Clear(); //remove previous stage times if the map has stages
                if (stageTriggers.Count != 0) playerTimers[player.Slot].StageVelos!.Clear(); //remove previous stage times if the map has stages
                RespawnPlayer(player);
            }
            else
            {
                playerTimers[player.Slot].TicksSinceLastCmd = 0;
                RespawnPlayer(player);
            }
        }

        [ConsoleCommand("css_end", "Teleports you to end")]
        [CommandHelper(whoCanExecute: CommandUsage.CLIENT_ONLY)]
        public void EndPlayerCommand(CCSPlayerController? player, CommandInfo command)
        {
            if (!IsAllowedPlayer(player) || respawnEndEnabled == false) return;
            SharpTimerDebug($"{player!.PlayerName} calling css_end...");

            if (CommandCooldown(player))
                return;

            if (ReplayCheck(player))
                return;

            playerTimers[player.Slot].TicksSinceLastCmd = 0;
            playerTimers[player.Slot].IsTimerRunning = false;
            playerTimers[player.Slot].TimerTicks = 0;
            playerTimers[player.Slot].IsBonusTimerRunning = false;
            playerTimers[player.Slot].BonusTimerTicks = 0;

            Server.NextFrame(() => RespawnPlayer(player, true));
        }

        [ConsoleCommand("css_noclip", "Noclip")]
        [ConsoleCommand("css_nc", "Noclip")]
        [CommandHelper(whoCanExecute: CommandUsage.CLIENT_ONLY)]
        public void NoclipCommand(CCSPlayerController? player, CommandInfo command)
        {
            if (enableNoclip == false) return;
            SharpTimerDebug($"{player!.PlayerName} calling css_noclip...");

            if (ReplayCheck(player))
                return;

            playerTimers[player.Slot].TicksSinceLastCmd = 0;
            playerTimers[player.Slot].IsTimerRunning = false;
            playerTimers[player.Slot].TimerTicks = 0;
            playerTimers[player.Slot].IsBonusTimerRunning = false;
            playerTimers[player.Slot].BonusTimerTicks = 0;

            if (player!.Pawn.Value!.MoveType == MoveType_t.MOVETYPE_NOCLIP)
		    {
			    player!.Pawn.Value!.MoveType = MoveType_t.MOVETYPE_WALK;
			    Schema.SetSchemaValue(player!.Pawn.Value!.Handle, "CBaseEntity", "m_nActualMoveType", 2); // walk
			    Utilities.SetStateChanged(player!.Pawn.Value!, "CBaseEntity", "m_MoveType");
                playerTimers[player.Slot].IsNoclip = false;
		    }
		    else
		    {
			    player!.Pawn.Value!.MoveType = MoveType_t.MOVETYPE_NOCLIP;
			    Schema.SetSchemaValue(player!.Pawn.Value!.Handle, "CBaseEntity", "m_nActualMoveType", 8); // noclip
			    Utilities.SetStateChanged(player!.Pawn.Value!, "CBaseEntity", "m_MoveType");
                playerTimers[player.Slot].IsNoclip = true;
		    }   
        }


        [ConsoleCommand("css_styles", "Styles command")]
        [ConsoleCommand("css_style", "Styles command")]
        [CommandHelper(whoCanExecute: CommandUsage.CLIENT_ONLY)]
        public void StyleCommand(CCSPlayerController? player, CommandInfo command)
        {
            if (!IsAllowedPlayer(player)) return;

            if (ReplayCheck(player))
                return;

            if (!usePostgres && !useMySQL)
            {
                player!.PrintToChat($" {Localizer["prefix"]} {Localizer["styles_not_supported"]}");
                return;
            }
            if(!enableStyles)
            {
                player!.PrintToChat($" {Localizer["prefix"]} {Localizer["styles_disabled"]}");
                return;
            }

            SharpTimerDebug($"{player!.PlayerName} calling css_style...");

            playerTimers[player.Slot].TicksSinceLastCmd = 0;
            playerTimers[player.Slot].IsTimerRunning = false;
            playerTimers[player.Slot].TimerTicks = 0;
            playerTimers[player.Slot].IsBonusTimerRunning = false;
            playerTimers[player.Slot].BonusTimerTicks = 0;
            
            var desiredStyle = command.GetArg(1);

            if (command.ArgByIndex(1) == "")
            {
                for (int i = 0; i < 9; i++) //runs 9 times for the 9 styles (i=0-8)
                {
                    player.PrintToChat($" {Localizer["prefix"]} {Localizer["styles_list", i, GetNamedStyle(i)]}");
                }
                player.PrintToChat($" {Localizer["prefix"]} {Localizer["style_example"]}");
                return;
            }

            if (Int32.TryParse(command.GetArg(1), out var desiredStyleInt)) 
            {
                switch(desiredStyleInt)
                {
                    case 0:
                        SetNormalStyle(player);
                        player.PrintToChat($" {Localizer["prefix"]} {Localizer["style_set", GetNamedStyle(desiredStyleInt)]}");
                        break;
                    case 1:
                        SetNormalStyle(player);
                        SetLowGravity(player);
                        player.PrintToChat($" {Localizer["prefix"]} {Localizer["style_set", GetNamedStyle(desiredStyleInt)]}");
                        break;
                    case 2:
                        SetNormalStyle(player);
                        SetSideways(player);
                        player.PrintToChat($" {Localizer["prefix"]} {Localizer["style_set", GetNamedStyle(desiredStyleInt)]}");
                        break;
                    case 3:
                        SetNormalStyle(player);
                        SetOnlyW(player);
                        player.PrintToChat($" {Localizer["prefix"]} {Localizer["style_set", GetNamedStyle(desiredStyleInt)]}");
                        break;
                    case 4:
                        SetNormalStyle(player);
                        Set400Vel(player);
                        player.PrintToChat($" {Localizer["prefix"]} {Localizer["style_set", GetNamedStyle(desiredStyleInt)]}");
                        break;
                    case 5:
                        SetNormalStyle(player);
                        SetHighGravity(player);
                        player.PrintToChat($" {Localizer["prefix"]} {Localizer["style_set", GetNamedStyle(desiredStyleInt)]}");
                        break;
                    case 6:
                        SetNormalStyle(player);
                        SetOnlyA(player);
                        player.PrintToChat($" {Localizer["prefix"]} {Localizer["style_set", GetNamedStyle(desiredStyleInt)]}");
                        break;
                    case 7:
                        SetNormalStyle(player);
                        SetOnlyD(player);
                        player.PrintToChat($" {Localizer["prefix"]} {Localizer["style_set", GetNamedStyle(desiredStyleInt)]}");
                        break;
                    case 8:
                        SetNormalStyle(player);
                        SetOnlyS(player);
                        player.PrintToChat($" {Localizer["prefix"]} {Localizer["style_set", GetNamedStyle(desiredStyleInt)]}");
                        break;
                    default:
                        player.PrintToChat($" {Localizer["prefix"]} {Localizer["style_not_found", desiredStyleInt]}");
                        break;
                }
            }
            else
            {
                switch(desiredStyle.ToLower())
                {
                    case "default":
                        SetNormalStyle(player);
                        player.PrintToChat($" {Localizer["prefix"]} {Localizer["style_set", GetNamedStyle(0)]}");
                        break;
                    case "normal":
                        SetNormalStyle(player);
                        player.PrintToChat($" {Localizer["prefix"]} {Localizer["style_set", GetNamedStyle(0)]}");
                        break;
                    case "lowgravity":
                        SetNormalStyle(player);
                        SetLowGravity(player);
                        player.PrintToChat($" {Localizer["prefix"]} {Localizer["style_set", GetNamedStyle(1)]}");
                        break;
                    case "lowgrav":
                        SetNormalStyle(player);
                        SetLowGravity(player);
                        player.PrintToChat($" {Localizer["prefix"]} {Localizer["style_set", GetNamedStyle(1)]}");
                        break;
                    case "sideways":
                        SetNormalStyle(player);
                        SetSideways(player);
                        player.PrintToChat($" {Localizer["prefix"]} {Localizer["style_set", GetNamedStyle(2)]}");
                        break;
                    case "sw":
                        SetNormalStyle(player);
                        SetSideways(player);
                        player.PrintToChat($" {Localizer["prefix"]} {Localizer["style_set", GetNamedStyle(2)]}");
                        break;
                    case "wonly":
                        SetNormalStyle(player);
                        SetOnlyW(player);
                        player.PrintToChat($" {Localizer["prefix"]} {Localizer["style_set", GetNamedStyle(3)]}");
                        break;
                    case "onlyw":
                        SetNormalStyle(player);
                        SetOnlyW(player);
                        player.PrintToChat($" {Localizer["prefix"]} {Localizer["style_set", GetNamedStyle(3)]}");
                        break;
                    case "400vel":
                        SetNormalStyle(player);
                        Set400Vel(player);
                        player.PrintToChat($" {Localizer["prefix"]} {Localizer["style_set", GetNamedStyle(4)]}");
                        break;
                    case "highgravity":
                        SetNormalStyle(player);
                        SetHighGravity(player);
                        player.PrintToChat($" {Localizer["prefix"]} {Localizer["style_set", GetNamedStyle(5)]}");
                        break;
                    case "highgrav":
                        SetNormalStyle(player);
                        SetHighGravity(player);
                        player.PrintToChat($" {Localizer["prefix"]} {Localizer["style_set", GetNamedStyle(5)]}");
                        break;
                    case "aonly":
                        SetNormalStyle(player);
                        SetOnlyA(player);
                        player.PrintToChat($" {Localizer["prefix"]} {Localizer["style_set", GetNamedStyle(6)]}");
                        break;
                    case "onlya":
                        SetNormalStyle(player);
                        SetOnlyA(player);
                        player.PrintToChat($" {Localizer["prefix"]} {Localizer["style_set", GetNamedStyle(6)]}");
                        break;
                    case "donly":
                        SetNormalStyle(player);
                        SetOnlyD(player);
                        player.PrintToChat($" {Localizer["prefix"]} {Localizer["style_set", GetNamedStyle(7)]}");
                        break;
                    case "onlyd":
                        SetNormalStyle(player);
                        SetOnlyD(player);
                        player.PrintToChat($" {Localizer["prefix"]} {Localizer["style_set", GetNamedStyle(7)]}");
                        break;
                    case "sonly":
                        SetNormalStyle(player);
                        SetOnlyS(player);
                        player.PrintToChat($" {Localizer["prefix"]} {Localizer["style_set", GetNamedStyle(8)]}");
                        break;
                    case "onlys":
                        SetNormalStyle(player);
                        SetOnlyS(player);
                        player.PrintToChat($" {Localizer["prefix"]} {Localizer["style_set", GetNamedStyle(8)]}");
                        break;
                    default:
                        player.PrintToChat($" {Localizer["prefix"]} {Localizer["style_not_found", desiredStyle.ToLower()]}");
                        break;
                }
            }
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
                            player.PrintToChat($" {Localizer["prefix"]} {Localizer["no_respawnpos"]}");
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
                        player.PrintToChat($" {Localizer["prefix"]} {Localizer["no_endpos"]}");
                    }
                }

                Server.NextFrame(() =>
                {
                    playerTimers[player.Slot].IsTimerRunning = false;
                    playerTimers[player.Slot].TimerTicks = 0;
                    playerTimers[player.Slot].IsBonusTimerRunning = false;
                    playerTimers[player.Slot].BonusTimerTicks = 0;
                    playerTimers[player.Slot].IsTimerBlocked = false;
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

            if (CommandCooldown(player))
                return;

            SharpTimerDebug($"{player!.PlayerName} calling css_rs...");

            if (stageTriggerCount == 0)
            {
                player.PrintToChat($" {Localizer["prefix"]} {Localizer["map_no_stages"]}");
                return;
            }

            if (!playerTimers.TryGetValue(player.Slot, out PlayerTimerInfo? playerTimer) || playerTimer.CurrentMapStage == 0)
            {
                player.PrintToChat($" {Localizer["prefix"]} {Localizer["error_occured"]}");
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
                    player.PrintToChat($" {Localizer["prefix"]} {Localizer["stages_unavalible_respawnpos"]}");
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

            if (CommandCooldown(player))
                return;

            if (ReplayCheck(player))
                return;

            playerTimers[player.Slot].TicksSinceLastCmd = 0;

            // Remove checkpoints for the current player
            playerCheckpoints.Remove(player.Slot);

            playerTimers[player.Slot].IsTimerBlocked = playerTimers[player.Slot].IsTimerBlocked ? false : true;
            playerTimers[player.Slot].IsRecordingReplay = false;


            if (playerTimers[player.Slot].IsTimerBlocked)
                player.PrintToChat($" {Localizer["prefix"]} {Localizer["timer_disabled"]}");
            else
                player.PrintToChat($" {Localizer["prefix"]} {Localizer["timer_enabled"]}");

            playerTimers[player.Slot].IsTimerRunning = false;
            playerTimers[player.Slot].TimerTicks = 0;
            playerTimers[player.Slot].IsBonusTimerRunning = false;
            playerTimers[player.Slot].BonusTimerTicks = 0;

            if (stageTriggers.Count != 0) playerTimers[player.Slot].StageTimes!.Clear(); //remove previous stage times if the map has stages
            if (stageTriggers.Count != 0) playerTimers[player.Slot].StageVelos!.Clear(); //remove previous stage times if the map has stages
            if (playerTimers[player.Slot].SoundsEnabled != false) player.ExecuteClientCommand($"play {beepSound}");
            SharpTimerDebug($"{player.PlayerName} css_timer to {playerTimers[player.Slot].IsTimerBlocked}");
        }

        public void QuietStopTimer(CCSPlayerController? player)
        {
            playerTimers[player!.Slot].TicksSinceLastCmd = 0;

            // Remove checkpoints for the current player
            playerCheckpoints.Remove(player.Slot);

            playerTimers[player.Slot].IsTimerBlocked = true;
            playerTimers[player.Slot].IsRecordingReplay = false;
            playerTimers[player.Slot].IsTimerRunning = false;
            playerTimers[player.Slot].TimerTicks = 0;
            playerTimers[player.Slot].IsBonusTimerRunning = false;
            playerTimers[player.Slot].BonusTimerTicks = 0;

            if (stageTriggers.Count != 0) playerTimers[player.Slot].StageTimes!.Clear(); //remove previous stage times if the map has stages
            if (stageTriggers.Count != 0) playerTimers[player.Slot].StageVelos!.Clear(); //remove previous stage times if the map has stages
            if (playerTimers[player.Slot].SoundsEnabled != false) player.ExecuteClientCommand($"play {beepSound}");
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

            if (CommandCooldown(player))
                return;

            playerTimers[player!.Slot].TicksSinceLastCmd = 0;

            player.PrintToChat($" {Localizer["prefix"]} {Localizer["info_version", ModuleVersion]}");
            player.PrintToChat($" {Localizer["prefix"]} {Localizer["info_os", RuntimeInformation.OSDescription]}");
            player.PrintToChat($" {Localizer["prefix"]} {Localizer["info_runtime", RuntimeInformation.RuntimeIdentifier]}");
        }

        [ConsoleCommand("css_goto", "Teleports you to a player")]
        [CommandHelper(minArgs: 1, usage: "[name]", whoCanExecute: CommandUsage.CLIENT_ONLY)]
        public void GoToPlayer(CCSPlayerController? player, CommandInfo command)
        {
            if (!IsAllowedPlayer(player) || goToEnabled == false) return;
            SharpTimerDebug($"{player!.PlayerName} calling css_goto...");

            if (CommandCooldown(player))
                return;

            if (ReplayCheck(player))
                return;

            if (IsTimerBlocked(player))
                return;

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
                player.PrintToChat($" {Localizer["prefix"]} {Localizer["goto_player_not_found"]}");
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
                player.PrintToChat($" {Localizer["prefix"]} {Localizer["goto_player", foundPlayer.PlayerName]}");

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
                player.PrintToChat($" {Localizer["prefix"]} {Localizer["goto_player_not_found"]}");
            }
        }

        [ConsoleCommand("css_cp", "Sets a checkpoint")]
        [ConsoleCommand("css_saveloc", "alias for !cp")]
        [CommandHelper(whoCanExecute: CommandUsage.CLIENT_ONLY)]
        public void SetPlayerCPCommand(CCSPlayerController? player, CommandInfo command)
        {
            if (!IsAllowedPlayer(player) || cpEnabled == false) return;
            SharpTimerDebug($"{player!.PlayerName} calling css_cp...");

            if (CommandCooldown(player))
                return;

            if (ReplayCheck(player))
                return;

            SetPlayerCP(player, command);
        }

        public void SetPlayerCP(CCSPlayerController? player, CommandInfo command)
        {
            if (((PlayerFlags)player!.Pawn.Value!.Flags & PlayerFlags.FL_ONGROUND) != PlayerFlags.FL_ONGROUND && removeCpRestrictEnabled == false)
            {
                player.PrintToChat($" {Localizer["prefix"]} {Localizer["cant_use_checkpoint_in_air", (currentMapName!.Contains("surf_") ? "loc" : "checkpoint")]}");
                if (playerTimers[player.Slot].SoundsEnabled != false) player.ExecuteClientCommand($"play {cpSoundAir}");
                return;
            }

            if (CanCheckpoint(player))
                return;

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
            player.PrintToChat($" {Localizer["prefix"]} {Localizer["checkpoint_set", (currentMapName!.Contains("surf_") ? "loc" : "checkpoint"), checkpointCount]}");
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

            if (CommandCooldown(player))
                return;

            if (ReplayCheck(player))
                return;

            TpPlayerCP(player, command);
        }

        public void TpPlayerCP(CCSPlayerController? player, CommandInfo command)
        {
            if (ReplayCheck(player))
                return;

            if (CanCheckpoint(player))
                return;

            playerTimers[player!.Slot].TicksSinceLastCmd = 0;

            // Check if the player has any checkpoints
            if (!playerCheckpoints.ContainsKey(player.Slot) || playerCheckpoints[player.Slot].Count == 0)
            {
                player.PrintToChat($" {Localizer["prefix"]} {Localizer["no_checkpoint_set", (currentMapName!.Contains("surf_") ? "loc" : "checkpoint")]}");
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
            player.PrintToChat($" {Localizer["prefix"]} {Localizer["used_recent_checkpoint", (currentMapName!.Contains("surf_") ? "loc" : "checkpoint")]}");
            SharpTimerDebug($"{player.PlayerName} css_tp to {position} {rotation} {speed}");
        }

        [ConsoleCommand("css_prevcp", "Tp to the previous checkpoint")]
        [ConsoleCommand("css_prevloc", "alias for !prevcp")]
        [CommandHelper(whoCanExecute: CommandUsage.CLIENT_ONLY)]
        public void TpPreviousCP(CCSPlayerController? player, CommandInfo command)
        {
            if (!IsAllowedPlayer(player) || cpEnabled == false) return;
            SharpTimerDebug($"{player!.PlayerName} calling css_prevcp...");

            if (CommandCooldown(player))
                return;

            if (ReplayCheck(player))
                return;

            if (CanCheckpoint(player))
                return;

            playerTimers[player.Slot].TicksSinceLastCmd = 0;

            if (!playerCheckpoints.TryGetValue(player.Slot, out List<PlayerCheckpoint>? checkpoints) || checkpoints.Count == 0)
            {
                player.PrintToChat($" {Localizer["prefix"]} {Localizer["no_checkpoint_set", (currentMapName!.Contains("surf_") ? "loc" : "checkpoint")]}");
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
                Vector speed = ParseVector(previousCheckpoint.SpeedString ?? "0 0 0");

                // Teleport the player to the previous checkpoint, including the saved rotation
                player.PlayerPawn.Value!.Teleport(position, rotation, speed);
                // Play a sound or provide feedback to the player
                if (playerTimers[player.Slot].SoundsEnabled != false) player.ExecuteClientCommand($"play {tpSound}");
                player.PrintToChat($" {Localizer["prefix"]} {Localizer["used_previous_checkpoint", (currentMapName!.Contains("surf_") ? "loc" : "checkpoint")]}");
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

            if (CommandCooldown(player))
                return;

            if (ReplayCheck(player))
                return;

            if (CanCheckpoint(player))
                return;

            playerTimers[player.Slot].TicksSinceLastCmd = 0;

            if (!playerCheckpoints.TryGetValue(player.Slot, out List<PlayerCheckpoint>? checkpoints) || checkpoints.Count == 0)
            {
                player!.PrintToChat($" {Localizer["prefix"]} {Localizer["no_checkpoint_set", (currentMapName!.Contains("surf_") ? "loc" : "checkpoint")]}");
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
                Vector speed = ParseVector(nextCheckpoint.SpeedString ?? "0 0 0");

                // Teleport the player to the next checkpoint, including the saved rotation
                player.PlayerPawn.Value!.Teleport(position, rotation, speed);

                // Play a sound or provide feedback to the player
                if (playerTimers[player.Slot].SoundsEnabled != false) player.ExecuteClientCommand($"play {tpSound}");
                player!.PrintToChat($" {Localizer["prefix"]} {Localizer["used_checkpoint", (currentMapName!.Contains("surf_") ? "loc" : "checkpoint")]}");
                SharpTimerDebug($"{player.PlayerName} css_nextcp to {position} {rotation}");
            }
        }
    }
}
