using System;
using Fdw.Roslyn.Commands.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Fdw.Roslyn.Commands;

/// <summary>
/// Extension methods for registering Roslyn command services.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds Roslyn command handler services to the service collection.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddRoslynCommandHandler(this IServiceCollection services)
    {
        if (services is null)
        {
            throw new ArgumentNullException(nameof(services));
        }

        services.TryAddSingleton<ITranslatorRegistry>(sp =>
            new TranslatorRegistry(sp.GetRequiredService<ILoggerFactory>()));
        services.TryAddSingleton<IChangeLedger, ChangeLedger>();
        services.TryAddScoped<IRoslynCommandHandler, RoslynCommandHandler>();

        return services;
    }

    /// <summary>
    /// Adds Roslyn command handler services with a custom translator registry.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configureRegistry">Action to configure the translator registry.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddRoslynCommandHandler(
        this IServiceCollection services,
        Action<ITranslatorRegistry> configureRegistry)
    {
        if (services is null)
        {
            throw new ArgumentNullException(nameof(services));
        }

        if (configureRegistry is null)
        {
            throw new ArgumentNullException(nameof(configureRegistry));
        }

        services.AddSingleton<ITranslatorRegistry>(sp =>
        {
            var registry = new TranslatorRegistry(sp.GetRequiredService<ILoggerFactory>());
            configureRegistry(registry);
            return registry;
        });

        services.TryAddSingleton<IChangeLedger, ChangeLedger>();
        services.TryAddScoped<IRoslynCommandHandler, RoslynCommandHandler>();

        return services;
    }

    /// <summary>
    /// Registers a translator in the service collection.
    /// </summary>
    /// <typeparam name="TTranslator">The translator type.</typeparam>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddTranslator<TTranslator>(this IServiceCollection services)
        where TTranslator : class, IRoslynCommandTranslator
    {
        if (services is null)
        {
            throw new ArgumentNullException(nameof(services));
        }

        services.AddSingleton<IRoslynCommandTranslator, TTranslator>();
        services.AddSingleton<TTranslator>();

        return services;
    }
}
