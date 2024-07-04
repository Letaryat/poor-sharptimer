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
using CounterStrikeSharp.API.Modules.Entities.Constants;
using CounterStrikeSharp.API.Modules.Memory;
using CounterStrikeSharp.API.Modules.Utils;

namespace SharpTimer
{
    public partial class SharpTimer
    {
        public void PlayerOnTick()
        {
            try
            {
                foreach (CCSPlayerController player in connectedPlayers.Values)
                {
                    if (player == null || !player.IsValid) continue;

                    var playerSlot = player.Slot;

                    if ((CsTeam)player.TeamNum == CsTeam.Spectator)
                    {
                        SpectatorOnTick(player);
                        continue;
                    }

                    if (playerTimers[playerSlot].IsAddingStartZone || playerTimers[playerSlot].IsAddingEndZone || playerTimers[playerSlot].IsAddingBonusStartZone || playerTimers[playerSlot].IsAddingBonusEndZone)
                    {
                        OnTickZoneTool(player);
                        continue;
                    }

                    if (playerTimers.TryGetValue(playerSlot, out PlayerTimerInfo? playerTimer) && IsAllowedPlayer(player))
                    {
                        if (!IsAllowedPlayer(player))
                        {
                            InvalidateTimer(player);
                            playerTimer.TicksSinceLastCmd++;
                            continue;
                        }

                        bool isOnBhopBlock = playerTimer.IsOnBhopBlock;
                        bool isTimerRunning = playerTimer.IsTimerRunning;
                        bool isBonusTimerRunning = playerTimer.IsBonusTimerRunning;
                        bool isTimerBlocked = playerTimer.IsTimerBlocked;
                        int timerTicks = playerTimer.TimerTicks;
                        PlayerButtons? playerButtons = player.Buttons;
                        Vector playerSpeed = player.PlayerPawn!.Value!.AbsVelocity;

                        bool keyEnabled = playerTimer.HideKeys != true && keysOverlayEnabled == true;
                        bool hudEnabled = playerTimer.HideTimerHud != true && hudOverlayEnabled == true;

                        string formattedPlayerVel = Math.Round(use2DSpeed ? playerSpeed.Length2D()
                                                                            : playerSpeed.Length())
                                                                            .ToString("0000");
                        string formattedPlayerPre = Math.Round(ParseVector(playerTimer.PreSpeed ?? "0 0 0").Length2D()).ToString("000");
                        string playerTime = FormatTime(timerTicks);
                        string playerBonusTime = FormatTime(playerTimer.BonusTimerTicks);
                        string timerLine = isBonusTimerRunning
                                            ? $" <font class='fontSize-s stratum-bold-italic' color='{tertiaryHUDcolor}'>Bonus #{playerTimer.BonusStage} Timer:</font> <font class='fontSize-l horizontal-center' color='{primaryHUDcolor}'>{playerBonusTime}</font> <br>"
                                            : isTimerRunning
                                                ? $" <font class='fontSize-s stratum-bold-italic' color='{tertiaryHUDcolor}'>Timer: </font><font class='fontSize-l horizontal-center' color='{primaryHUDcolor}'>{playerTime}</font> <font color='gray' class='fontSize-s stratum-bold-italic'>({GetPlayerPlacement(player)})</font>{((playerTimer.CurrentMapStage != 0 && useStageTriggers == true) ? $" <font color='gray' class='fontSize-s stratum-bold-italic'> {playerTimer.CurrentMapStage}/{stageTriggerCount}</font>" : "")} <br>"
                                                : playerTimer.IsReplaying
                                                    ? $" <font class='horizontal-center' color='red'>◉ REPLAY {FormatTime(playerReplays[playerSlot].CurrentPlaybackFrame)}</font> <br>"
                                                    : "";

                        string veloLine = $" {(playerTimer.IsTester ? playerTimer.TesterSmolGif : "")}<font class='fontSize-s stratum-bold-italic' color='{tertiaryHUDcolor}'>Speed:</font> {(playerTimer.IsReplaying ? "<font class=''" : "<font class='fontSize-l horizontal-center'")} color='{secondaryHUDcolor}'>{formattedPlayerVel}</font><font class='fontSize-s stratum-bold-italic' color='{tertiaryHUDcolor}'> u/s</font> <font class='fontSize-s stratum-bold-italic' color='gray'>({formattedPlayerPre} u/s)</font>{(playerTimer.IsTester ? playerTimer.TesterSmolGif : "")} <br>";

                        string infoLine = "";

                        if(playerTimer.CurrentZoneInfo.InBonusStartZone)
                        {
                            infoLine = GetBonusInfoLine(playerTimer);
                        }
                        else
                        {
                            infoLine = GetMainMapInfoLine(playerTimer);
                        }

                        string keysLineNoHtml = $"{(hudEnabled ? "<br>" : "")}<font class='fontSize-ml stratum-light-mono' color='{tertiaryHUDcolor}'>{((playerButtons & PlayerButtons.Moveleft) != 0 ? "A" : "_")} " +
                                                $"{((playerButtons & PlayerButtons.Forward) != 0 ? "W" : "_")} " +
                                                $"{((playerButtons & PlayerButtons.Moveright) != 0 ? "D" : "_")} " +
                                                $"{((playerButtons & PlayerButtons.Back) != 0 ? "S" : "_")} " +
                                                $"{((playerButtons & PlayerButtons.Jump) != 0 || playerTimer.MovementService!.OldJumpPressed ? "J" : "_")} " +
                                                $"{((playerButtons & PlayerButtons.Duck) != 0 ? "C" : "_")}";

                        if (!startzoneJumping && playerTimers[player.Slot].inStartzone)
                        {
                            if((playerButtons & PlayerButtons.Jump) != 0 || playerTimer.MovementService!.OldJumpPressed)
                            {
                                player!.Pawn.Value!.AbsVelocity.Z = 0f;
                            }
                        }

                        if (playerTimer.MovementService!.OldJumpPressed == true) playerTimer.MovementService.OldJumpPressed = false;

                        string hudContent = (hudEnabled ? timerLine + veloLine + infoLine : "") +
                                            (keyEnabled ? keysLineNoHtml : "") +
                                            ((playerTimer.IsTester && !playerTimer.IsReplaying) ? $"{(!keyEnabled ? "<br>" : "")}" + playerTimer.TesterBigGif : "") +
                                            ((playerTimer.IsVip && !playerTimer.IsTester && !playerTimer.IsReplaying) ? $"{(!keyEnabled ? "<br><br>" : "")}" + $"<br><img src='https://files.catbox.moe/{playerTimer.VipBigGif}.gif'><br>" : "") +
                                            ((playerTimer.IsReplaying && playerTimer.VipReplayGif != "x") ? playerTimer.VipReplayGif : "");

                        if (hudEnabled || keyEnabled)
                        {
                            player.PrintToCenterHtml(hudContent);
                        }

                        if (isTimerRunning)
                        {
                            playerTimer.TimerTicks++;
                        }
                        else if (isBonusTimerRunning)
                        {
                            playerTimer.BonusTimerTicks++;
                        }

                        if(playerTimer.currentStyle.Equals(4)) //check if 400vel
                        {
                            SetVelocity(player, player!.Pawn.Value!.AbsVelocity, 400);
                        }

                        if(playerTimer.currentStyle.Equals(10) && !player.PlayerPawn.Value.GroundEntity.IsValid) //check if ff
                        {
                            AddTimer(2.0f, () => { IncreaseVelocity(player); });
                        }

                        if (isOnBhopBlock)
                        {
                            playerTimer.TicksOnBhopBlock++;
                            if (playerTimer.TicksOnBhopBlock > bhopBlockTime)
                            {
                                RespawnPlayer(player);
                            }
                        }

                        if (useTriggers == false && isTimerBlocked == false)
                        {
                            CheckPlayerCoords(player, playerSpeed);
                        }
                        if (useTriggers == true && isTimerBlocked == false && useTriggersAndFakeZones == true)
                        {
                            CheckPlayerCoords(player, playerSpeed);
                        }

                        if (triggerPushFixEnabled == true)
                        {
                            CheckPlayerTriggerPushCoords(player, playerSpeed);
                        }

                        if (jumpStatsEnabled == true) OnJumpStatTick(player, playerSpeed, player.Pawn?.Value!.CBodyComponent?.SceneNode!.AbsOrigin!, player.PlayerPawn?.Value.EyeAngles!, playerButtons);

                        if (forcePlayerSpeedEnabled == true)
                        {
                            string designerName = player.Pawn!.Value!.WeaponServices!.ActiveWeapon?.Value?.DesignerName ?? "no_knife";
                            ForcePlayerSpeed(player, designerName);
                        }

                        if (playerTimer.IsRankPbCached == false)
                        {
                            var playerName = player.PlayerName;
                            var steamID = player.SteamID.ToString();
                            SharpTimerDebug($"{playerName} has rank and pb null... calling handler");
                            _ = Task.Run(async () => await RankCommandHandler(player, steamID, playerSlot, playerName, true, playerTimer.currentStyle));

                            playerTimer.IsRankPbCached = true;
                        }

                        //attempted bugfix on rank not appearing
                        if (playerTimer.CachedMapPlacement == null && !playerTimer.IsRankPbReallyCached)
                        {
                            var playerName = player.PlayerName;
                            var steamID = player.SteamID.ToString();
                            SharpTimerDebug($"{playerName} CachedMapPlacement is still null, calling rank handler once more");
                            AddTimer(3.0f, () => { _ = Task.Run(async () => await RankCommandHandler(player, steamID, playerSlot, playerName, true, playerTimer.currentStyle)); });                           
                            playerTimer.IsRankPbReallyCached = true;
                        }

                        if (playerTimer.changedStyle)
                        {
                            var playerName = player.PlayerName;
                            var steamID = player.SteamID.ToString();
                            _ = Task.Run(async () => await RankCommandHandler(player, steamID, playerSlot, playerName, true, playerTimer.currentStyle));                           
                            playerTimer.changedStyle = false;
                        }

                        if (hideAllPlayers == true)
                        {
                            foreach (var gun in player.PlayerPawn!.Value.WeaponServices!.MyWeapons)
                            {
                                if (gun.Value!.Render != Color.FromArgb(0, 255, 255, 255) ||
                                    gun.Value!.ShadowStrength != 0.0f)
                                {
                                    gun.Value!.Render = Color.FromArgb(0, 255, 255, 255);
                                    gun.Value!.ShadowStrength = 0.0f;
                                }
                            }
                        }

                        if (displayScoreboardTags == true)
                        {
                            if (playerTimer.TicksSinceLastRankUpdate > 511 &&
                            playerTimer.CachedRank != null &&
                            (player.Clan != null || !player.Clan!.Contains($"[{playerTimer.CachedRank}]")))
                            {
                                AddScoreboardTagToPlayer(player, playerTimer.CachedRank);
                                playerTimer.TicksSinceLastRankUpdate = 0;
                                SharpTimerDebug($"Setting Scoreboard Tag for {player.PlayerName} from TimerOnTick");
                            }
                        }

                        if (playerTimer.IsSpecTargetCached == false || specTargets.ContainsKey(player.Pawn!.Value!.EntityHandle.Index) == false)
                        {
                            specTargets[player.Pawn!.Value!.EntityHandle.Index] = new CCSPlayerController(player.Handle);
                            playerTimer.IsSpecTargetCached = true;
                            SharpTimerDebug($"{player.PlayerName} was not in specTargets, adding...");
                        }

                        if (removeCollisionEnabled == true)
                        {
                            if (player.PlayerPawn!.Value.Collision.CollisionGroup != (byte)CollisionGroup.COLLISION_GROUP_DISSOLVING || player.PlayerPawn.Value.Collision.CollisionAttribute.CollisionGroup != (byte)CollisionGroup.COLLISION_GROUP_DISSOLVING)
                            {
                                SharpTimerDebug($"{player.PlayerName} has wrong collision group... RemovePlayerCollision");
                                RemovePlayerCollision(player);
                            }
                        }

                        if (removeCrouchFatigueEnabled == true)
                        {
                            if (playerTimer.MovementService != null && playerTimer.MovementService.DuckSpeed != 7.0f)
                            {
                                playerTimer.MovementService.DuckSpeed = 7.0f;
                            }
                        }

                        if (hideAllPlayers == true)
                        {
                            if (player.PlayerPawn!.Value.Render != Color.FromArgb(0, 0, 0, 0))
                            {
                                player.PlayerPawn.Value.Render = Color.FromArgb(0, 0, 0, 0);
                                Utilities.SetStateChanged(player.PlayerPawn.Value, "CBaseModelEntity", "m_clrRender");
                            }
                        }

                        if (((PlayerFlags)player.Pawn.Value.Flags & PlayerFlags.FL_ONGROUND) != PlayerFlags.FL_ONGROUND)
                        {
                            playerTimer.TicksInAir++;
                            if (playerTimer.TicksInAir == 1)
                            {
                                playerTimer.PreSpeed = $"{playerSpeed.X} {playerSpeed.Y} {playerSpeed.Z}";
                            }
                        }
                        else
                        {
                            playerTimer.TicksInAir = 0;
                        }

                        if (enableReplays)
                        {
                            if (!playerTimer.IsReplaying && (timerTicks > 0 || playerTimer.BonusTimerTicks > 0) && playerTimer.IsRecordingReplay && !isTimerBlocked)
                            {
                                ReplayUpdate(player, timerTicks);
                            }

                            if (playerTimer.IsReplaying && !playerTimer.IsRecordingReplay && isTimerBlocked)
                            {
                                ReplayPlay(player);
                            }
                            else
                            {
                                if (!isTimerBlocked && (player.PlayerPawn!.Value.MoveType == MoveType_t.MOVETYPE_OBSERVER || player.PlayerPawn.Value.ActualMoveType == MoveType_t.MOVETYPE_OBSERVER)) SetMoveType(player, MoveType_t.MOVETYPE_WALK);
                            }
                        }

                        if (playerTimer.TicksSinceLastCmd < cmdCooldown) playerTimer.TicksSinceLastCmd++;
                        if (playerTimer.TicksSinceLastRankUpdate < 511) playerTimer.TicksSinceLastRankUpdate++;
                    }
                }
            }
            catch (Exception ex)
            {
                if (ex.Message != "Invalid game event") SharpTimerError($"Error in TimerOnTick: {ex.StackTrace}");
            }
        }

        private string GetMainMapInfoLine(PlayerTimerInfo playerTimer)
        {
           return !playerTimer.IsReplaying
                     ? $"<font class='fontSize-s stratum-bold-italic' color='gray'>" +
                     $"🏆 {playerTimer.CachedPB} " + 
                     $"({playerTimer.CachedMapPlacement}) | </font>" + 
                     $"{playerTimer.RankHUDIcon} <font class='fontSize-s stratum-bold-italic' color='gray'>" +
                     $" | {GetNamedStyle(playerTimer.currentStyle)}" +
                     $"{(currentMapTier != null ? $" | Tier: {currentMapTier}" : "")}" +
                     $"{(currentMapType != null ? $" | {currentMapType}" : "")}" +
                     $"{((currentMapType == null && currentMapTier == null) ? $" | {currentMapName} " : "")} </font>"
                   : $" <font class='fontSize-s stratum-bold-italic' color='gray'>{playerTimer.ReplayHUDString}</font>";
        }

        private string GetBonusInfoLine(PlayerTimerInfo playerTimer)
        {
            var currentBonusNumber = playerTimer.CurrentZoneInfo.CurrentBonusNumber;

            if (currentBonusNumber != 0)
            {
                var cachedBonusInfo = playerTimer.CachedBonusInfo.FirstOrDefault(x => x.Key == currentBonusNumber);

                return !playerTimer.IsReplaying
                        ? $"<font class='fontSize-s stratum-bold-italic' color='gray'>" +
                          $"🏆 {(cachedBonusInfo.Value != null ? $"{FormatTime(cachedBonusInfo.Value.PbTicks)}" : "Unranked")}" +
                          $"{(cachedBonusInfo.Value != null ? $" ({cachedBonusInfo.Value.Placement})" : "")}</font>" +
                          $"<font class='fontSize-s stratum-bold-italic' color='gray'>" +
                          $" | {GetNamedStyle(playerTimer.currentStyle)}" +
                          $" | Bonus #{currentBonusNumber} </font>"
                        : $" <font class='fontSize-s stratum-bold-italic' color='gray'>{playerTimer.ReplayHUDString}</font>";
            }
            else
            {
                return GetMainMapInfoLine(playerTimer);
            }
        }

        public void SpectatorOnTick(CCSPlayerController player)
        {
            if (!IsAllowedSpectator(player)) return;

            try
            {
                var target = specTargets[player.Pawn.Value!.ObserverServices!.ObserverTarget.Index];
                if (playerTimers.TryGetValue(target.Slot, out PlayerTimerInfo? playerTimer) && IsAllowedPlayer(target))
                {
                    bool isTimerRunning = playerTimer.IsTimerRunning;
                    bool isBonusTimerRunning = playerTimer.IsBonusTimerRunning;
                    int timerTicks = playerTimer.TimerTicks;
                    PlayerButtons? playerButtons = target.Buttons;
                    Vector playerSpeed = target.PlayerPawn!.Value!.AbsVelocity;

                    bool keyEnabled = playerTimer.HideKeys != true && playerTimer.IsReplaying != true && keysOverlayEnabled == true;
                    bool hudEnabled = playerTimer.HideTimerHud != true && hudOverlayEnabled == true;

                    string formattedPlayerVel = Math.Round(use2DSpeed ? playerSpeed.Length2D()
                                                                        : playerSpeed.Length())
                                                                        .ToString("0000");
                    string formattedPlayerPre = Math.Round(ParseVector(playerTimer.PreSpeed ?? "0 0 0").Length2D()).ToString("000");
                    string playerTime = FormatTime(timerTicks);
                    string playerBonusTime = FormatTime(playerTimer.BonusTimerTicks);
                    string timerLine = isBonusTimerRunning
                                        ? $" <font class='fontSize-s stratum-bold-italic' color='{tertiaryHUDcolor}'>Bonus #{playerTimer.BonusStage} Timer:</font> <font class='fontSize-l horizontal-center' color='{primaryHUDcolor}'>{playerBonusTime}</font> <br>"
                                        : isTimerRunning
                                            ? $" <font class='fontSize-s stratum-bold-italic' color='{tertiaryHUDcolor}'>Timer: </font><font class='fontSize-l horizontal-center' color='{primaryHUDcolor}'>{playerTime}</font> <font color='gray' class='fontSize-s stratum-bold-italic'>({GetPlayerPlacement(target)})</font>{((playerTimer.CurrentMapStage != 0 && useStageTriggers == true) ? $" <font color='gray' class='fontSize-s stratum-bold-italic'> {playerTimer.CurrentMapStage}/{stageTriggerCount}</font>" : "")} <br>"
                                            : playerTimer.IsReplaying
                                                ? $" <font class='horizontal-center' color='red'>◉ REPLAY {FormatTime(playerReplays[target.Slot].CurrentPlaybackFrame)}</font> <br>"
                                                : "";

                    string veloLine = $" {(playerTimer.IsTester ? playerTimer.TesterSmolGif : "")}<font class='fontSize-s stratum-bold-italic' color='{tertiaryHUDcolor}'>Speed:</font> {(playerTimer.IsReplaying ? "<font class=''" : "<font class='fontSize-l horizontal-center'")} color='{secondaryHUDcolor}'>{formattedPlayerVel}</font><font class='fontSize-s stratum-bold-italic' color='{tertiaryHUDcolor}'> u/s</font> <font class='fontSize-s stratum-bold-italic' color='gray'>({formattedPlayerPre} u/s)</font>{(playerTimer.IsTester ? playerTimer.TesterSmolGif : "")} <br>";

                    string infoLine = "";

                    if (playerTimer.CurrentZoneInfo.InBonusStartZone)
                    {
                        infoLine = GetBonusInfoLine(playerTimer);
                    }
                    else
                    {
                        infoLine = GetMainMapInfoLine(playerTimer);
                    }

                    string keysLineNoHtml = $"{(hudEnabled ? "<br>" : "")}<font class='fontSize-ml stratum-bold-mono' color='{tertiaryHUDcolor}'>{((playerButtons & PlayerButtons.Moveleft) != 0 ? "A" : "_")} " +
                                            $"{((playerButtons & PlayerButtons.Forward) != 0 ? "W" : "_")} " +
                                            $"{((playerButtons & PlayerButtons.Moveright) != 0 ? "D" : "_")} " +
                                            $"{((playerButtons & PlayerButtons.Back) != 0 ? "S" : "_")} " +
                                            $"{((playerButtons & PlayerButtons.Jump) != 0 || playerTimer.MovementService!.OldJumpPressed ? "J" : "_")} " +
                                            $"{((playerButtons & PlayerButtons.Duck) != 0 ? "C" : "_")}";

                    if (playerTimer.MovementService!.OldJumpPressed == true) playerTimer.MovementService.OldJumpPressed = false;

                    string hudContent = (hudEnabled ? timerLine + veloLine + infoLine : "") +
                                        (keyEnabled ? keysLineNoHtml : "") +
                                        ((playerTimer.IsTester && !playerTimer.IsReplaying) ? playerTimer.TesterBigGif : "") +
                                        ((playerTimer.IsVip && !playerTimer.IsTester && !playerTimer.IsReplaying) ? $"<br><img src='https://files.catbox.moe/{playerTimer.VipBigGif}.gif'><br>" : "") +
                                        ((playerTimer.IsReplaying && playerTimer.VipReplayGif != "x") ? playerTimer.VipReplayGif : "");

                    if (hudEnabled || keyEnabled)
                    {
                        player.PrintToCenterHtml(hudContent);
                    }
                }
            }
            catch (Exception ex)
            {
                if (ex.Message != "Invalid game event") SharpTimerError($"Error in SpectatorOnTick: {ex.Message}");
            }
        }
    }
}