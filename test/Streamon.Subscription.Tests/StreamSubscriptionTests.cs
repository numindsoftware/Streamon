using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Streamon.Subscription.Tests;

public class StreamSubscriptionTests
{
    public async Task Test()
    {
        StreamSubscription subscription = new(new SubscriptionId("test-subscription"), new EventHandlerResolver(), new CheckpointStore(), new SubscriptionStreamReader());
        subscription.AddEventHandler<TestEventHandler>();
        await subscription.PollAsync();
    }

}

