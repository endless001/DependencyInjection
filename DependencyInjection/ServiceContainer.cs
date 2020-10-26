using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;

namespace DependencyInjection
{
    public class ServiceContainer : IServiceContainer
    {
        internal readonly ConcurrentBag<ServiceDefinition> _services;

        private readonly ConcurrentDictionary<ServiceKey, object> _singletonInstances;

        private readonly ConcurrentDictionary<ServiceKey, object> _scopedInstances;

        private ConcurrentBag<object> _transientDisposables = new ConcurrentBag<object>();


        private readonly bool _isRootScope;

        private bool _disposed;


        public ServiceContainer()
        {
            _isRootScope = true;
            _singletonInstances = new ConcurrentDictionary<ServiceKey, object>();
            _services = new ConcurrentBag<ServiceDefinition>();

        }

        internal ServiceContainer(ServiceContainer serviceContainer)
        {
            _isRootScope = false;
            _singletonInstances = serviceContainer._singletonInstances;
            _services = serviceContainer._services;
            _scopedInstances = new ConcurrentDictionary<ServiceKey, object>();


        }

        public IServiceContainer Add(ServiceDefinition serviceDefinition)
        {
            if (_disposed)
            {
                throw new InvalidOperationException("the service container had been disposed");
            }
            if (_services.Any(a => a.ServiceType == serviceDefinition.ServiceType && a.GetImplementType() == serviceDefinition.GetImplementType()))
            {
                return this;
            }
            _services.Add(serviceDefinition);
            return this;
        }

        public IServiceContainer CreateScope()
        {
            return new ServiceContainer(this);
        }


        public object GetService(Type serviceType)
        {
            if (_disposed)
            {
                throw new InvalidOperationException($"can not get scope service from a disposed scope, serviceType: {serviceType.FullName}");
            }
            var serviceDefinition = _services.LastOrDefault(a => a.ServiceType == serviceType);
            if (null == serviceDefinition)
            {
                if (serviceType.IsGenericType)
                {
                    var genericType = serviceType.GetGenericTypeDefinition();
                    serviceDefinition = _services.LastOrDefault(a => a.ServiceType == genericType);
                    if (null == serviceDefinition)
                    {
                        var innerServiceType = serviceType.GetGenericArguments().First();
                        if (typeof(IEnumerable<>).MakeGenericType(innerServiceType)
                            .IsAssignableFrom(serviceType))
                        {
                            var innerRegType = innerServiceType;
                            if (innerServiceType.IsGenericType)
                            {
                                innerRegType = innerServiceType.GetGenericTypeDefinition();
                            }
                            var list = new List<object>(4);
                            foreach (var def in _services.Where(a => a.ServiceType == innerRegType))
                            {
                                object svc;
                                if (def.ServiceLifetime == ServiceLifetime.Singleton)
                                {
                                    svc = _singletonInstances.GetOrAdd(new ServiceKey(innerServiceType, def), (t) => GetServiceInstance(innerServiceType, def));
                                }
                                else if (def.ServiceLifetime == ServiceLifetime.Scoped)
                                {
                                    svc = _scopedInstances.GetOrAdd(new ServiceKey(innerServiceType, def), (t) => GetServiceInstance(innerServiceType, def));
                                }
                                else
                                {
                                    svc = GetServiceInstance(innerServiceType, def);
                                    if (svc is IDisposable)
                                    {
                                        _transientDisposables.Add(svc);
                                    }
                                }
                                if (null != svc)
                                {
                                    list.Add(svc);
                                }
                            }
                            var methodInfo = typeof(Enumerable).GetMethod("Cast", BindingFlags.Static | BindingFlags.Public);
                            if (methodInfo != null)
                            {
                                var genericMethod = methodInfo.MakeGenericMethod(innerServiceType);
                                var castedValue = genericMethod.Invoke(null, new object[] { list });
                                if (typeof(IEnumerable<>).MakeGenericType(innerServiceType) == serviceType)
                                {
                                    return castedValue;
                                }
                                var toArrayMethod = typeof(Enumerable).GetMethod("ToArray", BindingFlags.Static | BindingFlags.Public)
                                .MakeGenericMethod(innerServiceType);

                                return toArrayMethod.Invoke(null, new object[] { castedValue });

                            }
                            return list;
                        }
                        return null;
                    }
                    else
                    {
                        return null;
                    }

                }
            }
            if (_isRootScope && serviceDefinition.ServiceLifetime == ServiceLifetime.Scoped)
            {
                throw new InvalidOperationException($"can not get scope service from the root scope, serviceType: {serviceType.FullName}");
            }
            if (serviceDefinition.ServiceLifetime == ServiceLifetime.Singleton)
            {
                var svc = _singletonInstances.GetOrAdd(new ServiceKey(serviceType, serviceDefinition), (t) => GetServiceInstance(t.ServiceType, serviceDefinition));
                return svc;

            }
            else if (serviceDefinition.ServiceLifetime == ServiceLifetime.Scoped)
            {
                var svc = _scopedInstances.GetOrAdd(new ServiceKey(serviceType, serviceDefinition), (t) => GetServiceInstance(t.ServiceType, serviceDefinition));
                return svc;
            }
            else
            {
                var svc = GetServiceInstance(serviceType, serviceDefinition);
                if (svc is IDisposable)
                {
                    _transientDisposables.Add(svc);
                }
                return svc;
            }

        }

        private object GetServiceInstance(Type serviceType, ServiceDefinition serviceDefinition)
        {
            if (serviceDefinition.ImplementationInstance != null)
                return serviceDefinition.ImplementationInstance;
            if (serviceDefinition.ImplementationFactory != null)
                return serviceDefinition.ImplementationFactory.Invoke(this);

            var implementType = (serviceDefinition.ImplementType ?? serviceType);
            if (implementType.IsInterface||implementType.IsAbstract)
            {
                throw new InvalidOperationException($"invalid service registered, serviceType: {serviceType.FullName}, implementType: {serviceDefinition.ImplementType}");
            }
            if (implementType.IsGenericType)
            {
                implementType = implementType.MakeGenericType(serviceType.GetGenericArguments());

            }
            var ctorInfos = implementType.GetConstructors(BindingFlags.Instance | BindingFlags.Public);
            if (ctorInfos.Length==0)
            {
                throw new InvalidOperationException($"service {serviceType.FullName} does not have any public constructors");

            }
            ConstructorInfo ctor;
            if (ctorInfos.Length==1)
            {
                ctor = ctorInfos[0];
            }
            else
            {
                ctor = ctorInfos.OrderBy(a => a.GetParameters().Length).First();
            }

            var parameters = ctor.GetParameters();
            if (parameters.Length==0)
            {
                return Expression.Lambda<Func<object>>(Expression.New(ctor)).Compile().Invoke();
            }
            else
            {
                var ctorParams = new object[parameters.Length];
                for (int index = 0; index < ctorParams.Length; index++)
                {
                    var parameter = parameters[index];
                    var param = GetService(parameter.ParameterType);
                    if (param==null&&parameter.HasDefaultValue)
                    {
                        param = parameter.DefaultValue;
                    }
                    ctorParams[index] = param;
                }
                return Expression.Lambda<Func<object>>(Expression.New(ctor, ctorParams.Select(Expression.Constant))).Compile().Invoke();
            }
        }

        public IServiceContainer TryAdd(ServiceDefinition serviceDefinition)
        {
            if (_disposed)
            {
                throw new InvalidOperationException("the service container had been disposed");
            }
            if (_services.Any(a=>a.ServiceType==serviceDefinition.ServiceType))
            {
                return this;
            }
            _services.Add(serviceDefinition);
            return this;
        }
        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }
            if (_isRootScope)
            {
                lock (_singletonInstances)
                {
                    if (_disposed)
                    {
                        return;
                    }
                }
                _disposed = true;
                foreach (var instance in _singletonInstances.Values)
                {

                    (instance as IDisposable)?.Dispose();
                }
                foreach (var instance in _transientDisposables)
                {
                    (instance as IDisposable)?.Dispose();
                }
                _singletonInstances.Clear();
                _transientDisposables = null;
            }
            else
            {
                lock (_scopedInstances)
                {
                    if (_disposed)
                    {
                        return;
                    }
                    _disposed = true;
                    foreach (var instance in _scopedInstances.Values)
                    {

                        (instance as IDisposable)?.Dispose();
                    }
                    foreach (var instance in _transientDisposables)
                    {
                        (instance as IDisposable)?.Dispose();
                    }
                    _scopedInstances.Clear();
                    _transientDisposables = null;
                }
            }
        }
    }
}
