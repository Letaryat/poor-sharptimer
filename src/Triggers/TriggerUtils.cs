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

using System.Text.RegularExpressions;
using FixVectorLeak;

namespace SharpTimer
{
    public partial class SharpTimer
    {
        private bool IsValidStartTriggerName(string triggerName)
        {
            try
            {
                if (string.IsNullOrEmpty(triggerName)) return false;
                string[] validStartTriggers = ["map_start", "s1_start", "stage1_start", "timer_startzone", "zone_start", currentMapStartTrigger];
                return validStartTriggers.Contains(triggerName);
            }
            catch (NullReferenceException ex)
            {
                Utils.LogError($"Null ref in IsValidStartTriggerName: {ex.Message}");
                return false;
            }
            catch (Exception ex)
            {
                Utils.LogError($"Exception in IsValidStartTriggerName: {ex.Message}");
                return false;
            }
        }

        private (bool valid, int X) IsValidStartBonusTriggerName(string triggerName)
        {
            try
            {
                if (string.IsNullOrEmpty(triggerName)) return (false, 0);

                string[] patterns = [
                    @"^b([1-9][0-9]?)_start$",
                    @"^bonus([1-9][0-9]?)_start$",
                    @"^timer_bonus([1-9][0-9]?)_startzone$"
                ];

                foreach (var pattern in patterns)
                {
                    var match = Regex.Match(triggerName, pattern);
                    if (match.Success)
                    {
                        int X = int.Parse(match.Groups[1].Value);
                        try
                        {
                            if (totalBonuses[X] != 0)
                            {
                                Utils.LogDebug($"Fake bonus {X} found, overwriting real start trigger");
                                return (false, X);
                            }
                            else
                            {
                                return (true, X);
                            }
                        }
                        catch (Exception)
                        {
                            return (true, X);
                        }
                    }
                }

                return (false, 0);
            }
            catch (Exception ex)
            {
                Utils.LogError($"Exception in IsValidStartBonusTriggerName: {ex.Message}");
                return (false, 0);
            }
        }
        private (bool valid, int X) IsValidFakeStartBonusTriggerName(string triggerName)
        {
            try
            {
                if (string.IsNullOrEmpty(triggerName)) return (false, 0);

                string[] patterns = [
                    @"^b([1-9][0-9]?)_start$",
                    @"^bonus([1-9][0-9]?)_start$",
                    @"^timer_bonus([1-9][0-9]?)_startzone$"
                ];

                foreach (var pattern in patterns)
                {
                    var match = Regex.Match(triggerName, pattern);
                    if (match.Success)
                    {
                        int X = int.Parse(match.Groups[1].Value);
                        try
                        {
                            if (totalBonuses[X] != 0)
                            {
                                return (true, X);
                            }
                            else
                            {
                                return (false, X);
                            }
                        }
                        catch (Exception)
                        {
                            return (true, X);
                        }
                    }
                }

                return (false, 0);
            }
            catch (Exception ex)
            {
                Utils.LogError($"Exception in IsValidFakeStartBonusTriggerName: {ex.Message}");
                return (false, 0);
            }
        }

        private (bool valid, int X) IsValidStageTriggerName(string triggerName)
        {
            try
            {
                if (string.IsNullOrEmpty(triggerName)) return (false, 0);

                string[] patterns = [
                    @"^s([1-9][0-9]?)_start$",
                    @"^stage([1-9][0-9]?)_start$",
                    @"^map_start$",
                ];

                foreach (var pattern in patterns)
                {
                    var match = Regex.Match(triggerName, pattern);
                    if (match.Success)
                    {
                        if (pattern == @"^map_start$")
                        {
                            return (true, 1);
                        }
                        else
                        {
                            int X = int.Parse(match.Groups[1].Value);
                            return (true, X);
                        }
                    }
                }

                return (false, 0);
            }
            catch (Exception ex)
            {
                Utils.LogError($"Exception in IsValidStageTriggerName: {ex.Message}");
                return (false, 0);
            }
        }

        private (bool valid, int X) IsValidCheckpointTriggerName(string triggerName)
        {
            try
            {
                if (string.IsNullOrEmpty(triggerName)) return (false, 0);

                string[] patterns = [
                    @"^map_cp([1-9][0-9]?)$",
                    @"^map_checkpoint([1-9][0-9]?)$"
                ];

                foreach (var pattern in patterns)
                {
                    var match = Regex.Match(triggerName, pattern);
                    if (match.Success)
                    {
                        int X = int.Parse(match.Groups[1].Value);
                        return (true, X);
                    }
                }

                return (false, 0);
            }
            catch (Exception ex)
            {
                Utils.LogError($"Exception in IsValidCheckpointTriggerName: {ex.Message}");
                return (false, 0);
            }
        }

        private (bool valid, int X) IsValidBonusCheckpointTriggerName(string triggerName)
        {
            try
            {
                if (string.IsNullOrEmpty(triggerName)) return (false, 0);

                string[] patterns = [
                    @"^bonus_cp([1-9][0-9]?)$",
                    @"^bonus_checkpoint([1-9][0-9]?)$"
                ];

                foreach (var pattern in patterns)
                {
                    var match = Regex.Match(triggerName, pattern);
                    if (match.Success)
                    {
                        int X = int.Parse(match.Groups[1].Value);
                        return (true, X);
                    }
                }

                return (false, 0);
            }
            catch (Exception ex)
            {
                Utils.LogError($"Exception in IsValidBonusCheckpointTriggerName: {ex.Message}");
                return (false, 0);
            }
        }

        private bool IsValidEndTriggerName(string triggerName)
        {
            try
            {
                if (string.IsNullOrEmpty(triggerName)) return false;
                string[] validEndTriggers = ["map_end", "timer_endzone", "zone_end", currentMapEndTrigger];
                return validEndTriggers.Contains(triggerName);
            }
            catch (Exception ex)
            {
                Utils.LogError($"Exception in IsValidEndTriggerName: {ex.Message}");
                return false;
            }
        }

        private (bool valid, int X) IsValidEndBonusTriggerName(string triggerName, int slot)
        {
            try
            {
                if (string.IsNullOrEmpty(triggerName)) return (false, 0);
                string[] patterns = [
                    @"^b([1-9][0-9]?)_end$",
                    @"^bonus([1-9][0-9]?)_end$",
                    @"^timer_bonus([1-9][0-9]?)_endzone$"
                ];

                foreach (var pattern in patterns)
                {
                    var match = Regex.Match(triggerName, pattern);
                    if (match.Success)
                    {
                        int X = int.Parse(match.Groups[1].Value);
                        try
                        {
                            if (totalBonuses[X] != 0)
                            {
                                Utils.LogDebug($"Fake bonus {X} found, overwriting real end trigger");
                                return (false, X);
                            }
                            else
                            {
                                if (X != playerTimers[slot].BonusStage) return (false, 0);
                                return (true, X);
                            }
                        }
                        catch (Exception)
                        {
                            if (X != playerTimers[slot].BonusStage) return (false, 0);
                            return (true, X);
                        }

                    }
                }

                return (false, 0);
            }
            catch (Exception ex)
            {
                Utils.LogError($"Exception in IsValidEndBonusTriggerName: {ex.Message}");
                return (false, 0);
            }
        }

        private bool IsValidStopTriggerName(string triggerName)
        {
            try
            {
                if (string.IsNullOrEmpty(triggerName)) return false;
                string[] validStopTriggers = ["st_stop", "surftimer_stop", "timer_stop"];
                return validStopTriggers.Contains(triggerName);
            }
            catch (Exception ex)
            {
                Utils.LogError($"Exception in IsValidStopTriggerName: {ex.Message}");
                return false;
            }
        }

        private bool IsValidResetTriggerName(string triggerName)
        {
            try
            {
                if (string.IsNullOrEmpty(triggerName)) return false;
                string[] validResetTriggers = ["st_reset", "surftimer_reset", "timer_reset"];
                return validResetTriggers.Contains(triggerName);
            }
            catch (Exception ex)
            {
                Utils.LogError($"Exception in IsValidResetTriggerName: {ex.Message}");
                return false;
            }
        }

        private void UpdateEntityCache()
        {
            entityCache!.UpdateCache();
        }

        private (Vector_t?, QAngle_t?) FindStartTriggerPos()
        {
            currentRespawnPos = null;
            currentRespawnAng = null;

            foreach (var trigger in entityCache!.Triggers)
            {
                if (trigger == null || trigger.Entity!.Name == null || !IsValidStartTriggerName(trigger.Entity.Name.ToString()))
                    continue;

                foreach (var info_tp in entityCache.InfoTeleportDestinations)
                {
                    if (info_tp.Entity?.Name != null && Utils.IsVectorInsideBox(info_tp.AbsOrigin!.ToVector_t(),
                                                        (trigger.Collision.Mins + trigger.CBodyComponent!.SceneNode!.AbsOrigin!).ToVector_t(),
                                                        (trigger.Collision.Maxs + trigger.CBodyComponent!.SceneNode!.AbsOrigin!).ToVector_t()))
                    {
                        if (info_tp.CBodyComponent!.SceneNode!.AbsOrigin != null && info_tp.AbsRotation != null)
                        {
                            return (info_tp.CBodyComponent.SceneNode.AbsOrigin.ToVector_t(), info_tp.AbsRotation.ToQAngle_t());
                        }
                    }
                }

                if (trigger.CBodyComponent!.SceneNode!.AbsOrigin != null)
                {
                    return (trigger.CBodyComponent.SceneNode.AbsOrigin.ToVector_t(), null);
                }
            }

            return (null, null);
        }

        private Vector_t? FindEndTriggerPos()
        {
            currentEndPos = null;

            foreach (var trigger in entityCache!.Triggers)
            {
                if (trigger == null || trigger.Entity!.Name == null || !IsValidEndTriggerName(trigger.Entity.Name.ToString()))
                    continue;

                if (trigger.CBodyComponent!.SceneNode!.AbsOrigin != null) return trigger.CBodyComponent.SceneNode.AbsOrigin.ToVector_t();

            }

            return null;
        }

        private void FindStageTriggers()
        {
            stageTriggers.Clear();
            stageTriggerPoses.Clear();

            foreach (var trigger in entityCache!.Triggers)
            {
                if (trigger == null || trigger.Entity!.Name == null) continue;

                var (validStage, X) = IsValidStageTriggerName(trigger.Entity.Name.ToString());
                if (validStage)
                {
                    foreach (var info_tp in entityCache.InfoTeleportDestinations)
                    {
                        if (info_tp.Entity?.Name != null && Utils.IsVectorInsideBox(info_tp.AbsOrigin!.ToVector_t(),
                                                            (trigger.Collision.Mins + trigger.CBodyComponent!.SceneNode!.AbsOrigin).ToVector_t(),
                                                            (trigger.Collision.Maxs + trigger.CBodyComponent.SceneNode.AbsOrigin).ToVector_t()))
                        {
                            if (info_tp.CBodyComponent?.SceneNode?.AbsOrigin != null && info_tp.AbsRotation != null)
                            {
                                stageTriggerPoses[X] = info_tp.CBodyComponent.SceneNode.AbsOrigin.ToVector_t();
                                stageTriggerAngs[X] = info_tp.AbsRotation.ToQAngle_t();
                                Utils.LogDebug($"Added !stage {X} pos {stageTriggerPoses[X]} ang {stageTriggerAngs[X]}");
                            }
                        }
                    }

                    stageTriggers[trigger.Handle] = X;
                    Utils.LogDebug($"Added Stage {X} Trigger {trigger.Handle}");
                }
            }

            stageTriggerCount = stageTriggers.Any() ? stageTriggers.OrderByDescending(x => x.Value).First().Value : 0;

            if (stageTriggerCount == 1)
            {
                // If there's only one stage trigger, the map is linear
                stageTriggerCount = 0;
                useStageTriggers = false;
                stageTriggers.Clear();
                stageTriggerPoses.Clear();
                stageTriggerAngs.Clear();
                Utils.LogDebug($"Only one Stage Trigger found. Not enough. Cancelling...");
            }
            else if (stageTriggerCount > 1)
            {
                useStageTriggers = true;
            }

            Utils.LogDebug($"Found a max of {stageTriggerCount} Stage triggers");
            Utils.LogDebug($"Use stageTriggers is set to {useStageTriggers}");
        }

        private void FindCheckpointTriggers()
        {
            cpTriggers.Clear();
            cpTriggerCount = 0;

            foreach (var trigger in entityCache!.Triggers)
            {
                if (trigger == null || trigger.Entity!.Name == null) continue;

                var (validCp, X) = IsValidCheckpointTriggerName(trigger.Entity.Name.ToString());
                if (validCp)
                {
                    cpTriggerCount++;
                    cpTriggers[trigger.Handle] = X;
                    Utils.LogDebug($"Added Checkpoint {cpTriggerCount} Trigger {trigger.Handle}");
                }
            }

            useCheckpointTriggers = cpTriggerCount != 0;

            Utils.LogDebug($"Found a max of {cpTriggerCount} Checkpoint triggers");
            Utils.LogDebug($"Use useCheckpointTriggers is set to {useCheckpointTriggers}");
        }

        private void FindBonusCheckpointTriggers()
        {
            bonusCheckpointTriggers.Clear();

            foreach (var trigger in entityCache!.Triggers)
            {
                if (trigger == null || trigger.Entity!.Name == null) continue;

                var (validCp, X) = IsValidBonusCheckpointTriggerName(trigger.Entity.Name.ToString());
                if (validCp)
                {
                    bonusCheckpointTriggers[trigger.Handle] = X;
                    Utils.LogDebug($"Added Bonus Checkpoint {X} Trigger {trigger.Handle}");
                }
            }

            bonusCheckpointTriggerCount = bonusCheckpointTriggers.Any() ? bonusCheckpointTriggers.OrderByDescending(x => x.Value).First().Value : 0;

            useBonusCheckpointTriggers = bonusCheckpointTriggerCount != 0;

            Utils.LogDebug($"Found a max of {bonusCheckpointTriggerCount} Bonus Checkpoint triggers");
            Utils.LogDebug($"Use useBonusCheckpointTriggers is set to {useBonusCheckpointTriggers}");
        }

        private void FindBonusStartTriggerPos()
        {

            foreach (var trigger in entityCache!.Triggers)
            {
                if (trigger == null || trigger.Entity!.Name == null) continue;

                var (validStartBonus, bonusX) = IsValidStartBonusTriggerName(trigger.Entity.Name.ToString());
                if (validStartBonus)
                {
                    bool bonusPosAndAngSet = false;

                    foreach (var info_tp in entityCache.InfoTeleportDestinations)
                    {
                        if (info_tp.Entity?.Name != null && Utils.IsVectorInsideBox(info_tp.AbsOrigin!.ToVector_t(),
                                                            (trigger.Collision.Mins + trigger.CBodyComponent!.SceneNode!.AbsOrigin).ToVector_t(),
                                                            (trigger.Collision.Maxs + trigger.CBodyComponent.SceneNode.AbsOrigin).ToVector_t())
                            )
                        {
                            if (info_tp.CBodyComponent?.SceneNode?.AbsOrigin != null && info_tp.AbsRotation != null)
                            {
                                try
                                {
                                    if (bonusRespawnPoses[bonusX] != null)
                                    {
                                        Utils.LogDebug($"Fake bonus {bonusX} found, skipping real triggers");
                                    }
                                    else
                                    {
                                        bonusRespawnPoses[bonusX] = info_tp.CBodyComponent.SceneNode.AbsOrigin.ToVector_t();
                                        bonusRespawnAngs[bonusX] = info_tp.AbsRotation.ToQAngle_t();
                                        Utils.LogDebug($"Added Bonus !rb {bonusX} pos {bonusRespawnPoses[bonusX]} ang {bonusRespawnAngs[bonusX]}");
                                        bonusPosAndAngSet = true;
                                    }
                                }
                                catch (Exception)
                                {
                                    bonusRespawnPoses[bonusX] = info_tp.CBodyComponent.SceneNode.AbsOrigin.ToVector_t();
                                    bonusRespawnAngs[bonusX] = info_tp.AbsRotation.ToQAngle_t();
                                    Utils.LogDebug($"Added Bonus !rb {bonusX} pos {bonusRespawnPoses[bonusX]} ang {bonusRespawnAngs[bonusX]}");
                                    bonusPosAndAngSet = true;
                                }
                            }
                        }
                    }

                    if (!bonusPosAndAngSet && trigger.CBodyComponent?.SceneNode?.AbsOrigin != null)
                    {
                        try
                        {
                            if (bonusRespawnPoses[bonusX] != null)
                            {
                                Utils.LogDebug($"Fake bonus {bonusX} found, skipping real triggers");
                            }
                            else
                            {
                                bonusRespawnPoses[bonusX] = trigger.CBodyComponent.SceneNode.AbsOrigin.ToVector_t();
                                Utils.LogDebug($"Added Bonus !rb {bonusX} pos {bonusRespawnPoses[bonusX]}");
                            }
                        }
                        catch (Exception)
                        {
                            bonusRespawnPoses[bonusX] = trigger.CBodyComponent.SceneNode.AbsOrigin.ToVector_t();
                            Utils.LogDebug($"Added Bonus !rb {bonusX} pos {bonusRespawnPoses[bonusX]}");
                        }
                    }
                }
            }
        }

        private (Vector_t?, Vector_t?, Vector_t?, Vector_t?) FindTriggerBounds()
        {
            Vector_t? startMins = null;
            Vector_t? startMaxs = null;

            Vector_t? endMins = null;
            Vector_t? endMaxs = null;

            foreach (var trigger in entityCache!.Triggers)
            {
                if (trigger == null || trigger.Entity!.Name == null)
                    continue;

                if (IsValidStartTriggerName(trigger.Entity.Name.ToString()))
                {
                    startMins = (trigger.Collision.Mins + trigger.CBodyComponent!.SceneNode!.AbsOrigin).ToVector_t();
                    startMaxs = (trigger.Collision.Maxs + trigger.CBodyComponent.SceneNode.AbsOrigin).ToVector_t();
                    currentMapStartTriggerMaxs = startMaxs;
                    currentMapStartTriggerMins = startMins;
                    continue;
                }

                if (IsValidEndTriggerName(trigger.Entity.Name.ToString()))
                {
                    endMins = (trigger.Collision.Mins + trigger.CBodyComponent!.SceneNode!.AbsOrigin).ToVector_t();
                    endMaxs = (trigger.Collision.Maxs + trigger.CBodyComponent.SceneNode.AbsOrigin).ToVector_t();
                    continue;
                }
            }

            return (startMins, startMaxs, endMins, endMaxs);
        }
    }
}