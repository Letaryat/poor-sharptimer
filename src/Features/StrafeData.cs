using System.Data.Common;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes.Registration;
using CounterStrikeSharp.API.Modules.Admin;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Cvars;
using CounterStrikeSharp.API.Modules.Entities;
using CounterStrikeSharp.API.Modules.Utils;
using static SharpTimer.PlayerTimerInfo;

namespace SharpTimer
{
    public partial class SharpTimer
    {
        private float frametime = 0.015625f;

        // Store the last 100 viewangles of the player; viewangles are gathered (at fastest) each tick
        public void ParseStrafes(CCSPlayerController? player, QAngle viewangles)
        {
            var playerTimer = playerTimers[player!.Slot];

            playerTimer.ViewAngles.Add(new ViewAngle(viewangles));

            if (playerTimer.ViewAngles.Count > 2)
            {
                var lastYaw = playerTimer.ViewAngles[playerTimer.ViewAngles.Count-1].Y;
                var currentYaw = playerTimer.ViewAngles[playerTimer.ViewAngles.Count].Y;
                playerTimer.YawSpeed.Add(CalculateYawSpeed(lastYaw, currentYaw));

                if (playerTimer.YawSpeed.Count > 2)
                {
                    var lastSpeed = playerTimer.YawSpeed[playerTimer.YawSpeed.Count-1];
                    var currentSpeed = playerTimer.YawSpeed[playerTimer.YawSpeed.Count];
                    playerTimer.YawAccel.Add(CalculateYawAccel(lastSpeed, currentSpeed));

                    if (playerTimer.YawAccel.Count > 2)
                    {
                        var lastAccel = playerTimer.YawAccel[playerTimer.YawAccel.Count-1];
                        var currentAccel = playerTimer.YawAccel[playerTimer.YawAccel.Count];
                        var avgAccel = (currentAccel + lastAccel) * 0.5;
                        bool switchedStrafeDirection = Math.Sign(currentSpeed) != Math.Sign(lastSpeed);

                        if (avgAccel < 2 && currentAccel - avgAccel > 2.0f && switchedStrafeDirection)
                        {
                            playerTimer.YawAccelSpikes++;
                        }
                    }
                }
            }

            if (playerTimer.YawAccelSpikes > 4)
            {
                // maybe cheator if 4 extreme yaw accel spikes within 4s
                if (!playerTimer.ACFlagged)
                {
                    Server.NextFrame(() => 
                    {
                        var file = $"{currentMapName}_{player.PlayerName}_{(int)DateTimeOffset.UtcNow.ToUnixTimeSeconds()}";
                        SharpTimerConPrint($"[AC] Unusal strafing detected, demo will be available at {file} in 60 seconds");
                        Server.ExecuteCommand($"tv_record {file}");
                        AddTimer(60.0f, () => Server.ExecuteCommand("tv_stoprecord"));
                    });
                }
                playerTimer.ACFlagged = true;
            }

            // reset every 100 ticks
            if (playerTimer.ViewAngles.Count > 100)
            {
                playerTimer.ViewAngles.Clear();
                playerTimer.YawSpeed.Clear();
                playerTimer.YawAccel.Clear();
                playerTimer.YawAccelSpikes = 0;
            }
        }

        public float CalculateYawSpeed(float lastYaw, float currentYaw)
        {
            float yawDiff = currentYaw - lastYaw;
            return yawDiff / frametime;
        }

        public float CalculateYawAccel(float lastSpeed, float currentSpeed)
        {
            float speedDiff = currentSpeed - lastSpeed;
            return speedDiff / frametime;
        }
    }
}