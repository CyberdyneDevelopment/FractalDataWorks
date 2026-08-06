using System;
using Fdw.Collections.Attributes;
using Fdw.ServiceTypes;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using $namespace$.$serviceName$.Abstractions;

namespace $namespace$.$serviceName$.$implName$;

/// <summary>
/// ServiceType definition for $implName$ $serviceName$ services.
/// </summary>
[ServiceTypeOption]
public sealed class $implName$$serviceName$Type
    : $serviceName$TypeBase<I$serviceName$Service, I$serviceName$Factory, $implName$$serviceName$Configuration>
{
    public static readonly $implName$$serviceName$Type Instance = new();

    private $implName$$serviceName$Type()
        : base(
            name: "$implName$",
            displayName: "$implName$ $serviceName$",
            description: "$implName$ implementation for $serviceName$ services")
    {
    }

    /// <summary>
    /// Phase 1: Register infrastructure services in main IoC.
    /// </summary>
    public override IServiceCollection RegisterRequiredServices(IServiceCollection services)
    {
        RegisterConfiguration(services);
        // TODO: Add implementation-specific service registrations
        return services;
    }

    /// <summary>
    /// Phase 2: Register factory with provider.
    /// </summary>
    public override void RegisterFactory(
        Default$serviceName$Provider provider,
        IServiceProvider sp)
    {
        var loggerFactory = sp.GetRequiredService<ILoggerFactory>();
        provider.RegisterFactory(Name, () => new $implName$$serviceName$Factory(loggerFactory));
    }
}
