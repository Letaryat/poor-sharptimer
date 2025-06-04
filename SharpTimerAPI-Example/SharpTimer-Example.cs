using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Utils;
using Microsoft.Extensions.Logging;
using SharpTimerAPI;

public class SharpTimer_Example : BasePlugin
{
    public override string ModuleName => "SharpTimer API Example";
    public override string ModuleVersion => "";
    public override string ModuleAuthor => "";

    public ISharpTimerEventSender? eventSender { get; set; }
    public ISharpTimerManager? timerManager { get; set; }
    public ISharpTimerDatabase? databaseManager { get; set; }

    public override void Load(bool hotReload)
    {
        AddCommand("css_SharpTimerAPI_TimerCheck", "", Command_API_TimerCheck);
        AddCommand("css_SharpTimerAPI_ToggleTimer", "", Command_API_ToggleTimer);
        AddCommand("css_SharpTimerAPI_GetSR", "", Command_API_GetSR);

        RegisterEventHandler<EventPlayerDeath>(EventPlayerDeath);
    }

    public override void Unload(bool hotReload)
    {
        RemoveCommand("css_SharpTimerAPI_TimerCheck", Command_API_TimerCheck);
        RemoveCommand("css_SharpTimerAPI_ToggleTimer", Command_API_ToggleTimer);
        RemoveCommand("css_SharpTimerAPI_GetSR", Command_API_GetSR);

        DeregisterEventHandler<EventPlayerDeath>(EventPlayerDeath);
    }

    public override void OnAllPluginsLoaded(bool hotReload)
    {
        eventSender = ISharpTimerEventSender.Capability.Get();
        timerManager = ISharpTimerManager.Capability.Get();
        databaseManager = ISharpTimerDatabase.Capability.Get();

        if (eventSender == null || timerManager == null || databaseManager == null)
        {
            Logger.LogError("Error: Could not load SharpTimerAPI! Ensure everything is installed properly.");
            return;
        }
    }

    public void Command_API_TimerCheck(CCSPlayerController? player, CommandInfo command)
    {
        if (player == null)
            return;

        if (timerManager == null)
        {
            Logger.LogError("Error: ISharpTimerManager not loaded! Ensure everything is installed properly.");
            return;
        }

        command.ReplyToCommand($" {ChatColors.LightPurple}[SharpTimer-Example] {ChatColors.Grey}Timer: " + (timerManager.IsTimerOn(player) ? $"{ChatColors.Green}ON" : $"{ChatColors.Red}OFF"));
    }

    public void Command_API_ToggleTimer(CCSPlayerController? player, CommandInfo command)
    {
        if (player == null)
            return;

        if (timerManager == null)
        {
            Logger.LogError("Error: ISharpTimerManager not loaded! Ensure everything is installed properly.");
            return;
        }

        timerManager.ToggleTimer(player);

        command.ReplyToCommand($" {ChatColors.LightPurple}[SharpTimer-Example] {ChatColors.Grey}Timer has been toggeled: " + (timerManager.IsTimerOn(player) ? $"{ChatColors.Green}ON" : $"{ChatColors.Red}OFF"));
    }

    public void Command_API_GetSR(CCSPlayerController? player, CommandInfo command)
    {
        if (player == null)
            return;

        if (databaseManager == null)
        {
            Logger.LogError("Error: ISharpTimerDatabase not loaded! Ensure everything is installed properly.");
            return;
        }

        var record = databaseManager.GetSortedRecordsFromDatabase(1, 0, Server.MapName, 0).Result.FirstOrDefault().Value;

        string formattedTime;

        TimeSpan timeSpan = TimeSpan.FromSeconds(record.TimerTicks / 64.0);
        string milliseconds = $"{record.TimerTicks % 64 * (1000.0 / 64.0):000}";
        int totalMinutes = (int)timeSpan.TotalMinutes;

        if (totalMinutes >= 60)
            formattedTime = $"{totalMinutes / 60:D1}:{totalMinutes % 60:D2}:{timeSpan.Seconds:D2}.{milliseconds}";
        else
            formattedTime = $"{totalMinutes:D1}:{timeSpan.Seconds:D2}.{milliseconds}";

        command.ReplyToCommand($" {ChatColors.LightPurple}[SharpTimer-Example] {ChatColors.Grey}player: {ChatColors.White}{record.PlayerName} {ChatColors.Grey}has the SR on {ChatColors.White}{Server.MapName} {ChatColors.Grey}with time: {ChatColors.White}{formattedTime}");
    }


    public HookResult EventPlayerDeath(EventPlayerDeath @event, GameEventInfo gameEventInfo)
    {
        var player = @event.Userid;
        if (player == null || player.IsBot)
            return HookResult.Continue;

        timerManager?.RestartTimer(player);

        player.PrintToChat($" {ChatColors.LightPurple}[SharpTimer-Example] {ChatColors.Red}Timer has been reset, because you died :(");

        return HookResult.Continue;
    }
}