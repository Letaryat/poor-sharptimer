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
using System.Runtime.Serialization.Formatters.Binary;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;
using CounterStrikeSharp.API.Modules.Entities.Constants;
using System.Text.Json;
using FixVectorLeak;
using CounterStrikeSharp.API.Modules.Timers;

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
                ReplayVector currentPosition =
                    ReplayVector.GetVectorish(player.Pawn.Value!.CBodyComponent?.SceneNode?.AbsOrigin ?? new(0, 0, 0));
                ReplayVector currentSpeed = ReplayVector.GetVectorish(player.PlayerPawn.Value!.AbsVelocity);
                ReplayQAngle currentRotation = ReplayQAngle.GetQAngleish(player.PlayerPawn.Value.EyeAngles);

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

                    player.PlayerPawn.Value!.Teleport(ReplayVector.ToVector(replayFrame.Position!),
                        ReplayQAngle.ToQAngle(replayFrame.Rotation!), ReplayVector.ToVector(replayFrame.Speed!));

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

                if (playerReplays[player.Slot].CurrentPlaybackFrame < 0 ||
                    playerReplays[player.Slot].CurrentPlaybackFrame >= totalFrames)
                {
                    playerReplays[player.Slot].CurrentPlaybackFrame = 0;
                    Action<CCSPlayerController?, float, bool> adjustVelocity =
                        use2DSpeed ? AdjustPlayerVelocity2D : AdjustPlayerVelocity;
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

        public async Task DumpReplayToJson(CCSPlayerController player, string steamID, int slot, int bonusX = 0,
            int style = 0, string mode = "")
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
                playerReplaysDirectory = Path.Join(gameDir, "csgo", "cfg", "SharpTimer", "PlayerReplayData",
                    bonusX == 0 ? $"{currentMapName}" : $"{currentMapName}_bonus{bonusX}", GetNamedStyle(style), mode);
                string playerReplaysPath = Path.Join(playerReplaysDirectory, fileName);

                try
                {
                    if (!Directory.Exists(playerReplaysDirectory))
                    {
                        Directory.CreateDirectory(playerReplaysDirectory);
                    }

                    if (playerReplays[slot].replayFrames.Count >= maxReplayFrames) return;

                    var indexedReplayFrames = playerReplays[slot].replayFrames
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

        public async Task DumpReplayToBinary(CCSPlayerController player, string steamID, int playerSlot, int bonusX = 0,
            int style = 0, string mode = "")
        {
            await Task.Run(() =>
            {
                if (!IsAllowedPlayer(player))
                {
                    Utils.LogError($"Error in DumpReplayToBinary: Player not allowed or not on server anymore");
                    return;
                }

                string fileName = $"{steamID}_replay.dat";
                string playerReplaysDirectory;
                playerReplaysDirectory = Path.Join(gameDir, "csgo", "cfg", "SharpTimer", "PlayerReplayData",
                    bonusX == 0 ? $"{currentMapName}" : $"{currentMapName}_bonus{bonusX}", GetNamedStyle(style), mode);
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

                    using Stream stream = new FileStream(playerReplaysPath, FileMode.Create);
                    BinaryWriter writer = new BinaryWriter(stream);

                    writer.Write(REPLAY_VERSION);

                    foreach (var frame in indexedReplayFrames)
                    {
                        writer.Write(frame.Frame.Position!.X);
                        writer.Write(frame.Frame.Position!.Y);
                        writer.Write(frame.Frame.Position!.Z);
                        writer.Write(frame.Frame.Rotation!.Pitch);
                        writer.Write(frame.Frame.Rotation!.Yaw);
                        writer.Write(frame.Frame.Rotation!.Roll);
                        writer.Write(frame.Frame.Speed!.X);
                        writer.Write(frame.Frame.Speed!.Y);
                        writer.Write(frame.Frame.Speed!.Z);
                        writer.Write((int)frame.Frame.Buttons);
                        writer.Write((int)frame.Frame.Flags);
                        writer.Write((int)frame.Frame.MoveType);
                    }
                }
                catch (Exception ex)
                {
                    Utils.LogError($"Error during serialization: {ex.Message}");
                }
            });
        }

        public string SerializeFrameToBinaryString(List<IndexedReplayFrames> frames)
        {
            using Stream stream = new MemoryStream();
            BinaryWriter writer = new BinaryWriter(stream);

            writer.Write(REPLAY_VERSION);

            foreach (var frame in frames)
            {
                writer.Write(frame.Frame.Position!.X);
                writer.Write(frame.Frame.Position!.Y);
                writer.Write(frame.Frame.Position!.Z);
                writer.Write(frame.Frame.Rotation!.Pitch);
                writer.Write(frame.Frame.Rotation!.Yaw);
                writer.Write(frame.Frame.Rotation!.Roll);
                writer.Write(frame.Frame.Speed!.X);
                writer.Write(frame.Frame.Speed!.Y);
                writer.Write(frame.Frame.Speed!.Z);
                writer.Write((int)frame.Frame.Buttons);
                writer.Write((int)frame.Frame.Flags);
                writer.Write((int)frame.Frame.MoveType);
            }

            var memoryStream = (MemoryStream)stream;
            return Convert.ToBase64String(memoryStream.ToArray());
        }

        public string GetReplayBinary(CCSPlayerController player, int slot)
        {
            if (!IsAllowedPlayer(player))
            {
                Utils.LogError($"Error in GetReplayJson: Player not allowed or not on server anymore");
                return "";
            }

            try
            {
                if (playerReplays[slot].replayFrames.Count >= maxReplayFrames) return "";

                var indexedReplayFrames = playerReplays[slot].replayFrames
                    .Select((frame, index) => new IndexedReplayFrames { Index = index, Frame = frame })
                    .ToList();

                return SerializeFrameToBinaryString(indexedReplayFrames);
            }
            catch (Exception ex)
            {
                Utils.LogError($"Error during serialization: {ex.Message}");
                return "";
            }
        }

        private async Task ReadReplayFromJson(CCSPlayerController player, string steamId, int slot, int bonusX = 0,
            int style = 0, string mode = "")
        {
            string fileName = $"{steamId}_replay.json";
            string playerReplaysPath;
            playerReplaysPath = Path.Join(gameDir, "csgo", "cfg", "SharpTimer", "PlayerReplayData",
                bonusX == 0 ? currentMapName : $"{currentMapName}_bonus{bonusX}", GetNamedStyle(style), mode, fileName);
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

                            if (!playerReplays.TryGetValue(slot, out PlayerReplays? value))
                            {
                                value = new PlayerReplays();
                                playerReplays[slot] = value;
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

        private async Task ReadReplayFromBinary(CCSPlayerController player, string steamId, int playerSlot,
            int bonusX = 0, int style = 0, string mode = "")
        {
            string fileName = $"{steamId}_replay.dat";
            string playerReplaysPath;
            playerReplaysPath = Path.Join(gameDir, "csgo", "cfg", "SharpTimer", "PlayerReplayData",
                bonusX == 0 ? currentMapName : $"{currentMapName}_bonus{bonusX}", GetNamedStyle(style), mode, fileName);

            try
            {
                if (!File.Exists(playerReplaysPath))
                {
                    Utils.LogError($"File does not exist: {playerReplaysPath}");
                    Server.NextFrame(() => Utils.PrintToChat(player, Localizer["replay_dont_exist"]));
                    return;
                }

                using Stream stream = new FileStream(playerReplaysPath, FileMode.Open);
                BinaryReader reader = new BinaryReader(stream);

                var version = reader.ReadInt32();
                if (version != REPLAY_VERSION)
                {
                    Utils.LogError($"Unsupported replay version: {version}");
                    Server.NextFrame(() => Utils.PrintToChat(player, $"Unsupported replay version: {version}"));
                    return;
                }

                var replayFrames = new List<PlayerReplays.ReplayFrames>();
                await Server.NextFrameAsync(() =>
                {
                    while (reader.BaseStream.Position != reader.BaseStream.Length)
                    {
                        var position = new Vector(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle());
                        var rotation = new QAngle(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle());
                        var speed = new Vector(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle());
                        var buttons = (PlayerButtons)reader.ReadInt32();
                        var flags = (uint)reader.ReadInt32();
                        var moveType = (MoveType_t)reader.ReadInt32();

                        replayFrames.Add(new PlayerReplays.ReplayFrames
                        {
                            Position = ReplayVector.GetVectorish(position),
                            Rotation = ReplayQAngle.GetQAngleish(rotation),
                            Speed = ReplayVector.GetVectorish(speed),
                            Buttons = buttons,
                            Flags = flags,
                            MoveType = moveType
                        });
                    }
                });

                if (!playerReplays.TryGetValue(playerSlot, out PlayerReplays? value))
                {
                    value = new PlayerReplays();
                    playerReplays[playerSlot] = value;
                }

                value.replayFrames = replayFrames;
            }
            catch (Exception ex)
            {
                Utils.LogError($"Error during deserialization: {ex.Message}");
            }
        }

        private async Task ReadReplayFromGlobal(CCSPlayerController player, int recordId, string mode, int bonusX = 0)
        {
            var payload = new
            {
                record_id = recordId,
                map_id = mapCache.MapID,
                mode,
                bonus = bonusX
            };

            try
            {
                byte[] replayData = Convert.FromBase64String(await GetReplayFromGlobal(payload));
                using Stream stream = new MemoryStream(replayData);
                using BinaryReader reader = new BinaryReader(stream);

                var version = reader.ReadInt32();
                if (version != REPLAY_VERSION)
                {
                    Utils.LogError($"Unsupported replay version: {version}");
                    Server.NextFrame(() => Utils.PrintToChat(player, $"Unsupported replay version: {version}"));
                    return;
                }

                var replayFrames = new List<PlayerReplays.ReplayFrames>();
                await Server.NextFrameAsync(() =>
                {
                    while (reader.BaseStream.Position != reader.BaseStream.Length)
                    {
                        var position = new Vector(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle());
                        var rotation = new QAngle(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle());
                        var speed = new Vector(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle());
                        var buttons = (PlayerButtons)reader.ReadInt32();
                        var flags = (uint)reader.ReadInt32();
                        var moveType = (MoveType_t)reader.ReadInt32();

                        replayFrames.Add(new PlayerReplays.ReplayFrames
                        {
                            Position = ReplayVector.GetVectorish(position),
                            Rotation = ReplayQAngle.GetQAngleish(rotation),
                            Speed = ReplayVector.GetVectorish(speed),
                            Buttons = buttons,
                            Flags = flags,
                            MoveType = moveType
                        });
                    }
                });

                if (!playerReplays.TryGetValue(player.Slot, out PlayerReplays? value))
                {
                    value = new PlayerReplays();
                    playerReplays[player.Slot] = value;
                }

                value.replayFrames = replayFrames!;
            }
            catch (Exception ex)
            {
                Utils.LogError($"Error during deserialization: {ex.Message}");
            }
        }

        private async Task SpawnReplayBot()
        {
            if (!await CheckSRReplay("x", 0, 0, GetModeName(defaultMode)))
            {
                Utils.LogError("Replay check failed, not spawning bot.");
                return;
            }

            Server.NextFrame(() =>
            {
                AddTimer(3.0f, () =>
                {
                    Server.ExecuteCommand("bot_quota_mode normal");
                    Server.ExecuteCommand("bot_quota 0");
                    Server.ExecuteCommand("bot_chatter off");
                    Server.ExecuteCommand("bot_controllable 0");
                    Server.ExecuteCommand("bot_kick");
                    replayBotController = null;

                    AddTimer(3.0f, () =>
                    {
                        // wtf is this game even
                        Server.ExecuteCommand("bot_quota 1");
                        Server.ExecuteCommand("bot_add_ct");
                        Server.ExecuteCommand("bot_quota 1");

                        Utils.LogDebug("Searching for replay bot...");

                        AddTimer(0.0f, () =>
                        {
                            // find and setup bot
                            var bot = Utilities.GetPlayers().Where(b => b.IsBot && !b.IsHLTV).FirstOrDefault();
                            if (bot != null)
                            {
                                replayBotController = bot;
                                Utils.LogDebug($"Found replay bot: {bot.PlayerName}");

                                var botPlayerPawn = bot.PlayerPawn();
                                if (botPlayerPawn == null) return;

                                // bot settings
                                bot.RemoveWeapons();
                                botPlayerPawn.Bot!.IsStopping = true;
                                botPlayerPawn.Bot.IsSleeping = true;
                                botPlayerPawn.Bot.AllowActive = true;

                                // start bot replay
                                OnPlayerConnect(bot, true);
                                ChangePlayerName(bot, replayBotName);
                                playerTimers[bot.Slot].IsTimerBlocked = true;
                                _ = Task.Run(async () =>
                                    await ReplayHandler(bot, bot.Slot, "1", "69", "unknown", 0, 0, false, GetModeName(defaultMode)));
                                Utils.LogDebug($"Starting replay for {bot.PlayerName}");
                            }
                            else
                            {
                                Utils.LogError($"Failed to spawn replay bot");
                                return;
                            }

                            // kick unused bots if there are any
                            var bots = Utilities.GetPlayers()
                                .Where(b => b.IsBot && !b.IsHLTV && b != replayBotController);
                            foreach (var kicked in bots)
                            {
                                OnPlayerDisconnect(kicked, true);
                                Server.ExecuteCommand("bot_quota 1");
                                Server.ExecuteCommand($"kickid {kicked.UserId}");
                                Server.ExecuteCommand("bot_quota 1");
                                Utils.LogDebug($"Kicking unused bot on spawn... {kicked.PlayerName}");
                            }
                        });
                    }, TimerFlags.STOP_ON_MAPCHANGE);
                }, TimerFlags.STOP_ON_MAPCHANGE);
            });
        }

        public async Task<bool> CheckSRReplay(string topSteamID = "x", int bonusX = 0, int style = 0, string mode = "")
        {
            var (srSteamID, srPlayerName, srTime) = ("null", "null", 0);

            if (topSteamID == "x")
            {
                if (enableDb)
                    (srSteamID, srPlayerName, srTime) = await GetMapRecordSteamIDFromDatabase(bonusX, 0, style, mode);
                else
                    (srSteamID, srPlayerName, srTime) = await GetMapRecordSteamID(bonusX);
            }
            
            string ext = useBinaryReplays ? "dat" : "json";
            string fileName = $"{(topSteamID == "x" ? $"{srSteamID}" : $"{topSteamID}")}_replay.{ext}";
            string playerReplaysPath;
            playerReplaysPath = Path.Join(gameDir, "csgo", "cfg", "SharpTimer", "PlayerReplayData",
                (bonusX == 0 ? currentMapName : $"{currentMapName}_bonus{bonusX}"), GetNamedStyle(style), mode,
                fileName);
            try
            {
                if (File.Exists(playerReplaysPath))
                {
                    if (useBinaryReplays)
                    {
                        using var reader = new BinaryReader(File.Open(playerReplaysPath, FileMode.Open));
                        var version = reader.ReadInt32();
                        return version == REPLAY_VERSION;
                    }

                    var jsonString = await File.ReadAllTextAsync(playerReplaysPath);
                    if (!jsonString.Contains("PositionString"))
                    {
                        var indexedReplayFrames = JsonSerializer.Deserialize<List<IndexedReplayFrames>>(jsonString);

                        if (indexedReplayFrames != null)
                        {
                            return true;
                        }
                    }
                }
                return false;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error during deserialization: {ex.Message}");
                return false;
            }
        }
    }
}