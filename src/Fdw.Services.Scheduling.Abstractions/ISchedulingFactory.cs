using Fdw.Configuration;
using Fdw.Abstractions;

namespace Fdw.Services.Scheduling.Abstractions;

/// <summary>
/// Marker interface for scheduling factories.
/// </summary>
public interface ISchedulingFactory
{
}

/// <summary>
/// Generic interface for scheduling factories with typed configuration.
/// </summary>
/// <typeparam name="TService">The type of scheduling service this factory creates.</typeparam>
/// <typeparam name="TConfiguration">The type of configuration this factory requires.</typeparam>
public interface ISchedulingFactory<TService, TConfiguration> : ISchedulingFactory, IServiceFactory<TService, TConfiguration>
    where TService : IFrameworkSchedulingService
    where TConfiguration : IGenericConfiguration
{
}
