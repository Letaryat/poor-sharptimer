using CounterStrikeSharp.API.Core;

namespace SharpTimerAPI;

public interface ISharpTimerManager
{
    void RestartTimer(CCSPlayerController player);
    bool IsTimerOn(CCSPlayerController player);
    void ToggleTimer(CCSPlayerController player);
    void BlockTimerCmd(CCSPlayerController player, bool block);
    void BlockRespawnCmd(CCSPlayerController player, bool block);
}