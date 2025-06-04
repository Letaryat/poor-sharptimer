using CounterStrikeSharp.API.Core;
using SharpTimerAPI.Events;
using SharpTimerAPI;

namespace SharpTimer;

public class SharpTimerAPI_Manager : ISharpTimerManager
{
    public void RestartTimer(CCSPlayerController player)
    {
        SharpTimer.Instance.RespawnPlayer(player);
    }

    public bool IsTimerOn(CCSPlayerController player)
    {
        return !SharpTimer.Instance.playerTimers[player.Slot].IsTimerBlocked;
    }

    public void ToggleTimer(CCSPlayerController player)
    {
        //should probably create a method that actually forces timer toggle.
        //this one wont toggle if player is in command cd or is mid replay.
        SharpTimer.Instance.ForceStopTimer(player, null!);
    }

    public void BlockTimerCmd(CCSPlayerController player, bool block)
    {
        SharpTimer.Instance.playerTimers[player.Slot].TimerCmdBlocked = block;
    }

    public void BlockRespawnCmd(CCSPlayerController player, bool block)
    {
        SharpTimer.Instance.playerTimers[player.Slot].RespawnCmdBlocked = block;
    }
}

public class SharpTimerAPI_EventSender : ISharpTimerEventSender
{
    public void TriggerEvent(ISharpTimerPlayerEvent @event)
    {
        STEventSender?.Invoke(this, @event);
    }

    public event EventHandler<ISharpTimerPlayerEvent>? STEventSender;
}

public class SharpTimerAPI_Database : ISharpTimerDatabase
{
    public async Task<Dictionary<int, ISharpTimerDatabase.PlayerRecord>> GetSortedRecordsFromDatabase(int limit = 0, int bonusX = 0, string mapName = "", int style = 0)
    {
        var sortedRecords = await SharpTimer.Instance.GetSortedRecordsFromDatabase(limit, bonusX, mapName, style);
        var mappedRecords = sortedRecords.ToDictionary(
            kvp => kvp.Key,
            kvp => new ISharpTimerDatabase.PlayerRecord
            {
                RecordID = kvp.Value.RecordID,
                PlayerName = kvp.Value.PlayerName,
                SteamID = kvp.Value.SteamID,
                MapName = kvp.Value.MapName,
                TimerTicks = kvp.Value.TimerTicks,
                Replay = kvp.Value.Replay
            });
        return mappedRecords;
    }

    public async Task<List<ISharpTimerDatabase.PlayerRecord>> GetAllSortedRecordsFromDatabase(int limit = 0, int bonusX = 0, int style = 0)
    {
        var sortedRecords = await SharpTimer.Instance.GetAllSortedRecordsFromDatabase(limit, bonusX, style);
        var mappedRecords = sortedRecords.Select(record => new ISharpTimerDatabase.PlayerRecord
        {
            RecordID = record.RecordID,
            PlayerName = record.PlayerName,
            SteamID = record.SteamID,
            MapName = record.MapName,
            TimerTicks = record.TimerTicks,
            Replay = record.Replay
        }).ToList();
        return mappedRecords;
    }
}
