using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Streamon.Tests.Fixtures;

internal record OrderCaptured(string Id);

internal record OrderConfirmed(string Id);

internal record OrderArchived(string Id);

internal record OrderFulfilled(string Id);
