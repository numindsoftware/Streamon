namespace Streamon.Subscription;

public class StreamSubscriptionOptions
{
    public StreamSubscriptionType StreamSubscriptionType { get; set; }
    public SubscriptionErrorHandling ErrorHandling { get; set; }
    public EventDispatchType EventDispatchType { get; set; }

}