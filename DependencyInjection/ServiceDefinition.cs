using System;
using System.Collections.Generic;
using System.Text;

namespace DependencyInjection
{
   public class ServiceDefinition
    {
        public ServiceLifetime ServiceLifetime { get; }
        public Type ImplementType { get; }
        public Type ServiceType { get; }
        public object ImplementationInstance { get; }
        public Func<IServiceProvider,object> ImplementationFactory { get; }


        public Type GetImplementType()
        {
            if (ImplementationInstance != null)
                return ImplementationInstance.GetType();

            if (ImplementationFactory != null)
                return ImplementationFactory.Method.DeclaringType;

            if (ImplementType != null)
                return ImplementType;

            return ServiceType;
        }

        public ServiceDefinition(object instace,Type serviceType)
        {
            ImplementationInstance = instace;
            ServiceType = serviceType;

        }

        public ServiceDefinition(Type serviceType,ServiceLifetime serviceLifetime)
                  :this(serviceType, serviceType, serviceLifetime)
        {

        }
        public ServiceDefinition(Type serviceType, Type implementType, ServiceLifetime serviceLifetime)
        {
            ServiceType = serviceType;
            ImplementType = implementType ?? serviceType;
            ServiceLifetime = serviceLifetime;
        }
        public ServiceDefinition(Type serviceType,Func<IServiceProvider,object> factory,ServiceLifetime serviceLifetime)
        {
            ServiceType = serviceType;
            ImplementationFactory = factory;
            ServiceLifetime = serviceLifetime;
        }


        public static ServiceDefinition Singleton<IService>(Func<IServiceProvider,object> factory)
        {
            return new ServiceDefinition(typeof(IService),factory, ServiceLifetime.Singleton);
        }

        public static ServiceDefinition Scoped<IService>(Func<IServiceProvider, object> factory)
        {
            return new ServiceDefinition(typeof(IService), factory, ServiceLifetime.Scoped);
        }

        public static ServiceDefinition Transient<IService>(Func<IServiceProvider, object> factory)
        {
            return new ServiceDefinition(typeof(IService), factory, ServiceLifetime.Transient);
        }

        public static ServiceDefinition Singleton<IService>()
        {
            return new ServiceDefinition(typeof(IService), ServiceLifetime.Singleton);
        }

        public static ServiceDefinition Scoped<IService>()
        {
            return new ServiceDefinition(typeof(IService), ServiceLifetime.Scoped);
        }

        public static ServiceDefinition Transient<IService>()
        {
            return new ServiceDefinition(typeof(IService), ServiceLifetime.Transient);
        }
        public static ServiceDefinition Singleton<TService, TServiceImplement>() where TServiceImplement : TService
        {
            return new ServiceDefinition(typeof(TService), typeof(TServiceImplement), ServiceLifetime.Singleton);
        }

        public static ServiceDefinition Scoped<TService, TServiceImplement>() where TServiceImplement : TService
        {
            return new ServiceDefinition(typeof(TService), typeof(TServiceImplement), ServiceLifetime.Scoped);
        }
        public static ServiceDefinition Transient<TService, TServiceImplement>() where TServiceImplement : TService
        {
            return new ServiceDefinition(typeof(TService), typeof(TServiceImplement), ServiceLifetime.Transient);
        }

    }

}
