using Streamon;

namespace Streamon.Tests.Fixtures;

internal record SalesOrder()
{
    public SalesOrder When<TEvent>(TEvent @event) =>
        @event switch
        {
            OrderCaptured => this with { },
            OrderConfirmed => this with { },
            OrderArchived => this with { },
            OrderFulfilled => this with { },
            _ => this
        };
}
