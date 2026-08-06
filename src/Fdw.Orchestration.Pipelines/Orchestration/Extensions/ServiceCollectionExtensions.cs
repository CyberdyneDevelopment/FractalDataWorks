using System;
using Fdw.Orchestration.Abstractions;
using Fdw.Orchestration.Abstractions.Caching;
using Fdw.Orchestration.Abstractions.Resilience;
using Fdw.Orchestration.Caching;
using Fdw.Orchestration.Execution;
using Fdw.Orchestration.Resilience;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Fdw.Orchestration.Extensions;

// Why: No owning ServiceTypeOption exists for orchestration yet. These methods have no external
// callers. When an OrchestrationTypes ServiceTypeOption is created, move these registrations there.
/// <summary>
/// Extension methods for registering orchestration services with the DI container.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds core orchestration services to the service collection.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddOrchestration(this IServiceCollection services)
    {
        services.TryAddSingleton<IResiliencePipelineFactory, PollyResiliencePipelineFactory>();
        services.TryAddSingleton<IOrchestrationExecutor, OrchestrationExecutor>();
        services.TryAddSingleton<OrchestrationExecutor>();
        return services;
    }

    /// <summary>
    /// Adds orchestration services with in-memory caching.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddOrchestrationWithInMemoryCache(this IServiceCollection services)
    {
        services.AddOrchestration();
        services.AddMemoryCache();
        services.TryAddSingleton<IOrchestrationCache, InMemoryOrchestrationCache>();
        return services;
    }

    /// <summary>
    /// Adds orchestration services with distributed caching.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    /// <remarks>
    /// Requires an IDistributedCache implementation to be registered separately
    /// (e.g., Redis, SQL Server, etc.).
    /// </remarks>
    public static IServiceCollection AddOrchestrationWithDistributedCache(this IServiceCollection services)
    {
        services.AddOrchestration();
        services.TryAddSingleton<IOrchestrationCache, DistributedOrchestrationCache>();
        return services;
    }

    /// <summary>
    /// Adds orchestration services with a custom cache implementation.
    /// </summary>
    /// <typeparam name="TCache">The cache implementation type.</typeparam>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddOrchestrationWithCache<TCache>(this IServiceCollection services)
        where TCache : class, IOrchestrationCache
    {
        services.AddOrchestration();
        services.TryAddSingleton<IOrchestrationCache, TCache>();
        return services;
    }

    /// <summary>
    /// Adds orchestration services with custom configuration.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configure">Action to configure orchestration options.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddOrchestration(
        this IServiceCollection services,
        Action<OrchestrationBuilder> configure)
    {
        var builder = new OrchestrationBuilder(services);
        configure(builder);
        return services;
    }
}