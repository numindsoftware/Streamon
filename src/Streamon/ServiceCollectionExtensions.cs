using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Streamon;
public static class ServiceCollectionExtensions
{
    public static StreamProvisionerBuilder AddStreamon(this IServiceCollection services)
    {
        return new StreamProvisionerBuilder(services);
    }
}
