namespace Streamon.Subscription;

public interface IEventHandlerResolver
{
    IEventHandler Resolve(Type handlerType);
}
