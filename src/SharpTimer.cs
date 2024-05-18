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

namespace SharpTimer
{
    [MinimumApiVersion(228)]
    public partial class SharpTimer : BasePlugin
    {
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

            RegisterEventHandler<EventPlayerSpawned>((@event, info) =>
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

                            if (useMySQL && player.DesiredFOV != (uint)playerTimers[player.Slot].PlayerFov)
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

        public override void Unload(bool hotReload)
        {
            DamageUnHook();
            RemoveCommandListener("say", OnPlayerChatAll, HookMode.Pre);
            RemoveCommandListener("say_team", OnPlayerChatTeam, HookMode.Pre);
            RemoveCommandListener("jointeam", OnCommandJoinTeam, HookMode.Pre);
            SharpTimerConPrint("Plugin Unloaded");
        }
    }
}
