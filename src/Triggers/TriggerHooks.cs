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
        public HookResult TriggerMultiple_OnStartTouch(CEntityIOOutput output, string name, CEntityInstance activator, CEntityInstance caller, CVariant value, float delay)
        {
            try
            {
                if (activator == null || caller == null)
                {
                    Utils.LogDebug("Null reference detected in trigger_multiple OnStartTouch hook.");
                    return HookResult.Continue;
                }

                if (activator.DesignerName != "player" || useTriggers == false) return HookResult.Continue;

                var player = new CCSPlayerController(new CCSPlayerPawn(activator.Handle).Controller.Value!.Handle);

                if (player == null)
                {
                    Utils.LogDebug("Player is null in trigger_multiple OnStartTouch hook.");
                    return HookResult.Continue;
                }
                if (player.IsBot)
                {
                    Utils.LogDebug("Player is bot in trigger_multiple OnStartTouch hook.");
                    return HookResult.Continue;
                }


                if (!IsAllowedPlayer(player) || caller.Entity!.Name == null || !connectedPlayers.TryGetValue(player.Slot, out var connected)) return HookResult.Continue;

                var callerHandle = caller.Handle;
                var slot = player.Slot;
                var playerName = player.PlayerName;
                var steamID = player.SteamID.ToString();
                var callerName = caller.Entity.Name;

                if (caller.Entity.Name.ToString() == "bhop_block" && !playerTimers[player.Slot].IsTimerBlocked)
                {
                    playerTimers[player.Slot].IsOnBhopBlock = true;
                    return HookResult.Continue;
                }

                if (useStageTriggers == true && stageTriggers.ContainsKey(callerHandle) && playerTimers[slot].IsTimerBlocked == false && playerTimers[slot].IsTimerRunning == true)
                {
                    if (stageTriggers[callerHandle] == 1)
                        playerTimers[slot].CurrentMapStage = 1;
                    else
                    {
                        _ = Task.Run(async () => await HandlePlayerStageTimes(player, callerHandle, slot, steamID, playerName));
                        return HookResult.Continue;
                    }
                }

                if (useCheckpointTriggers == true && cpTriggers.ContainsKey(callerHandle) && playerTimers[slot].IsTimerBlocked == false && playerTimers[slot].IsTimerRunning == true)
                {
                    _ = Task.Run(async () => await HandlePlayerCheckpointTimes(player, callerHandle, slot, steamID, playerName));
                    return HookResult.Continue;
                }

                if (useBonusCheckpointTriggers == true && bonusCheckpointTriggers.ContainsKey(callerHandle) && playerTimers[slot].IsTimerBlocked == false && playerTimers[slot].IsBonusTimerRunning == true)
                {
                    _ = Task.Run(async () => await HandlePlayerBonusCheckpointTimes(player, callerHandle, slot, steamID, playerName));
                    return HookResult.Continue;
                }

                if (IsValidEndTriggerName(callerName) && playerTimers[slot].IsTimerRunning && !playerTimers[slot].IsTimerBlocked)
                {
                    OnTimerStop(player);
                    if (enableReplays) OnRecordingStop(player);
                    Utils.LogDebug($"Player {playerName} entered EndZone");
                    return HookResult.Continue;
                }

                if (IsValidStartTriggerName(callerName))
                {
                    if(playerTimers.TryGetValue(slot, out PlayerTimerInfo? playerTimer))
                    {
                        playerTimer.inStartzone = true;
                    }

                    InvalidateTimer(player, callerHandle);

                    if ((maxStartingSpeedEnabled == true && use2DSpeed == false && Math.Round(player.PlayerPawn.Value!.AbsVelocity.Length()) > maxStartingSpeed) ||
                        (maxStartingSpeedEnabled == true && use2DSpeed == true && Math.Round(player.PlayerPawn.Value!.AbsVelocity.Length2D()) > maxStartingSpeed))
                    {
                        Action<CCSPlayerController?, float, bool> adjustVelocity = use2DSpeed ? AdjustPlayerVelocity2D : AdjustPlayerVelocity;
                        adjustVelocity(player, maxStartingSpeed, false);
                    }

                    playerTimers[slot].CurrentZoneInfo = new()
                    {
                        InMainMapStartZone = true,
                        InBonusStartZone = false,
                        CurrentBonusNumber = 0
                    };

                    Utils.LogDebug($"Player {playerName} entered StartZone");

                    return HookResult.Continue;
                }

                var (validEndBonus, endBonusX) = IsValidEndBonusTriggerName(callerName, slot);

                if (validEndBonus && playerTimers[slot].IsBonusTimerRunning && !playerTimers[slot].IsTimerBlocked)
                {
                    OnBonusTimerStop(player, endBonusX);
                    if (enableReplays) OnRecordingStop(player);
                    Utils.LogDebug($"Player {playerName} entered Bonus{endBonusX} EndZone");
                    return HookResult.Continue;
                }

                var (validStartBonus, startBonusX) = IsValidStartBonusTriggerName(callerName);
                var (validStartFakeBonus, fakeBonusX) = IsValidFakeStartBonusTriggerName(callerName);

                if (validStartBonus || validStartFakeBonus)
                {
                    InvalidateTimer(player, callerHandle);

                    if ((maxStartingSpeedEnabled == true && use2DSpeed == false && Math.Round(player.PlayerPawn.Value!.AbsVelocity.Length()) > maxBonusStartingSpeed) ||
                        (maxStartingSpeedEnabled == true && use2DSpeed == true && Math.Round(player.PlayerPawn.Value!.AbsVelocity.Length2D()) > maxBonusStartingSpeed))
                    {
                        Action<CCSPlayerController?, float, bool> adjustVelocity = use2DSpeed ? AdjustPlayerVelocity2D : AdjustPlayerVelocity;
                        adjustVelocity(player, maxBonusStartingSpeed, false);
                    }

                    playerTimers[slot].CurrentZoneInfo = new()
                    {
                        InMainMapStartZone = false,
                        InBonusStartZone = true,
                        CurrentBonusNumber = (startBonusX != 0 ? startBonusX : fakeBonusX)
                    };

                    Utils.LogDebug($"Player {playerName} entered Bonus {(startBonusX != 0 ? startBonusX : fakeBonusX)} StartZone");
                    return HookResult.Continue;
                }

                if (IsValidStopTriggerName(callerName))
                {
                    InvalidateTimer(player, callerHandle);
                    Utils.PrintToChat(player, Localizer["timer_cancelled"]);
                }

                if (IsValidResetTriggerName(callerName))
                {
                    InvalidateTimer(player, callerHandle);
                    RespawnPlayer(player);
                    Utils.PrintToChat(player, Localizer["timer_reset"]);
                }

                return HookResult.Continue;
            }
            catch (Exception ex)
            {
                Utils.LogError($"Exception in trigger_multiple OnStartTouch hook: {ex.Message}");
                return HookResult.Continue;
            }
        }

        public HookResult TriggerMultiple_OnEndTouch(CEntityIOOutput output, string name, CEntityInstance activator, CEntityInstance caller, CVariant value, float delay)
        {

            try
            {
                if (activator == null || caller == null)
                {
                    Utils.LogDebug("Null reference detected in trigger_multiple OnEndTouch hook.");
                    return HookResult.Continue;
                }

                if (activator.DesignerName != "player" || useTriggers == false) return HookResult.Continue;

                var player = new CCSPlayerController(new CCSPlayerPawn(activator.Handle).Controller.Value!.Handle);

                if (player == null)
                {
                    Utils.LogDebug("Player is null in trigger_multiple OnEndTouch hook.");
                    return HookResult.Continue;
                }
                if (player.IsBot)
                {
                    Utils.LogDebug("Player is bot in trigger_multiple OnEndTouch hook.");
                    return HookResult.Continue;
                }

                if (!IsAllowedPlayer(player) || caller.Entity!.Name == null || !connectedPlayers.TryGetValue(player.Slot, out var connected)) return HookResult.Continue;

                var slot = player.Slot;
                var playerName = player.PlayerName;
                var callerName = caller.Entity.Name;

                if (caller.Entity.Name.ToString() == "bhop_block" && IsAllowedPlayer(player) && !playerTimers[player.Slot].IsTimerBlocked)
                {
                    playerTimers[player.Slot].IsOnBhopBlock = false;
                    playerTimers[player.Slot].TicksOnBhopBlock = 0;

                    return HookResult.Continue;
                }

                if (IsValidStartTriggerName(callerName) && !playerTimers[slot].IsTimerBlocked)
                {
                    if(playerTimers.TryGetValue(slot, out PlayerTimerInfo? playerTimer))
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

                    Utils.LogDebug($"Player {playerName} left StartZone");

                    return HookResult.Continue;
                }

                var (validStartBonus, StartBonusX) = IsValidStartBonusTriggerName(callerName);

                if (validStartBonus == true && !playerTimers[slot].IsTimerBlocked)
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

                    Utils.LogDebug($"Player {playerName} left BonusStartZone {StartBonusX}");

                    return HookResult.Continue;
                }
                return HookResult.Continue;
            }
            catch (Exception ex)
            {
                Utils.LogError($"Exception in trigger_multiple OnEndTouch hook: {ex.Message}");
                return HookResult.Continue;
            }
        }

        public HookResult TriggerTeleport_OnStartTouch(CEntityIOOutput output, string name, CEntityInstance activator, CEntityInstance caller, CVariant value, float delay)
        {

            try
            {
                if (activator == null || caller == null)
                {
                    Utils.LogDebug("Null reference detected in trigger_teleport hook.");
                    return HookResult.Continue;
                }

                if (activator.DesignerName != "player")
                {
                    Utils.LogDebug("activator.DesignerName != player in trigger_teleport hook.");
                    return HookResult.Continue;
                }

                var player = new CCSPlayerController(new CCSPlayerPawn(activator.Handle).Controller.Value!.Handle);

                if (player == null || player.IsBot || player.IsHLTV || !player.IsValid)
                {
                    return HookResult.Continue;
                }

                if (!IsAllowedPlayer(player))
                {
                    Utils.LogDebug("Player not allowed in trigger_teleport hook.");
                    return HookResult.Continue;
                }

                return HookResult.Continue;
            }
            catch (Exception ex)
            {
                Utils.LogError(ex.Message);
                return HookResult.Continue;
            }
        }

        public HookResult TriggerTeleport_OnEndTouch(CEntityIOOutput output, string name, CEntityInstance activator, CEntityInstance caller, CVariant value, float delay)
        {
            try
            {
                if (activator == null || caller == null)
                {
                    Utils.LogDebug("Null reference detected in trigger_teleport hook.");
                    return HookResult.Continue;
                }

                if (activator.DesignerName != "player" || resetTriggerTeleportSpeedEnabled == false)
                {
                    return HookResult.Continue;
                }

                var player = new CCSPlayerController(new CCSPlayerPawn(activator.Handle).Controller.Value!.Handle);

                if (player == null || player.IsBot || player.IsHLTV || !player.IsValid)
                {
                    Utils.LogDebug("Player is null in trigger_teleport hook.");
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
                Utils.LogError(ex.Message);
                return HookResult.Continue;
            }
        }
    }
}