using CounterStrikeSharp.API.Core.Capabilities;
using SharpTimerAPI.Events;

namespace SharpTimerAPI;

public interface ISharpTimerEventSender
{
    public static readonly PluginCapability<ISharpTimerEventSender> Capability = new("sharptimer:event_sender");

    public event EventHandler<ISharpTimerPlayerEvent> STEventSender;
    public void TriggerEvent(ISharpTimerPlayerEvent @event);
}