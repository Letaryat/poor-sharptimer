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
using CounterStrikeSharp.API.Modules.Entities.Constants;
using CounterStrikeSharp.API.Modules.Utils;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;

namespace SharpTimer
{
    public partial class SharpTimer
    {
        public void PrintAllEnabledCommands(CCSPlayerController player)
        {
            SharpTimerDebug($"Printing Commands for {player.PlayerName}");
            player.PrintToChat($"{Localizer["prefix"]} {Localizer["Check_console"]}");

            if (respawnEnabled) player.PrintToConsole($"{Localizer["console_r"]}");
            if (respawnEnabled && bonusRespawnPoses.Count != 0) player.PrintToConsole($"{Localizer["console_rb"]}");
            if (respawnEnabled && bonusRespawnPoses.Count != 0) player.PrintToConsole($"{Localizer["console_setresp"]}");
            if (topEnabled) player.PrintToConsole($"{Localizer["console_top"]}");
            if (topEnabled && bonusRespawnPoses.Count != 0) player.PrintToConsole($"{Localizer["console_topbonus"]}");
            if (rankEnabled) player.PrintToConsole($"{Localizer["console_rank"]}");
            if (rankEnabled) player.PrintToConsole($"{Localizer["console_ranks"]}");
            if (globalRanksEnabled) player.PrintToConsole($"{Localizer["console_points"]}");
            if (goToEnabled) player.PrintToConsole($"{Localizer["console_goto"]}");
            if (stageTriggerPoses.Count != 0) player.PrintToConsole($"{Localizer["console_stage"]}");
            player.PrintToConsole($"{Localizer["console_sounds"]}");
            player.PrintToConsole($"{Localizer["console_hud"]}");
            player.PrintToConsole($"{Localizer["console_keys"]}");
            player.PrintToConsole($"{Localizer["console_fov"]}");

            if (cpEnabled)
            {
                if (currentMapName!.Contains("surf_"))
                {
                    player.PrintToConsole($"{Localizer["console_saveloc"]}");
                    player.PrintToConsole($"{Localizer["console_loadloc"]}");
                    player.PrintToConsole($"{Localizer["console_prevloc"]}");
                    player.PrintToConsole($"{Localizer["console_nextloc"]}");
                }
                else
                {
                    player.PrintToConsole($"{Localizer["console_cp"]}");
                    player.PrintToConsole($"{Localizer["console_tp"]}");
                    player.PrintToConsole($"{Localizer["console_prevcp"]}");
                    player.PrintToConsole($"{Localizer["console_nextcp"]}");
                }
            }

            if (enableReplays)
            {
                player.PrintToConsole($"{Localizer["console_replay"]}");
                player.PrintToConsole($"{Localizer["console_replaytop"]}");
                player.PrintToConsole($"{Localizer["console_replaypb"]}");
                player.PrintToConsole($"{Localizer["console_replaybonus"]}");
                player.PrintToConsole($"{Localizer["console_replaybonuspb"]}");
            }

            if (jumpStatsEnabled) player.PrintToConsole($"{Localizer["console_jumpstats"]}");
            player.PrintToConsole($"{Localizer["console_hideweapon"]}");
            player.PrintToConsole($"{Localizer["console_spec"]}");
            if (enableStyles) player.PrintToConsole($"{Localizer["console_styles"]}");
        }

        public void ForcePlayerSpeed(CCSPlayerController player, string activeWeapon)
        {

            try
            {
                activeWeapon ??= "no_knife";
                if (!weaponSpeedLookup.TryGetValue(activeWeapon, out WeaponSpeedStats weaponStats) || !player.IsValid) return;

                if(player.PlayerPawn.Value!.ActualMoveType.HasFlag(MoveType_t.MOVETYPE_LADDER))
                    player.PlayerPawn.Value!.VelocityModifier = 1.0f;
                else
                    player.PlayerPawn.Value!.VelocityModifier = (float)(forcedPlayerSpeed / weaponStats.GetSpeed(player.PlayerPawn.Value.IsWalking));
            }
            catch (Exception ex)
            {
                SharpTimerError($"Error in ForcePlayerSpeed: {ex.Message}");
            }
        }

        private void AdjustPlayerVelocity(CCSPlayerController? player, float velocity, bool forceNoDebug = false)
        {
            if (!IsAllowedPlayer(player)) return;

            try
            {
                var currentX = player!.PlayerPawn.Value!.AbsVelocity.X;
                var currentY = player!.PlayerPawn.Value!.AbsVelocity.Y;
                var currentZ = player!.PlayerPawn.Value!.AbsVelocity.Z;

                var currentSpeedSquared = currentX * currentX + currentY * currentY + currentZ * currentZ;

                // Check if current speed is not zero to avoid division by zero
                if (currentSpeedSquared > 0)
                {
                    var currentSpeed = Math.Sqrt(currentSpeedSquared);

                    var normalizedX = currentX / currentSpeed;
                    var normalizedY = currentY / currentSpeed;
                    var normalizedZ = currentZ / currentSpeed;

                    var adjustedX = normalizedX * velocity; // Adjusted speed limit
                    var adjustedY = normalizedY * velocity; // Adjusted speed limit
                    var adjustedZ = normalizedZ * velocity; // Adjusted speed limit

                    player!.PlayerPawn.Value!.AbsVelocity.X = (float)adjustedX;
                    player!.PlayerPawn.Value!.AbsVelocity.Y = (float)adjustedY;
                    player!.PlayerPawn.Value!.AbsVelocity.Z = (float)adjustedZ;

                    if (!forceNoDebug) SharpTimerDebug($"Adjusted Velo for {player.PlayerName} to {player.PlayerPawn.Value.AbsVelocity}");
                }
                else
                {
                    if (!forceNoDebug) SharpTimerDebug($"Cannot adjust velocity for {player.PlayerName} because current speed is zero.");
                }
            }
            catch (Exception ex)
            {
                SharpTimerError($"Error in AdjustPlayerVelocity: {ex.Message}");
            }
        }

        private void AdjustPlayerVelocity2D(CCSPlayerController? player, float velocity, bool forceNoDebug = false)
        {
            if (!IsAllowedPlayer(player)) return;

            try
            {
                var currentX = player!.PlayerPawn.Value!.AbsVelocity.X;
                var currentY = player!.PlayerPawn.Value!.AbsVelocity.Y;

                var currentSpeedSquared = currentX * currentX + currentY * currentY;

                // Check if current speed is not zero to avoid division by zero
                if (currentSpeedSquared > 0)
                {
                    var currentSpeed2D = Math.Sqrt(currentSpeedSquared);

                    var normalizedX = currentX / currentSpeed2D;
                    var normalizedY = currentY / currentSpeed2D;

                    var adjustedX = normalizedX * velocity; // Adjusted speed limit
                    var adjustedY = normalizedY * velocity; // Adjusted speed limit

                    player.PlayerPawn.Value.AbsVelocity.X = (float)adjustedX;
                    player.PlayerPawn.Value.AbsVelocity.Y = (float)adjustedY;

                    if (!forceNoDebug) SharpTimerDebug($"Adjusted Velo for {player.PlayerName} to {player.PlayerPawn.Value.AbsVelocity}");
                }
                else
                {
                    if (!forceNoDebug) SharpTimerDebug($"Cannot adjust velocity for {player.PlayerName} because current speed is zero.");
                }
            }
            catch (Exception ex)
            {
                SharpTimerError($"Error in AdjustPlayerVelocity2D: {ex.Message}");
            }
        }

        private void RemovePlayerCollision(CCSPlayerController? player)
        {
            try
            {
                Server.NextFrame(() =>
                {
                    if (removeCollisionEnabled == false || !IsAllowedPlayer(player)) return;

                    player!.Pawn.Value!.Collision.CollisionAttribute.CollisionGroup = (byte)CollisionGroup.COLLISION_GROUP_DISSOLVING;
                    player!.Pawn.Value!.Collision.CollisionGroup = (byte)CollisionGroup.COLLISION_GROUP_DISSOLVING;

                    Utilities.SetStateChanged(player, "CCollisionProperty", "m_CollisionGroup");
                    Utilities.SetStateChanged(player, "CCollisionProperty", "m_collisionAttribute");

                    SharpTimerDebug($"Removed Collison for {player.PlayerName}");
                });
            }
            catch (Exception ex)
            {
                SharpTimerError($"Error in RemovePlayerCollision: {ex.Message}");
            }
        }

        public async Task<(int, string)> GetStageTime(string steamId, int stageIndex)
        {
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

                            if (playerData!.TryGetValue(steamId, out var playerStageData))
                            {
                                if (playerStageData.StageTimes != null && playerStageData.StageTimes.TryGetValue(stageIndex, out var time) &&
                                    playerStageData.StageVelos != null && playerStageData.StageVelos.TryGetValue(stageIndex, out var speed))
                                {
                                    return (time, speed);
                                }
                            }
                        }
                    }
                    else
                    {
                        SharpTimerDebug($"Error in GetStageTime jsonDoc was null");
                    }
                }
            }
            catch (Exception ex)
            {
                SharpTimerError($"Error in GetStageTime: {ex.Message}");
            }

            return (0, string.Empty);
        }

        public async Task<int> GetPreviousPlayerRecord(string steamId, int bonusX = 0)
        {
            string currentMapNamee = bonusX == 0 ? currentMapName! : $"{currentMapName}_bonus{bonusX}";
            string mapRecordsFileName = $"{currentMapNamee}.json";
            string mapRecordsPath = Path.Combine(playerRecordsPath!, mapRecordsFileName);

            try
            {
                JsonDocument? jsonDoc = await LoadJson(mapRecordsPath);
                if (jsonDoc != null)
                {
                    var root = jsonDoc.RootElement;
                    if (root.TryGetProperty(steamId, out var playerRecordElement))
                    {
                        return playerRecordElement.GetProperty("TimerTicks").GetInt32();
                    }
                }
                else
                {
                    SharpTimerDebug($"Map records file does not exist: {mapRecordsPath}");
                }
            }
            catch (Exception ex)
            {
                SharpTimerError($"Error in GetPreviousPlayerRecord: {ex.Message}");
            }

            return 0;
        }

        public string GetPlayerPlacement(CCSPlayerController? player)
        {
            if (!IsAllowedPlayer(player) || !playerTimers[player!.Slot].IsTimerRunning)
                return "";

            int currentPlayerTime = playerTimers[player.Slot].TimerTicks;

            int placement = 1;

            foreach (var kvp in SortedCachedRecords!.Take(100))
            {
                int recordTimerTicks = kvp.Value.TimerTicks;

                if (currentPlayerTime > recordTimerTicks)
                {
                    placement++;
                }
                else
                {
                    break;
                }
            }
            if (placement > 100)
            {
                return "#100" + "+";
            }
            else
            {
                return "#" + placement;
            }
        }

        public async Task<string> GetPlayerMapPlacementWithTotal(CCSPlayerController? player, string steamId, string playerName, bool getRankImg = false, bool getPlacementOnly = false, int bonusX = 0, int style = 0, bool getPercentileOnly = false)
        {
            try
            {
                if (!IsPlayerOrSpectator(player))
                    return "";

                string currentMapNamee = bonusX == 0 ? currentMapName! : $"{currentMapName}_bonus{bonusX}";

                int savedPlayerTime = await GetPreviousPlayerRecordFromDatabase(steamId, currentMapName!, playerName, bonusX, style);

                if (savedPlayerTime == 0)
                    return getRankImg ? UnrankedIcon : UnrankedTitle;

                Dictionary<int, PlayerRecord> sortedRecords = await GetSortedRecordsFromDatabase(0, bonusX, currentMapNamee, style);

                int placement = sortedRecords.Count(kv => kv.Value.TimerTicks < savedPlayerTime) + 1;
                int totalPlayers = sortedRecords.Count;
                double percentage = (double)placement / totalPlayers * 100;

                return CalculateRankStuff(totalPlayers, placement, percentage, getRankImg, getPlacementOnly);
            }
            catch (Exception ex)
            {
                SharpTimerError($"Error in GetPlayerMapPlacementWithTotal: {ex}");
                return UnrankedTitle;
            }
        }
        public async Task<double> GetPlayerMapPercentile(string steamId, string playerName, string mapname = "", int bonusX = 0, int style = 0, bool global = false, int timerTicks = 0)
        {
            try
            {
                string currentMapNamee;
                if (mapname == "")
                    currentMapNamee = bonusX == 0 ? currentMapName! : $"{currentMapName}_bonus{bonusX}";
                else
                    currentMapNamee = bonusX == 0 ? mapname! : $"{mapname}_bonus{bonusX}";

                int savedPlayerTime;

                if (!global)
                    savedPlayerTime = await GetPreviousPlayerRecordFromDatabase(steamId, currentMapNamee!, playerName, bonusX, style);
                else
                    savedPlayerTime = await GetPreviousPlayerRecordFromGlobal(steamId, currentMapNamee!, playerName, bonusX, style);

                if (savedPlayerTime == 0)
                    savedPlayerTime = timerTicks;

                Dictionary<int, PlayerRecord> sortedRecords;

                if (!global)
                    sortedRecords = await GetSortedRecordsFromDatabase(0, bonusX, currentMapNamee, style);
                else
                    sortedRecords = await GetSortedRecordsFromGlobal(0, bonusX, currentMapNamee, style);

                int placement = 1;
                int totalPlayers = sortedRecords.Count;

                if (totalPlayers > 0)
                {
                    placement = sortedRecords.Count(kv => kv.Value.TimerTicks < savedPlayerTime) + 1;

                    if (placement > totalPlayers)
                    {
                        placement = totalPlayers;
                    }
                }

                double percentage = totalPlayers == 0 ? 100 : (double)placement / totalPlayers * 100;

                SharpTimerDebug($"Player: {playerName}, Placement: {placement}, Total Players: {totalPlayers}, Percentage: {percentage}th");

                return percentage;
            }
            catch (Exception ex)
            {
                SharpTimerError($"Error in GetPlayerMapPercentile: {ex}");
                return 0;
            }
        }
        public async Task<string> GetPlayerStagePlacementWithTotal(CCSPlayerController? player, string steamId, string playerName, int stage, bool getRankImg = false, bool getPlacementOnly = false, int bonusX = 0)
        {
            try
            {
                if (!IsPlayerOrSpectator(player))
                    return "";

                string currentMapNamee = bonusX == 0 ? currentMapName! : $"{currentMapName}_bonus{bonusX}";

                int savedPlayerTime = await GetPreviousPlayerStageRecordFromDatabase(player, steamId, currentMapName!, stage, playerName, bonusX);

                if (savedPlayerTime == 0)
                    return getRankImg ? UnrankedIcon : UnrankedTitle;

                Dictionary<string, PlayerRecord> sortedRecords = await GetSortedStageRecordsFromDatabase(stage, 0, bonusX, currentMapNamee);

                int placement = sortedRecords.Count(kv => kv.Value.TimerTicks < savedPlayerTime) + 1;
                int totalPlayers = sortedRecords.Count;
                double percentage = (double)placement / totalPlayers * 100;

                return CalculateRankStuff(totalPlayers, placement, percentage, getRankImg, getPlacementOnly);
            }
            catch (Exception ex)
            {
                SharpTimerError($"Error in GetPlayerStagePlacementWithTotal: {ex}");
                return UnrankedTitle;
            }
        }

        public async Task<string> GetPlayerServerPlacement(CCSPlayerController? player, string steamId, string playerName, bool getRankImg = false, bool getPlacementOnly = false, bool getPointsOnly = false)
        {
            try
            {
                if (!IsPlayerOrSpectator(player))
                    return "";

                int savedPlayerPoints = enableDb ? await GetPlayerPointsFromDatabase(player, steamId, playerName) : 0;

                if (getPointsOnly)
                    return savedPlayerPoints.ToString();

                if (savedPlayerPoints == 0 || savedPlayerPoints <= minGlobalPointsForRank)
                    return getRankImg ? UnrankedIcon : UnrankedTitle;

                Dictionary<string, PlayerPoints> sortedPoints = enableDb ? await GetSortedPointsFromDatabase() : [];

                int placement = sortedPoints.Count(kv => kv.Value.GlobalPoints > savedPlayerPoints) + 1;
                int totalPlayers = sortedPoints.Count;
                double percentage = (double)placement / totalPlayers * 100;

                return CalculateRankStuff(totalPlayers, placement, percentage, getRankImg, getPlacementOnly);
            }
            catch (Exception ex)
            {
                SharpTimerError($"Error in GetPlayerServerPlacement: {ex}");
                return UnrankedTitle;
            }
        }

        public string CalculateRankStuff(int totalPlayers, int placement, double percentage, bool getRankImg = false, bool getPlacementOnly = false)
        {
            try
            {
                foreach (var rank in rankDataList)
                {
                    if (rank.Placement > 0 && placement == rank.Placement)
                        return getRankImg ? rank.Icon : (getPlacementOnly ? $"{placement}/{totalPlayers}" : rank.Title);

                    if (rank.Percent > 0 && percentage <= rank.Percent)
                        return getRankImg ? rank.Icon : (getPlacementOnly ? $"{placement}/{totalPlayers}" : rank.Title);
                }
                return getRankImg ? UnrankedIcon : (getPlacementOnly ? $"{placement}/{totalPlayers}" : UnrankedTitle);
            }
            catch (Exception ex)
            {
                SharpTimerError($"Error in CalculateRankStuff: {ex}");
                return UnrankedTitle;
            }
        }

        public void InvalidateTimer(CCSPlayerController player, nint callerHandle = 0)
        {
            if (player.IsValid && playerTimers!.TryGetValue(player.Slot, out var playerTimer))
            {
                if (!playerTimers[player.Slot].IsTimerBlocked)
                {
                    playerCheckpoints.Remove(player.Slot);
                }
                playerTimers[player.Slot].TimerTicks = 0;
                playerTimers[player.Slot].StageTicks = 0;
                playerTimers[player.Slot].BonusTimerTicks = 0;
                playerTimers[player.Slot].IsTimerRunning = false;
                playerTimers[player.Slot].IsBonusTimerRunning = false;
                if (stageTriggerCount != 0 && useStageTriggers == true)
                {
                    playerTimers[player.Slot].StageTimes!.Clear();
                    playerTimers[player.Slot].StageVelos!.Clear();
                    playerTimers[player.Slot].CurrentMapStage = stageTriggers.GetValueOrDefault(callerHandle, 0);
                }
                if (cpTriggerCount != 0)
                {
                    playerTimers[player.Slot].StageTimes!.Clear();
                    playerTimers[player.Slot].StageVelos!.Clear();
                    playerTimers[player.Slot].CurrentMapCheckpoint = 0;
                }
            }
        }

        private HookResult OnCommandJoinTeam(CCSPlayerController? player, CounterStrikeSharp.API.Modules.Commands.CommandInfo commandInfo)
        {
            if (player == null || !player.IsValid) return HookResult.Handled;
            InvalidateTimer(player);
            return HookResult.Continue;
        }

        public async Task PrintMapTimeToChat(CCSPlayerController player, string steamID, string playerName, int oldticks, int newticks, int bonusX = 0, int timesFinished = 0, int style = 0, int prevSR = 0)
        {
            if (!IsAllowedPlayer(player))
            {
                SharpTimerError($"Error in PrintMapTimeToChat: Player {playerName} not allowed or not on server anymore");
                return;
            }

            string ranking = await GetPlayerMapPlacementWithTotal(player, steamID, playerName, false, true, bonusX, style);

            bool newSR = GetNumberBeforeSlash(ranking) == 1 && (oldticks > newticks || oldticks == 0);
            bool beatPB = oldticks > newticks;
            string newTime = FormatTime(newticks);
            string timeDifferenceNoCol = "";
            string timeDifference = "";
            if (oldticks != 0)
            {
                if (discordWebhookEnabled) timeDifferenceNoCol = FormatTimeDifference(newticks, oldticks, true);
                timeDifference = $"[{FormatTimeDifference(newticks, oldticks)}{ChatColors.White}] ";
            }

            Server.NextFrame(() =>
            {
                if (newSR)
                {
                    if (prevSR != 0)
                    {
                        timeDifference = $"[{FormatTimeDifference(newticks, prevSR)}{ChatColors.White}] ";
                    }
                    if (bonusX != 0) PrintToChatAll(Localizer["new_server_record_bonus", playerName, bonusX]);
                    else
                    {
                        PrintToChatAll(Localizer["new_server_record", playerName]);
                        PlaySound(player, srSound, srSoundAll ? true : false);
                    }
                    if (discordWebhookPrintSR && discordWebhookEnabled && enableDb) _ = Task.Run(async () => await DiscordRecordMessage(player, playerName, newTime, steamID, ranking, timesFinished, true, timeDifferenceNoCol, bonusX));
                }
                else if (beatPB)
                {
                    if (bonusX != 0) PrintToChatAll(Localizer["new_pb_record_bonus", playerName, bonusX]);
                    else PrintToChatAll(Localizer["new_pb_record", playerName]);
                    if (discordWebhookPrintPB && discordWebhookEnabled && enableDb) _ = Task.Run(async () => await DiscordRecordMessage(player, playerName, newTime, steamID, ranking, timesFinished, false, timeDifferenceNoCol, bonusX));
                    PlaySound(player, pbSound);
                }
                else
                {
                    if (bonusX != 0) PrintToChatAll(Localizer["map_finish_bonus", playerName, bonusX]);
                    else PrintToChatAll(Localizer["map_finish", playerName]);
                    if (discordWebhookPrintPB && discordWebhookEnabled && timesFinished == 1 && enableDb) _ = Task.Run(async () => await DiscordRecordMessage(player, playerName, newTime, steamID, ranking, timesFinished, false, timeDifferenceNoCol, bonusX));
                    PlaySound(player, timerSound);
                }

                if (enableDb || bonusX != 0)
                    PrintToChatAll(Localizer["map_finish_rank", ranking, timesFinished]);

                PrintToChatAll(Localizer["timer_time", newTime, timeDifference]);
                if (enableStyles) PrintToChatAll(Localizer["timer_style", GetNamedStyle(style)]);
                if (enableReplays == true && enableSRreplayBot == true && newSR && (oldticks > newticks || oldticks == 0))
                {
                    _ = Task.Run(async () => await SpawnReplayBot());
                }
            });
        }
        public async Task PrintStageTimeToChat(CCSPlayerController player, string steamID, string playerName, int oldticks, int newticks, int stage, int bonusX = 0, int prevSR = 0)
        {
            if (!IsAllowedPlayer(player))
            {
                SharpTimerError($"Error in PrintStageTimeToChat: Player {playerName} not allowed or not on server anymore");
                return;
            }

            string ranking = await GetPlayerStagePlacementWithTotal(player, steamID, playerName, stage, false, true, bonusX);

            bool newSR = GetNumberBeforeSlash(ranking) == 1 && (oldticks > newticks || oldticks == 0);
            bool beatPB = oldticks > newticks;
            string newTime = FormatTime(newticks);
            string timeDifferenceNoCol = "";
            string timeDifference = "";
            if (oldticks != 0)
            {
                if (discordWebhookEnabled) timeDifferenceNoCol = FormatTimeDifference(newticks, oldticks, true);
                timeDifference = $"[{FormatTimeDifference(newticks, oldticks)}{ChatColors.White}] ";
            }

            Server.NextFrame(() =>
            {
                if (newSR)
                {
                    if (prevSR != 0)
                    {
                        timeDifference = $"[{FormatTimeDifference(newticks, prevSR)}{ChatColors.White}] ";
                    }
                    PrintToChatAll(Localizer["new_stage_server_record", playerName]);
                    PlaySound(player, srSound, stageSoundAll ? true : false);
                    PrintToChatAll(Localizer["timer_time", newTime, timeDifference]);
                    //TODO: Discord webhook stage sr
                    //if (discordWebhookPrintSR && discordWebhookEnabled && enableDb) _ = Task.Run(async () => await DiscordRecordMessage(player, playerName, newTime, steamID, ranking, timesFinished, true, timeDifferenceNoCol, bonusX));
                }
            });
        }

        public void AddScoreboardTagToPlayer(CCSPlayerController player, string tag)
        {
            try
            {
                if (string.IsNullOrEmpty(tag))
                    return;

                if (player == null || !player.IsValid)
                    return;

                string originalPlayerName = player.PlayerName;

                string stripedClanTag = RemovePlayerTags(player.Clan ?? "");
                
                player.Clan = $" {stripedClanTag}{(playerTimers[player.Slot].IsVip ? $"{customVIPTag}" : "")}{tag}";

                player.PlayerName = originalPlayerName + " ";

                AddTimer(0.1f, () =>
                {
                    if (player.IsValid)
                    {
                        Utilities.SetStateChanged(player, "CCSPlayerController", "m_szClan");
                        Utilities.SetStateChanged(player, "CBasePlayerController", "m_iszPlayerName");
                    }
                });

                AddTimer(0.2f, () =>
                {
                    if (player.IsValid) player.PlayerName = originalPlayerName;
                });

                AddTimer(0.3f, () =>
                {
                    if (player.IsValid) Utilities.SetStateChanged(player, "CBasePlayerController", "m_iszPlayerName");
                });

                SharpTimerDebug($"Set Scoreboard Tag for {player.Clan} {player.PlayerName}");
            }
            catch (Exception ex)
            {
                SharpTimerError($"Error in AddScoreboardTagToPlayer: {ex.Message}");
            }
        }

        public void ChangePlayerName(CCSPlayerController player, string name)
        {
            if (string.IsNullOrEmpty(name))
                return;

            if (player == null || !player.IsValid)
                return;

            player.PlayerName = name;
            Utilities.SetStateChanged(player, "CBasePlayerController", "m_iszPlayerName");

            SharpTimerDebug($"Changed PlayerName to {player.PlayerName}");
        }

        static void SetMoveType(CCSPlayerController player, MoveType_t nMoveType)
        {
            if (!player.IsValid) return;

            player.PlayerPawn.Value!.MoveType = nMoveType; // necessary to maintain client prediction
            player.PlayerPawn.Value!.ActualMoveType = nMoveType;
            Utilities.SetStateChanged(player!.Pawn.Value!, "CBaseEntity", "m_MoveType");
        }

        public string GetRankColorForChat(CCSPlayerController player)
        {
            try
            {
                if (playerTimers.TryGetValue(player.Slot, out PlayerTimerInfo? playerTimer))
                {
                    if (string.IsNullOrEmpty(playerTimer.CachedRank))
                        return $"{ChatColors.Default}";

                    string color = $"{ChatColors.Default}";

                    if (playerTimer.CachedRank.Contains(UnrankedTitle))
                        color = ReplaceVars(UnrankedColor);

                    else
                    {
                        foreach (var rank in rankDataList)
                        {
                            if (playerTimer.CachedRank.Contains(rank.Title!))
                            {
                                color = ReplaceVars(rank.Color!);
                                break;
                            }
                        }
                    }

                    return color;
                }
                else
                {
                    return $"{ChatColors.Default}";
                }
            }
            catch (Exception ex)
            {
                SharpTimerError($"Error in GetRankColorForChat: {ex.Message}");
                return $"{ChatColors.Default}";
            }
        }

        public static (int, int) GetPlayerTeamCount()
        {
            int ct_count = 0;
            int t_count = 0;

            Utilities.GetPlayers().ForEach(player =>
            {
                if (player is { PawnIsAlive: true, IsValid: true })
                {
                    if (player.Team == CsTeam.CounterTerrorist)
                        ct_count++;
                    else if (player.Team == CsTeam.Terrorist)
                        t_count++;
                }
            });

            return (ct_count, t_count);
        }

        public void PlaySound(CCSPlayerController? player, string sound, bool allPlayers = false)
        {
            if (allPlayers)
            {
                foreach (var target in connectedPlayers.Values)
                {
                    if (playerTimers[target!.Slot].SoundsEnabled && IsPlayerOrSpectator(target))
                    {
                        if (soundeventsEnabled)
                            target.EmitSound(sound, new(target));

                        else target.ExecuteClientCommand($"play {sound}");
                    }
                }
            }
            else
            {
                if (playerTimers[player!.Slot].SoundsEnabled && IsPlayerOrSpectator(player))
                {
                    if (soundeventsEnabled)
                        player.EmitSound(sound, new(player));

                    else player.ExecuteClientCommand($"play {sound}");
                }
            }
        }

        public void PrintToChat(CCSPlayerController player, string message)
        {
            player?.PrintToChat($" {Localizer["prefix"]} {message}");
        }

        public void PrintToChatAll(string message)
        {
            Server.PrintToChatAll($" {Localizer["prefix"]} {message}");
        }
    }

    public static class EntityExtends
    {
        public static bool Valid(this CCSPlayerController? player)
        {
            return player == null || player.IsValid || player.PlayerPawn.Value != null || player.PlayerPawn.IsValid || !player.IsBot || !player.IsHLTV || player.SteamID > 0;
        }

        public static bool Alive([NotNullWhen(true)] this CCSPlayerController player)
        {
            return player.PawnIsAlive && player.PlayerPawn.Value?.LifeState == (byte)LifeState_t.LIFE_ALIVE;
        }

        public static CCSPlayerPawn? PlayerPawn([NotNullWhen(true)] this CCSPlayerController player)
        {
            CCSPlayerPawn? playerPawn = player.PlayerPawn.Value;

            return playerPawn;
        }

        public static CBasePlayerPawn? Pawn([NotNullWhen(true)] this CCSPlayerController player)
        {
            CBasePlayerPawn? pawn = player.Pawn.Value;

            return pawn;
        }

        public static bool TeamT([NotNullWhen(true)] this CCSPlayerController player)
        {
            return player.Team == CsTeam.Terrorist;
        }
        public static bool TeamCT([NotNullWhen(true)] this CCSPlayerController player)
        {
            return player.Team == CsTeam.CounterTerrorist;
        }
        public static bool TeamSpec([NotNullWhen(true)] this CCSPlayerController player)
        {
            return player.Team == CsTeam.Spectator;
        }
        public static bool TeamNone([NotNullWhen(true)] this CCSPlayerController player)
        {
            return player.Team == CsTeam.None;
        }

        public static bool isAdmin([NotNullWhen(true)] this CCSPlayerController player)
        {
            if (AdminManager.PlayerHasPermissions(player, "@css/ban"))
                return true;

            return false;
        }
        public static bool isVIP([NotNullWhen(true)] this CCSPlayerController player)
        {
            if (AdminManager.PlayerHasPermissions(player, "@css/reservation"))
                return true;

            return false;
        }
    }
}
