using SharpTimerAPI.Events;

namespace SharpTimerAPI;

public interface ISharpTimerEventSender
{
    public event EventHandler<ISharpTimerPlayerEvent> STEventSender;
    public void TriggerEvent(ISharpTimerPlayerEvent @event);
}