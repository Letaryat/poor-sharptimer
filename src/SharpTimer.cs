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
using CounterStrikeSharp.API.Core.Capabilities;
using System.Runtime.InteropServices;
using System.Drawing;
using System.Globalization;
using CounterStrikeSharp.API.Modules.Memory;
using FixVectorLeak;

namespace SharpTimer;

public partial class SharpTimer : BasePlugin
{
    private void EnsureConfigFilesExist()
    {
        string cfgDir = Path.Join(gameDir, "csgo", "cfg", "SharpTimer");
        string[] configFiles =
        {
            "admessages.txt",
            "config.cfg",
            "custom_exec.cfg",
            "discordConfig.json",
            "mysqlConfig.json",
            "postgresConfig.json",
            "ranks.json"
        };

        foreach (string fileName in configFiles)
        {
            try
            {
                string realPath = Path.Join(cfgDir, fileName);
                if (File.Exists(realPath)) continue;

                string examplePath = Path.Join(cfgDir, $"example.{fileName}");
                if (!File.Exists(examplePath))
                {
                    Utils.LogError($"Missing config template: example.{fileName}");
                    continue;
                }

                File.Copy(examplePath, realPath);
                Utils.LogDebug($"Created {fileName} from example.{fileName}");
            }
            catch (Exception ex)
            {
                Utils.LogError($"Failed to create {fileName} from template: {ex.Message}");
            }
        }

        string mapDataDir = Path.Join(cfgDir, "MapData");
        EnsureExampleConfigsInDir(Path.Join(mapDataDir, "local_data"));
        EnsureExampleConfigsInDir(Path.Join(mapDataDir, "MapExecs"));
    }

    // Copies every "example.<name>" in dir to "<name>" when the real file is missing.
    private void EnsureExampleConfigsInDir(string dir)
    {
        try
        {
            if (!Directory.Exists(dir)) return;

            foreach (string examplePath in Directory.GetFiles(dir, "example.*"))
            {
                try
                {
                    string realName = Path.GetFileName(examplePath).Substring("example.".Length);
                    string realPath = Path.Join(dir, realName);
                    if (File.Exists(realPath)) continue;

                    File.Copy(examplePath, realPath);
                    Utils.LogDebug($"Created {realName} from example.{realName}");
                }
                catch (Exception ex)
                {
                    Utils.LogError($"Failed to create file from {Path.GetFileName(examplePath)}: {ex.Message}");
                }
            }
        }
        catch (Exception ex)
        {
            Utils.LogError($"Failed to process example configs in {dir}: {ex.Message}");
        }
    }

    public override void Load(bool hotReload)
    {
        Instance = this;

        Utils = new Utils(this);

        Capabilities.RegisterPluginCapability(StEventSenderCapability, () => new SharpTimerAPI_EventSender());
        Capabilities.RegisterPluginCapability(StManagerCapability, () => new SharpTimerAPI_Manager());
        Capabilities.RegisterPluginCapability(StDatabaseCapability, () => new SharpTimerAPI_Database());

        // Utils.CheckForUpdate(); // Not currently needed

        defaultServerHostname = ConVar.Find("hostname")!.StringValue;

        gameDir = Server.GameDirectory;
        Utils.LogDebug($"Set gameDir to {gameDir}");

        EnsureConfigFilesExist();
        Server.ExecuteCommand($"execifexists SharpTimer/config.cfg");

        CheckMissingFakeConvars();

        currentMapName = Server.MapName;

        string recordsFileName = $"SharpTimer/PlayerRecords/";
        playerRecordsPath = Path.Join(gameDir + "/csgo/cfg", recordsFileName);

        isLinux = RuntimeInformation.IsOSPlatform(OSPlatform.Linux);

        movementServices = isLinux ? 0 : 3;
        movementPtr = isLinux ? 1 : 2;
        RunCommand = isLinux ? new RunCommandLinux() : new RunCommandWindows();

        try
        {
            if (isLinux)
                RunCommand.Hook(OnRunCommandPre, HookMode.Pre);
        }
        catch (Exception)
        {
            Utils.LogError($"RunCommand hook failed. Signature is likely outdated. Check for the latest stgamedata.json file on GitHub. Movement features disabled until updated.");
        }

        try
        {
            SnapBaseAngles = new SnapBaseAngles();
        }
        catch (Exception)
        {
            Utils.LogError($"SnapBaseAngles bind failed. Signature is likely outdated. Check for the latest stgamedata.json file on GitHub. Teleport view angles will not snap until updated.");
        }

        if (disableDamage)
            RegisterListener<Listeners.OnPlayerTakeDamagePre>(OnPlayerTakeDamagePre);

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
        if (isLinux)
            RunCommand?.Unhook(OnRunCommandPre, HookMode.Pre);

        if (disableDamage)
            RemoveListener<Listeners.OnPlayerTakeDamagePre>(OnPlayerTakeDamagePre);

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

    private HookResult OnRunCommandPre(DynamicHook h)
    {
        var player = h.GetParam<CCSPlayer_MovementServices>(movementServices).Pawn.Value.Controller.Value
            ?.As<CCSPlayerController>();

        if (player == null || player.IsBot || !player.IsValid || player.IsHLTV) return HookResult.Continue;

        var userCmd = new CUserCmd(h.GetParam<IntPtr>(movementPtr));
        var baseCmd = userCmd.GetBaseCmd();
        var getMovementButton = userCmd.GetMovementButton();

        if (player != null && !player.IsBot && player.IsValid && !player.IsHLTV)
        {
            try
            {
                ApplyModeCvars(player);

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
                    ParseStrafes(player, new(viewAngle.X, viewAngle.Y, viewAngle.Z));
                }
                
                // Startzonejump
                if (startzoneSingleJumpEnabled && (playerTimers[player.Slot].inStartzone || playerTimers[player.Slot].CurrentZoneInfo.InBonusStartZone) && playerTimers[player.Slot].StartZoneJumps >= 1)
                {
                    baseCmd.DisableForwardMove();
                    baseCmd.DisableSideMove();
                    return HookResult.Changed;
                }

                // Style Stuff
                if ((playerTimers[player.Slot].IsTimerRunning || playerTimers[player.Slot].IsBonusTimerRunning) &&
                    playerTimers[player.Slot].currentStyle.Equals(2) && (moveLeft || moveRight)) //sideways
                {
                    userCmd.DisableInput(h.GetParam<IntPtr>(movementPtr),
                        1536); //disable left (512) + right (1024) = 1536
                    baseCmd.DisableSideMove(); //disable side movement
                    return HookResult.Changed;
                }

                if ((playerTimers[player.Slot].IsTimerRunning || playerTimers[player.Slot].IsBonusTimerRunning) &&
                    playerTimers[player.Slot].currentStyle.Equals(9) && (moveLeft || moveRight) &&
                    !(moveForward || moveBackward)) //halfsideways
                {
                    userCmd.DisableInput(h.GetParam<IntPtr>(movementPtr),
                        1536); //disable left (512) + right (1024) = 1536
                    baseCmd.DisableSideMove(); //disable side movement
                    return HookResult.Changed;
                }

                if ((playerTimers[player.Slot].IsTimerRunning || playerTimers[player.Slot].IsBonusTimerRunning) &&
                    playerTimers[player.Slot].currentStyle.Equals(9) && !(moveLeft || moveRight) &&
                    (moveForward || moveBackward)) //halfsideways pt2
                {
                    userCmd.DisableInput(h.GetParam<IntPtr>(movementPtr),
                        24); //disable backward (16) + forward (8) = 24
                    baseCmd.DisableForwardMove(); //disable forward movement
                    return HookResult.Changed;
                }

                if ((playerTimers[player.Slot].IsTimerRunning || playerTimers[player.Slot].IsBonusTimerRunning) &&
                    playerTimers[player.Slot].currentStyle.Equals(3) &&
                    (moveLeft || moveRight || moveBackward)) //only w
                {
                    userCmd.DisableInput(h.GetParam<IntPtr>(movementPtr),
                        1552); //disable backward (16) + left (512) + right (1024) = 1552
                    baseCmd.DisableSideMove(); //disable side movement
                    baseCmd.DisableForwardMove(); //set forward move to 0 ONLY if player is moving backwards; ie: disable s
                    return HookResult.Changed;
                }

                if ((playerTimers[player.Slot].IsTimerRunning || playerTimers[player.Slot].IsBonusTimerRunning) &&
                    playerTimers[player.Slot].currentStyle.Equals(6) &&
                    (moveForward || moveRight || moveBackward)) //only a
                {
                    userCmd.DisableInput(h.GetParam<IntPtr>(movementPtr),
                        1048); //disable backward (16) + forward (8) + right (1024) = 1048
                    baseCmd.DisableSideMove(); //disable only right movement
                    baseCmd.DisableForwardMove(); //disable forward movement
                    return HookResult.Changed;
                }

                if ((playerTimers[player.Slot].IsTimerRunning || playerTimers[player.Slot].IsBonusTimerRunning) &&
                    playerTimers[player.Slot].currentStyle.Equals(7) &&
                    (moveForward || moveLeft || moveBackward)) //only d
                {
                    userCmd.DisableInput(h.GetParam<IntPtr>(movementPtr),
                        536); //disable backward (16) + forward (8) + left (512) = 536
                    baseCmd.DisableSideMove(); //disable only left movement
                    baseCmd.DisableForwardMove(); //disable forward movement
                    return HookResult.Changed;
                }

                if ((playerTimers[player.Slot].IsTimerRunning || playerTimers[player.Slot].IsBonusTimerRunning) &&
                    playerTimers[player.Slot].currentStyle.Equals(8) && (moveForward || moveLeft || moveRight)) //only s
                {
                    userCmd.DisableInput(h.GetParam<IntPtr>(movementPtr),
                        1544); //disable right (1024) + forward (8) + left (512) = 1544
                    baseCmd.DisableSideMove(); //disable side movement
                    baseCmd.DisableForwardMove(); //disable only forward movement
                    return HookResult.Changed;
                }

                if ((playerTimers[player.Slot].IsTimerRunning || playerTimers[player.Slot].IsBonusTimerRunning) &&
                    playerTimers[player.Slot].currentStyle.Equals(11) && usingUse) //parachute
                {
                    Schema.SetSchemaValue(player!.Pawn.Value!.Handle, "CBaseEntity", "m_flActualGravityScale", 0.2f);
                    return HookResult.Changed;
                }

                if ((playerTimers[player.Slot].IsTimerRunning || playerTimers[player.Slot].IsBonusTimerRunning) &&
                    playerTimers[player.Slot].currentStyle.Equals(11) && !usingUse) //parachute
                {
                    Schema.SetSchemaValue(player!.Pawn.Value!.Handle, "CBaseEntity", "m_flActualGravityScale", 1f);
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

    private void TeleportPlayerWithViewAngles(CCSPlayerController player, Vector_t? position, QAngle_t angles, Vector_t? velocity)
    {
        var pawn = player.PlayerPawn.Value;
        if (pawn == null || !pawn.IsValid)
            return;

        pawn.Teleport(position, angles, velocity);
        SnapBaseAngles.Snap(pawn, angles);
    }

    // Reused across ticks
    private readonly List<uint> _hideTransmitPawnIndices = new(64);

    private void CheckTransmit(CCheckTransmitInfoList infoList)
    {
        // Early out
        bool anyHiding = false;
        foreach (var t in playerTimers.Values)
        {
            if (t != null && t.HidePlayers)
            {
                anyHiding = true;
                break;
            }
        }

        if (!anyHiding)
            return;

        // Collect every player pawn index once per tick
        var pawnIndices = _hideTransmitPawnIndices;
        pawnIndices.Clear();
        foreach (var target in Utilities.GetPlayers())
        {
            if (target == null || target.IsHLTV || !target.IsValid)
                continue;

            var pawnHandle = target.Pawn;
            if (pawnHandle == null || !pawnHandle.IsValid)
                continue;

            pawnIndices.Add(pawnHandle.Index);
        }

        if (pawnIndices.Count == 0)
            return;

        foreach ((CCheckTransmitInfo info, CCSPlayerController? player) in infoList)
        {
            if (player == null || player.IsBot || !player.IsValid || player.IsHLTV)
                continue;

            int slot = player.Slot;
            if (!playerTimers.TryGetValue(slot, out var timer) || timer == null || !timer.HidePlayers)
                continue;

            if (!connectedPlayers.ContainsKey(slot))
                continue;

            var viewerPawn = player.Pawn?.Value;
            if (viewerPawn == null || viewerPawn.As<CCSPlayerPawnBase>().PlayerState == CSPlayerState.STATE_OBSERVER_MODE)
                continue;

            uint ownIndex = viewerPawn.Index;
            foreach (uint pawnIndex in pawnIndices)
            {
                if (pawnIndex == ownIndex)
                    continue;

                info.TransmitEntities.Remove(pawnIndex);
            }
        }
    }

    private HookResult EventPlayerConnectFull(EventPlayerConnectFull @event, GameEventInfo @eventInfo)
    {
        var player = @event.Userid;
        if (player == null || !player.Valid())
            return HookResult.Continue;

        OnPlayerConnect(player);

        return HookResult.Continue;
    }

    private HookResult EventPlayerTeam(EventPlayerTeam @event, GameEventInfo @eventInfo)
    {
        var player = @event.Userid;
        if (player == null || !player.Valid()) return HookResult.Continue;

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
        //fck this shit game, entities doesnt seem to spawn so logs get confusing af
        if (Utils.PlayersCount() <= 0)
            return HookResult.Continue;

        ClearMapData();
        LoadMapData(Server.MapName);
        return HookResult.Continue;
    }

    private HookResult EventRoundEnd(EventRoundEnd @event, GameEventInfo @eventInfo)
    {
        foreach (CCSPlayerController player in connectedPlayers.Values)
            InvalidateTimer(player);

        return HookResult.Continue;
    }

    private HookResult EventPlayerSpawn(EventPlayerSpawn @event, GameEventInfo @eventInfo)
    {
        var player = @event.Userid;
        if (player == null || !player.Valid())
            return HookResult.Continue;

        var playerPawn = player.PlayerPawn();
        if (playerPawn == null)
            return HookResult.Continue;

        //just.. dont ask.
        AddTimer(0f, () =>
        {
            if (spawnOnRespawnPos == true && currentRespawnPos != null)
                playerPawn.Teleport(currentRespawnPos);

            if (removeLegsEnabled == true)
            {
                Server.NextWorldUpdate(() =>
                {
                    playerPawn.Render = Color.FromArgb(254, playerPawn.Render.R, playerPawn.Render.G, playerPawn.Render.B);
                    Utilities.SetStateChanged(playerPawn, "CBaseModelEntity", "m_clrRender");
                });
            }
        });

        if (apiKey != "")
        {
            Server.NextFrame(() =>
            {
                if (player == null || !player.IsValid) return;
                long steamId = (long)player.SteamID;
                string playerName = player.PlayerName;
                int slot = player.Slot;
                _ = Task.Run(async () =>
                {
                    try
                    {
                        await UpdatePlayerAsync(steamId, playerName);
                        int playerId = await GetPlayerIDAsync(steamId);
                        Server.NextFrame(() => { if (!globalDisabled) playerCache.PlayerID[slot] = playerId; });
                    }
                    catch (Exception ex)
                    {
                        Server.NextFrame(() => Utils.LogError($"Error caching global player ID: {ex.Message}"));
                    }
                });
            });
        }

        if (playerTimers.TryGetValue(player.Slot, out var playerTimer))
        {
            playerTimer.GivenWeapon = false;

            if (enableStyles)
                setStyle(player, playerTimers[player.Slot].currentStyle);

            Server.NextFrame( () =>
            {
                if (!string.IsNullOrEmpty(playerTimer.Mode) &&
                    TryParseMode(playerTimer.Mode.ToLower(), out Mode newMode) &&
                    newMode != defaultMode)
                {
                    SetPlayerMode(player, newMode);
                    Utils.LogDebug($"Player has been set to custom mode: {playerTimer.Mode}");
                }
                else
                {
                    SetPlayerMode(player, defaultMode);
                    Utils.LogDebug($"Player has been set to default mode: {playerTimer.Mode}");
                }
            });

            AddTimer(3.0f, () =>
            {
                if (enableDb && playerTimers.ContainsKey(player.Slot) &&
                    player.DesiredFOV != (uint)playerTimers[player.Slot].PlayerFov)
                {
                    Utils.LogDebug(
                        $"{player.PlayerName} has wrong PlayerFov {player.DesiredFOV}... SetFov to {(uint)playerTimers[player.Slot].PlayerFov}");
                    SetFov(player, playerTimers[player.Slot].PlayerFov, true);
                }
            });

            Server.NextFrame(() => InvalidateTimer(player));
        }

        return HookResult.Continue;
    }

    private HookResult EventPlayerDisconnect(EventPlayerDisconnect @event, GameEventInfo @eventInfo)
    {
        var player = @event.Userid;
        if (player == null || !player.Valid())
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
