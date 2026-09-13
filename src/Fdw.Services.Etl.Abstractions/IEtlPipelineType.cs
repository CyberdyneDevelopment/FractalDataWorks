using System;
using Fdw.Configuration;
using Fdw.Results;
using Fdw.ServiceTypes;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Etl.Abstractions;

/// <summary>
/// Generic interface for typed ETL pipeline type definitions.
/// </summary>
/// <typeparam name="TPipeline">The pipeline service type.</typeparam>
/// <typeparam name="TConfiguration">The configuration type.</typeparam>
/// <typeparam name="TFactory">The factory type.</typeparam>
public interface IEtlPipelineType<TPipeline, TConfiguration, TFactory> : IServiceType<Guid, TPipeline, TFactory, TConfiguration>, IEtlPipelineType
    where TPipeline : IEtlPipeline
    where TConfiguration : IGenericConfiguration
    where TFactory : IEtlPipelineFactory<TPipeline, TConfiguration>
{
}

/// <summary>
/// Non-generic interface for ETL pipeline type definitions.
/// </summary>
public interface IEtlPipelineType : IServiceType
{
    /// <summary>
    /// Registers this option's implementation provider factory with the domain provider being
    /// constructed, using the constructing scope's own <paramref name="serviceProvider"/>.
    /// </summary>
    /// <remarks>
    /// Called from <c>EtlPipelineTypes</c>'s own <c>AddScoped</c> factory — once per domain
    /// provider instance, not once at startup — so the closure this hands to the domain
    /// provider's <c>Register</c> always resolves against whichever scope actually constructed
    /// THIS instance (root at startup, a real request's own scope for a real request), never a
    /// reference captured from a different one. The prior approach registered from each option's
    /// <c>Initialize</c>, which only ever runs once against the root container — so every scope
    /// but root's own found this domain's factory registry empty.
    /// </remarks>
    /// <param name="domainProvider">The domain provider instance being constructed.</param>
    /// <param name="serviceProvider">The service provider that constructed it — root at startup, a request's own scope for a request.</param>
    /// <param name="logger">The logger to report the outcome to.</param>
    /// <returns>Success, or a structured failure. The base no-op returns success for options with nothing to contribute.</returns>
    IGenericResult RegisterImplementationProvider(IEtlPipelineProvider domainProvider, IServiceProvider serviceProvider, ILogger logger);
}
