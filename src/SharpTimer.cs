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

using System.Security.Cryptography.X509Certificates;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes;
using CounterStrikeSharp.API.Modules.Cvars;
using System.Runtime.CompilerServices;
using CounterStrikeSharp.API.Modules.Memory;
using CounterStrikeSharp.API.Modules.Memory.DynamicFunctions;
using System.Runtime.InteropServices;

namespace SharpTimer
{
    [MinimumApiVersion(228)]
    public partial class SharpTimer : BasePlugin
    {
        public required MemoryFunctionVoid<CCSPlayer_MovementServices, IntPtr> RunCommandLinux;
        public required MemoryFunctionVoid<IntPtr, IntPtr, IntPtr, CCSPlayer_MovementServices> RunCommandWindows;
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

            string mysqlConfigFileName = "SharpTimer/mysqlConfig.json";
            mySQLpath = Path.Join(gameDir + "/csgo/cfg", mysqlConfigFileName);
            SharpTimerDebug($"Set mySQLpath to {mySQLpath}");

            string postgresConfigFileName = "SharpTimer/postgresConfig.json";
            postgresPath = Path.Join(gameDir + "/csgo/cfg", postgresConfigFileName);

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux)) isLinux = true;
            else isLinux = false;

            if (isLinux)
            {
                movementServices = 0;
                movementPtr = 1;
                RunCommandLinux = new(GameData.GetSignature("RunCommand"));
                RunCommandLinux.Hook(OnRunCommand, HookMode.Pre);
            }
            else if (!isLinux)
            {
                movementServices = 3;
                movementPtr = 2;
                RunCommandWindows = new(GameData.GetSignature("RunCommand"));
                RunCommandWindows.Hook(OnRunCommand, HookMode.Pre);
            }

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
                    else if (player.IsValid)
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
                if (@event.Userid!.IsValid)
                {
                    if (@event.Userid == null) return HookResult.Continue;

                    var player = @event.Userid;

                    if (player.IsBot || !player.IsValid || player == null)
                    {
                        return HookResult.Continue;
                    }
                    else if (player.IsValid)
                    {
                        /* if (removeCollisionEnabled == true && player.PlayerPawn != null)
                        {
                            RemovePlayerCollision(player);
                        }

                        specTargets[player.Pawn.Value.EntityHandle.Index] = new CCSPlayerController(player.Handle); */
                        AddTimer(5.0f, () =>
                        {
                            if (!player.IsValid || player == null || !IsAllowedPlayer(player)) return;

                            if ((useMySQL || usePostgres) && player.DesiredFOV != (uint)playerTimers[player.Slot].PlayerFov)
                            {
                                SharpTimerDebug($"{player.PlayerName} has wrong PlayerFov {player.DesiredFOV}... SetFov to {(uint)playerTimers[player.Slot].PlayerFov}");
                                SetFov(player, playerTimers[player.Slot].PlayerFov, true);
                            }
                        });

                        Server.NextFrame(() => InvalidateTimer(player));
                    }
                }
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

            HookEntityOutput("trigger_push", "OnStartTouch", (CEntityIOOutput output, string name, CEntityInstance activator, CEntityInstance caller, CVariant value, float delay) =>
            {
                TriggerPushOnStartTouch(activator, caller);
                return HookResult.Continue;
            });

            AddTimer(1.0f, () =>
            {
                DamageHook();
            });

            AddCommandListener("say", OnPlayerChatAll);
            AddCommandListener("say_team", OnPlayerChatTeam);
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
                    if (playerTimers[player.Slot].IsTimerRunning && playerTimers[player.Slot].currentStyle.Equals(2) && (getMovementButton.Contains("Left") || getMovementButton.Contains("Right"))) //sideways
                    {
                        userCmd.DisableInput(h.GetParam<IntPtr>(movementPtr), 1536); //disable left (512) + right (1024) = 1536
                        baseCmd.DisableSideMove(); //disable side movement
                        return HookResult.Changed;
                    }
                    if (playerTimers[player.Slot].IsTimerRunning && playerTimers[player.Slot].currentStyle.Equals(3) && (getMovementButton.Contains("Left") || getMovementButton.Contains("Right") || getMovementButton.Contains("Backward"))) //only w
                    {
                        userCmd.DisableInput(h.GetParam<IntPtr>(movementPtr), 1552); //disable backward (16) + left (512) + right (1024) = 1552
                        baseCmd.DisableSideMove(); //disable side movement
                        baseCmd.DisableForwardMove(); //set forward move to 0 ONLY if player is moving backwards; ie: disable s
                        return HookResult.Changed;
                    }
                    if (playerTimers[player.Slot].IsTimerRunning && playerTimers[player.Slot].currentStyle.Equals(6) && (getMovementButton.Contains("Forward") || getMovementButton.Contains("Right") || getMovementButton.Contains("Backward"))) //only a
                    {
                        userCmd.DisableInput(h.GetParam<IntPtr>(movementPtr), 1048); //disable backward (16) + forward (8) + right (1024) = 1048
                        baseCmd.DisableSideMove(); //disable only right movement
                        baseCmd.DisableForwardMove(); //disable forward movement
                        return HookResult.Changed;
                    }
                    if (playerTimers[player.Slot].IsTimerRunning && playerTimers[player.Slot].currentStyle.Equals(7) && (getMovementButton.Contains("Forward") || getMovementButton.Contains("Left") || getMovementButton.Contains("Backward"))) //only d
                    {
                        userCmd.DisableInput(h.GetParam<IntPtr>(movementPtr), 536); //disable backward (16) + forward (8) + left (512) = 536
                        baseCmd.DisableSideMove(); //disable only left movement
                        baseCmd.DisableForwardMove(); //disable forward movement
                        return HookResult.Changed;
                    }
                    if (playerTimers[player.Slot].IsTimerRunning && playerTimers[player.Slot].currentStyle.Equals(8) && (getMovementButton.Contains("Forward") || getMovementButton.Contains("Left") || getMovementButton.Contains("Right"))) //only s
                    {
                        userCmd.DisableInput(h.GetParam<IntPtr>(movementPtr), 1544); //disable right (1024) + forward (8) + left (512) = 1544
                        baseCmd.DisableSideMove(); //disable side movement
                        baseCmd.DisableForwardMove(); //disable only forward movement
                        return HookResult.Changed;
                    }
                    return HookResult.Changed;
                }
                catch (Exception ex)
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
            RemoveCommandListener("say", OnPlayerChatAll, HookMode.Pre);
            RemoveCommandListener("say_team", OnPlayerChatTeam, HookMode.Pre);
            RemoveCommandListener("jointeam", OnCommandJoinTeam, HookMode.Pre);
            if (isLinux) RunCommandLinux.Unhook(OnRunCommand, HookMode.Pre);
            else RunCommandWindows.Unhook(OnRunCommand, HookMode.Pre);
            SharpTimerConPrint("Plugin Unloaded");
        }
    }
}
