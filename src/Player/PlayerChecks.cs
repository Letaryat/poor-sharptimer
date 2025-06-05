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
using FixVectorLeak;

namespace SharpTimer
{
    public partial class SharpTimer
    {
        public bool IsAllowedPlayer(CCSPlayerController? player)
        {
            if (player == null)
                return false;

            if (playerTimers.TryGetValue(player.Slot, out var playTimer))
            {
                if (playTimer.IsNoclip)
                    return false;
            }

            bool isConnected = connectedPlayers.ContainsKey(player.Slot) && playerTimers.ContainsKey(player.Slot);

            bool isAlive = player.PawnIsAlive;
            bool isTeamValid = player.TeamCT() || player.TeamT();

            return isConnected && isAlive && isTeamValid;
        }

        private bool IsAllowedSpectator(CCSPlayerController? player)
        {
            if (player == null)
                return false;

            bool isConnected = connectedPlayers.ContainsKey(player.Slot) && playerTimers.ContainsKey(player.Slot);
            bool isObservingValid = player.Pawn?.Value!.ObserverServices?.ObserverTarget != null &&
                                    specTargets.ContainsKey(player.Pawn.Value.ObserverServices.ObserverTarget.Index);

            return isConnected && isObservingValid;
        }

        public bool IsPlayerOrSpectator(CCSPlayerController? player)
        {
            if (player == null)
                return false;

            return IsAllowedPlayer(player) || IsAllowedSpectator(player);
        }

        async Task IsPlayerATester(string steamId64, int slot)
        {
            try
            {
                string response = await httpClient.GetStringAsync(testerPersonalGifsSource);

                using (JsonDocument jsonDocument = JsonDocument.Parse(response))
                {
                    if (playerTimers.TryGetValue(slot, out PlayerTimerInfo? playerTimer))
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
                        Utils.LogError($"Error in IsPlayerATester: player not on server anymore");
                    }
                }
            }
            catch (Exception ex)
            {
                Utils.LogError($"Error in IsPlayerATester: {ex.Message}");
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
                Utils.LogError($"Error in GetTesterBigGif: {ex.Message}");
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
                Utils.LogError($"Error in GetTesterSmolGif: {ex.Message}");
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
                Utils.LogError($"Error in IsSteamIDaTester: {ex.Message}");
                return false;
            }
        }


        private void CheckPlayerCoords(CCSPlayerController? player, Vector_t playerSpeed)
        {
            try
            {
                if (player == null || !IsAllowedPlayer(player))
                {
                    return;
                }

                Vector_t incorrectVector = new(0, 0, 0);
                Vector_t playerPos = player.Pawn?.Value!.CBodyComponent?.SceneNode!.AbsOrigin.ToVector_t() ?? new();
                bool isInsideStartBox = false;
                bool isInsideEndBox = false;

                if (playerPos.Equals(incorrectVector) || currentMapStartC1.Equals(incorrectVector) || currentMapStartC2.Equals(incorrectVector) ||
                    currentMapEndC1.Equals(incorrectVector) || currentMapEndC2.Equals(incorrectVector))
                {
                    return;
                }
                if (!useTriggersAndFakeZones)
                {
                    isInsideStartBox = Utils.IsVectorInsideBox(playerPos, currentMapStartC1, currentMapStartC2, true);
                    isInsideEndBox = Utils.IsVectorInsideBox(playerPos, currentMapEndC1, currentMapEndC2, true);
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
                            Utils.LogError($"Invalid bonus coordinates for bonus {bonus}");

                        }
                        else
                        {
                            isInsideBonusStartBox[bonus] = Utils.IsVectorInsideBox(playerPos, currentBonusStartC1[bonus], currentBonusStartC2[bonus], true);
                            isInsideBonusEndBox[bonus] = Utils.IsVectorInsideBox(playerPos, currentBonusEndC1[bonus], currentBonusEndC2[bonus], true);
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
                        if (playerTimers.TryGetValue(player.Slot, out PlayerTimerInfo? playerTimer))
                        {
                            playerTimer.inStartzone = true;
                            InvalidateTimer(player);
                        }
                    }
                    else if (!isInsideStartBox && playerTimers.TryGetValue(player.Slot, out PlayerTimerInfo? playerTimer))
                    {
                        if (playerTimer.inStartzone == true)
                        {
                            OnTimerStart(player);
                            if (enableReplays) OnRecordingStart(player);

                            if ((maxStartingSpeedEnabled == true && use2DSpeed == false && Math.Round(playerSpeed.Length()) > maxStartingSpeed) ||
                                (maxStartingSpeedEnabled == true && use2DSpeed == true && Math.Round(playerSpeed.Length2D()) > maxStartingSpeed))
                            {
                                Action<CCSPlayerController?, float, bool> adjustVelocity = use2DSpeed ? AdjustPlayerVelocity2D : AdjustPlayerVelocity;
                                adjustVelocity(player, maxStartingSpeed, true);
                            }
                        }
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
                            Utils.LogError($"Invalid bonus coordinates for bonus {bonus}");

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
                                if (playerTimers.TryGetValue(player.Slot, out PlayerTimerInfo? playerTimer))
                                {
                                    playerTimer.inStartzone = true;
                                }

                                OnTimerStart(player, bonus);
                                if (enableReplays) OnRecordingStart(player, bonus);

                                if ((maxStartingSpeedEnabled == true && use2DSpeed == false && Math.Round(playerSpeed.Length()) > maxBonusStartingSpeed) ||
                                    (maxStartingSpeedEnabled == true && use2DSpeed == true && Math.Round(playerSpeed.Length2D()) > maxBonusStartingSpeed))
                                {
                                    Action<CCSPlayerController?, float, bool> adjustVelocity = use2DSpeed ? AdjustPlayerVelocity2D : AdjustPlayerVelocity;
                                    adjustVelocity(player, maxBonusStartingSpeed, true);
                                }
                            }
                            else if (!isInsideBonusStartBox[bonus])
                            {
                                if (playerTimers.TryGetValue(player.Slot, out PlayerTimerInfo? playerTimer))
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
                Utils.LogError($"Error in CheckPlayerCoords: {ex.Message}");
            }
        }

        public bool CommandCooldown(CCSPlayerController player)
        {
            if (playerTimers.TryGetValue(player.Slot, out var playerTimer))
            {
                double secondsRemaining = (playerTimer.CmdCooldown - DateTime.Now).TotalSeconds;
                if (secondsRemaining > 0)
                {
                    Utils.PrintToChat(player, Localizer["command_cooldown", secondsRemaining]);
                    return true;
                }
                else
                {
                    playerTimer.CmdCooldown = DateTime.Now.AddSeconds(cmdCooldown);
                    return false;
                }
            }
            return false;
        }

        public bool IsTimerBlocked(CCSPlayerController? player)
        {
            if (!playerTimers[player!.Slot].IsTimerBlocked)
            {
                Utils.PrintToChat(player, Localizer["stop_using_timer"]);
                return true;
            }
            return false;
        }

        public bool ReplayCheck(CCSPlayerController? player)
        {
            if (playerTimers[player!.Slot].IsReplaying)
            {
                Utils.PrintToChat(player, Localizer["end_your_replay"]);
                return true;
            }
            return false;
        }

        public bool CanCheckpoint(CCSPlayerController? player)
        {
            if (cpOnlyWhenTimerStopped == true && playerTimers[player!.Slot].IsTimerBlocked == false)
            {
                if (playerTimers[player.Slot].currentStyle == 12)
                    return true;
                Utils.PrintToChat(player, Localizer["cant_use_checkpoint", (currentMapName!.Contains("surf_") ? "loc" : "checkpoint")]);
                PlaySound(player, cpSoundError);
                return false;
            }
            return true;
        }
    }
}