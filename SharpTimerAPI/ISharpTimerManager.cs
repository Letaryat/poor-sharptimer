using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Capabilities;

namespace SharpTimerAPI;

public interface ISharpTimerManager
{
    public static readonly PluginCapability<ISharpTimerManager> Capability = new("sharptimer:manager");

    public void RestartTimer(CCSPlayerController player);
    public bool IsTimerOn(CCSPlayerController player);
    public void ToggleTimer(CCSPlayerController player);
    public void BlockTimerCmd(CCSPlayerController player, bool block);
    public void BlockRespawnCmd(CCSPlayerController player, bool block);
}