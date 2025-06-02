using System.Security.AccessControl;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes.Registration;
using CounterStrikeSharp.API.Modules.Admin;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Utils;
using static SharpTimer.PlayerTimerInfo;

namespace SharpTimer
{
    public partial class SharpTimer
    {
        private float frametime = 0.015625f;
        private string? demos;

        // Strafe optimization detection
        // Store the last 200 viewangles of the player; viewangles are gathered (at fastest) each tick. 200ticks is around 4 strafes
        public void ParseStrafes(CCSPlayerController? player, QAngle viewangles)
        {
            var playerTimer = playerTimers[player!.Slot];

            if (playerTimer.YawSpikeFlagged && playerTimer.PerfectStrafesFlagged)
                return;

            playerTimer.ViewAngles.Add(new ViewAngle(viewangles));

            if (playerTimer.ViewAngles.Count > 2)
            {
                var lastYaw = playerTimer.ViewAngles[playerTimer.ViewAngles.Count - 2].Y;
                var currentYaw = playerTimer.ViewAngles[playerTimer.ViewAngles.Count - 1].Y;
                playerTimer.YawSpeed.Add(CalculateYawSpeed(currentYaw, lastYaw));

                if (playerTimer.YawSpeed.Count > 2)
                {
                    var lastLastSpeed = playerTimer.YawSpeed[playerTimer.YawSpeed.Count - 3];
                    var lastSpeed = playerTimer.YawSpeed[playerTimer.YawSpeed.Count - 2];
                    var currentSpeed = playerTimer.YawSpeed[playerTimer.YawSpeed.Count - 1];
                    bool switchedStrafeDirection = Math.Sign(lastSpeed) != Math.Sign(lastLastSpeed);
                    playerTimer.YawAccel.Add(Math.Abs(CalculateYawAccel(currentSpeed, lastSpeed)));

                    var lastLeft = playerTimer.MoveLeft[playerTimer.MoveLeft.Count - 2];
                    var currentLeft = playerTimer.MoveLeft[playerTimer.MoveLeft.Count - 1];
                    var lastRight = playerTimer.MoveRight[playerTimer.MoveRight.Count - 2];
                    var currentRight = playerTimer.MoveRight[playerTimer.MoveRight.Count - 1];

                    if (((lastLeft == false && currentLeft == true) || (lastRight == false && currentRight == true)) && switchedStrafeDirection) // player had tick perfect turn switch and input switch
                        playerTimer.PerfectStrafes++;
                    if (switchedStrafeDirection)
                        playerTimer.TotalStrafes++;

                    if (playerTimer.YawAccel.Count > 2)
                    {
                        var lastLastAccel = playerTimer.YawAccel[playerTimer.YawAccel.Count - 3];
                        var lastAccel = playerTimer.YawAccel[playerTimer.YawAccel.Count - 2];
                        var currentAccel = playerTimer.YawAccel[playerTimer.YawAccel.Count - 1];
                        float lastNextDiff = Math.Abs(currentAccel - lastLastAccel);

                        if (lastNextDiff < 1)
                        {
                            var avgAccel = (currentAccel + lastLastAccel) * 0.5;
                            if (avgAccel < 1 && (lastAccel - avgAccel) > 2.0f && switchedStrafeDirection)
                            {
                                playerTimer.YawAccelPercent = playerTimer.YawAccelPercent * 0.99 + 0.01;  // ++ rolling avg
                            }
                            else if (switchedStrafeDirection)
                            {
                                playerTimer.YawAccelPercent = playerTimer.YawAccelPercent * 0.9 + 0.0;  // -- rolling avg
                            }
                        }
                    }
                }
            }

            if (playerTimer.YawAccelPercent > 0.9)
            {
                // maybe cheator if more than 90% of yaw accel is noisy/jumpy
                if (!playerTimer.YawSpikeFlagged)
                {
                    playerTimer.YawSpikeFlagged = true;
                    StartStopRecord(player, "Unusually frequent m_yaw accel spikes (Strafe optimizer)");
                    Server.NextFrame(async () => await DiscordACMessage(player, "Unusually frequent m_yaw accel spikes (Strafe optimizer)"));
                    SharpTimerConPrint($"::::BEGIN:::: Yaw Accel Spike % of Total");
                    foreach (var percent in playerTimer.YawAccelPercents)
                    {
                        SharpTimerConPrint($"Spike %: {percent}");
                    }
                    SharpTimerConPrint($"::::END:::: Yaw Accel Spike % of Total");
                }
            }

            
            if ((playerTimer.PerfectStrafes / playerTimer.TotalStrafes) > 0.7 && playerTimer.TotalStrafes > 100)
            {
                if (!playerTimer.PerfectStrafesFlagged)
                {
                    playerTimer.PerfectStrafesFlagged = true;
                    StartStopRecord(player, "Unusually frequent perfect strafe/inputs (Autostrafe)");
                    Server.NextFrame(async () => await DiscordACMessage(player, "Unusually frequent perfect strafe/inputs (Autostrafe)"));
                }
            }
            

            // reset every 200 ticks
            if (playerTimer.ViewAngles.Count > 200)
            {
                playerTimer.ViewAngles.Clear();
                playerTimer.YawSpeed.Clear();
                playerTimer.YawAccel.Clear();
                playerTimer.YawAccelPercents.Add(playerTimer.YawAccelPercent); // log some data alongside the demo to get a look into why the player was flagged
            }
        }

        // Strafe sync/autostrafe input detection
        private void ParseInputs(CCSPlayerController player, float sidemove, bool moveleft, bool moveright)
        {
            if (playerTimers[player.Slot].MismatchedInputsFlagged)
                return;

            if (playerTimers[player.Slot].MismatchedInputs >= 10)
            {
                StartStopRecord(player, "Mismatched Inputs (Strafe sync/autostrafe)");
                playerTimers[player.Slot].MismatchedInputsFlagged = true;
                Server.NextFrame(async () => await DiscordACMessage(player, "Mismatched Inputs (Strafe sync/autostrafe)"));
            }

            playerTimers[player.Slot].MoveLeft.Add(moveleft);
            playerTimers[player.Slot].MoveRight.Add(moveright);
            if (playerTimers[player.Slot].MoveLeft.Count > 200)
            {
                playerTimers[player.Slot].MoveLeft.Clear();
                playerTimers[player.Slot].MoveRight.Clear();
            }

            switch (sidemove)    // 1: server moving player left(a); -1: server moving player right(d); 0: server not sidemoving player
            {
                case 1:
                    if (!moveleft)
                        playerTimers[player.Slot].MismatchedInputs++;
                    break;
                case -1:
                    if (!moveright)
                        playerTimers[player.Slot].MismatchedInputs++;
                    break;
                case 0:
                    if (moveright && moveleft) // player is overlapping, causing 0 sidemove, this is normal
                        break;
                    if ((moveright && !moveleft) || (!moveright && moveleft))
                        playerTimers[player.Slot].MismatchedInputs++;
                    break;
                default:
                    // if sidemove is not -1,0,1, then maybe god is real?
                    break;
            }
        }

        public float CalculateYawSpeed(float currentYaw, float lastYaw)
        {
            float yawDiff = currentYaw - lastYaw;
            return yawDiff / frametime;
        }

        public float CalculateYawAccel(float currentSpeed, float lastSpeed)
        {
            float speedDiff = currentSpeed - lastSpeed;
            return speedDiff / frametime;
        }
        private void StartStopRecord(CCSPlayerController player, string reason)
        {
            Server.NextFrame(() =>
            {
                demos = Path.Join(gameDir, "csgo/demos");
                if(!Directory.Exists(demos))
                    Directory.CreateDirectory(demos);

                var name = player.PlayerName.Replace(" ", "-").Replace("\\", "-").Replace("/", "-");
                var file = $"{currentMapName}_{name}_{DateTimeOffset.UtcNow.ToUnixTimeSeconds()}";
                SharpTimerConPrint($"[AC] Unusal input detected, demo will be available at demos/{file} in 60 seconds");
                SharpTimerConPrint($"[AC] Reason: {reason}");
                Server.ExecuteCommand($"tv_record demos/{file}");
                AddTimer(60.0f, () => Server.ExecuteCommand("tv_stoprecord"));
            });
        }

        [ConsoleCommand("css_flaggedplayers", "Gets flagged players")]
        [CommandHelper(whoCanExecute: CommandUsage.CLIENT_AND_SERVER)]
        [RequiresPermissions("@css/cheats")]
        public void GetFlaggedCommand(CCSPlayerController? player, CommandInfo command)
        {
            if (!IsAllowedPlayer(player)) return;
            demos = Path.Join(gameDir, "csgo/demos");
            if(!Directory.Exists(demos))
                Directory.CreateDirectory(demos);
            int flaggedPlayers = 0;
            foreach (var file in Directory.GetFiles(demos))
            {
                flaggedPlayers++;
            }
            Server.NextFrame(() =>
            {
                SharpTimerConPrint($"Flagged players: {flaggedPlayers}");
                if (player is not null) player!.PrintToChat($"Flagged players: {flaggedPlayers}");
            });
        }
    }
}