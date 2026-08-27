using System;
using System.Collections.Concurrent;
using System.Linq;
using Fdw.Results;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Data.Logging;
using Fdw.Services.Data.Results;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Services.Data;

/// <summary>
/// Holds the configuration gateways by the connection each one opened.
/// </summary>
public sealed class ConfigurationGatewayProvider : IConfigurationGatewayProvider
{
    private readonly ConcurrentDictionary<string, IConfigurationGateway> _gateways =
        new(StringComparer.OrdinalIgnoreCase);

    private readonly ILogger<ConfigurationGatewayProvider> _logger;

    /// <summary>Initializes a new instance of the <see cref="ConfigurationGatewayProvider"/> class.</summary>
    /// <param name="logger">The logger.</param>
    public ConfigurationGatewayProvider(ILogger<ConfigurationGatewayProvider>? logger = null)
        => _logger = logger ?? NullLogger<ConfigurationGatewayProvider>.Instance;

    /// <inheritdoc />
    public IGenericResult<IConfigurationGateway> Get(string connectionName)
    {
        if (string.IsNullOrWhiteSpace(connectionName))
            return GenericResult<IConfigurationGateway>.Failure(
                ConfigurationGatewayProviderLog.ConnectionNameMissing(_logger));

        return _gateways.TryGetValue(connectionName, out var gateway)
            ? GenericResult<IConfigurationGateway>.Success(gateway)

            // Why the held connections are in the failure: the caller named the connection its
            // collection declares, so a miss is a question about which gateways came up, not about
            // whether the name was spelled right.
            : GenericResult<IConfigurationGateway>.Failure(
                DataServiceResultCodes.ByName("NoConfigurationGateway"),
                ResultDetails.Create("ConnectionName", connectionName, "Registered", Held()));
    }

    /// <inheritdoc />
    public IGenericResult Register(IConfigurationGateway gateway)
    {
        if (gateway is null)
            return GenericResult.Failure(ConfigurationGatewayProviderLog.GatewayNull(_logger));

        if (string.IsNullOrWhiteSpace(gateway.ConnectionName))
            return GenericResult.Failure(
                ConfigurationGatewayProviderLog.GatewayNamesNoConnection(_logger, gateway.GetType().Name));

        if (!_gateways.TryAdd(gateway.ConnectionName, gateway))
            return GenericResult.Failure(
                DataServiceResultCodes.ByName("ConfigurationGatewayAlreadyRegistered"),
                ResultDetails.Create("ConnectionName", gateway.ConnectionName));

        ConfigurationGatewayProviderLog.GatewayRegistered(_logger, gateway.ConnectionName, gateway.GetType().Name);
        return GenericResult.Success();
    }

    private string Held()
        => _gateways.IsEmpty
            ? "(none)"
            : string.Join(", ", _gateways.Keys.OrderBy(k => k, StringComparer.Ordinal));
}
