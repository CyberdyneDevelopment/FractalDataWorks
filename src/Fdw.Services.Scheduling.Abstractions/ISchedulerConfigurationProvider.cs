using Fdw.Services.Abstractions;

namespace Fdw.Services.Scheduling.Abstractions;

/// <summary>
/// Resolves configured schedulers and routes each to the implementation provider that owns it.
/// </summary>
public interface ISchedulerConfigurationProvider
    : IDomainConfigurationProvider<ISchedulerImplementationConfiguration>
{
}
