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
using CounterStrikeSharp.API.Modules.Entities.Constants;
using CounterStrikeSharp.API.Modules.Utils;
using FixVectorLeak;

namespace SharpTimer
{
    public partial class SharpTimer
    {
        public void PlayerOnTick()
        {
            try
            {
                int currentTick = Server.TickCount;

                foreach (CCSPlayerController player in connectedPlayers.Values)
                {
                    if (player == null || !player.IsValid) continue;

                    int slot = player.Slot;
                    string playerName = player.PlayerName;
                    string steamID = player.SteamID.ToString();

                    if ((CsTeam)player.TeamNum == CsTeam.Spectator || !player.PawnIsAlive)
                    {
                        if (currentTick % (64 / hudTickrate) != 0)
                            continue;

                        SpectatorOnTick(player);
                        continue;
                    }

                    if (playerTimers.TryGetValue(slot, out PlayerTimerInfo? playerTimer))
                    {
                        if (playerTimer.IsAddingStartZone || playerTimer.IsAddingEndZone || playerTimer.IsAddingBonusStartZone || playerTimer.IsAddingBonusEndZone)
                        {
                            OnTickZoneTool(player);
                            continue;
                        }

                        if (!IsAllowedPlayer(player))
                        {
                            InvalidateTimer(player);
                            continue;
                        }

                        var playerPawn = player.PlayerPawn?.Value;
                        if (playerPawn == null) continue;

                        PlayerButtons? playerButtons = player.Buttons;
                        Vector_t playerSpeed = playerPawn.AbsVelocity.ToVector_t();
                        bool isTimerBlocked = playerTimer.IsTimerBlocked;

                        /* afk */
                        if (connectedAFKPlayers.ContainsKey(player.Slot))
                        {
                            if (!playerSpeed.IsZero())
                            {
                                connectedAFKPlayers.Remove(player.Slot);
                                playerTimer.AFKWarned = false;
                                playerTimer.AFKTicks = 0;
                            }
                            else continue;
                        }

                        if (playerTimer.AFKTicks >= afkSeconds*48 && !playerTimer.AFKWarned && afkWarning)
                        {
                            Utils.PrintToChat(player, $"{Localizer["afk_message"]}");
                            playerTimer.AFKWarned = true;
                        }
                            
                        if (playerTimer.AFKTicks >= afkSeconds*64)
                            connectedAFKPlayers[player.Slot] = connectedPlayers[player.Slot];

                        if (playerSpeed.IsZero())
                            playerTimer.AFKTicks++;
                        else
                            playerTimer.AFKTicks = 0;
                        /* afk */

                        /* timer counting */
                        bool isTimerRunning = playerTimer.IsTimerRunning;
                        bool isBonusTimerRunning = playerTimer.IsBonusTimerRunning;

                        if (isTimerRunning)
                        {
                            playerTimer.TimerTicks++;

                            if (useStageTriggers)
                                playerTimer.StageTicks++;
                        }
                        else if (isBonusTimerRunning)
                        {
                            playerTimer.BonusTimerTicks++;
                        }
                        /* timer counting */

                        // remove jumping in startzone
                        if (!startzoneJumping && playerTimers[player.Slot].inStartzone)
                        {
                            if((playerButtons & PlayerButtons.Jump) != 0 || playerTimer.MovementService!.OldJumpPressed)
                                playerPawn.AbsVelocity.Z = 0f;
                        }

                        /* hide weapons */
                        bool hasWeapons = playerPawn.WeaponServices?.MyWeapons?.Count > 0;
                        if (playerTimer.HideWeapon)
                        {
                            if (hasWeapons)
                            {
                                player.RemoveWeapons();
                                playerTimer.GivenWeapon = false;
                            }
                        }
                        else
                        {
                            if (!hasWeapons && !playerTimer.GivenWeapon)
                            {
                                if (player.Team == CsTeam.Terrorist)
                                {
                                    player.GiveNamedItem("weapon_knife_t");
                                    player.GiveNamedItem("weapon_glock");
                                }
                                else if (player.Team == CsTeam.CounterTerrorist)
                                {
                                    player.GiveNamedItem("weapon_knife");
                                    player.GiveNamedItem("weapon_usp_silencer");
                                }

                                playerTimer.GivenWeapon = true;
                            }
                        }
                        /* hide weapons */

                        /* styles */
                        if (playerTimer.currentStyle.Equals(4)) //check if 400vel
                            SetVelocity(player, playerPawn.AbsVelocity.ToVector_t(), 400);

                        if (playerTimer.currentStyle.Equals(10) && !playerPawn.GroundEntity.IsValid && currentTick % 2 != 0) //check if ff
                            IncreaseVelocity(player);

                        if (playerTimer.changedStyle)
                        {
                            _ = Task.Run(async () => await RankCommandHandler(player, steamID, slot, playerName, true, playerTimer.currentStyle));
                            playerTimer.changedStyle = false;
                        }
                        /* styles */

                        // respawn player if on bhop block too long
                        bool isOnBhopBlock = playerTimer.IsOnBhopBlock;
                        if (isOnBhopBlock)
                        {
                            playerTimer.TicksOnBhopBlock++;

                            if (playerTimer.TicksOnBhopBlock > bhopBlockTime)
                                RespawnPlayer(player);
                        }

                        /* checking if player in zones */
                        if (useTriggers == false && isTimerBlocked == false)
                            CheckPlayerCoords(player, playerSpeed);

                        // idk why there is another one but replay breaks with them merged into one :D
                        if (useTriggers == true && isTimerBlocked == false && useTriggersAndFakeZones)
                            CheckPlayerCoords(player, playerSpeed);
                        /* checking if player in zones */

                        /* hud strafe sync % */
                        if (StrafeHudEnabled)
                            OnSyncTick(player, playerButtons, playerPawn.EyeAngles!);

                        // reset in startzone
                        if (StrafeHudEnabled && playerTimer.inStartzone && playerTimer.Rotation.Count > 0) 
                        { 
                            playerTimer.Sync = 100.00f;
                            playerTimer.Rotation.Clear();
                        }
                        /* hud strafe sync % */

                        if (forcePlayerSpeedEnabled)
                        {
                            string designerName = playerPawn.WeaponServices!.ActiveWeapon?.Value?.DesignerName ?? "no_knife";
                            ForcePlayerSpeed(player, designerName);
                        }

                        /* ranks */
                        if (playerTimer.IsRankPbCached == false)
                        {
                            Utils.LogDebug($"{playerName} has rank and pb null... calling handler");
                            _ = Task.Run(async () => await RankCommandHandler(player, steamID, slot, playerName, true, playerTimer.currentStyle));

                            playerTimer.IsRankPbCached = true;
                        }

                        // attempted bugfix on rank not appearing
                        if (playerTimer.CachedMapPlacement == null && !playerTimer.IsRankPbReallyCached)
                        {
                            Utils.LogDebug($"{playerName} CachedMapPlacement is still null, calling rank handler once more");
                            AddTimer(3.0f, () => { _ = Task.Run(async () => await RankCommandHandler(player, steamID, slot, playerName, true, playerTimer.currentStyle)); });                           
                            playerTimer.IsRankPbReallyCached = true;
                        }

                        if (displayScoreboardTags || displayChatTags)
                        {
                            if (playerTimer.TicksSinceLastRankUpdate > 511 && playerTimer.CachedRank != null)
                            {
                                AddRankTagToPlayer(player, playerTimer.CachedRank);
                                playerTimer.TicksSinceLastRankUpdate = 0;
                                Utils.LogDebug($"Setting Scoreboard/Chat Tag for {player.PlayerName} from TimerOnTick");
                            }
                        }

                        if (playerTimer.TicksSinceLastRankUpdate < 511)
                            playerTimer.TicksSinceLastRankUpdate++;
                        /* ranks */

                        if (playerTimer.IsSpecTargetCached == false || specTargets.ContainsKey(playerPawn.EntityHandle.Index) == false)
                        {
                            specTargets[playerPawn.EntityHandle.Index] = new CCSPlayerController(player.Handle);
                            playerTimer.IsSpecTargetCached = true;
                            Utils.LogDebug($"{playerName} was not in specTargets, adding...");
                        }

                        if (removeCollisionEnabled)
                        {
                            if (playerPawn.Collision.CollisionGroup != (byte)CollisionGroup.COLLISION_GROUP_DISSOLVING ||
                                playerPawn.Collision.CollisionAttribute.CollisionGroup != (byte)CollisionGroup.COLLISION_GROUP_DISSOLVING
                                )
                            {
                                Utils.LogDebug($"{playerName} has wrong collision group... RemovePlayerCollision");
                                RemovePlayerCollision(player);
                            }
                        }

                        if (removeCrouchFatigueEnabled)
                        {
                            if (playerTimer.MovementService != null && playerTimer.MovementService.DuckSpeed != 7.0f)
                                playerTimer.MovementService.DuckSpeed = 7.0f;
                        }

                        // update pre speed for hud
                        if (((PlayerFlags)playerPawn.Flags & PlayerFlags.FL_ONGROUND) != PlayerFlags.FL_ONGROUND)
                        {
                            playerTimer.TicksInAir++;

                            if (playerTimer.TicksInAir == 1)
                                playerTimer.PreSpeed = $"{playerSpeed.X} {playerSpeed.Y} {playerSpeed.Z}";
                        }
                        else playerTimer.TicksInAir = 0;

                        // replays
                        if (enableReplays)
                        {
                            int timerTicks = playerTimer.TimerTicks;
                            if (!playerTimer.IsReplaying && (timerTicks > 0 || playerTimer.BonusTimerTicks > 0) && playerTimer.IsRecordingReplay && !isTimerBlocked)
                                ReplayUpdate(player, timerTicks);

                            if (playerTimer.IsReplaying && !playerTimer.IsRecordingReplay && isTimerBlocked)
                                ReplayPlay(player);

                            else if (
                                !isTimerBlocked &&
                                (playerPawn.MoveType.HasFlag(MoveType_t.MOVETYPE_OBSERVER) || playerPawn.ActualMoveType.HasFlag(MoveType_t.MOVETYPE_OBSERVER)) &&
                                !playerPawn.MoveType.HasFlag(MoveType_t.MOVETYPE_LADDER))
                            {
                                SetMoveType(player, MoveType_t.MOVETYPE_WALK);
                            }
                        }

                        // timer hud content
                        if (currentTick % (64 / hudTickrate) != 0)
                            continue;

                        string hudContent = GetHudContent(playerTimer, player);

                        if (!string.IsNullOrEmpty(hudContent))
                            player.PrintToCenterHtml(hudContent);
                        
                        // idk what this is for
                        playerTimer.MovementService!.OldJumpPressed = false;
                    }
                }
            }
            catch (Exception ex)
            {
                if (ex.Message != "Invalid game event") Utils.LogError($"Error in TimerOnTick: {ex.StackTrace}");
            }
        }

        private string GetHudContent(PlayerTimerInfo playerTimer, CCSPlayerController player)
        {
            bool isTimerRunning = playerTimer.IsTimerRunning;
            bool isBonusTimerRunning = playerTimer.IsBonusTimerRunning;
            int timerTicks = playerTimer.TimerTicks;
            PlayerButtons? playerButtons = player.Buttons;
            Vector_t playerSpeed = player.PlayerPawn!.Value!.AbsVelocity.ToVector_t();
            bool keyEnabled = !playerTimer.HideKeys && !playerTimer.IsReplaying && keysOverlayEnabled;
            bool hudEnabled = !playerTimer.HideTimerHud && hudOverlayEnabled;

            string formattedPlayerVel = Math.Round(use2DSpeed
                ? playerSpeed.Length2D()
                : playerSpeed.Length())
                .ToString("0000");

            int playerVel = int.Parse(formattedPlayerVel);

            string secondaryHUDcolorDynamic = "LimeGreen";
            int[] velocityThresholds = { 349, 699, 1049, 1399, 1749, 2099, 2449, 2799, 3149, 3499 };
            string[] hudColors = { "LimeGreen", "Lime", "GreenYellow", "Yellow", "Gold", "Orange", "DarkOrange", "Tomato", "OrangeRed", "Red", "Crimson" };

            for (int i = 0; i < velocityThresholds.Length; i++)
            {
                if (playerVel < velocityThresholds[i])
                {
                    secondaryHUDcolorDynamic = hudColors[i];
                    break;
                }
            }

            string playerVelColor = useDynamicColor ? secondaryHUDcolorDynamic : secondaryHUDcolor;
            string formattedPlayerPre = Math.Round(Utils.ParseVector_t(playerTimer.PreSpeed ?? "0 0 0").Length2D()).ToString("000");
            string playerTime = Utils.FormatTime(timerTicks);
            string playerBonusTime = Utils.FormatTime(playerTimer.BonusTimerTicks);

            string timerLine =
                isBonusTimerRunning
                    ? $" <font class='fontSize-s stratum-bold-italic' color='{tertiaryHUDcolor}'>Bonus #{playerTimer.BonusStage} Timer:</font> " +
                        $"<font class='fontSize-l horizontal-center' color='{primaryHUDcolor}'>{playerBonusTime}</font> " +
                        $"<br>"
                    : isTimerRunning
                        ? $" <font class='fontSize-s stratum-bold-italic' color='{tertiaryHUDcolor}'>Timer: </font>" +
                            $"<font class='fontSize-l horizontal-center' color='{primaryHUDcolor}'>{playerTime}</font> " +
                            $"<font color='gray' class='fontSize-s stratum-bold-italic'>({GetPlayerPlacement(player)})</font>" +
                            $"{((playerTimer.CurrentMapStage != 0 && useStageTriggers == true) ? $" " +
                            $"<font color='gray' class='fontSize-s stratum-bold-italic'> {playerTimer.CurrentMapStage}/{stageTriggerCount}</font>" : "")} " +
                            $"<br>"
                        : playerTimer.IsReplaying
                            ? $" <font class='horizontal-center' color='red'>◉ REPLAY {Utils.FormatTime(playerReplays[player.Slot].CurrentPlaybackFrame)}</font> " +
                            $"<br>"
                            : "";

            string veloLine =
                $" {(playerTimer.IsTester ? playerTimer.TesterSmolGif : "")}" +
                $"<font class='fontSize-s stratum-bold-italic' color='{tertiaryHUDcolor}'>Speed:</font> " +
                $"{(playerTimer.IsReplaying ? "<font class=''" : "<font class='fontSize-l horizontal-center'")} color='{playerVelColor}'>{formattedPlayerVel}</font> " +
                $"<font class='fontSize-s stratum-bold-italic' color='gray'>({formattedPlayerPre})</font>{(playerTimer.IsTester ? playerTimer.TesterSmolGif : "")} " +
                $"<br>";

            string syncLine =
                $"<font class='fontSize-s stratum-bold-italic' color='{tertiaryHUDcolor}'>Sync:</font> " +
                $"<font class='fontSize-l horizontal-center color='{secondaryHUDcolor}'>{playerTimer.Sync:F2}%</font> " +
                $"<br>";

            string infoLine = "";

            if (playerTimer.CurrentZoneInfo.InBonusStartZone)
                infoLine = GetBonusInfoLine(playerTimer);

            else infoLine = GetMainMapInfoLine(playerTimer);

            string keysLineNoHtml = $"{(hudEnabled ? "<br>" : "")}<font class='fontSize-ml stratum-light-mono' color='{tertiaryHUDcolor}'>{((playerButtons & PlayerButtons.Moveleft) != 0 ? "A" : "_")} " +
                                    $"{((playerButtons & PlayerButtons.Forward) != 0 ? "W" : "_")} " +
                                    $"{((playerButtons & PlayerButtons.Moveright) != 0 ? "D" : "_")} " +
                                    $"{((playerButtons & PlayerButtons.Back) != 0 ? "S" : "_")} " +
                                    $"{((playerButtons & PlayerButtons.Jump) != 0 || playerTimer.MovementService!.OldJumpPressed ? "J" : "_")} " +
                                    $"{((playerButtons & PlayerButtons.Duck) != 0 ? "C" : "_")}";


            string hudContent = (hudEnabled ? timerLine +
                                (VelocityHudEnabled ? veloLine : "") +
                                (StrafeHudEnabled && !playerTimer.IsReplaying ? syncLine : "") +
                                infoLine : "") +
                                (keyEnabled && !playerTimer.IsReplaying ? keysLineNoHtml : "") +
                                ((playerTimer.IsTester && !playerTimer.IsReplaying) ? $"{(!keyEnabled ? "<br>" : "")}" + playerTimer.TesterBigGif : "") +
                                ((playerTimer.IsVip && !playerTimer.IsTester && !playerTimer.IsReplaying) ? $"{(!keyEnabled ? "<br><br>" : "")}" + $"<br><img src='https://files.catbox.moe/{playerTimer.VipBigGif}.gif'><br>" : "") +
                                ((playerTimer.IsReplaying && playerTimer.VipReplayGif != "x") ? playerTimer.VipReplayGif : "");

            return hudContent;
        }

        private string GetMainMapInfoLine(PlayerTimerInfo playerTimer)
        {
            return !playerTimer.IsReplaying
                ? $"<font class='fontSize-s stratum-bold-italic' color='gray'>" +

                    $"{playerTimer.CachedPB} " +
                    $"({playerTimer.CachedMapPlacement})" +
                    $"{(RankIconsEnabled ? $" |</font> <img src='{playerTimer.RankHUDIcon}'><font class='fontSize-s stratum-bold-italic' color='gray'>" : "")}" +
                    $"{(enableStyles ? $" | {GetNamedStyle(playerTimer.currentStyle)}" : "")}" +
                    $"{((MapTierHudEnabled && currentMapTier != null) ? $" | Tier: {currentMapTier}" : "")}" +
                    $"{((MapTypeHudEnabled && currentMapType != null) ? $" | {currentMapType}" : "")}" +
                    $"{((MapNameHudEnabled && currentMapType == null && currentMapTier == null) ? $" | {currentMapName}" : "")}" +
                    $"</font>"

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
                        $"{(cachedBonusInfo.Value != null ? $"{Utils.FormatTime(cachedBonusInfo.Value.PbTicks)}" : "Unranked")}" +
                        $"{(cachedBonusInfo.Value != null ? $" ({cachedBonusInfo.Value.Placement})" : "")}</font>" +
                        $"<font class='fontSize-s stratum-bold-italic' color='gray'>" +
                        $"{(enableStyles ? $" | {GetNamedStyle(playerTimer.currentStyle)}" : "")}" +
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
            if (!IsAllowedSpectator(player))
                return;

            try
            {
                var target = specTargets[player.Pawn.Value!.ObserverServices!.ObserverTarget.Index];
                if (playerTimers.TryGetValue(target.Slot, out PlayerTimerInfo? playerTimer) && IsAllowedPlayer(target))
                {
                    string hudContent = GetHudContent(playerTimer, target);

                    if (!string.IsNullOrEmpty(hudContent))
                        player.PrintToCenterHtml(hudContent);
                }
            }
            catch (Exception ex)
            {
                if (ex.Message != "Invalid game event") Utils.LogError($"Error in SpectatorOnTick: {ex.Message}");
            }
        }
    }
}