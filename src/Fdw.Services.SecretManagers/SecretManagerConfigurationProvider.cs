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
/// <see cref="ImplementationConfigurationProviderBase{TConfig,TCommand}"/>. This subclass additionally captures the
/// concrete typed CLR type (for endpoint deserialization) and a reflection-free factory (for default-body
/// creation on Save), and registers typed providers via the inherited <c>Register</c>.
/// </summary>
public class SecretManagerConfigurationProvider
    : ServiceConfigurationProviderBase<
          SecretManagerConfiguration,
          ISecretManagerImplementationConfiguration,
          SecretManagerConfigurationCommand>,
      ISecretManagerConfigurationProvider
{

    // Why: tracks the concrete typed-body CLR type for each discriminator. Endpoints
    // deserialize the incoming JSON Configuration body into the correct strongly-typed
    // object before save; the header provider also uses this for cascade-save when the
    // caller didn't supply Configuration on a Create request.

    // Why: captured parameterless factory per discriminator — reflection-free replacement for
    // Activator.CreateInstance(typedType) when building a default typed body on Create.

    private readonly ILogger<SecretManagerConfigurationProvider> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="SecretManagerConfigurationProvider"/> class.
    /// </summary>
    public SecretManagerConfigurationProvider(
        ILogger<SecretManagerConfigurationProvider> logger,
        IConfigurationGatewayProvider gatewayProvider,
        string dataStoreName,
        string pathName = "sec")
        : base(logger ?? NullLogger<SecretManagerConfigurationProvider>.Instance,
               gatewayProvider,
               dataStoreName, pathName)
    {
        _logger = logger ?? NullLogger<SecretManagerConfigurationProvider>.Instance;
    }
    /// <summary>
    /// Loads the parent header row without dispatching to a typed provider. Use for management
    /// flows (Delete, exists-check) that don't need the typed body and shouldn't fail if no
    /// typed provider is registered for the header's ServiceOptionType (e.g. stale or
    /// plugin-removed types).
    /// </summary>
    public Task<IGenericResult<SecretManagerConfiguration>> GetHeader(string name, CancellationToken ct = default)
        => GetHeaderByName(name, ct);

    /// <inheritdoc />
    protected override SecretManagerConfiguration Compose<T>(
        string serviceOptionType,
        string name,
        T implementationConfiguration)
        => new()
        {
            Name = name,
            ServiceOptionType = serviceOptionType,
            Configuration = implementationConfiguration,
        };
}
