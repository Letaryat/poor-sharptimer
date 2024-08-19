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

using CounterStrikeSharp.API.Core;

namespace SharpTimer
{
    public partial class SharpTimer
    {
        public HookResult TriggerMultipleOnStartTouch(CEntityInstance activator, CEntityInstance caller)
        {
            try
            {
                if (activator == null || caller == null)
                {
                    SharpTimerDebug("Null reference detected in trigger_multiple OnStartTouch hook.");
                    return HookResult.Continue;
                }

                if (activator.DesignerName != "player" || useTriggers == false) return HookResult.Continue;

                var player = new CCSPlayerController(new CCSPlayerPawn(activator.Handle).Controller.Value!.Handle);

                if (player == null)
                {
                    SharpTimerDebug("Player is null in trigger_multiple OnStartTouch hook.");
                    return HookResult.Continue;
                }

                if (!IsAllowedPlayer(player) || caller.Entity!.Name == null) return HookResult.Continue;

                var callerHandle = caller.Handle;
                var playerSlot = player.Slot;
                var playerName = player.PlayerName;
                var steamID = player.SteamID.ToString();
                var callerName = caller.Entity.Name;

                if (caller.Entity.Name.ToString() == "bhop_block" && !playerTimers[player.Slot].IsTimerBlocked)
                {
                    playerTimers[player.Slot].IsOnBhopBlock = true;
                    return HookResult.Continue;
                }

                if (useStageTriggers == true && stageTriggers.ContainsKey(callerHandle) && playerTimers[playerSlot].IsTimerBlocked == false && playerTimers[playerSlot].IsTimerRunning == true)
                {
                    if (stageTriggers[callerHandle] == 1)
                    {
                        playerTimers[playerSlot].CurrentMapStage = 1;
                        return HookResult.Continue;
                    }
                    else
                    {
                        _ = Task.Run(async () => await HandlePlayerStageTimes(player, callerHandle, playerSlot, steamID, playerName));
                        return HookResult.Continue;
                    }
                }

                if (useCheckpointTriggers == true && cpTriggers.ContainsKey(callerHandle) && playerTimers[playerSlot].IsTimerBlocked == false && playerTimers[playerSlot].IsTimerRunning == true)
                {
                    _ = Task.Run(async () => await HandlePlayerCheckpointTimes(player, callerHandle, playerSlot, steamID, playerName));
                    return HookResult.Continue;
                }

                if (useBonusCheckpointTriggers == true && bonusCheckpointTriggers.ContainsKey(callerHandle) && playerTimers[playerSlot].IsTimerBlocked == false && playerTimers[playerSlot].IsBonusTimerRunning == true)
                {
                    _ = Task.Run(async () => await HandlePlayerBonusCheckpointTimes(player, callerHandle, playerSlot, steamID, playerName));
                    return HookResult.Continue;
                }

                if (IsValidEndTriggerName(callerName) && playerTimers[playerSlot].IsTimerRunning && !playerTimers[playerSlot].IsTimerBlocked)
                {
                    OnTimerStop(player);
                    if (enableReplays) OnRecordingStop(player);
                    SharpTimerDebug($"Player {playerName} entered EndZone");
                    return HookResult.Continue;
                }

                if (IsValidStartTriggerName(callerName))
                {
                    if(playerTimers.TryGetValue(playerSlot, out PlayerTimerInfo? playerTimer))
                    {
                        playerTimer.inStartzone = true;
                    }
                    if (!playerTimers[playerSlot].IsTimerBlocked)
                    {
                        playerCheckpoints.Remove(playerSlot);
                    }

                    InvalidateTimer(player, callerHandle);

                    if ((maxStartingSpeedEnabled == true && use2DSpeed == false && Math.Round(player.PlayerPawn.Value!.AbsVelocity.Length()) > maxStartingSpeed) ||
                        (maxStartingSpeedEnabled == true && use2DSpeed == true && Math.Round(player.PlayerPawn.Value!.AbsVelocity.Length2D()) > maxStartingSpeed))
                    {
                        Action<CCSPlayerController?, float, bool> adjustVelocity = use2DSpeed ? AdjustPlayerVelocity2D : AdjustPlayerVelocity;
                        adjustVelocity(player, maxStartingSpeed, false);
                    }

                    playerTimers[playerSlot].CurrentZoneInfo = new()
                    {
                        InMainMapStartZone = true,
                        InBonusStartZone = false,
                        CurrentBonusNumber = 0
                    };

                    SharpTimerDebug($"Player {playerName} entered StartZone");

                    return HookResult.Continue;
                }

                var (validEndBonus, endBonusX) = IsValidEndBonusTriggerName(callerName, playerSlot);

                if (validEndBonus && playerTimers[playerSlot].IsBonusTimerRunning && !playerTimers[playerSlot].IsTimerBlocked)
                {
                    OnBonusTimerStop(player, endBonusX);
                    if (enableReplays) OnRecordingStop(player);
                    SharpTimerDebug($"Player {playerName} entered Bonus{endBonusX} EndZone");
                    return HookResult.Continue;
                }

                var (validStartBonus, startBonusX) = IsValidStartBonusTriggerName(callerName);
                var (validStartFakeBonus, fakeBonusX) = IsValidFakeStartBonusTriggerName(callerName);

                if (validStartBonus || validStartFakeBonus)
                {
                    if (!playerTimers[playerSlot].IsTimerBlocked)
                    {
                        playerCheckpoints.Remove(playerSlot);
                    }

                    InvalidateTimer(player, callerHandle);

                    if ((maxStartingSpeedEnabled == true && use2DSpeed == false && Math.Round(player.PlayerPawn.Value!.AbsVelocity.Length()) > maxBonusStartingSpeed) ||
                        (maxStartingSpeedEnabled == true && use2DSpeed == true && Math.Round(player.PlayerPawn.Value!.AbsVelocity.Length2D()) > maxBonusStartingSpeed))
                    {
                        Action<CCSPlayerController?, float, bool> adjustVelocity = use2DSpeed ? AdjustPlayerVelocity2D : AdjustPlayerVelocity;
                        adjustVelocity(player, maxBonusStartingSpeed, false);
                    }

                    playerTimers[playerSlot].CurrentZoneInfo = new()
                    {
                        InMainMapStartZone = false,
                        InBonusStartZone = true,
                        CurrentBonusNumber = (startBonusX != 0 ? startBonusX : fakeBonusX)
                    };

                    SharpTimerDebug($"Player {playerName} entered Bonus {(startBonusX != 0 ? startBonusX : fakeBonusX)} StartZone");
                    return HookResult.Continue;
                }

                if (IsValidStopTriggerName(callerName))
                {
                    InvalidateTimer(player, callerHandle);
                    PrintToChat(player, Localizer["timer_cancelled"]);
                }

                if (IsValidResetTriggerName(callerName))
                {
                    InvalidateTimer(player, callerHandle);
                    RespawnPlayer(player);
                    PrintToChat(player, Localizer["timer_reset"]);
                }

                return HookResult.Continue;
            }
            catch (Exception ex)
            {
                SharpTimerError($"Exception in trigger_multiple OnStartTouch hook: {ex.Message}");
                return HookResult.Continue;
            }
        }

        public HookResult TriggerMultipleOnEndTouch(CEntityInstance activator, CEntityInstance caller)
        {

            try
            {
                if (activator == null || caller == null)
                {
                    SharpTimerDebug("Null reference detected in trigger_multiple OnEndTouch hook.");
                    return HookResult.Continue;
                }

                if (activator.DesignerName != "player" || useTriggers == false) return HookResult.Continue;

                var player = new CCSPlayerController(new CCSPlayerPawn(activator.Handle).Controller.Value!.Handle);

                if (player == null)
                {
                    SharpTimerDebug("Player is null in trigger_multiple OnEndTouch hook.");
                    return HookResult.Continue;
                }

                if (!IsAllowedPlayer(player) || caller.Entity!.Name == null) return HookResult.Continue;

                var playerSlot = player.Slot;
                var playerName = player.PlayerName;
                var callerName = caller.Entity.Name;

                if (caller.Entity.Name.ToString() == "bhop_block" && IsAllowedPlayer(player) && !playerTimers[player.Slot].IsTimerBlocked)
                {
                    playerTimers[player.Slot].IsOnBhopBlock = false;
                    playerTimers[player.Slot].TicksOnBhopBlock = 0;

                    return HookResult.Continue;
                }

                if (IsValidStartTriggerName(callerName) && !playerTimers[playerSlot].IsTimerBlocked)
                {
                    if(playerTimers.TryGetValue(playerSlot, out PlayerTimerInfo? playerTimer))
                    {
                        playerTimer.inStartzone = false;
                    }
                    OnTimerStart(player);
                    if (enableReplays) OnRecordingStart(player);

                    if (((maxStartingSpeedEnabled == true && use2DSpeed == false && Math.Round(player.PlayerPawn.Value!.AbsVelocity.Length()) > maxStartingSpeed) ||
                        (maxStartingSpeedEnabled == true && use2DSpeed == true && Math.Round(player.PlayerPawn.Value!.AbsVelocity.Length2D()) > maxStartingSpeed)) &&
                        !currentMapOverrideMaxSpeedLimit!.Contains(callerName) && currentMapOverrideMaxSpeedLimit != null)
                    {
                        Action<CCSPlayerController?, float, bool> adjustVelocity = use2DSpeed ? AdjustPlayerVelocity2D : AdjustPlayerVelocity;
                        adjustVelocity(player, maxStartingSpeed, false);
                    }

                    SharpTimerDebug($"Player {playerName} left StartZone");

                    return HookResult.Continue;
                }

                var (validStartBonus, StartBonusX) = IsValidStartBonusTriggerName(callerName);

                if (validStartBonus == true && !playerTimers[playerSlot].IsTimerBlocked)
                {
                    OnTimerStart(player, StartBonusX);
                    if (enableReplays) OnRecordingStart(player, StartBonusX);

                    if (((maxStartingSpeedEnabled == true && use2DSpeed == false && Math.Round(player.PlayerPawn.Value!.AbsVelocity.Length()) > maxBonusStartingSpeed) ||
                        (maxStartingSpeedEnabled == true && use2DSpeed == true && Math.Round(player.PlayerPawn.Value!.AbsVelocity.Length2D()) > maxBonusStartingSpeed)) &&
                        !currentMapOverrideMaxSpeedLimit!.Contains(callerName) && currentMapOverrideMaxSpeedLimit != null)
                    {
                        Action<CCSPlayerController?, float, bool> adjustVelocity = use2DSpeed ? AdjustPlayerVelocity2D : AdjustPlayerVelocity;
                        adjustVelocity(player, maxBonusStartingSpeed, false);
                    }

                    SharpTimerDebug($"Player {playerName} left BonusStartZone {StartBonusX}");

                    return HookResult.Continue;
                }
                return HookResult.Continue;
            }
            catch (Exception ex)
            {
                SharpTimerError($"Exception in trigger_multiple OnEndTouch hook: {ex.Message}");
                return HookResult.Continue;
            }
        }

        public HookResult TriggerTeleportOnStartTouch(CEntityInstance activator, CEntityInstance caller)
        {

            try
            {
                if (activator == null || caller == null)
                {
                    SharpTimerDebug("Null reference detected in trigger_teleport hook.");
                    return HookResult.Continue;
                }

                if (activator.DesignerName != "player")
                {
                    SharpTimerDebug("activator.DesignerName != player in trigger_teleport hook.");
                    return HookResult.Continue;
                }

                var player = new CCSPlayerController(new CCSPlayerPawn(activator.Handle).Controller.Value!.Handle);

                if (player == null)
                {

                    return HookResult.Continue;
                }

                if (!IsAllowedPlayer(player))
                {
                    SharpTimerDebug("Player not allowed in trigger_teleport hook.");
                    return HookResult.Continue;
                }

                if (jumpStatsEnabled) InvalidateJS(player.Slot);

                return HookResult.Continue;
            }
            catch (Exception ex)
            {
                SharpTimerError($"Exception in trigger_teleport hook: {ex.Message}");
                return HookResult.Continue;
            }
        }

        public HookResult TriggerTeleportOnEndTouch(CEntityInstance activator, CEntityInstance caller)
        {
            try
            {
                if (activator == null || caller == null)
                {
                    SharpTimerDebug("Null reference detected in trigger_teleport hook.");
                    return HookResult.Continue;
                }

                if (activator.DesignerName != "player" || resetTriggerTeleportSpeedEnabled == false)
                {
                    return HookResult.Continue;
                }

                var player = new CCSPlayerController(new CCSPlayerPawn(activator.Handle).Controller.Value!.Handle);

                if (player == null)
                {
                    SharpTimerDebug("Player is null in trigger_teleport hook.");
                    return HookResult.Continue;
                }

                if (!IsAllowedPlayer(player)) return HookResult.Continue;

                if (resetTriggerTeleportSpeedEnabled)
                {
                    string triggerName = caller.Entity!.Name.ToString();
                    if (currentMapOverrideDisableTelehop != null && (!currentMapOverrideDisableTelehop!.Contains(triggerName) || currentMapOverrideDisableTelehop![0].ToLower() == "true"))
                    {
                        Action<CCSPlayerController?, float, bool> adjustVelocity = use2DSpeed ? AdjustPlayerVelocity2D : AdjustPlayerVelocity;
                        adjustVelocity(player, 0, false);
                    }
                    /* if (!currentMapOverrideDisableTelehop)
                    {
                        Action<CCSPlayerController?, float, bool> adjustVelocity = use2DSpeed ? AdjustPlayerVelocity2D : AdjustPlayerVelocity;
                        adjustVelocity(player, 0, false);
                    } */
                }

                return HookResult.Continue;
            }
            catch (Exception ex)
            {
                SharpTimerError($"Exception in trigger_teleport hook: {ex.Message}");
                return HookResult.Continue;
            }
        }
    }
}