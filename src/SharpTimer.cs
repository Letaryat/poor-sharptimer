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
using CounterStrikeSharp.API.Core.Attributes;
using CounterStrikeSharp.API.Modules.Cvars;
using CounterStrikeSharp.API.Modules.Memory.DynamicFunctions;
using System.Runtime.InteropServices;

namespace SharpTimer
{
    [MinimumApiVersion(281)]
    public partial class SharpTimer : BasePlugin
    {
        //public required MemoryFunctionVoid<CCSPlayer_MovementServices, IntPtr> RunCommandLinux;
        //public required MemoryFunctionVoid<IntPtr, IntPtr, IntPtr, CCSPlayer_MovementServices> RunCommandWindows;
        public required IRunCommand RunCommand;
        private int movementServices;
        private int movementPtr;
        public override void Load(bool hotReload)
        {
            SharpTimerConPrint("Loading Plugin...");
            CheckForUpdate();

            defaultServerHostname = ConVar.Find("hostname")!.StringValue;
            Server.ExecuteCommand($"execifexists SharpTimer/config.cfg");

            gameDir = Server.GameDirectory;
            SharpTimerDebug($"Set gameDir to {gameDir}");

            string recordsFileName = $"SharpTimer/PlayerRecords/";
            playerRecordsPath = Path.Join(gameDir + "/csgo/cfg", recordsFileName);


            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux)) isLinux = true;
            else isLinux = false;

            if(isLinux)
            {
                movementServices = 0;
                movementPtr = 1;
                RunCommand = new RunCommandLinux();;
            }
            else if (!isLinux)
            {
                movementServices = 3;
                movementPtr = 2;
                RunCommand = new RunCommandWindows();
            }

            if(isLinux) RunCommand.Hook(OnRunCommand, HookMode.Pre);

            currentMapName = Server.MapName;

            RegisterListener<Listeners.OnMapStart>(OnMapStartHandler);

            RegisterEventHandler<EventPlayerConnectFull>((@event, info) =>
            {
                if (@event.Userid!.IsValid)
                {
                    var player = @event.Userid;

                    if (player.IsValid && !player.IsBot)
                    {
                        OnPlayerConnect(player);
                    }
                }
                return HookResult.Continue;
            });

            RegisterEventHandler<EventPlayerTeam>((@event, info) =>
            {
                if (@event.Userid!.IsValid)
                {
                    if (@event.Userid == null) return HookResult.Continue;
                    var player = @event.Userid;

                    if (player.IsValid && player.IsBot)
                    {
                        if (startKickingAllFuckingBotsExceptReplayOneIFuckingHateValveDogshitFuckingCompanySmile)
                        {
                            AddTimer(4.0f, () =>
                            {
                                Server.ExecuteCommand($"kickid {player.Slot}");
                                SharpTimerDebug($"Kicking unused bot on spawn...");
                            });
                        }
                    }
                    else if (player.IsValid && !player.IsBot)
                    {
                        Server.NextFrame(() => InvalidateTimer(player));
                    }
                }
                return HookResult.Continue;
            }, HookMode.Pre);

            RegisterEventHandler<EventRoundStart>((@event, info) =>
            {
                var mapName = Server.MapName;
                LoadMapData(mapName);
                SharpTimerDebug($"Loading MapData on RoundStart...");
                return HookResult.Continue;
            });

            RegisterEventHandler<EventRoundEnd>((@event, info) =>
            {
                foreach (CCSPlayerController player in connectedPlayers.Values)
                {
                    InvalidateTimer(player);
                }
                return HookResult.Continue;
            }, HookMode.Pre);

            RegisterEventHandler<EventPlayerSpawn>((@event, info) =>
            {
                var player = @event.Userid!;

                if (player.IsBot || !player.IsValid || player == null)
                    return HookResult.Continue;

                if (player.IsValid && !player.IsBot)
                {
                    OnPlayerSpawn(player);
                }

                if (enableStyles && playerTimers.ContainsKey(player.Slot))
                    setStyle(player, playerTimers[player.Slot].currentStyle);

                AddTimer(3.0f, () =>
                {
                    if (enableDb && playerTimers.ContainsKey(player.Slot) && player.DesiredFOV != (uint)playerTimers[player.Slot].PlayerFov)
                    {
                        SharpTimerDebug($"{player.PlayerName} has wrong PlayerFov {player.DesiredFOV}... SetFov to {(uint)playerTimers[player.Slot].PlayerFov}");
                        SetFov(player, playerTimers[player.Slot].PlayerFov, true);
                    }
                });

                Server.NextFrame(() => InvalidateTimer(player));

                return HookResult.Continue;
            }, HookMode.Pre);

            RegisterEventHandler<EventPlayerDisconnect>((@event, info) =>
            {
                if (@event.Userid!.IsValid)
                {
                    var player = @event.Userid;

                    if (player.IsBot || !player.IsValid)
                    {
                        return HookResult.Continue;
                    }
                    else
                    {
                        OnPlayerDisconnect(player);
                    }
                }
                return HookResult.Continue;
            });

            RegisterEventHandler<EventPlayerJump>((@event, info) =>
            {
                if (@event.Userid!.IsValid)
                {
                    var player = @event.Userid;

                    if (player.IsBot || !player.IsValid)
                    {
                        return HookResult.Continue;
                    }
                    else
                    {
                        if (jumpStatsEnabled == true) OnJumpStatJumped(player);
                    }
                }
                return HookResult.Continue;
            });

            RegisterEventHandler<EventPlayerSound>((@event, info) =>
            {
                if (@event.Userid!.IsValid)
                {
                    var player = @event.Userid;

                    if (player.IsBot || !player.IsValid)
                    {
                        return HookResult.Continue;
                    }
                    else
                    {
                        if (jumpStatsEnabled == true && @event.Step == true) OnJumpStatSound(player);
                    }
                }
                return HookResult.Continue;
            });

            RegisterListener<Listeners.OnTick>(PlayerOnTick);

            HookEntityOutput("trigger_multiple", "OnStartTouch", (CEntityIOOutput output, string name, CEntityInstance activator, CEntityInstance caller, CVariant value, float delay) =>
            {
                TriggerMultipleOnStartTouch(activator, caller);
                return HookResult.Continue;
            });

            HookEntityOutput("trigger_multiple", "OnEndTouch", (CEntityIOOutput output, string name, CEntityInstance activator, CEntityInstance caller, CVariant value, float delay) =>
            {
                TriggerMultipleOnEndTouch(activator, caller);
                return HookResult.Continue;
            });

            HookEntityOutput("trigger_teleport", "OnEndTouch", (CEntityIOOutput output, string name, CEntityInstance activator, CEntityInstance caller, CVariant value, float delay) =>
            {
                TriggerTeleportOnEndTouch(activator, caller);
                return HookResult.Continue;
            });

            HookEntityOutput("trigger_teleport", "OnStartTouch", (CEntityIOOutput output, string name, CEntityInstance activator, CEntityInstance caller, CVariant value, float delay) =>
            {
                TriggerTeleportOnStartTouch(activator, caller);
                return HookResult.Continue;
            });

            AddTimer(1.0f, () =>
            {
                DamageHook();
            });

            AddCommandListener("say", OnPlayerChat);
            AddCommandListener("say_team", OnPlayerChat);
            AddCommandListener("jointeam", OnCommandJoinTeam);

            SharpTimerConPrint("Plugin Loaded");
        }
        private HookResult OnRunCommand(DynamicHook h)
        {
            var player = h.GetParam<CCSPlayer_MovementServices>(movementServices).Pawn.Value.Controller.Value?.As<CCSPlayerController>();

            if (player == null || player.IsBot || !player.IsValid || player.IsHLTV) return HookResult.Continue;

            var userCmd = new CUserCmd(h.GetParam<IntPtr>(movementPtr));
            var baseCmd = userCmd.GetBaseCmd();
            var getMovementButton = userCmd.GetMovementButton();

            if (player != null && !player.IsBot && player.IsValid && !player.IsHLTV)
            {
                try
                {
                    var moveForward = getMovementButton.Contains("Forward");
                    var moveBackward = getMovementButton.Contains("Backward");
                    var moveLeft = getMovementButton.Contains("Left");
                    var moveRight = getMovementButton.Contains("Right");
                    if ((playerTimers[player.Slot].IsTimerRunning || playerTimers[player.Slot].IsBonusTimerRunning) && playerTimers[player.Slot].currentStyle.Equals(2) && (moveLeft || moveRight)) //sideways
                    {
                        userCmd.DisableInput(h.GetParam<IntPtr>(movementPtr), 1536); //disable left (512) + right (1024) = 1536
                        baseCmd.DisableSideMove(); //disable side movement
                        return HookResult.Changed;
                    }
                    if ((playerTimers[player.Slot].IsTimerRunning || playerTimers[player.Slot].IsBonusTimerRunning) && playerTimers[player.Slot].currentStyle.Equals(9) && (moveLeft || moveRight) && !(moveForward || moveBackward)) //halfsideways
                    {
                        userCmd.DisableInput(h.GetParam<IntPtr>(movementPtr), 1536); //disable left (512) + right (1024) = 1536
                        baseCmd.DisableSideMove(); //disable side movement
                        return HookResult.Changed;
                    }
                    if ((playerTimers[player.Slot].IsTimerRunning || playerTimers[player.Slot].IsBonusTimerRunning) && playerTimers[player.Slot].currentStyle.Equals(9) && !(moveLeft || moveRight) && (moveForward || moveBackward)) //halfsideways pt2
                    {
                        userCmd.DisableInput(h.GetParam<IntPtr>(movementPtr), 24); //disable backward (16) + forward (8) = 24
                        baseCmd.DisableForwardMove(); //disable forward movement
                        return HookResult.Changed;
                    }
                    if ((playerTimers[player.Slot].IsTimerRunning || playerTimers[player.Slot].IsBonusTimerRunning) && playerTimers[player.Slot].currentStyle.Equals(3) && (moveLeft || moveRight || moveBackward)) //only w
                    {
                        userCmd.DisableInput(h.GetParam<IntPtr>(movementPtr), 1552); //disable backward (16) + left (512) + right (1024) = 1552
                        baseCmd.DisableSideMove(); //disable side movement
                        baseCmd.DisableForwardMove(); //set forward move to 0 ONLY if player is moving backwards; ie: disable s
                        return HookResult.Changed;
                    }
                    if ((playerTimers[player.Slot].IsTimerRunning || playerTimers[player.Slot].IsBonusTimerRunning) && playerTimers[player.Slot].currentStyle.Equals(6) && (moveForward || moveRight || moveBackward)) //only a
                    {
                        userCmd.DisableInput(h.GetParam<IntPtr>(movementPtr), 1048); //disable backward (16) + forward (8) + right (1024) = 1048
                        baseCmd.DisableSideMove(); //disable only right movement
                        baseCmd.DisableForwardMove(); //disable forward movement
                        return HookResult.Changed;
                    }
                    if ((playerTimers[player.Slot].IsTimerRunning || playerTimers[player.Slot].IsBonusTimerRunning) && playerTimers[player.Slot].currentStyle.Equals(7) && (moveForward || moveLeft || moveBackward)) //only d
                    {
                        userCmd.DisableInput(h.GetParam<IntPtr>(movementPtr), 536); //disable backward (16) + forward (8) + left (512) = 536
                        baseCmd.DisableSideMove(); //disable only left movement
                        baseCmd.DisableForwardMove(); //disable forward movement
                        return HookResult.Changed;
                    }
                    if ((playerTimers[player.Slot].IsTimerRunning || playerTimers[player.Slot].IsBonusTimerRunning) && playerTimers[player.Slot].currentStyle.Equals(8) && (moveForward || moveLeft || moveRight)) //only s
                    {
                        userCmd.DisableInput(h.GetParam<IntPtr>(movementPtr), 1544); //disable right (1024) + forward (8) + left (512) = 1544
                        baseCmd.DisableSideMove(); //disable side movement
                        baseCmd.DisableForwardMove(); //disable only forward movement
                        return HookResult.Changed;
                    }
                    return HookResult.Changed;
                }
                catch (Exception)
                {
                    //i dont fucking know why it spams errors when the player disconnects but is also passing all the null checks
                    //so here lies my humble try catch
                    return HookResult.Continue; // :)
                }
            }

            return HookResult.Continue;
        }
        public override void Unload(bool hotReload)
        {
            DamageUnHook();
            RemoveCommandListener("say", OnPlayerChat, HookMode.Pre);
            RemoveCommandListener("say_team", OnPlayerChat, HookMode.Pre);
            RemoveCommandListener("jointeam", OnCommandJoinTeam, HookMode.Pre);

            if(isLinux) RunCommand.Unhook(OnRunCommand, HookMode.Pre);

            SharpTimerConPrint("Plugin Unloaded");
        }
    }
}
