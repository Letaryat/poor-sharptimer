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
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Cvars;
using CounterStrikeSharp.API.Modules.Memory.DynamicFunctions;
using CounterStrikeSharp.API.Modules.Timers;
using CounterStrikeSharp.API.Modules.UserMessages;
using FixVectorLeak;
using System.Runtime.InteropServices;

namespace SharpTimer;

public partial class SharpTimer : BasePlugin
{
    public override string ModuleName => "SharpTimer";
    public override string ModuleVersion => $"0.3.1w";
    public override string ModuleAuthor => "dea & sharptimer community";
    public override string ModuleDescription => "A CS2 Timer Plugin";

    public Utils Utils = null!;
    public RemoveDamage RemoveDamage = null!;

    public override void Load(bool hotReload)
    {
        Utils = new Utils(this);
        RemoveDamage = new RemoveDamage(this);

        Utils.CheckForUpdate();

        defaultServerHostname = ConVar.Find("hostname")!.StringValue;
        Server.ExecuteCommand($"execifexists SharpTimer/config.cfg");

        gameDir = Server.GameDirectory;
        Utils.LogDebug($"Set gameDir to {gameDir}");

        float randomf = new Random().Next(5, 31);
        if (apiKey != "")
            AddTimer(randomf, () => CheckCvarsAndMaxVelo(), TimerFlags.REPEAT);

        currentMapName = Server.MapName;

        string recordsFileName = $"SharpTimer/PlayerRecords/";
        playerRecordsPath = Path.Join(gameDir + "/csgo/cfg", recordsFileName);

        isLinux = RuntimeInformation.IsOSPlatform(OSPlatform.Linux) ? true : false;

        movementServices = isLinux ? 0 : 3;
        movementPtr = isLinux ? 1 : 2;
        RunCommand = isLinux ? new RunCommandLinux() : new RunCommandWindows();

        if (isLinux) RunCommand?.Hook(OnRunCommand, HookMode.Pre);
        StateTransition.Hook(Hook_StateTransition, HookMode.Post);
        RemoveDamage?.Hook();

        RegisterListener<Listeners.OnMapStart>(OnMapStartHandler);
        RegisterListener<Listeners.OnTick>(PlayerOnTick);
        RegisterListener<Listeners.CheckTransmit>(CheckTransmit);

        RegisterEventHandler<EventPlayerConnectFull>(EventPlayerConnectFull);
        RegisterEventHandler<EventPlayerTeam>(EventPlayerTeam);
        RegisterEventHandler<EventRoundStart>(EventRoundStart);
        RegisterEventHandler<EventRoundEnd>(EventRoundEnd);
        RegisterEventHandler<EventPlayerSpawn>(EventPlayerSpawn);
        RegisterEventHandler<EventPlayerDisconnect>(EventPlayerDisconnect);
        RegisterEventHandler<EventWeaponFire>(EventWeaponFire);

        AddCommandListener("jointeam", OnCommandJoinTeam, HookMode.Pre);

        HookUserMessage(452, OnUserMessage_RemoveSound, HookMode.Pre);
        HookUserMessage(369, OnUserMessage_RemoveSound, HookMode.Pre);
        HookUserMessage(208, OnUserMessage_RemoveSound, HookMode.Pre);

        HookEntityOutput("trigger_multiple", "OnStartTouch", TriggerMultiple_OnStartTouch, HookMode.Pre);
        HookEntityOutput("trigger_multiple", "OnEndTouch", TriggerMultiple_OnEndTouch, HookMode.Pre);

        HookEntityOutput("trigger_teleport", "OnStartTouch", TriggerTeleport_OnStartTouch, HookMode.Pre);
        HookEntityOutput("trigger_teleport", "OnEndTouch", TriggerTeleport_OnEndTouch, HookMode.Pre);
    }

    public override void Unload(bool hotReload)
    {
        if (isLinux) RunCommand?.Unhook(OnRunCommand, HookMode.Pre);
        StateTransition.Unhook(Hook_StateTransition, HookMode.Post);
        RemoveDamage?.Unhook();

        RemoveListener<Listeners.OnMapStart>(OnMapStartHandler);
        RemoveListener<Listeners.OnTick>(PlayerOnTick);
        RemoveListener<Listeners.CheckTransmit>(CheckTransmit);

        DeregisterEventHandler<EventPlayerConnectFull>(EventPlayerConnectFull);
        DeregisterEventHandler<EventPlayerTeam>(EventPlayerTeam);
        DeregisterEventHandler<EventRoundStart>(EventRoundStart);
        DeregisterEventHandler<EventRoundEnd>(EventRoundEnd);
        DeregisterEventHandler<EventPlayerSpawn>(EventPlayerSpawn);
        DeregisterEventHandler<EventPlayerDisconnect>(EventPlayerDisconnect);
        DeregisterEventHandler<EventWeaponFire>(EventWeaponFire);

        RemoveCommandListener("jointeam", OnCommandJoinTeam, HookMode.Pre);

        UnhookUserMessage(452, OnUserMessage_RemoveSound, HookMode.Pre);
        UnhookUserMessage(369, OnUserMessage_RemoveSound, HookMode.Pre);
        UnhookUserMessage(208, OnUserMessage_RemoveSound, HookMode.Pre);

        UnhookEntityOutput("trigger_multiple", "OnStartTouch", TriggerMultiple_OnStartTouch, HookMode.Pre);
        UnhookEntityOutput("trigger_multiple", "OnEndTouch", TriggerMultiple_OnEndTouch, HookMode.Pre);

        UnhookEntityOutput("trigger_teleport", "OnStartTouch", TriggerTeleport_OnStartTouch, HookMode.Pre);
        UnhookEntityOutput("trigger_teleport", "OnEndTouch", TriggerTeleport_OnEndTouch, HookMode.Pre);
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
                var usingUse = getMovementButton.Contains("Use");

                // AC Stuff
                if (useAnticheat)
                {
                    ParseInputs(player, baseCmd.GetSideMove(), moveLeft, moveRight);
                    QAngle_t viewAngle = userCmd.GetViewAngles()!.Value;
                    ParseStrafes(player, new (viewAngle.X, viewAngle.Y, viewAngle.Z));
                }
                    
                // Style Stuff
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
                if ((playerTimers[player.Slot].IsTimerRunning || playerTimers[player.Slot].IsBonusTimerRunning) && playerTimers[player.Slot].currentStyle.Equals(11) && usingUse) //parachute
                {
                    player.Pawn.Value!.GravityScale = 0.2f;
                    return HookResult.Changed;
                }
                if ((playerTimers[player.Slot].IsTimerRunning || playerTimers[player.Slot].IsBonusTimerRunning) && playerTimers[player.Slot].currentStyle.Equals(11) && !usingUse) //parachute
                {
                    player.Pawn.Value!.GravityScale = 1f;
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
    private HookResult Hook_StateTransition(DynamicHook h)
    {
        var player = h.GetParam<CCSPlayerPawn>(0).OriginalController.Value;
        var state = h.GetParam<CSPlayerState>(1);

        if (player is null) return HookResult.Continue;

        if (state != _oldPlayerState[player.Index])
        {
            if (state == CSPlayerState.STATE_OBSERVER_MODE || _oldPlayerState[player.Index] == CSPlayerState.STATE_OBSERVER_MODE)
                ForceFullUpdate(player);
        }

        _oldPlayerState[player.Index] = state;

        return HookResult.Continue;
    }
    private void ForceFullUpdate(CCSPlayerController? player)
    {
        if (player is null || !player.IsValid) return;

        var networkGameServer = networkServerService.GetIGameServer();
        networkGameServer.GetClientBySlot(player.Slot)?.ForceFullUpdate();

        player.PlayerPawn.Value?.Teleport(null, player.PlayerPawn.Value.EyeAngles, null);
    }

    private void CheckTransmit(CCheckTransmitInfoList infoList)
    {
        IEnumerable<CCSPlayerController> players = Utilities.FindAllEntitiesByDesignerName<CCSPlayerController>("cs_player_controller");

        if (!players.Any())
            return;

        foreach ((CCheckTransmitInfo info, CCSPlayerController? player) in infoList)
        {
            if (player == null || player.IsBot || !player.IsValid || player.IsHLTV)
                continue;

            if (!connectedPlayers.TryGetValue(player.Slot, out var connected) || connected == null)
                continue;

            if (!playerTimers.TryGetValue(player.Slot, out var timer) || timer == null || !timer.HidePlayers)
                continue;

            foreach (var target in Utilities.GetPlayers())
            {
                if (target == null || target.IsHLTV || target.IsBot || !target.IsValid)
                    continue;

                var pawn = target.Pawn?.Value;
                if (pawn is null)
                    continue;

                var playerPawn = player.Pawn.Value?.As<CCSPlayerPawnBase>().PlayerState;
                if (playerPawn == null || playerPawn == CSPlayerState.STATE_OBSERVER_MODE)
                    continue;

                if (pawn == player.Pawn.Value)
                    continue;

                if ((LifeState_t)pawn.LifeState != LifeState_t.LIFE_ALIVE)
                {
                    info.TransmitEntities.Remove(pawn);
                    continue;
                }

                info.TransmitEntities.Remove(pawn);
            }
        }
    }

    private HookResult EventPlayerConnectFull(EventPlayerConnectFull @event, GameEventInfo @eventInfo)
    {
        var player = @event.Userid;
        if (player == null || player.NotValid())
            return HookResult.Continue;

        OnPlayerConnect(player);
        _oldPlayerState[player.Index] = CSPlayerState.STATE_WELCOME;

        return HookResult.Continue;
    }

    private HookResult EventPlayerTeam(EventPlayerTeam @event, GameEventInfo @eventInfo)
    {
        var player = @event.Userid;
        if (player == null) return HookResult.Continue;

        Server.NextFrame(() =>
        {
            InvalidateTimer(player);
            try
            {
                if (playerTimers.TryGetValue(player.Slot, out var data) && data.IsReplaying)
                    StopReplay(player);
            }
            catch (Exception ex)
            {
                // playerTimers for requested player does not exist
                Utils.LogError("(EventPlayerTeam) " + ex.Message);
            }
        });

        return HookResult.Continue;
    }

    private HookResult EventRoundStart(EventRoundStart @event, GameEventInfo @eventInfo)
    {
        var mapName = Server.MapName;
        LoadMapData(mapName);
        Utils.LogDebug($"Loading MapData on RoundStart...");
        return HookResult.Continue;
    }

    private HookResult EventRoundEnd(EventRoundEnd @event, GameEventInfo @eventInfo)
    {
        foreach (CCSPlayerController player in connectedPlayers.Values)
        {
            InvalidateTimer(player);
        }
        return HookResult.Continue;
    }

    private HookResult EventPlayerSpawn(EventPlayerSpawn @event, GameEventInfo @eventInfo)
    {
        var player = @event.Userid;
        if (player == null || player.NotValid())
            return HookResult.Continue;

        //just.. dont ask.
        AddTimer(0f, () =>
        {
            if (spawnOnRespawnPos == true && currentRespawnPos != null)
                player!.PlayerPawn.Value!.Teleport(currentRespawnPos, player.PlayerPawn.Value?.EyeAngles.ToQAngle_t());
        });

        if (enableStyles && playerTimers.ContainsKey(player.Slot))
            setStyle(player, playerTimers[player.Slot].currentStyle);

        AddTimer(3.0f, () =>
        {
            if (enableDb && playerTimers.ContainsKey(player.Slot) && player.DesiredFOV != (uint)playerTimers[player.Slot].PlayerFov)
            {
                Utils.LogDebug($"{player.PlayerName} has wrong PlayerFov {player.DesiredFOV}... SetFov to {(uint)playerTimers[player.Slot].PlayerFov}");
                SetFov(player, playerTimers[player.Slot].PlayerFov, true);
            }
        });

        Server.NextFrame(() => InvalidateTimer(player));

        if (enableReplays && enableSRreplayBot && replayBotController == null)
            _ = Task.Run(async () => await SpawnReplayBot());

        return HookResult.Continue;
    }

    private HookResult EventPlayerDisconnect(EventPlayerDisconnect @event, GameEventInfo @eventInfo)
    {
        var player = @event.Userid;
        if (player == null || player.NotValid())
            return HookResult.Continue;

        OnPlayerDisconnect(player);

        return HookResult.Continue;
    }

    private HookResult EventWeaponFire(EventWeaponFire @event, GameEventInfo @eventInfo)
    {
        if (@event.Userid == null || !@event.Userid.IsValid) return HookResult.Continue;

        var player = @event.Userid;

        if (!applyInfiniteAmmo)
            return HookResult.Continue;

        var activeWeaponHandle = player.PlayerPawn.Value?.WeaponServices?.ActiveWeapon;
        if (activeWeaponHandle?.Value != null)
        {
            activeWeaponHandle.Value.Clip1 = 100;
            activeWeaponHandle.Value.ReserveAmmo[0] = 100;
        }

        return HookResult.Continue;
    }

    private HookResult OnCommandJoinTeam(CCSPlayerController? player, CommandInfo commandInfo)
    {
        if (player == null || !player.IsValid) return HookResult.Handled;
        InvalidateTimer(player);
        return HookResult.Continue;
    }

    private HookResult OnUserMessage_RemoveSound(UserMessage um)
    {
        foreach (var p in connectedPlayers)
        {
            if (connectedPlayers.TryGetValue(p.Key, out var player))
            {
                if (player is null || !player.IsValid)
                    return HookResult.Continue;

                if (playerTimers[player.Slot].HidePlayers)
                    um.Recipients.Remove(player);
            }
        }

        return HookResult.Continue;
    }
}