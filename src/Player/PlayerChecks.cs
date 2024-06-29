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

using System.Text.Json;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;

namespace SharpTimer
{
    public partial class SharpTimer
    {
        public bool IsAllowedPlayer(CCSPlayerController? player)
        {
            if (player == null || !player.IsValid || player.Pawn == null || !player.PlayerPawn.IsValid || !player.PawnIsAlive || playerTimers[player.Slot].IsNoclip)
            {
                return false;
            }

            int playerSlot = player.Slot;

            CsTeam teamNum = (CsTeam)player.TeamNum;

            bool isAlive = player.PawnIsAlive;
            bool isTeamValid = teamNum == CsTeam.CounterTerrorist || teamNum == CsTeam.Terrorist;

            bool isTeamSpectatorOrNone = teamNum != CsTeam.Spectator && teamNum != CsTeam.None;
            bool isConnected = connectedPlayers.ContainsKey(playerSlot) && playerTimers.ContainsKey(playerSlot);
            bool isConnectedJS = !jumpStatsEnabled || playerJumpStats.ContainsKey(playerSlot);

            return isTeamValid && isTeamSpectatorOrNone && isConnected && isConnectedJS && isAlive;
        }

        private bool IsAllowedSpectator(CCSPlayerController? player)
        {
            if (player == null || !player.IsValid || player.IsBot)
            {
                return false;
            }

            CsTeam teamNum = (CsTeam)player.TeamNum;
            bool isTeamValid = teamNum == CsTeam.Spectator;
            bool isConnected = connectedPlayers.ContainsKey(player.Slot) && playerTimers.ContainsKey(player.Slot);
            bool isObservingValid = player.Pawn?.Value!.ObserverServices?.ObserverTarget != null &&
                                     specTargets.ContainsKey(player.Pawn.Value.ObserverServices.ObserverTarget.Index);

            return isTeamValid && isConnected && isObservingValid;
        }

        async Task IsPlayerATester(string steamId64, int playerSlot)
        {
            try
            {
                string response = await httpClient.GetStringAsync(testerPersonalGifsSource);

                using (JsonDocument jsonDocument = JsonDocument.Parse(response))
                {
                    if (playerTimers.TryGetValue(playerSlot, out PlayerTimerInfo? playerTimer))
                    {
                        playerTimer.IsTester = jsonDocument.RootElement.TryGetProperty(steamId64, out JsonElement steamData);

                        if (playerTimer.IsTester)
                        {
                            if (steamData.TryGetProperty("SmolGif", out JsonElement smolGifElement))
                            {
                                playerTimer.TesterSmolGif = smolGifElement.GetString() ?? "";
                            }

                            if (steamData.TryGetProperty("BigGif", out JsonElement bigGifElement))
                            {
                                playerTimer.TesterBigGif = bigGifElement.GetString() ?? "";
                            }
                        }
                    }
                    else
                    {
                        SharpTimerError($"Error in IsPlayerATester: player not on server anymore");
                    }
                }
            }
            catch (Exception ex)
            {
                SharpTimerError($"Error in IsPlayerATester: {ex.Message}");
            }
        }

        async Task<string> GetTesterBigGif(string steamId64)
        {
            try
            {
                string response = await httpClient.GetStringAsync(testerPersonalGifsSource);

                using (JsonDocument jsonDocument = JsonDocument.Parse(response))
                {
                    jsonDocument.RootElement.TryGetProperty(steamId64, out JsonElement steamData);

                    if (steamData.TryGetProperty("BigGif", out JsonElement bigGifElement))
                        return bigGifElement.GetString() ?? "";
                    else
                        return "";
                }
            }
            catch (Exception ex)
            {
                SharpTimerError($"Error in GetTesterBigGif: {ex.Message}");
                return "";
            }
        }

        async Task<string> GetTesterSmolGif(string steamId64)
        {
            try
            {
                string response = await httpClient.GetStringAsync(testerPersonalGifsSource);

                using (JsonDocument jsonDocument = JsonDocument.Parse(response))
                {
                    jsonDocument.RootElement.TryGetProperty(steamId64, out JsonElement steamData);

                    if (steamData.TryGetProperty("SmolGif", out JsonElement smolGifElement))
                        return smolGifElement.GetString() ?? "";
                    else
                        return "";
                }
            }
            catch (Exception ex)
            {
                SharpTimerError($"Error in GetTesterSmolGif: {ex.Message}");
                return "";
            }
        }

        async Task<bool> IsSteamIDaTester(string steamId64)
        {
            try
            {
                string response = await httpClient.GetStringAsync(testerPersonalGifsSource);

                using (JsonDocument jsonDocument = JsonDocument.Parse(response))
                {
                    if (jsonDocument.RootElement.TryGetProperty(steamId64, out JsonElement isTester))
                        return true;
                    else
                        return false;
                }
            }
            catch (Exception ex)
            {
                SharpTimerError($"Error in IsSteamIDaTester: {ex.Message}");
                return false;
            }
        }

        private void CheckPlayerCoords(CCSPlayerController? player, Vector playerSpeed)
        {
            try
            {
                if (player == null || !IsAllowedPlayer(player))
                {
                    return;
                }

                Vector incorrectVector = new(0, 0, 0);
                Vector? playerPos = player.Pawn?.Value!.CBodyComponent?.SceneNode!.AbsOrigin;
                bool isInsideStartBox = false;
                bool isInsideEndBox = false;

                if (playerPos == null || currentMapStartC1 == incorrectVector || currentMapStartC2 == incorrectVector ||
                    currentMapEndC1 == incorrectVector || currentMapEndC2 == incorrectVector)
                {
                    return;
                }
                if (!useTriggersAndFakeZones)
                {
                    isInsideStartBox = IsVectorInsideBox(playerPos, currentMapStartC1, currentMapStartC2);
                    isInsideEndBox = IsVectorInsideBox(playerPos, currentMapEndC1, currentMapEndC2);
                }
                bool[] isInsideBonusStartBox = new bool[11];
                bool[] isInsideBonusEndBox = new bool[11];
                foreach (int bonus in totalBonuses)
                {
                    if (bonus == 0)
                    {

                    }
                    else
                    {
                        if (currentBonusStartC1 == null || currentBonusStartC1.Length <= bonus ||
                            currentBonusStartC2 == null || currentBonusStartC2.Length <= bonus ||
                            currentBonusEndC1 == null || currentBonusEndC1.Length <= bonus ||
                            currentBonusEndC2 == null || currentBonusEndC2.Length <= bonus)
                        {
                            SharpTimerError($"Invalid bonus coordinates for bonus {bonus}");

                        }
                        else
                        {
                            isInsideBonusStartBox[bonus] = IsVectorInsideBox(playerPos, currentBonusStartC1[bonus], currentBonusStartC2[bonus]);
                            isInsideBonusEndBox[bonus] = IsVectorInsideBox(playerPos, currentBonusEndC1[bonus], currentBonusEndC2[bonus]);
                        }
                    }
                }

                if (!useTriggersAndFakeZones)
                {
                    if (!isInsideStartBox && isInsideEndBox)
                    {
                        OnTimerStop(player);
                        if (enableReplays) OnRecordingStop(player);
                    }
                    else if (isInsideStartBox)
                    {
                        if(playerTimers.TryGetValue(player.Slot, out PlayerTimerInfo? playerTimer))
                        {
                            playerTimer.inStartzone = true;
                        }

                        OnTimerStart(player);
                        if (enableReplays) OnRecordingStart(player);

                        if ((maxStartingSpeedEnabled == true && use2DSpeed == false && Math.Round(playerSpeed.Length()) > maxStartingSpeed) ||
                            (maxStartingSpeedEnabled == true && use2DSpeed == true && Math.Round(playerSpeed.Length2D()) > maxStartingSpeed))
                        {
                            Action<CCSPlayerController?, float, bool> adjustVelocity = use2DSpeed ? AdjustPlayerVelocity2D : AdjustPlayerVelocity;
                            adjustVelocity(player, maxStartingSpeed, true);
                        }
                    }
                    else if (!isInsideStartBox && playerTimers.TryGetValue(player.Slot, out PlayerTimerInfo? playerTimer))
                    {
                        playerTimer.inStartzone = false;
                    }
                }
                foreach (int bonus in totalBonuses)
                {
                    if (bonus == 0)
                    {

                    }
                    else
                    {
                        if (currentBonusStartC1 == null || currentBonusStartC1.Length <= bonus ||
                            currentBonusStartC2 == null || currentBonusStartC2.Length <= bonus ||
                            currentBonusEndC1 == null || currentBonusEndC1.Length <= bonus ||
                            currentBonusEndC2 == null || currentBonusEndC2.Length <= bonus)
                        {
                            SharpTimerError($"Invalid bonus coordinates for bonus {bonus}");

                        }
                        else
                        {
                            if (!isInsideBonusStartBox[bonus] && isInsideBonusEndBox[bonus])
                            {
                                OnBonusTimerStop(player, bonus);
                                if (enableReplays) OnRecordingStop(player);
                            }
                            else if (isInsideBonusStartBox[bonus])
                            {
                                if(playerTimers.TryGetValue(player.Slot, out PlayerTimerInfo? playerTimer))
                                {
                                    playerTimer.inStartzone = true;
                                }

                                OnTimerStart(player, bonus);
                                if (enableReplays) OnRecordingStart(player, bonus);

                                if ((maxStartingSpeedEnabled == true && use2DSpeed == false && Math.Round(playerSpeed.Length()) > maxStartingSpeed) ||
                                    (maxStartingSpeedEnabled == true && use2DSpeed == true && Math.Round(playerSpeed.Length2D()) > maxStartingSpeed))
                                {
                                    Action<CCSPlayerController?, float, bool> adjustVelocity = use2DSpeed ? AdjustPlayerVelocity2D : AdjustPlayerVelocity;
                                    adjustVelocity(player, maxStartingSpeed, true);
                                }
                            }
                            else if (!isInsideBonusStartBox[bonus])
                            {
                                if(playerTimers.TryGetValue(player.Slot, out PlayerTimerInfo? playerTimer))
                                {
                                    playerTimer.inStartzone = false;
                                }
                                
                            }
                        }
                    }
                }

            }
            catch (Exception ex)
            {
                SharpTimerError($"Error in CheckPlayerCoords: {ex.Message}");
            }
        }

        private void CheckPlayerTriggerPushCoords(CCSPlayerController player, Vector playerSpeed)
        {
            try
            {
                if (player == null || !IsAllowedPlayer(player) || triggerPushData.Count == 0) return;

                Vector? playerPos = player.Pawn?.Value!.CBodyComponent?.SceneNode!.AbsOrigin;

                if (playerPos == null) return;

                var data = GetTriggerPushDataForVector(playerPos);
                if (data != null)
                {
                    var (pushDirEntitySpace, pushSpeed) = data.Value;
                    float currentSpeed = playerSpeed.Length();
                    float speedDifference = pushSpeed - currentSpeed;

                    if (speedDifference > 0)
                    {
                        float velocityChange = speedDifference;
                        player.PlayerPawn.Value!.AbsVelocity.X += pushDirEntitySpace.X * velocityChange;
                        player.PlayerPawn.Value!.AbsVelocity.Y += pushDirEntitySpace.Y * velocityChange;
                        player.PlayerPawn.Value!.AbsVelocity.Z += pushDirEntitySpace.Z * velocityChange;
                        SharpTimerDebug($"trigger_push fix: Player velocity adjusted for {player.PlayerName} by {speedDifference}");
                    }
                }
            }
            catch (Exception ex)
            {
                SharpTimerError($"Error in CheckPlayerTriggerPushCoords: {ex.Message}");
            }
        }
    }
}