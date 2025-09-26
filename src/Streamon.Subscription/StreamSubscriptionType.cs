namespace Streamon.Subscription;

public enum StreamSubscriptionType
{
    CatchUp,
    Live,
    InMemory
}

public enum DispatchMode
{
    Sequential,
    Parallel
}