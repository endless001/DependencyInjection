using System;
using System.Collections.Generic;
using System.Text;

namespace DependencyInjection
{
    public interface IServiceContainer:IScope, IServiceProvider
    {
        IServiceContainer TryAdd(ServiceDefinition serviceDefinition);
        IServiceContainer Add(ServiceDefinition serviceDefinition);
        IServiceContainer CreateScope();
    }
}
