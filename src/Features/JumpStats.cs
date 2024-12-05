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
using CounterStrikeSharp.API.Modules.Utils;
using Vector = CounterStrikeSharp.API.Modules.Utils.Vector;

namespace SharpTimer;

public partial class SharpTimer
{
    //based on https://github.com/deafps/cs2-kz-lua/blob/main/kz.lua
    public void OnJumpStatJumped(CCSPlayerController player)
    {
        if (IsAllowedPlayer(player))
        {
            var playerSlot = player.Slot;
            playerJumpStats[playerSlot].Jumped = true;
            playerJumpStats[playerSlot].OldJumpPos = string.IsNullOrEmpty(playerJumpStats[playerSlot].JumpPos)
                ? $"{player.Pawn.Value!.AbsOrigin!.X} {player.Pawn.Value!.AbsOrigin.Y!} {player.Pawn.Value!.AbsOrigin.Z}"
                : playerJumpStats[playerSlot].JumpPos;
            playerJumpStats[playerSlot].JumpPos =
                $"{player.Pawn.Value!.AbsOrigin!.X} {player.Pawn.Value!.AbsOrigin.Y!} {player.Pawn.Value!.AbsOrigin.Z!}";
        }
    }

    public void OnJumpStatSound(CCSPlayerController player)
    {
        if (IsAllowedPlayer(player))
        {
            var playerSlot = player.Slot;
            if (playerJumpStats[playerSlot].Jumped && playerJumpStats[playerSlot].FramesOnGround == 0)
            {
                playerJumpStats[playerSlot].LandedFromSound = true;
                Server.NextFrame(() =>
                {
                    if (IsAllowedPlayer(player)) playerJumpStats[playerSlot].LandedFromSound = false;
                });
            }
        }
    }

    public void OnJumpStatTick(CCSPlayerController player, Vector velocity, Vector playerpos, QAngle eyeangle,
        PlayerButtons? buttons)
    {
        try
        {
            if (playerJumpStats.TryGetValue(player.Slot, out var playerJumpStat))
            {
                playerJumpStat.OnGround = ((PlayerFlags)player.Pawn.Value!.Flags & PlayerFlags.FL_ONGROUND) ==
                                          PlayerFlags
                                              .FL_ONGROUND; //need hull trace for this to detect surf and edgebug etc

                if (playerJumpStat.OnGround)
                {
                    playerJumpStat.FramesOnGround++;
                }
                else
                {
                    playerJumpStat.FramesOnGround = 0;
                    OnJumpStatTickInAir(player, playerJumpStat, buttons, playerpos, velocity, eyeangle);
                }

                if (playerJumpStat.FramesOnGround == 2)
                {
                    if (movementUnlockerCapEnabled && velocity.Length2D() > movementUnlockerCapValue)
                    {
                        var mult = movementUnlockerCapValue / velocity.Length2D();
                        velocity.X *= mult;
                        velocity.Y *= mult;
                        player.PlayerPawn.Value!.AbsVelocity.X = velocity.X;
                        player.PlayerPawn.Value!.AbsVelocity.Y = velocity.Y;
                    }
                }
                else if (playerJumpStat.FramesOnGround == 1)
                {
                    if (playerJumpStat.Jumped)
                    {
                        var distance =
                            Calculate2DDistanceWithVerticalMargins(ParseVector(playerJumpStat.JumpPos!), playerpos);
                        if (distance != 0 && playerJumpStat.LastFramesOnGround > 2)
                        {
                            playerJumpStat.LastJumpType = "LJ";
                            PrintJS(player, playerJumpStat, distance, playerpos);
                        }
                        else if (distance != 0 && playerJumpStat.LastFramesOnGround <= 2 &&
                                 (playerJumpStat.LastJumpType == "LJ" || playerJumpStat.LastJumpType == "JB"))
                        {
                            playerJumpStat.LastJumpType = "BH";
                            PrintJS(player, playerJumpStat, distance, playerpos);
                        }
                        else if (distance != 0 && playerJumpStat.LastFramesOnGround <= 2 &&
                                 (playerJumpStat.LastJumpType == "BH" || playerJumpStat.LastJumpType == "MBH" ||
                                  playerJumpStat.LastJumpType == "JB"))
                        {
                            playerJumpStat.LastJumpType = "MBH";
                            PrintJS(player, playerJumpStat, distance, playerpos);
                        }
                    }

                    playerJumpStat.Jumped = false;

                    playerJumpStat.jumpFrames.Clear();
                    playerJumpStat.jumpInterp.Clear();
                    playerJumpStat.WTicks = 0;
                }
                else if (playerJumpStat.LandedFromSound) //workaround for PlayerFlags.FL_ONGROUND being 1 frame late
                {
                    if (playerJumpStat.Jumped)
                    {
                        var distance = Calculate2DDistanceWithVerticalMargins(ParseVector(playerJumpStat.OldJumpPos!),
                            playerpos, true);
                        if (distance != 0 && !playerJumpStat.LastOnGround && playerJumpStat.LastDucked &&
                            ((PlayerFlags)player.Pawn.Value.Flags & PlayerFlags.FL_DUCKING) != PlayerFlags.FL_DUCKING)
                        {
                            playerJumpStat.LastJumpType = "JB";
                            PrintJS(player, playerJumpStat, distance, playerpos);
                            playerJumpStat.LastFramesOnGround = playerJumpStat.FramesOnGround;
                            playerJumpStat.Jumped = true; // assume player jumped again if JB is successful
                            if (playerTimers[player.Slot].SoundsEnabled)
                                player.ExecuteClientCommand($"play player/death_fem_0{new Random().Next(1, 9)}");
                        }
                    }
                    else
                    {
                        playerJumpStat.Jumped = false;
                    }

                    playerJumpStat.jumpFrames.Clear();
                    playerJumpStat.jumpInterp.Clear();
                    playerJumpStat.WTicks = 0;

                    playerJumpStat.FramesOnGround++;
                }

                playerJumpStat.LastLandedFromSound = playerJumpStat.LandedFromSound;

                playerJumpStat.LastOnGround = playerJumpStat.OnGround;
                playerJumpStat.LastDucked = ((PlayerFlags)player.Pawn.Value.Flags & PlayerFlags.FL_DUCKING) ==
                                            PlayerFlags.FL_DUCKING;
                if (playerJumpStat.OnGround)
                {
                    playerJumpStat.LastSpeed = $"{velocity.X} {velocity.Y} {velocity.Z}";
                    playerJumpStat.LastFramesOnGround = playerJumpStat.FramesOnGround;
                    playerJumpStat.LastPosOnGround = $"{playerpos.X} {playerpos.Y} {playerpos.Z}";
                }
            }
        }
        catch (Exception ex)
        {
            SharpTimerDebug($"Exception in OnJumpStatTick: {ex}");
        }
    }

    public void OnJumpStatTickInAir(CCSPlayerController player, PlayerJumpStats playerJumpStat, PlayerButtons? buttons,
        Vector playerpos, Vector velocity, QAngle eyeangle)
    {
        try
        {
            var LastJumpFrame = playerJumpStat.jumpFrames.Count != 0
                ? playerJumpStat.jumpFrames.Last()
                : new PlayerJumpStats.IFrame
                {
                    PositionString = " ",
                    SpeedString = " ",
                    LastLeft = false,
                    LastRight = false,
                    LastLeftRight = false,
                    MaxHeight = 0,
                    MaxSpeed = 0
                };

            var left = false;
            var right = false;
            var leftRight = false;
            if ((buttons & PlayerButtons.Moveleft) != 0 && (buttons & PlayerButtons.Moveright) != 0)
                leftRight = true;
            else if ((buttons & PlayerButtons.Moveleft) != 0)
                left = true;
            else if ((buttons & PlayerButtons.Moveright) != 0)
                right = true;

            if ((buttons & PlayerButtons.Forward) != 0)
                playerJumpStat.WTicks++;

            double maxHeight;
            if (IsVectorHigherThan(playerpos, ParseVector(LastJumpFrame.PositionString!)))
                maxHeight = playerpos.Z - ParseVector(playerJumpStat.LastPosOnGround ?? "0 0 0").Z;
            else
                maxHeight = LastJumpFrame?.MaxHeight ?? 0;

            double maxSpeed;
            if (velocity.Length2D() > LastJumpFrame!.MaxSpeed)
                maxSpeed = velocity.Length2D();
            else
                maxSpeed = LastJumpFrame?.MaxSpeed ?? 0;

            var JumpFrame = new PlayerJumpStats.IFrame
            {
                PositionString = $"{playerpos.X} {playerpos.Y} {playerpos.Z}",
                SpeedString = $"{velocity.X} {velocity.Y} {velocity.Z}",
                RotationString = $"{eyeangle.X} {eyeangle.Y} {eyeangle.Z}",
                LastLeft = left,
                LastRight = right,
                LastLeftRight = leftRight,
                MaxHeight = maxHeight,
                MaxSpeed = maxSpeed
            };

            playerJumpStat.jumpFrames.Add(JumpFrame);
        }
        catch (Exception ex)
        {
            SharpTimerDebug($"Exception in OnJumpStatTickInAir: {ex}");
        }
    }

    public void OnSyncTick(CCSPlayerController player, PlayerButtons? buttons, QAngle eyeangle)
    {
        try
        {
            var playerTimer = playerTimers[player.Slot];
            var strafingLeft = false;
            var strafingRight = false;
            // Start with 100% sync initially
            if (playerTimers[player.Slot].inStartzone) playerTimer.Sync = 100.00f;

            if ((buttons & PlayerButtons.Moveleft) != 0 &&
                (buttons & PlayerButtons.Moveright) != 0) return; // Ignore if both left and right are pressed

            if ((buttons & PlayerButtons.Moveleft) != 0)
                strafingLeft = true;
            else if ((buttons & PlayerButtons.Moveright) != 0)
                strafingRight = true;
            else
                return; // Ignore if neither left nor right is pressed

            // Add the current eye angle to the rotation history
            var newEyeAngle = new QAngle(eyeangle.X, eyeangle.Y, eyeangle.Z);
            playerTimer.Rotation.Add(newEyeAngle);

            // Cap rotation history at 1000 entries
            if (playerTimer.Rotation.Count > 1000) playerTimer.Rotation.RemoveAt(0); // Remove the oldest entry

            // Only proceed if we have enough data points in Rotation
            if (playerTimer.Rotation.Count > 1)
            {
                var previousEyeAngleY =
                    playerTimer.Rotation[playerTimer.Rotation.Count - 2].Y; // Use Rotation.Count - 2
                var currentEyeAngleY = eyeangle.Y;

                // Normalize angle difference to handle wrapping from -180 to 180
                var deltaY = currentEyeAngleY - previousEyeAngleY;
                if (deltaY > 180)
                    deltaY -= 360;
                else if (deltaY < -180) deltaY += 360;

                if (Math.Abs(deltaY) < 0.01f) return;

                var onGround = ((PlayerFlags)player.Pawn.Value!.Flags & PlayerFlags.FL_ONGROUND) ==
                               PlayerFlags.FL_ONGROUND;
                if (onGround ||
                    (!onGround &&
                     (buttons & (PlayerButtons.Moveleft | PlayerButtons.Moveright)) ==
                     0))
                    return; // Ignore calculation if the player is on the ground or airborne without pressing movement buttons

                // Increment frames in the air
                playerTimer.TotalSync++;

                // Determine rotation direction
                var rotatingLeft = deltaY > 0;
                var rotatingRight = deltaY < 0;

                // Add sync frame if strafing and rotating match and the player is airborne
                if (!onGround && ((strafingLeft && rotatingLeft) || (strafingRight && rotatingRight)))
                    playerTimer.GoodSync++; // Increment sync frames
            }

            // Calculate sync percentage
            if (playerTimer.TotalSync >= 2) // Adjust threshold as needed
                playerTimer.Sync = playerTimer.TotalSync > 0
                    ? playerTimer.GoodSync / (float)playerTimer.TotalSync * 100
                    : 0;
        }
        catch (Exception ex)
        {
            SharpTimerDebug($"Exception in OnSyncTick: {ex}");
        }
    }

    public static char GetJSColor(double distance)
    {
        if (distance < 230) return ChatColors.Grey;

        if (distance < 235) return ChatColors.Blue;

        if (distance < 240) return ChatColors.Green;

        if (distance < 244) return ChatColors.DarkRed;

        if (distance < 246) return ChatColors.Gold;

        return ChatColors.Purple;
    }

    private double Calculate2DDistanceWithVerticalMargins(Vector vector1, Vector vector2, bool noVertCheck = false)
    {
        if (vector1 == null || vector2 == null) return 0;

        var verticalDistance = Math.Abs(vector1.Z - vector2.Z);

        if (verticalDistance >= 32 && noVertCheck == false) return 0;

        var distance2D = Distance(vector1, vector2);

        if (distance2D > jumpStatsMinDist || noVertCheck)
        {
            var result = distance2D + 16.0f;
            return result;
        }

        return 0;
    }

    public static (int lastLeftGroups, int leftSync, int leftFrames) CountLeftGroupsAndSync(
        PlayerJumpStats playerJumpStat, bool timersync)
    {
        var lastLeftGroups = 0;
        var leftSync = 0;
        var leftFrames = 0;
        var inGroup = false;
        QAngle previousRotation = null!;

        var frames = timersync ? playerJumpStat.timerSyncFrames : playerJumpStat.jumpFrames;
        foreach (var frame in frames)
        {
            if (frame.LastLeftRight || frame.LastRight)
            {
                if (inGroup)
                    lastLeftGroups++;
                inGroup = false;
            }
            else if (frame.LastLeft)
            {
                inGroup = true;
                leftFrames++;
                if (previousRotation != null && ParseQAngle(frame.RotationString!).Y > previousRotation.Y)
                    leftSync++;
            }

            previousRotation = ParseQAngle(frame.RotationString!);
        }

        if (inGroup)
            lastLeftGroups++;

        return (lastLeftGroups, leftSync, leftFrames);
    }

    public static (int lastRightGroups, int rightSync, int rightFrames) CountRightGroupsAndSync(
        PlayerJumpStats playerJumpStat, bool timersync)
    {
        var lastRightGroups = 0;
        var rightSync = 0;
        var rightFrames = 0;
        var inGroup = false;
        QAngle previousRotation = null!;

        var frames = timersync ? playerJumpStat.timerSyncFrames : playerJumpStat.jumpFrames;
        foreach (var frame in frames)
        {
            if (frame.LastLeftRight || frame.LastLeft)
            {
                if (inGroup)
                    lastRightGroups++;
                inGroup = false;
            }
            else if (frame.LastRight)
            {
                inGroup = true;
                rightFrames++;
                if (previousRotation != null && ParseQAngle(frame.RotationString!).Y < previousRotation.Y)
                    rightSync++;
            }

            previousRotation = ParseQAngle(frame.RotationString!);
        }

        if (inGroup)
            lastRightGroups++;

        return (lastRightGroups, rightSync, rightFrames);
    }

    public static float GetMaxWidth(Vector playerpos, PlayerJumpStats playerJumpStat)
    {
        InterpolateVectors(ParseVector(playerJumpStat.JumpPos!), playerpos, playerJumpStat);

        float distance = 0;
        for (var jumpFrameIndex = 0;
             jumpFrameIndex < Math.Min(playerJumpStat.jumpFrames.Count, playerJumpStat.jumpInterp.Count);
             jumpFrameIndex++)
        {
            var frame = playerJumpStat.jumpFrames[jumpFrameIndex];
            var width = (float)Distance2D(ParseVector(frame.PositionString!),
                ParseVector(playerJumpStat.jumpInterp[jumpFrameIndex].InterpString!));
            if (width > distance) distance = width;
        }

        return (float)Math.Round(distance, 2);
    }

    public static void InterpolateVectors(Vector vector1, Vector vector2, PlayerJumpStats playerJumpStat)
    {
        var numInterpolations = playerJumpStat.jumpFrames.Count;
        var stepX = (vector2.X - vector1.X) / (numInterpolations + 1);
        var stepY = (vector2.Y - vector1.Y) / (numInterpolations + 1);
        var stepZ = (vector2.Z - vector1.Z) / (numInterpolations + 1);

        for (var i = 0; i < numInterpolations; i++)
        {
            var interpolatedX = vector1.X + stepX * (i + 1);
            var interpolatedY = vector1.Y + stepY * (i + 1);
            var interpolatedZ = vector1.Z + stepZ * (i + 1);

            var interpFrame = new PlayerJumpStats.JumpInterp
            {
                InterpString = $"{interpolatedX} {interpolatedY} {interpolatedZ}"
            };

            playerJumpStat.jumpInterp.Add(interpFrame);
        }
    }

    public void InvalidateJS(int playerSlot)
    {
        try
        {
            if (playerJumpStats.TryGetValue(playerSlot, out var value))
            {
                value.LastFramesOnGround = 0;
                value.Jumped = false;
            }
        }
        catch (Exception ex)
        {
            // Suppress trigger_teleport null reference exceptions (the player has likely disconnected)
        }
    }

    public void PrintJS(CCSPlayerController player, PlayerJumpStats playerJumpStat, double distance, Vector playerpos)
    {
        if (playerTimers[player.Slot].HideJumpStats) return;

        var color = GetJSColor(distance);

        var (lStrafes, lSync, lFrames) = CountLeftGroupsAndSync(playerJumpStat, false);
        var (rStrafes, rSync, rFrames) = CountRightGroupsAndSync(playerJumpStat, false);

        var strafes = rStrafes + lStrafes;
        var strafeFrames = rFrames + lFrames;
        var syncedFrames = rSync + lSync;

        var sync = strafeFrames != 0 ? Math.Round(syncedFrames * 100f / strafeFrames, 2) : 0;

        PrintToChat(player,
            Localizer["js_msg1", playerJumpStat.LastJumpType!, color, Math.Round(distance, 2),
                Math.Round(ParseVector(playerJumpStat.LastSpeed!).Length2D(), 2),
                Math.Round(playerJumpStat.jumpFrames.Last().MaxSpeed, 2), strafes]);
        PrintToChat(player,
            Localizer["js_msg2", Math.Round(playerJumpStat.jumpFrames.Last().MaxHeight, 2),
                GetMaxWidth(playerpos, playerJumpStat), playerJumpStat.WTicks, sync]);

        player.PrintToConsole(
            "-----------------------------------------------------------------------------------------------------------------------");
        player.PrintToConsole(
            $" {Localizer["js_msg1", playerJumpStat.LastJumpType!, color, Math.Round(distance, 2), Math.Round(ParseVector(playerJumpStat.LastSpeed!).Length2D(), 2), Math.Round(playerJumpStat.jumpFrames.Last().MaxSpeed, 2), strafes]}");
        player.PrintToConsole(
            $" {Localizer["js_msg2", Math.Round(playerJumpStat.jumpFrames.Last().MaxHeight, 2), GetMaxWidth(playerpos, playerJumpStat), playerJumpStat.WTicks, sync]}");
    }
}