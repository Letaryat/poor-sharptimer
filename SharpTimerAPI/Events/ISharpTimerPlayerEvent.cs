using CounterStrikeSharp.API.Core;

namespace SharpTimerAPI.Events;
using CounterStrikeSharp;

public interface ISharpTimerPlayerEvent
{
    public CCSPlayerController? Player { get;}
}