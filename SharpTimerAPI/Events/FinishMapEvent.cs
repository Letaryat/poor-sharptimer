using CounterStrikeSharp.API.Core;

namespace SharpTimerAPI.Events;

public record FinishMapEvent(CCSPlayerController? Player, bool IsSr, bool IsPb, int Tier) : ISharpTimerPlayerEvent;