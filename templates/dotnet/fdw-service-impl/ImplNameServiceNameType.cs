using Fdw.Collections.Attributes;
using Fdw.ServiceTypes;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using RootNamespace.ServiceName;
using RootNamespace.ServiceName.Abstractions;

namespace RootNamespace.ServiceName.ImplName;

/// <summary>
/// ServiceType definition for ImplName ServiceName services.
/// Handles two-phase registration: infrastructure/factory services and factory registration with provider.
/// </summary>
/// <remarks>
/// <para>
/// This ServiceType uses the two-phase registration pattern:
/// <list type="bullet">
/// <item><description>Phase 1: <see cref="RegisterRequiredServices"/> registers infrastructure AND factory with main DI</description></item>
/// <item><description>Phase 2: <see cref="RegisterFactory"/> resolves factory from DI and registers with provider</description></item>
/// </list>
/// </para>
/// <para>
/// Configuration is NOT bound here. Configuration binding happens at runtime via the value bag
/// pattern in the provider. The provider uses ServiceType.ConfigurationType to determine the
/// target type for binding.
/// </para>
/// </remarks>
[ServiceTypeOption(typeof(ServiceNameTypes), "ImplName")]
public sealed class ImplNameServiceNameType
    : ServiceNameTypeBase<IServiceNameService, IServiceNameFactory, ImplNameServiceNameConfiguration>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ImplNameServiceNameType"/> class.
    /// </summary>
    public ImplNameServiceNameType()
        : base(
            name: "ImplName",
            sectionName: "ImplName",
            displayName: "ImplName ServiceName",
            description: "ImplName implementation for ServiceName services",
            category: "General")
    {
    }

    /// <summary>
    /// Phase 1: Register infrastructure AND factory services with the main IoC container.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This method is called BEFORE the provider is created. Register:
    /// <list type="bullet">
    /// <item><description>Infrastructure services (IHttpClientFactory, IMemoryCache, translators)</description></item>
    /// <item><description>The factory as a singleton - DI handles all constructor dependencies</description></item>
    /// </list>
    /// </para>
    /// <para>
    /// DO NOT call RegisterConfiguration() here - configuration binding happens at runtime
    /// via the value bag pattern in the provider.
    /// </para>
    /// </remarks>
    /// <param name="services">The service collection to register services with.</param>
    /// <returns>The service collection for chaining.</returns>
    public override IServiceCollection RegisterRequiredServices(IServiceCollection services)
    {
        // Infrastructure (if needed)
        // services.AddHttpClient();
        // services.AddMemoryCache();
        // services.AddSingleton<IMyTranslator, MyTranslator>();

        // Factory - DI handles all constructor dependencies
        services.AddSingleton<IImplNameServiceNameFactory, ImplNameServiceNameFactory>();

        return services;
    }

    /// <summary>
    /// Phase 2: Resolve factory from DI and register with provider.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This method is called AFTER all Phase 1 registrations are complete. The IServiceProvider
    /// parameter provides access to all registered services including the factory.
    /// </para>
    /// <para>
    /// Simply resolve the factory registered in Phase 1 and add it to the provider.
    /// The factory is a stateless singleton - dependencies were injected via DI constructor.
    /// </para>
    /// </remarks>
    /// <param name="provider">The provider to register the factory with.</param>
    /// <param name="services">The service provider for resolving the factory.</param>
    public override void RegisterFactory(ServiceProvider provider, IServiceProvider services)
    {
        // Resolve factory from DI (registered in Phase 1)
        var factory = services.GetRequiredService<IImplNameServiceNameFactory>();

        // Register factory instance with provider
        ((DefaultServiceNameProvider)provider).RegisterFactory(Name, factory);
    }
}
