using System;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.Services.Hosts.Abstractions;
using Fdw.Services.SecretManagers.Abstractions;
using Fdw.Services.SecretManagers.Commands;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.WebMcp.Hosting;

/// <summary>
/// Reads what a host's WebMCP surface needs from its WebMcp configuration row: the header the client
/// key travels in, the key itself from the secret manager the row names, and where the address the
/// host reaches its own routes on comes from.
/// </summary>
/// <remarks>
/// Read once and kept. The row is startup configuration -- an edit to it is seen after a restart, like
/// every configuration read -- and a tool call should not cost a secret-manager round trip. Nothing
/// is filled in: a row that says too little, or contradicts itself, is refused and says what is wrong.
/// </remarks>
public sealed class WebMcpHostSettingsReader
{
    private readonly IWebMcpConfigurationProvider _configurations;
    private readonly ISecretManagerProvider _secretManagers;
    private readonly ILogger<WebMcpHostSettingsReader> _logger;
    private WebMcpHostSettings? _settings;

    /// <summary>Initializes the reader.</summary>
    /// <param name="configurations">The WebMcp domain's configuration provider.</param>
    /// <param name="secretManagers">Resolves the secret manager a row names.</param>
    /// <param name="logger">The logger.</param>
    public WebMcpHostSettingsReader(
        IWebMcpConfigurationProvider configurations,
        ISecretManagerProvider secretManagers,
        ILogger<WebMcpHostSettingsReader>? logger = null)
    {
        _configurations = configurations;
        _secretManagers = secretManagers;
        _logger = logger ?? NullLogger<WebMcpHostSettingsReader>.Instance;
    }

    /// <summary>Reads the settings, the first time from the row and after that from what was read.</summary>
    /// <param name="cancellationToken">Cancels the read.</param>
    /// <returns>The settings, or a failure naming what the row is missing or gets wrong.</returns>
    public async Task<IGenericResult<WebMcpHostSettings>> Read(CancellationToken cancellationToken = default)
    {
        if (Volatile.Read(ref _settings) is { } known)
        {
            return GenericResult<WebMcpHostSettings>.Success(known);
        }

        // Two first callers may both read; the row says the same thing to both, and the first to land
        // is the one kept.
        var read = await ReadRow(cancellationToken).ConfigureAwait(false);
        if (read.IsSuccess)
        {
            Interlocked.CompareExchange(ref _settings, read.Value, null);
        }

        return read;
    }

    private async Task<IGenericResult<WebMcpHostSettings>> ReadRow(CancellationToken cancellationToken)
    {
        var name = WebMcpHosting.ConfigurationName;
        var row = await _configurations.Get(name, cancellationToken).ConfigureAwait(false);
        if (row.IsFailure || row.Value is not { } configuration)
        {
            return GenericResult<WebMcpHostSettings>.Failure(WebMcpLog.NotConfigured(_logger, name));
        }

        if (string.IsNullOrWhiteSpace(configuration.ApiKeyHeader))
        {
            return GenericResult<WebMcpHostSettings>.Failure(
                WebMcpLog.ConfigurationMissing(_logger, name, nameof(configuration.ApiKeyHeader)));
        }

        var clientKey = await ClientKey(name, configuration, cancellationToken).ConfigureAwait(false);
        if (clientKey.IsFailure)
        {
            return clientKey.ToNewResult<WebMcpHostSettings>();
        }

        var source = SelfAddressSource(name, configuration);
        if (source.IsFailure)
        {
            return source.ToNewResult<WebMcpHostSettings>();
        }

        var selfAddress = source.Value is { } chosen
            ? chosen.Check(name, configuration.SelfBaseAddress, _logger)
            : GenericResult<Uri?>.Success(null);
        if (selfAddress.IsFailure)
        {
            return selfAddress.ToNewResult<WebMcpHostSettings>();
        }

        var settings = new WebMcpHostSettings(configuration.ApiKeyHeader, clientKey.Value, source.Value, selfAddress.Value);
        WebMcpLog.SettingsRead(_logger, name, settings.ApiKeyHeader,
            servesClientKey: settings.ClientApiKey is not null, selfAddressSource: settings.SelfAddressSource?.Name ?? string.Empty);
        return GenericResult<WebMcpHostSettings>.Success(settings);
    }

    /// <summary>The client key the row names, or <see langword="null"/> when it names none.</summary>
    private async Task<IGenericResult<string?>> ClientKey(
        string name,
        IWebMcpImplementationConfiguration configuration,
        CancellationToken cancellationToken)
    {
        var managerName = configuration.SecretManagerName;
        var keyName = configuration.SecretKeyName;
        if (string.IsNullOrWhiteSpace(managerName) && string.IsNullOrWhiteSpace(keyName))
        {
            return GenericResult<string?>.Success(null);
        }

        if (string.IsNullOrWhiteSpace(managerName) || string.IsNullOrWhiteSpace(keyName))
        {
            return GenericResult<string?>.Failure(WebMcpLog.ConfigurationInvalid(_logger, name,
                "SecretManagerName and SecretKeyName name the client key together; one without the other names nothing"));
        }

        return await ResolveKey(name, managerName, keyName, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>The client key's value, from the secret manager and name the row gives.</summary>
    private async Task<IGenericResult<string?>> ResolveKey(
        string name,
        string managerName,
        string keyName,
        CancellationToken cancellationToken)
    {
        var manager = await _secretManagers.Get(managerName, cancellationToken).ConfigureAwait(false);
        if (manager.IsFailure || manager.Value is not { } secretManager)
        {
            return manager.ToNewResult<string?>();
        }

        var secret = await secretManager
            .Execute(GetSecretManagerCommand.Latest(container: null, secretKey: keyName), cancellationToken)
            .ConfigureAwait(false);
        if (secret.IsFailure || secret.Value is not { } value)
        {
            return secret.ToNewResult<string?>();
        }

        using (value)
        {
            return value.GetStringValue() is { Length: > 0 } key
                ? GenericResult<string?>.Success(key)
                : GenericResult<string?>.Failure(WebMcpLog.ConfigurationMissing(_logger, name, keyName));
        }
    }

    /// <summary>
    /// The source the row names for the host's own address (FDW-5721), or <see langword="null"/> when it names
    /// none and states no address, which is a host that does not serve MCP.
    /// </summary>
    /// <remarks>
    /// An address stated with no source is refused rather than read as FixedAddress: the row has to say
    /// where the address comes from, and nothing is assumed for it.
    /// </remarks>
    private IGenericResult<WebMcpSelfAddressSourceBase?> SelfAddressSource(string name, IWebMcpImplementationConfiguration configuration)
    {
        if (string.IsNullOrWhiteSpace(configuration.SelfAddressSource))
        {
            return string.IsNullOrWhiteSpace(configuration.SelfBaseAddress)
                ? GenericResult<WebMcpSelfAddressSourceBase?>.Success(null)
                : GenericResult<WebMcpSelfAddressSourceBase?>.Failure(
                    WebMcpLog.SelfAddressSourceNotStated(_logger, name, configuration.SelfBaseAddress));
        }

        var source = WebMcpSelfAddressSources.ByName(configuration.SelfAddressSource.Trim());
        return source.IsEmpty
            ? GenericResult<WebMcpSelfAddressSourceBase?>.Failure(
                WebMcpLog.SelfAddressSourceUnknown(_logger, name, configuration.SelfAddressSource))
            : GenericResult<WebMcpSelfAddressSourceBase?>.Success(source);
    }
}
