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
using CounterStrikeSharp.API.Modules.Utils;
using CounterStrikeSharp.API.Modules.Entities.Constants;
using System.Text.Json;
using Vector = CounterStrikeSharp.API.Modules.Utils.Vector;

namespace SharpTimer
{
    public partial class SharpTimer
    {
        private void ReplayUpdate(CCSPlayerController player, int timerTicks)
        {
            try
            {
                if (!IsAllowedPlayer(player)) return;

                // Get the player's current position and rotation
                ReplayVector currentPosition = ReplayVector.GetVectorish(player.Pawn.Value!.CBodyComponent?.SceneNode?.AbsOrigin ?? new Vector(0, 0, 0));
                ReplayVector currentSpeed = ReplayVector.GetVectorish(player.PlayerPawn.Value!.AbsVelocity ?? new Vector(0, 0, 0));
                ReplayQAngle currentRotation = ReplayQAngle.GetQAngleish(player.PlayerPawn.Value.EyeAngles ?? new QAngle(0, 0, 0));

                var buttons = player.Buttons;
                var flags = player.Pawn.Value.Flags;
                var moveType = player.Pawn.Value.MoveType;

                var ReplayFrame = new PlayerReplays.ReplayFrames
                {
                    Position = currentPosition,
                    Rotation = currentRotation,
                    Speed = currentSpeed,
                    Buttons = buttons,
                    Flags = flags,
                    MoveType = moveType
                };

                playerReplays[player.Slot].replayFrames.Add(ReplayFrame);
            }
            catch (Exception ex)
            {
                Utils.LogError($"Error in ReplayUpdate: {ex.Message}");
            }
        }

        private void ReplayPlayback(CCSPlayerController player, int plackbackTick)
        {
            try
            {
                if (!IsAllowedPlayer(player)) return;

                //player.LerpTime = 0.0078125f;

                if (playerTimers.TryGetValue(player.Slot, out PlayerTimerInfo? value))
                {
                    
                    var replayFrame = playerReplays[player.Slot].replayFrames[plackbackTick];

                    if (((PlayerFlags)replayFrame.Flags & PlayerFlags.FL_ONGROUND) != 0)
                    {
                        SetMoveType(player, MoveType_t.MOVETYPE_WALK);
                    }
                    else
                    {
                        SetMoveType(player, MoveType_t.MOVETYPE_OBSERVER);
                    }

                    if (((PlayerFlags)replayFrame.Flags & PlayerFlags.FL_DUCKING) != 0)
                    {
                        value.MovementService!.DuckAmount = 1;
                    }
                    else
                    {
                        value.MovementService!.DuckAmount = 0;
                    }

                    player.PlayerPawn.Value!.Teleport(ReplayVector.ToVector(replayFrame.Position!), ReplayQAngle.ToQAngle(replayFrame.Rotation!), ReplayVector.ToVector(replayFrame.Speed!));

                    var replayButtons = $"{((replayFrame.Buttons & PlayerButtons.Moveleft) != 0 ? "A" : "_")} " +
                                        $"{((replayFrame.Buttons & PlayerButtons.Forward) != 0 ? "W" : "_")} " +
                                        $"{((replayFrame.Buttons & PlayerButtons.Moveright) != 0 ? "D" : "_")} " +
                                        $"{((replayFrame.Buttons & PlayerButtons.Back) != 0 ? "S" : "_")} " +
                                        $"{((replayFrame.Buttons & PlayerButtons.Jump) != 0 ? "J" : "_")} " +
                                        $"{((replayFrame.Buttons & PlayerButtons.Duck) != 0 ? "C" : "_")}";

                    if (value.HideKeys != true && value.IsReplaying == true && keysOverlayEnabled == true)
                    {
                        player.PrintToCenter(replayButtons);
                    }
                }
            }
            catch (Exception ex)
            {
                Utils.LogError($"Error in ReplayPlayback: {ex.Message}");
            }
        }

        private void ReplayPlay(CCSPlayerController player)
        {
            try
            {
                int totalFrames = playerReplays[player.Slot].replayFrames.Count;

                if (totalFrames <= 128)
                {
                    OnRecordingStop(player);
                }

                if (playerReplays[player.Slot].CurrentPlaybackFrame < 0 || playerReplays[player.Slot].CurrentPlaybackFrame >= totalFrames)
                {
                    playerReplays[player.Slot].CurrentPlaybackFrame = 0;
                    Action<CCSPlayerController?, float, bool> adjustVelocity = use2DSpeed ? AdjustPlayerVelocity2D : AdjustPlayerVelocity;
                    adjustVelocity(player, 0, false);
                }

                ReplayPlayback(player, playerReplays[player.Slot].CurrentPlaybackFrame);

                playerReplays[player.Slot].CurrentPlaybackFrame++;
            }
            catch (Exception ex)
            {
                Utils.LogError($"Error in ReplayPlay: {ex.Message}");
            }
        }

        private void OnRecordingStart(CCSPlayerController player, int bonusX = 0, int style = 0)
        {
            try
            {
                playerReplays.Remove(player.Slot);
                playerReplays[player.Slot] = new PlayerReplays
                {
                    BonusX = bonusX,
                    Style = style
                };
                playerTimers[player.Slot].IsRecordingReplay = true;
            }
            catch (Exception ex)
            {
                Utils.LogError($"Error in OnRecordingStart: {ex.Message}");
            }
        }

        private void OnRecordingStop(CCSPlayerController player)
        {
            try
            {
                playerTimers[player.Slot].IsRecordingReplay = false;
                SetMoveType(player, MoveType_t.MOVETYPE_WALK);
            }
            catch (Exception ex)
            {
                Utils.LogError($"Error in OnRecordingStop: {ex.Message}");
            }
        }

        public async Task DumpReplayToJson(CCSPlayerController player, string steamID, int playerSlot, int bonusX = 0, int style = 0)
        {
            await Task.Run(() =>
            {
                if (!IsAllowedPlayer(player))
                {
                    Utils.LogError($"Error in DumpReplayToJson: Player not allowed or not on server anymore");
                    return;
                }

                string fileName = $"{steamID}_replay.json";
                string playerReplaysDirectory;
                if (style != 0) playerReplaysDirectory = Path.Join(gameDir, "csgo", "cfg", "SharpTimer", "PlayerReplayData", bonusX == 0 ? $"{currentMapName}" : $"{currentMapName}_bonus{bonusX}", GetNamedStyle(style));
                else playerReplaysDirectory = Path.Join(gameDir, "csgo", "cfg", "SharpTimer", "PlayerReplayData", bonusX == 0 ? $"{currentMapName}" : $"{currentMapName}_bonus{bonusX}");
                string playerReplaysPath = Path.Join(playerReplaysDirectory, fileName);

                try
                {
                    if (!Directory.Exists(playerReplaysDirectory))
                    {
                        Directory.CreateDirectory(playerReplaysDirectory);
                    }

                    if (playerReplays[playerSlot].replayFrames.Count >= maxReplayFrames) return;

                    var indexedReplayFrames = playerReplays[playerSlot].replayFrames
                        .Select((frame, index) => new IndexedReplayFrames { Index = index, Frame = frame })
                        .ToList();

                    using (Stream stream = new FileStream(playerReplaysPath, FileMode.Create))
                    {
                        JsonSerializer.Serialize(stream, indexedReplayFrames);
                    }
                }
                catch (Exception ex)
                {
                    Utils.LogError($"Error during serialization: {ex.Message}");
                }
            });
        }

        public string GetReplayJson(CCSPlayerController player, int playerSlot)
        {
            if (!IsAllowedPlayer(player))
            {
                Utils.LogError($"Error in GetReplayJson: Player not allowed or not on server anymore");
                return "";
            }

            try
            {
                if (playerReplays[playerSlot].replayFrames.Count >= maxReplayFrames) return "";

                var indexedReplayFrames = playerReplays[playerSlot].replayFrames
                    .Select((frame, index) => new IndexedReplayFrames { Index = index, Frame = frame })
                    .ToList();

                return JsonSerializer.Serialize(indexedReplayFrames);
            }
            catch (Exception ex)
            {
                Utils.LogError($"Error during serialization: {ex.Message}");
                return "";
            }
        }

        private async Task ReadReplayFromJson(CCSPlayerController player, string steamId, int playerSlot, int bonusX = 0, int style = 0)
        {
            string fileName = $"{steamId}_replay.json";
            string playerReplaysPath;
            if (style != 0) playerReplaysPath = Path.Join(gameDir, "csgo", "cfg", "SharpTimer", "PlayerReplayData", bonusX == 0 ? currentMapName : $"{currentMapName}_bonus{bonusX}", GetNamedStyle(style), fileName);
            else playerReplaysPath = Path.Join(gameDir, "csgo", "cfg", "SharpTimer", "PlayerReplayData", bonusX == 0 ? currentMapName : $"{currentMapName}_bonus{bonusX}", fileName);

            try
            {
                if (File.Exists(playerReplaysPath))
                {
                    var jsonString = await File.ReadAllTextAsync(playerReplaysPath);
                    if (!jsonString.Contains("PositionString"))
                    {
                        var indexedReplayFrames = JsonSerializer.Deserialize<List<IndexedReplayFrames>>(jsonString);

                        if (indexedReplayFrames != null)
                        {
                            var replayFrames = indexedReplayFrames
                                .OrderBy(frame => frame.Index)
                                .Select(frame => frame.Frame)
                                .ToList();

                            if (!playerReplays.TryGetValue(playerSlot, out PlayerReplays? value))
                            {
                                value = new PlayerReplays();
                                playerReplays[playerSlot] = value;
                            }

                            value.replayFrames = replayFrames!;
                        }
                    }
                    else
                    {
                        Server.NextFrame(() => { Utils.PrintToChat(player, $"Unsupported replay format"); });
                    }
                }
                else
                {
                    Utils.LogError($"File does not exist: {playerReplaysPath}");
                    Server.NextFrame(() => Utils.PrintToChat(player, Localizer["replay_dont_exist"]));
                }
            }
            catch (Exception ex)
            {
                Utils.LogError($"Error during deserialization: {ex.Message}");
            }
        }

        private async Task ReadReplayFromGlobal(CCSPlayerController player, int recordId, int style, int bonusX = 0)
        {
            string currentMapFull = bonusX == 0 ? currentMapName! : $"{currentMapName}_bonus{bonusX}";
            var payload = new
            {
                record_id = recordId,
                map_name = currentMapFull,
                style = style
            };

            try
            {

                var jsonString = await GetReplayFromGlobal(payload);
                var indexedReplayFrames = JsonSerializer.Deserialize<List<IndexedReplayFrames>>(jsonString);

                if (indexedReplayFrames != null)
                {
                    var replayFrames = indexedReplayFrames
                        .OrderBy(frame => frame.Index)
                        .Select(frame => frame.Frame)
                        .ToList();

                    if (!playerReplays.TryGetValue(player.Slot, out PlayerReplays? value))
                    {
                        value = new PlayerReplays();
                        playerReplays[player.Slot] = value;
                    }

                    value.replayFrames = replayFrames!;
                }
            }
            catch (Exception ex)
            {
                Utils.LogError($"Error during deserialization: {ex.Message}");
            }
        }

        private async Task SpawnReplayBot(bool respawn = false)
        {
            if (!await CheckSRReplay())
            {
                Utils.LogDebug("Replay check failed, not spawning bot.");
                return;
            }

            Server.NextFrame(() =>
            {
                if (replayBotController == null)
                {
                    // wtf is this game even
                    Server.ExecuteCommand("bot_quota 1");
                    Server.ExecuteCommand("bot_add_ct");

                    Utils.LogDebug("Searching for replay bot...");

                    AddTimer(1.0f, () =>
                    {
                        // find and setup bot
                        var bot = Utilities.GetPlayers().Where(b => b.IsBot && !b.IsHLTV).FirstOrDefault();
                        if (bot != null)
                        {
                            replayBotController = bot;
                            Utils.LogDebug($"Found replay bot: {bot.PlayerName}");

                            var botPawn = bot.PlayerPawn.Value;
                            if (botPawn == null) return;

                            // bot settings
                            bot.RemoveWeapons();
                            botPawn.Bot!.IsStopping = true;
                            botPawn.Bot.IsSleeping = true;
                            botPawn.Bot.AllowActive = true;
                            bot!.Pawn.Value!.Collision.CollisionAttribute.CollisionGroup = (byte)CollisionGroup.COLLISION_GROUP_DISSOLVING;
                            bot!.Pawn.Value!.Collision.CollisionGroup = (byte)CollisionGroup.COLLISION_GROUP_DISSOLVING;
                            Utilities.SetStateChanged(bot, "CCollisionProperty", "m_CollisionGroup");
                            Utilities.SetStateChanged(bot, "CCollisionProperty", "m_collisionAttribute");
                            Utils.LogDebug($"Configured replay bot collision and weapons for {bot.PlayerName}");

                            // start bot replay
                            OnPlayerConnect(bot, true);
                            ChangePlayerName(bot, replayBotName);
                            playerTimers[bot.Slot].IsTimerBlocked = true;
                            _ = Task.Run(async () => await ReplayHandler(bot, bot.Slot));
                            Utils.LogDebug($"Starting replay for {bot.PlayerName}");
                        }
                        else
                        {
                            Utils.LogError($"Failed to spawn replay bot");
                            return;
                        }

                        // kick unused bots if there are any
                        var bots = Utilities.GetPlayers().Where(b => b.IsBot && !b.IsHLTV && b != replayBotController);
                        foreach (var kicked in bots)
                        {
                            OnPlayerDisconnect(kicked, true);
                            Server.ExecuteCommand($"kickid {kicked.UserId}");
                            Utils.LogDebug($"Kicking unused bot on spawn... {kicked.PlayerName}");
                        }
                    });
                }
                else if (replayBotController != null && respawn)
                {
                    var bot = replayBotController;
                    OnPlayerDisconnect(bot, true);
                    Server.ExecuteCommand($"kickid {bot.UserId}");
                    replayBotController = null;
                    Utils.LogDebug($"Respawning bot, probably new record...");
                    _ = Task.Run(async () => await SpawnReplayBot());
                }
            });
        }

        public async Task<bool> CheckSRReplay(string topSteamID = "x", int bonusX = 0, int style = 0)
        {
            var (srSteamID, srPlayerName, srTime) = ("null", "null", "null");

            if (enableDb)
            {
                (srSteamID, srPlayerName, srTime) = await GetMapRecordSteamIDFromDatabase(bonusX);
            }
            else
            {
                (srSteamID, srPlayerName, srTime) = await GetMapRecordSteamID(bonusX);
            }

            if ((srSteamID == "null" || srPlayerName == "null" || srTime == "null") && topSteamID != "x") return false;

            string fileName = $"{(topSteamID == "x" ? $"{srSteamID}" : $"{topSteamID}")}_replay.json";
            string playerReplaysPath;
            if (style != 0) playerReplaysPath = Path.Join(gameDir, "csgo", "cfg", "SharpTimer", "PlayerReplayData", (bonusX == 0 ? currentMapName : $"{currentMapName}_bonus{bonusX}"), GetNamedStyle(style), fileName);
            else playerReplaysPath = Path.Join(gameDir, "csgo", "cfg", "SharpTimer", "PlayerReplayData", (bonusX == 0 ? currentMapName : $"{currentMapName}_bonus{bonusX}"), fileName);

            try
            {
                if (File.Exists(playerReplaysPath))
                {
                    var jsonString = await File.ReadAllTextAsync(playerReplaysPath);
                    if (!jsonString.Contains("PositionString"))
                    {
                        var indexedReplayFrames = JsonSerializer.Deserialize<List<IndexedReplayFrames>>(jsonString);

                        if (indexedReplayFrames != null)
                        {
                            return true;
                        }
                        return false;
                    }
                    else
                    {
                        return false;
                    }
                }
                else
                {
                    return false;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error during deserialization: {ex.Message}");
                return false;
            }
        }
    }
}