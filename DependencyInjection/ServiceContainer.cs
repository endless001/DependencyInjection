using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;

namespace DependencyInjection
{
    public class ServiceContainer : IServiceContainer
    {
        internal readonly List<ServiceDefinition> _services;

        private readonly ConcurrentDictionary<Type, object> _singletonInstances;

        private readonly ConcurrentDictionary<Type, object> _scopedInstances;

        private readonly List<object> __transientDisposables = new List<object>();

        private readonly bool _isRootScope;

        public ServiceContainer()
        {
            _isRootScope = true;
            _singletonInstances = new ConcurrentDictionary<Type, object>();
            _services = new List<ServiceDefinition>();

        }
         
        internal ServiceContainer(ServiceContainer serviceContainer)
        {
            _isRootScope = false;
            _singletonInstances = serviceContainer._singletonInstances;
            _services = serviceContainer._services;
            _scopedInstances = new ConcurrentDictionary<Type, object>();


        }

        public IServiceContainer CreateScope()
        {
            throw new NotImplementedException();
        }

        public void Dispose()
        {
            throw new NotImplementedException();
        }

        public object GetService(Type serviceType)
        {
            throw new NotImplementedException();
        }
    }
}
