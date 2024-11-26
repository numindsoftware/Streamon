using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Streamon.Subscription;

public interface IEventConsumer
{
    Task DispatchAsync<T>(EventConsumeContext<T> context, CancellationToken cancellationToken = default);
}
