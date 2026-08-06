using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Configuration;
using Fdw.Results;
using Fdw.Services.Abstractions;
using Fdw.Services.Configuration;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.SecretManagers.Abstractions;
using Fdw.Services.SecretManagers.Commands;
using Fdw.Services.SecretManagers.Logging;
using Fdw.ServiceTypes;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Fdw.Services.SecretManagers;

/// <summary>
/// Domain-specific configuration provider for secret managers.
/// The polymorphic typed-body read (dispatch on
/// <see cref="Fdw.Configuration.IGenericConfiguration.ServiceOptionType"/>, e.g.
/// "EnvironmentVariable"/"AzureKeyVault", to load the typed body row and attach it to
/// <see cref="SecretManagerConfiguration.Configuration"/>) is composed uniformly by
/// <see cref="DefaultConfigurationProvider{TConfig,TCommand}"/>. This subclass additionally captures the
/// concrete typed CLR type (for endpoint deserialization) and a reflection-free factory (for default-body
/// creation on Save), and registers typed providers via the inherited <c>RegisterTypedProvider</c>.
/// </summary>
public class SecretManagerConfigurationProvider : DefaultConfigurationProvider<SecretManagerConfiguration, SecretManagerConfigurationCommand>
{
    /// <summary>
    /// Registers the SecretManagerConfigurationProvider with DI, targeting this domain's own default
    /// location. To override, call <c>SetConfiguration</c> on the resolved singleton.
    /// </summary>
    public static void RegisterDomainConfiguration(IServiceCollection services)
    {
        services.TryAddSingleton<SecretManagerConfigurationProvider>(sp =>
            new SecretManagerConfigurationProvider(
                sp.GetService<ILogger<SecretManagerConfigurationProvider>>()!,
                sp.GetRequiredService<Lazy<IConfigurationGateway>>(),
                invalidator: new Lazy<ICacheInvalidator?>(() => sp.GetService<ICacheInvalidator>())));
        // Why: Consumers inject DefaultConfigurationProvider<TConfig, TCommand> (the new base) —
        // forward to the concrete subclass.
        services.TryAddSingleton<DefaultConfigurationProvider<SecretManagerConfiguration, SecretManagerConfigurationCommand>>(
            sp => sp.GetRequiredService<SecretManagerConfigurationProvider>());
        // Why: Generated Initialize() links IServiceConfigurationProvider<T> as the parent on the
        // domain provider (SecretManagerProvider); this forward lets that lookup succeed.
        services.TryAddSingleton<IServiceConfigurationProvider<SecretManagerConfiguration>>(
            sp => sp.GetRequiredService<SecretManagerConfigurationProvider>());
        // Why: publishes the domain's own provider interface. The generated Register only registers the
        // provider under IFdwServiceProvider<,>, which no factory may take by constructor (FDW045).
        // Consumers — the connection factories above all — depend on ISecretManagerProvider instead, and
        // this forward hands them the SAME instance, so its factory registrations and cache are shared.
        // The cast is deliberate and fail-loud: the generator constructs DefaultSecretManagerProvider,
        // and if anything ever replaces that registration with a type that is not the domain provider,
        // the composition root must break loudly rather than resolve a second, empty provider.
        services.TryAddSingleton<ISecretManagerProvider>(
            sp => (ISecretManagerProvider)sp.GetRequiredService<IFdwServiceProvider<ISecretManager, SecretManagerConfiguration>>());
    }

    // Why: tracks the concrete typed-body CLR type for each discriminator. Endpoints
    // deserialize the incoming JSON Configuration body into the correct strongly-typed
    // object before save; the header provider also uses this for cascade-save when the
    // caller didn't supply Configuration on a Create request.
    private readonly ConcurrentDictionary<string, Type> _typedConfigTypes
        = new(StringComparer.OrdinalIgnoreCase);

    // Why: captured parameterless factory per discriminator — reflection-free replacement for
    // Activator.CreateInstance(typedType) when building a default typed body on Create.
    private readonly ConcurrentDictionary<string, Func<ISecretManagerConfiguration>> _typedConfigFactories
        = new(StringComparer.OrdinalIgnoreCase);

    private readonly ILogger<SecretManagerConfigurationProvider> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="SecretManagerConfigurationProvider"/> class.
    /// </summary>
    public SecretManagerConfigurationProvider(
        ILogger<SecretManagerConfigurationProvider> logger,
        Lazy<IConfigurationGateway> lazyGateway,
        string dataStoreName = "ConfigurationDb",
        string pathName = "sec",
        Lazy<ICacheInvalidator?>? invalidator = null)
        : base(logger ?? NullLogger<SecretManagerConfigurationProvider>.Instance,
               lazyGateway,
               dataStoreName, pathName,
               invalidator)
    {
        _logger = logger ?? NullLogger<SecretManagerConfigurationProvider>.Instance;
    }

    /// <summary>
    /// Registers a typed child provider for a specific ServiceOptionType using a typed body configuration type.
    /// Forwards the provider registration to the base (which composes the typed body on read) and additionally
    /// captures the concrete CLR type + a parameterless factory for this domain's deserialize/default-body paths.
    /// </summary>
    /// <typeparam name="TDerived">A configuration type that implements <see cref="ISecretManagerConfiguration"/>.</typeparam>
    /// <param name="serviceOptionType">The service option type key (e.g., "AzureKeyVault").</param>
    /// <param name="provider">The typed configuration provider for that service option.</param>
    // Why: a generic sibling of the base's non-generic RegisterTypedProvider, so this domain can ALSO capture the
    // typed CLR type (GetTypedConfigType, used by endpoints) and a reflection-free ctor (Save's default
    // body). The actual typed-provider registration + read composition lives in the base; this only adds
    // the two domain-specific captures and then delegates. Constraint adds new() for the factory capture.
    public void RegisterTypedProvider<TDerived>(string serviceOptionType, IServiceConfigurationProvider<TDerived> provider)
        where TDerived : class, ISecretManagerConfiguration, new()
    {
        base.RegisterTypedProvider(serviceOptionType, provider);
        _typedConfigTypes[serviceOptionType] = typeof(TDerived);
        // Why: capture the closed-generic ctor here so Save() can build a default typed body without
        // Activator/reflection (TDerived is statically known at the registration call site).
        _typedConfigFactories[serviceOptionType] = static () => new TDerived();
        SecretManagerConfigurationProviderLog.TypedCacheRegistered(_logger, serviceOptionType);
    }

    /// <summary>
    /// Returns the registered typed-body CLR type for a given discriminator, or null when
    /// no typed provider is registered for that discriminator. Used by endpoints to
    /// deserialize an inbound JSON Configuration body into the matching strongly-typed
    /// configuration before save.
    /// </summary>
    public Type? GetTypedConfigType(string serviceOptionType)
        => _typedConfigTypes.TryGetValue(serviceOptionType, out var t) ? t : null;

    /// <summary>
    /// Persists the SecretManager record. When the caller didn't supply
    /// <see cref="SecretManagerConfiguration.Configuration"/>, builds a default typed-body
    /// instance for the configured <c>ServiceOptionType</c> so the parent + typed-body rows
    /// stay in sync on initial INSERT.
    /// </summary>
    // Why: SM Create endpoints accept just {name, secretManagerType, description} — the typed
    // body has all-optional fields with defaults. Persisting a default child row on Create lets
    // subsequent Get/Update/Delete operations find a complete SM record. Without this, the
    // parent row exists but typed-body lookups during Get return failure.
    public override Task<IGenericResult<SecretManagerConfiguration>> Save(
        SecretManagerConfiguration record, CancellationToken ct = default)
    {
        if (record.Configuration is null
            && !string.IsNullOrEmpty(record.ServiceOptionType)
            && _typedConfigFactories.TryGetValue(record.ServiceOptionType, out var factory))
        {
            var instance = factory();
            if (instance.Id == Guid.Empty)
                instance.Id = Guid.CreateVersion7();

            // Why: stamp the logical FK to the parent header — SecretManagerId is declared on
            // ISecretManagerConfiguration, so it is set directly with no reflection.
            instance.SecretManagerId = record.Id;
            record.Configuration = instance;
        }

        return base.Save(record, ct);
    }

    /// <summary>
    /// Loads the parent header row without dispatching to a typed provider. Use for management
    /// flows (Delete, exists-check) that don't need the typed body and shouldn't fail if no
    /// typed provider is registered for the header's ServiceOptionType (e.g. stale or
    /// plugin-removed types).
    /// </summary>
    public Task<IGenericResult<SecretManagerConfiguration>> GetHeader(string name, CancellationToken ct = default)
        => GetHeaderByName(name, ct);
}
