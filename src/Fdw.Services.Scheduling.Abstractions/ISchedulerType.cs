using Fdw.Configuration;
using System;
using Fdw.Results;
using Fdw.ServiceTypes;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Scheduling.Abstractions;

/// <summary>
/// Interface for scheduler service types.
/// </summary>
/// <typeparam name="TService">The scheduler service interface type.</typeparam>
/// <typeparam name="TConfiguration">The configuration type for the scheduler service.</typeparam>
/// <typeparam name="TFactory">The factory type for creating scheduler service instances.</typeparam>
public interface ISchedulerType<TService, TConfiguration, TFactory> : IServiceType<Guid, TService, TFactory, TConfiguration>, ISchedulerType
    where TService : IFrameworkSchedulingService
    where TConfiguration : IGenericConfiguration
    where TFactory : ISchedulingFactory<TService, TConfiguration>
{
}

/// <summary>
/// Non-generic interface for scheduler service types.
/// </summary>
public interface ISchedulerType : IServiceType
{
    /// <summary>
    /// Gets the name of the underlying scheduling engine (e.g., "Quartz.NET", "Hangfire").
    /// </summary>
    string SchedulingEngine { get; }

    /// <summary>
    /// Gets the job executor type used by this scheduler.
    /// </summary>
    Type JobExecutorType { get; }

    /// <summary>
    /// Gets the trigger type used by this scheduler.
    /// </summary>
    Type TriggerType { get; }

    /// <summary>
    /// Gets whether this scheduler supports recurring jobs.
    /// </summary>
    bool SupportsRecurring { get; }

    /// <summary>
    /// Gets whether this scheduler supports delayed/scheduled jobs.
    /// </summary>
    bool SupportsDelayed { get; }

    /// <summary>
    /// Gets whether this scheduler supports cron expressions.
    /// </summary>
    bool SupportsCronExpressions { get; }

    /// <summary>
    /// Gets whether this scheduler supports interval-based scheduling.
    /// </summary>
    bool SupportsIntervalScheduling { get; }

    /// <summary>
    /// Gets whether this scheduler supports job persistence across restarts.
    /// </summary>
    bool SupportsJobPersistence { get; }

    /// <summary>
    /// Gets whether this scheduler supports clustering for high availability.
    /// </summary>
    bool SupportsClustering { get; }

    /// <summary>
    /// Gets whether this scheduler supports job queuing and prioritization.
    /// </summary>
    bool SupportsJobQueuing { get; }

    /// <summary>
    /// Gets the maximum number of concurrent jobs this scheduler can handle.
    /// </summary>
    int MaxConcurrentJobs { get; }

    /// <summary>
    /// Sets the body this option runs to register itself with the domain service provider and/or
    /// the domain configuration provider, when the caller has one to hand it.
    /// </summary>
    /// <remarks>
    /// An overload of <see cref="ServiceTypeBase{TService,TFactory,TConfiguration}.Registration"/> —
    /// same gerund, same call-and-store shape, an additional signature rather than a new phase or a
    /// new method name. Set from the option's own constructor exactly like its DI-wiring
    /// <c>Registration((builder, loggerFactory) => ...)</c> call; invoked by
    /// <c>SchedulerTypes</c>'s own <c>AddScoped</c> factory via <see cref="Register"/>, once per
    /// domain provider instance it constructs — not once at startup — so whichever scope actually
    /// builds an instance (root at startup, a real request's own scope for a real request) is the
    /// same scope whose <c>IServiceProvider</c> the closure resolves against. Registering from each
    /// option's <c>Initialize</c> would run only once against the root container, so every scope
    /// but root's own would find this domain's factory registry empty.
    /// </remarks>
    /// <param name="method">
    /// The body this option runs. Receives the constructing scope's <c>IServiceProvider</c>, the
    /// domain service provider instance being constructed (or null if none is being constructed),
    /// the domain configuration provider (or null if none is available), and the logger the caller
    /// already built (with its own <c>NullLogger</c> fallback already applied) — not re-resolved
    /// here, so every option logs through the same logger instance the domain provider's own
    /// construction used.
    /// </param>
    void Registration(Func<IServiceProvider, ISchedulerServiceProvider?, ISchedulerConfigurationProvider?, ILogger<ISchedulerType>, IGenericResult> method);

    /// <summary>
    /// Runs the body set by the <see cref="Registration"/> overload above, registering this option
    /// with whichever of <paramref name="domainProvider"/>/<paramref name="domainConfigurationProvider"/>
    /// were supplied.
    /// </summary>
    /// <param name="serviceProvider">The scope actually constructing whatever called this — root at startup, a request's own scope for a request.</param>
    /// <param name="domainProvider">The domain service provider instance being constructed, or null.</param>
    /// <param name="domainConfigurationProvider">The domain configuration provider, or null.</param>
    /// <param name="logger">The logger to report the outcome to — the caller's own, already built with its <c>NullLogger</c> fallback applied.</param>
    /// <returns>Success, or a structured failure. The base no-op returns success for options with nothing to contribute.</returns>
    IGenericResult Register(IServiceProvider serviceProvider, ISchedulerServiceProvider? domainProvider, ISchedulerConfigurationProvider? domainConfigurationProvider, ILogger<ISchedulerType> logger);
}
