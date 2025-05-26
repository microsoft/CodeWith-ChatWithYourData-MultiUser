using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.Helpers
{
    /// <summary>
    /// Defines the different types of services that can throw exceptions
    /// </summary>
    public enum ServiceType
    {
        AIService,
        SearchService,
        ThreadRepository,
        DocumentRegistry,
        DocumentStore
    }

    /// <summary>
    /// Represents an exception that occurs in a service
    /// </summary>
    public class ServiceException : Exception
    {
        public ServiceType ServiceType { get; }


        public ServiceException(string message, Exception innerException, ServiceType serviceType) 
            : base(message, innerException)
        {
            ServiceType = serviceType;
        }

        public ServiceException(string message, ServiceType serviceType) 
            : base(message)
        {
            ServiceType = serviceType;
        }

        public override string ToString()
        {
            return $"Service exception in {ServiceType}: {Message}";
        }
    }
}