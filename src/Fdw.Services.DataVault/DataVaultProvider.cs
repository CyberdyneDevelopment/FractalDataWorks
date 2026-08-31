using System;
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
/// Wraps <see cref="PlatformServiceProviderBase{TService,TConfiguration,TFactory,TConfigurationProvider}"/>
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
public sealed class DataVaultProvider
    : PlatformServiceProviderBase<
          IDataVault,
          IDataVaultImplementationConfiguration,
          IDataVaultFactory<IDataVault, IDataVaultImplementationConfiguration>,
          IDataVaultConfigurationProvider>,
      IDataVaultProvider
{
    private readonly ILogger<DataVaultProvider> _logger;

    private IDataConnectionProvider? _connectionProvider;
    private ISecretManagerProvider? _secretManagerProvider;

    /// <summary>
    /// Initializes a new instance of the <see cref="DataVaultProvider"/> class.
    /// </summary>
    /// <param name="logger">Logger instance.</param>
    /// <param name="services">The container this provider resolves factories from.</param>
    public DataVaultProvider(IServiceProvider services, ILogger<DataVaultProvider> logger)
        : base(services, logger ?? NullLogger<DataVaultProvider>.Instance)
    {
        _logger = logger ?? NullLogger<DataVaultProvider>.Instance;
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
        ISecretManagerProvider secretManagerProvider)
    {
        _connectionProvider = connectionProvider ?? throw new ArgumentNullException(nameof(connectionProvider));
        _secretManagerProvider = secretManagerProvider ?? throw new ArgumentNullException(nameof(secretManagerProvider));
    }

    /// <inheritdoc />
    public async Task<IGenericResult<IDataVault>> Get(DataVaultRequest request, CancellationToken cancellationToken = default)
    {
        if (request is null || (request.Id is null && string.IsNullOrWhiteSpace(request.Name)))
            return GenericResult<IDataVault>.Failure(DataVaultLog.EmptyVaultRequest(_logger));

        // Not the base Get: that creates from configuration alone, and a vault cannot be built without
        // its resolved connection and pepper — the factory refuses a config-only call by design.
        var configuration = request.Id.HasValue
            ? await DomainConfigurationProvider!.Get(request.Id.Value, cancellationToken).ConfigureAwait(false)
            : await DomainConfigurationProvider!.Get(request.Name!, cancellationToken).ConfigureAwait(false);

        if (!configuration.IsSuccess || configuration.Value is null)
            return configuration.ToNewResult<IDataVault>();

        var body = configuration.Value;
        var vaultName = body.Name;

        if (_connectionProvider is null || _secretManagerProvider is null)
            return GenericResult<IDataVault>.Failure(DataVaultLog.ResolutionProvidersNotConfigured(_logger, vaultName));

        var resolved = await ResolveConnectionAndPepper(body, vaultName, cancellationToken).ConfigureAwait(false);
        if (!resolved.IsSuccess)
            return resolved.ToNewResult<IDataVault>();

        if (string.IsNullOrWhiteSpace(body.ServiceOptionType))
            return GenericResult<IDataVault>.Failure(DataVaultLog.NoServiceOptionType(_logger, vaultName));

        if (!Factories.TryGetValue(body.ServiceOptionType, out var factory)
            || factory is not IDataVaultFactory<IDataVault, IDataVaultImplementationConfiguration> vaultFactory)
            return GenericResult<IDataVault>.Failure(
                DataVaultLog.NoTypedProviderForServiceOptionType(_logger, body.ServiceOptionType, vaultName));

        var createResult = vaultFactory.Create(body, resolved.Value.Connection, resolved.Value.Pepper);
        if (createResult.IsSuccess)
            DataVaultLog.VaultInitialized(_logger, vaultName, body.ConnectionName);

        return createResult;
    }

    private async Task<IGenericResult<(IDataConnection Connection, byte[] Pepper)>> ResolveConnectionAndPepper(
        IDataVaultImplementationConfiguration body, string vaultName, CancellationToken cancellationToken)
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
