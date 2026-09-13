using System;
using Fdw.Results;
using Fdw.Services.Pipelines.Abstractions;
using Fdw.ServiceTypes;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Pipelines;

/// <summary>
/// Interface for pipeline-service domain service types (the gateway-backed pipeline
/// configuration provider domain, distinct from the EtlPipelineTypes engine collection).
/// </summary>
public interface IPipelineServiceType : IServiceType
{
    /// <summary>
    /// Registers this option's implementation provider factory with the domain provider being
    /// constructed, using the constructing scope's own <paramref name="serviceProvider"/>.
    /// </summary>
    /// <remarks>
    /// Called from <c>PipelineServiceTypes</c>'s own <c>AddScoped</c> factory — once per domain
    /// provider instance, not once at startup — so the closure this hands to the domain
    /// provider's <c>Register</c> always resolves against whichever scope actually constructed
    /// THIS instance (root at startup, a real request's own scope for a real request), never a
    /// reference captured from a different one. Registering from an option's <c>Initialization</c>
    /// instead (as HealthMonitor's fix, commit 691033663, replaced) only ever runs once against the
    /// root container — so every scope but root's own would find this domain's factory registry
    /// empty.
    /// </remarks>
    /// <param name="domainProvider">The domain provider instance being constructed.</param>
    /// <param name="serviceProvider">The service provider that constructed it — root at startup, a request's own scope for a request.</param>
    /// <param name="logger">The logger to report the outcome to.</param>
    /// <returns>Success, or a structured failure. The base no-op returns success for options with nothing to contribute.</returns>
    IGenericResult RegisterImplementationProvider(IPipelineServiceProvider domainProvider, IServiceProvider serviceProvider, ILogger logger);
}
