using System;
using System.Collections.Concurrent;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.ServiceTypes;
using Fdw.Services;
using Fdw.Services.Abstractions;
using Fdw.Services.Connections.Abstractions;
using Fdw.Services.DataVault.Abstractions;
using Fdw.Services.DataVault.Logging;
using Fdw.Services.SecretManagers;
using Fdw.Services.SecretManagers.Abstractions;
using Fdw.Services.SecretManagers.Commands;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Services.DataVault;

/// <summary>
/// Default implementation of <see cref="IDataVaultProvider"/>.
/// Wraps <see cref="DefaultServiceProvider{TService,TConfiguration,TFactory,TConfigurationProvider}"/>
/// and adds vault-specific cache-by-name lookup plus the typed
/// <see cref="Get(DataVaultRequest, CancellationToken)"/> entry point.
/// </summary>
/// <remarks>
/// <para>
/// A vault is fully resolved AT CONSTRUCTION. The async resolution — connection + pepper — lives
/// ONLY here, in the cache factory, and runs exactly ONCE per vault name. Every <c>Get</c> overload
/// routes through that single resolve-once-then-cache path; the registered factory is a pure
/// constructor that receives the already-resolved connection and pepper. Any resolution failure is
/// fail-loud (MessageLogging) and EVICTS the cache entry so a misconfiguration is never served from
/// cache forever.
/// </para>
/// </remarks>
public sealed class DefaultDataVaultProvider
    : DefaultServiceProvider<IDataVault, DataVaultConfiguration, IDataVaultFactory<IDataVault, DataVaultConfiguration>, IServiceConfigurationProvider<DataVaultConfiguration>>,
      IDataVaultProvider
{
    private readonly ILogger<DefaultDataVaultProvider> _logger;

    // Why: vault instances are expensive to build (resolve + cache a connection and pepper) so we
    // cache them by name. ConcurrentDictionary<string, Lazy<...>> mirrors DefaultConnectionProvider —
    // the Lazy ensures a single resolution per name even under concurrent first-access. The Lazy
    // stores the Task itself, so .Value returns the Task without blocking. Entries are evicted on
    // failure so a transient/misconfig error is re-attempted on the next Get.
    private readonly ConcurrentDictionary<string, Lazy<Task<IGenericResult<IDataVault>>>> _cache
        = new(StringComparer.OrdinalIgnoreCase);

    // Why: the ServiceTypeCollection generator constructs this provider with logger ONLY
    // (new DefaultDataVaultProvider(providerLogger)); it cannot inject these. They are wired ONCE
    // during the phase-3 RegisterFactory hook (CredentialVaultType.RegisterFactory), which receives
    // the built IServiceProvider — exactly like RegisterParentProvider. They are immutable thereafter.
    private IDataConnectionProvider? _connectionProvider;
    private IPlatformServiceProvider<ISecretManager, SecretManagerConfiguration>? _secretManagerProvider;

    /// <summary>
    /// Initializes a new instance of the <see cref="DefaultDataVaultProvider"/> class.
    /// </summary>
    /// <param name="logger">Logger instance.</param>
    /// <param name="services">The container this provider resolves factories from.</param>
    public DefaultDataVaultProvider(IServiceProvider services, ILogger<DefaultDataVaultProvider> logger)
        : base(services, logger ?? NullLogger<DefaultDataVaultProvider>.Instance)
    {
        _logger = logger ?? NullLogger<DefaultDataVaultProvider>.Instance;
    }

    /// <summary>
    /// Wires the connection + secret-manager providers used to resolve each vault's connection and
    /// pepper. Called ONCE from <c>CredentialVaultType.RegisterFactory</c> (phase 3) with services
    /// resolved from the built container. This is registration-time wiring (like
    /// <c>Register</c>), NOT a per-request init.
    /// </summary>
    /// <param name="connectionProvider">Resolves the vault's single data connection by name.</param>
    /// <param name="secretManagerProvider">Resolves the secret manager that holds the pepper.</param>
    public void ConfigureResolution(
        IDataConnectionProvider connectionProvider,
        IPlatformServiceProvider<ISecretManager, SecretManagerConfiguration> secretManagerProvider)
    {
        _connectionProvider = connectionProvider ?? throw new ArgumentNullException(nameof(connectionProvider));
        _secretManagerProvider = secretManagerProvider ?? throw new ArgumentNullException(nameof(secretManagerProvider));
    }

    /// <inheritdoc />
    public Task<IGenericResult<IDataVault>> Get(DataVaultRequest request, CancellationToken cancellationToken = default)
    {
        if (request is null || (request.Id is null && string.IsNullOrWhiteSpace(request.Name)))
            return Task.FromResult<IGenericResult<IDataVault>>(
                GenericResult<IDataVault>.Failure(DataVaultLog.EmptyVaultRequest(_logger)));

        if (request.Id.HasValue)
            return Get(request.Id.Value, cancellationToken);

        return Get(request.Name!, cancellationToken);
    }

    /// <inheritdoc />
    public override Task<IGenericResult<IDataVault>> Get(string name, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(name))
            return Task.FromResult(GenericResult<IDataVault>.Failure(DataVaultLog.EmptyVaultRequest(_logger)));

        // Why: cache key is the vault name; the cache factory resolves the composed configuration
        // (header + typed body) via the parent provider, then resolves connection + pepper once.
        return GetCached(name, ResolveConfigByName);
    }

    /// <inheritdoc />
    public override async Task<IGenericResult<IDataVault>> Get(Guid id, CancellationToken cancellationToken = default)
    {
        if (ParentProvider is null)
            return GenericResult<IDataVault>.Failure(DataVaultLog.ResolutionProvidersNotConfigured(_logger, id.ToString()));

        // Why: resolve the header by id first to obtain the vault Name — the cache (and every other
        // Get overload) is keyed by name, so an id and a name for the same vault share one entry.
        var configResult = await ParentProvider.Get(id, cancellationToken).ConfigureAwait(false);
        if (!configResult.IsSuccess || configResult.Value is null)
            return configResult.ToNewResult<IDataVault>();

        var config = configResult.Value;
        return await GetCached(config.Name, (_, _) => Task.FromResult(GenericResult<DataVaultConfiguration>.Success(config))).ConfigureAwait(false);
    }

    /// <inheritdoc />
    // Why: the caller already holds the composed configuration (e.g. an admin UI path) — use it
    // directly; do NOT re-resolve it by name. It still routes through the resolve-once-then-cache
    // path keyed by name, so it shares the cache with the by-name/by-id overloads.
    public override Task<IGenericResult<IDataVault>> Get(DataVaultConfiguration configuration, CancellationToken cancellationToken = default)
    {
        if (configuration is null)
            return Task.FromResult(GenericResult<IDataVault>.Failure(DataVaultLog.EmptyVaultRequest(_logger)));

        return GetCached(configuration.Name,
            (_, _) => Task.FromResult(GenericResult<DataVaultConfiguration>.Success(configuration)));
    }

    // Why: resolves the composed vault configuration (header + typed body) by name via the parent
    // provider, which runs PopulateTypedBody. Used as the cache factory's config source for Get(name).
    private Task<IGenericResult<DataVaultConfiguration>> ResolveConfigByName(string name, CancellationToken cancellationToken)
    {
        if (ParentProvider is null)
            return Task.FromResult(GenericResult<DataVaultConfiguration>.Failure(
                DataVaultLog.ResolutionProvidersNotConfigured(_logger, name)));

        return ParentProvider.Get(name, cancellationToken);
    }

    // Why: single cache entry point. The Lazy<Task<...>> guarantees one resolution per name. On any
    // failure the entry is evicted so the next caller re-attempts rather than being served a cached
    // failure forever.
    private Task<IGenericResult<IDataVault>> GetCached(
        string cacheKey,
        Func<string, CancellationToken, Task<IGenericResult<DataVaultConfiguration>>> configFactory)
    {
        // Why: VSTHRD011/VSTHRD002 fire on Lazy<Task<T>> value factories; the Lazy stores the Task
        // (not the result), so .Value just returns the Task without blocking. ExecutionAndPublication
        // ensures a single Task per name.
#pragma warning disable VSTHRD011, VSTHRD002
        var lazy = _cache.GetOrAdd(cacheKey, key =>
            new Lazy<Task<IGenericResult<IDataVault>>>(
                () => BuildVault(key, configFactory),
                LazyThreadSafetyMode.ExecutionAndPublication));
#pragma warning restore VSTHRD011, VSTHRD002

        return lazy.Value;
    }

    private async Task<IGenericResult<IDataVault>> BuildVault(
        string cacheKey,
        Func<string, CancellationToken, Task<IGenericResult<DataVaultConfiguration>>> configFactory)
    {
        // Why: vaults are long-lived system objects; resolution must not be cancelled by a single
        // caller's token (matches DefaultConnectionProvider, which resolves under None inside the Lazy).
        var configResult = await configFactory(cacheKey, CancellationToken.None).ConfigureAwait(false);
        if (!configResult.IsSuccess || configResult.Value is null)
        {
            _cache.TryRemove(cacheKey, out _);
            return configResult.ToNewResult<IDataVault>();
        }

        var vaultResult = await ResolveVault(configResult.Value, CancellationToken.None).ConfigureAwait(false);
        if (!vaultResult.IsSuccess)
            _cache.TryRemove(cacheKey, out _);

        return vaultResult;
    }

    // Why: THE single resolve-once path. Validates the providers + typed body, resolves the
    // connection + pepper (delegated to keep this under the complexity gate), then hands both to the
    // registered factory — a pure constructor. The pepper bytes never touch the cache key or any log.
    private async Task<IGenericResult<IDataVault>> ResolveVault(DataVaultConfiguration config, CancellationToken cancellationToken)
    {
        var vaultName = config.Name;

        if (_connectionProvider is null || _secretManagerProvider is null)
            return GenericResult<IDataVault>.Failure(DataVaultLog.ResolutionProvidersNotConfigured(_logger, vaultName));

        if (config.Configuration is not IDataVaultConfiguration body)
            return GenericResult<IDataVault>.Failure(DataVaultLog.FactoryConfigurationInvalid(_logger, vaultName));

        var resolved = await ResolveConnectionAndPepper(body, vaultName, cancellationToken).ConfigureAwait(false);
        if (!resolved.IsSuccess)
            return resolved.ToNewResult<IDataVault>();

        if (string.IsNullOrWhiteSpace(config.ServiceOptionType))
            return GenericResult<IDataVault>.Failure(DataVaultLog.NoServiceOptionType(_logger, vaultName));

        if (!Factories.TryGetValue(config.ServiceOptionType, out var factory)
            || factory is not IDataVaultFactory<IDataVault, DataVaultConfiguration> vaultFactory)
            return GenericResult<IDataVault>.Failure(
                DataVaultLog.NoTypedProviderForServiceOptionType(_logger, config.ServiceOptionType, vaultName));

        var createResult = vaultFactory.Create(config, resolved.Value.Connection, resolved.Value.Pepper);
        if (createResult.IsSuccess)
            DataVaultLog.VaultInitialized(_logger, vaultName, body.ConnectionName);

        return createResult;
    }

    // Why: resolves the vault's single connection + its pepper from the typed-body pointers, in system
    // context, fail-loud on every missing input. The pepper bytes never leave this method except into
    // the vault the factory builds.
    private async Task<IGenericResult<(IDataConnection Connection, byte[] Pepper)>> ResolveConnectionAndPepper(
        IDataVaultConfiguration body, string vaultName, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(body.ConnectionName))
            return GenericResult<(IDataConnection, byte[])>.Failure(DataVaultLog.ConnectionNameMissing(_logger, vaultName));
        if (string.IsNullOrWhiteSpace(body.SecretManagerName))
            return GenericResult<(IDataConnection, byte[])>.Failure(DataVaultLog.SecretManagerNameMissing(_logger, vaultName));
        if (string.IsNullOrWhiteSpace(body.PepperSecretName))
            return GenericResult<(IDataConnection, byte[])>.Failure(DataVaultLog.PepperSecretNameMissing(_logger, vaultName));

        var connectionResult = await _connectionProvider!.Get(body.ConnectionName, cancellationToken).ConfigureAwait(false);
        if (!connectionResult.IsSuccess || connectionResult.Value is null)
            return GenericResult<(IDataConnection, byte[])>.Failure(DataVaultLog.ConnectionResolveFailed(_logger, vaultName, body.ConnectionName));

        var managerResult = await _secretManagerProvider!.Get(body.SecretManagerName, cancellationToken).ConfigureAwait(false);
        if (!managerResult.IsSuccess || managerResult.Value is null)
            return GenericResult<(IDataConnection, byte[])>.Failure(DataVaultLog.SecretManagerResolveFailed(_logger, vaultName, body.SecretManagerName));

        var secretResult = await managerResult.Value
            .Execute(GetSecretManagerCommand.Latest(container: null, secretKey: body.PepperSecretName), cancellationToken)
            .ConfigureAwait(false);
        if (!secretResult.IsSuccess || secretResult.Value is not SecretValue secret)
            return GenericResult<(IDataConnection, byte[])>.Failure(DataVaultLog.PepperReadFailed(_logger, vaultName, body.SecretManagerName, body.PepperSecretName));

        // Why: binary secret → raw key bytes; string secret → its UTF-8 bytes as the HMAC key.
        // The SecretValue is disposed immediately; the pepper lives only in the vault from here.
        byte[] pepper;
        using (secret)
        {
            pepper = secret.IsBinary
                ? secret.GetBinaryValue()
                : Encoding.UTF8.GetBytes(secret.GetStringValue());
        }

        if (pepper.Length == 0)
            return GenericResult<(IDataConnection, byte[])>.Failure(DataVaultLog.PepperEmpty(_logger, vaultName));

        return GenericResult<(IDataConnection, byte[])>.Success((connectionResult.Value, pepper));
    }
}
