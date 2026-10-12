using System;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Fdw.Services.Abstractions;
using Fdw.Services.Scheduling.Abstractions;

namespace Fdw.Services.Scheduling;

/// <summary>
/// The scheduling domain's service provider.
/// </summary>
/// <remarks>
/// Inherits the whole resolution path from
/// <see cref="DomainServiceProviderBase{TService, TConfiguration, TFactory, TConfigurationProvider}"/>
/// and overrides nothing — schedulers resolve exactly the way the platform does. A domain that needs
/// different behaviour overrides the virtual member here rather than reaching into the base.
/// <para>
/// Closed over <see cref="ISchedulerImplementationConfiguration"/> rather than the configuration class:
/// <c>IServiceConfigurationProvider&lt;T&gt;</c> is invariant, so a base closed over the class cannot
/// satisfy an interface closed over the contract.
/// </para>
/// </remarks>
public sealed class SchedulerServiceProvider
    : DomainServiceProviderBase<
        IFrameworkSchedulingService,
        ISchedulerImplementationConfiguration,
        ISchedulingFactory<IFrameworkSchedulingService, ISchedulerImplementationConfiguration>,
        ISchedulerConfigurationProvider>,
      ISchedulerServiceProvider
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SchedulerServiceProvider"/> class.
    /// </summary>
    /// <param name="logger">The logger for this provider.</param>
    public SchedulerServiceProvider(ILogger<SchedulerServiceProvider> logger)
        : base(logger ?? NullLogger<SchedulerServiceProvider>.Instance)
    {
    }
}
