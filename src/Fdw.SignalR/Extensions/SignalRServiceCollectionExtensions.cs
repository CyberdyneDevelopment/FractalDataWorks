using System;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Fdw.SignalR;

/// <summary>
/// Extension methods for registering SignalR broadcasters with dependency injection.
/// </summary>
public static class SignalRServiceCollectionExtensions
{
    /// <summary>
    /// Registers a SignalR broadcaster with the service collection.
    /// </summary>
    /// <typeparam name="TInterface">The broadcaster interface type.</typeparam>
    /// <typeparam name="TImplementation">The broadcaster implementation type.</typeparam>
    /// <typeparam name="THub">The SignalR hub type.</typeparam>
    /// <typeparam name="TClient">The strongly-typed client interface.</typeparam>
    /// <param name="services">The service collection.</param>
    /// <param name="loggerFactory">Optional logger factory for registration logging.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddBroadcaster<TInterface, TImplementation, THub, TClient>(
        this IServiceCollection services,
        ILoggerFactory? loggerFactory = null)
        where TInterface : class
        where TImplementation : SignalRBroadcaster<THub, TClient>, TInterface
        where THub : Hub<TClient>
        where TClient : class
    {
        services.AddScoped<TInterface, TImplementation>();

        var logger = loggerFactory?.CreateLogger("SignalR");
        if (logger != null)
        {
            SignalRLog.BroadcasterRegistered(logger, typeof(TImplementation).Name, typeof(THub).Name);
        }

        return services;
    }

    /// <summary>
    /// Registers a SignalR broadcaster as a singleton with the service collection.
    /// </summary>
    /// <typeparam name="TInterface">The broadcaster interface type.</typeparam>
    /// <typeparam name="TImplementation">The broadcaster implementation type.</typeparam>
    /// <typeparam name="THub">The SignalR hub type.</typeparam>
    /// <typeparam name="TClient">The strongly-typed client interface.</typeparam>
    /// <param name="services">The service collection.</param>
    /// <param name="loggerFactory">Optional logger factory for registration logging.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddSingletonBroadcaster<TInterface, TImplementation, THub, TClient>(
        this IServiceCollection services,
        ILoggerFactory? loggerFactory = null)
        where TInterface : class
        where TImplementation : SignalRBroadcaster<THub, TClient>, TInterface
        where THub : Hub<TClient>
        where TClient : class
    {
        services.AddSingleton<TInterface, TImplementation>();

        var logger = loggerFactory?.CreateLogger("SignalR");
        if (logger != null)
        {
            SignalRLog.BroadcasterRegistered(logger, typeof(TImplementation).Name, typeof(THub).Name);
        }

        return services;
    }
}
