using System;


namespace DependencyInjection
{

    public class ServiceKey : IEquatable<ServiceKey>
    {
        public Type ServiceType { get; }
        public Type ImplementType { get; }
        
        public ServiceKey(Type serviceType, ServiceDefinition definition)
        {
            ServiceType = serviceType;
            ImplementType = definition.GetImplementType();
        }
        
        public bool Equals(ServiceKey other)
        {
            return ServiceType == other?.ServiceType && ImplementType == other?.ImplementType;
        }
        public override bool Equals(object obj)
        {
            return base.Equals((ServiceKey)obj);    
        }
        public override int GetHashCode()
        {
            var key = $"{ServiceType.FullName}_{ServiceType.FullName}";
            return key.GetHashCode();
        }
    }

}