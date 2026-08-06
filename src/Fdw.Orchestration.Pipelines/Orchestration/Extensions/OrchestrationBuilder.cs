using System;
using Fdw.Orchestration.Abstractions;
using Fdw.Orchestration.Abstractions.Caching;
using Fdw.Orchestration.Abstractions.Resilience;
using Fdw.Orchestration.Caching;
using Fdw.Orchestration.Execution;
using Fdw.Orchestration.Resilience;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Fdw.Orchestration.Extensions;

/// <summary>
/// Builder for configuring orchestration services.
/// </summary>
public sealed class OrchestrationBuilder
{
    /// <summary>
    /// Gets the service collection.
    /// </summary>
    public IServiceCollection Services { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="OrchestrationBuilder"/> class.
    /// </summary>
    /// <param name="services">The service collection.</param>
    public OrchestrationBuilder(IServiceCollection services)
    {
        Services = services ?? throw new ArgumentNullException(nameof(services));
        Services.TryAddSingleton<IResiliencePipelineFactory, PollyResiliencePipelineFactory>();
    }

    /// <summary>
    /// Configures in-memory caching.
    /// </summary>
    /// <param name="configure">Optional action to configure memory cache options.</param>
    /// <returns>The builder for chaining.</returns>
    public OrchestrationBuilder UseInMemoryCache(Action<MemoryCacheOptions>? configure = null)
    {
        if (configure != null)
        {
            Services.AddMemoryCache(configure);
        }
        else
        {
            Services.AddMemoryCache();
        }

        Services.TryAddSingleton<IOrchestrationCache, InMemoryOrchestrationCache>();
        return this;
    }

    /// <summary>
    /// Configures distributed caching.
    /// </summary>
    /// <returns>The builder for chaining.</returns>
    /// <remarks>
    /// Requires an IDistributedCache implementation to be registered separately.
    /// </remarks>
    public OrchestrationBuilder UseDistributedCache()
    {
        Services.TryAddSingleton<IOrchestrationCache, DistributedOrchestrationCache>();
        return this;
    }

    /// <summary>
    /// Configures a custom cache implementation.
    /// </summary>
    /// <typeparam name="TCache">The cache implementation type.</typeparam>
    /// <returns>The builder for chaining.</returns>
    public OrchestrationBuilder UseCache<TCache>()
        where TCache : class, IOrchestrationCache
    {
        Services.TryAddSingleton<IOrchestrationCache, TCache>();
        return this;
    }

    /// <summary>
    /// Configures a custom resilience pipeline factory.
    /// </summary>
    /// <typeparam name="TFactory">The factory implementation type.</typeparam>
    /// <returns>The builder for chaining.</returns>
    public OrchestrationBuilder UseResiliencePipelineFactory<TFactory>()
        where TFactory : class, IResiliencePipelineFactory
    {
        Services.AddSingleton<IResiliencePipelineFactory, TFactory>();
        return this;
    }

    /// <summary>
    /// Configures the default orchestration executor.
    /// </summary>
    /// <returns>The builder for chaining.</returns>
    public OrchestrationBuilder UseExecutor()
    {
        Services.TryAddSingleton<IOrchestrationExecutor, OrchestrationExecutor>();
        Services.TryAddSingleton<OrchestrationExecutor>();
        return this;
    }

    /// <summary>
    /// Configures a custom orchestration executor.
    /// </summary>
    /// <typeparam name="TExecutor">The executor implementation type.</typeparam>
    /// <returns>The builder for chaining.</returns>
    public OrchestrationBuilder UseExecutor<TExecutor>()
        where TExecutor : class, IOrchestrationExecutor
    {
        Services.AddSingleton<IOrchestrationExecutor, TExecutor>();
        return this;
    }
}