using Fdw.Configuration;
using Fdw.Abstractions;
using Fdw.Services.Abstractions;

namespace Fdw.Services.Scheduling.Abstractions;

/// <summary>
/// Non-generic marker interface for scheduling service factories.
/// </summary>
public interface IFrameworkSchedulingServiceFactory : IServiceFactory
{
}

/// <summary>
/// Interface for scheduling service factories that create specific scheduling service implementations.
/// </summary>
/// <typeparam name="TSchedulingService">The scheduling service type to create.</typeparam>
public interface IFrameworkSchedulingServiceFactory<TSchedulingService> : IFrameworkSchedulingServiceFactory, IServiceFactory<TSchedulingService>
    where TSchedulingService : class, IFrameworkSchedulingService
{
}

/// <summary>
/// Interface for scheduling service factories that create scheduling services with configuration.
/// </summary>
/// <typeparam name="TSchedulingService">The scheduling service type to create.</typeparam>
/// <typeparam name="TConfiguration">The configuration type for the scheduling service.</typeparam>
public interface IFrameworkSchedulingServiceFactory<TSchedulingService, TConfiguration> : IFrameworkSchedulingServiceFactory<TSchedulingService>, IServiceFactory<TSchedulingService, TConfiguration>
    where TSchedulingService : class, IFrameworkSchedulingService
    where TConfiguration : IGenericConfiguration
{
}