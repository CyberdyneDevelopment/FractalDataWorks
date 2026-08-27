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
/// Holds the configuration gateways by the connection each operates on.
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

        if (_gateways.TryGetValue(connectionName, out var gateway))
            return GenericResult<IConfigurationGateway>.Success(gateway);

        // Why the registered names are in the failure: the caller named a connection its collection
        // holds, so the miss is a wiring question — which gateways did come up — not a lookup question.
        return GenericResult<IConfigurationGateway>.Failure(
            DataServiceResultCodes.ByName("NoConfigurationGateway"),
            ResultDetails.Create(
                "ConnectionName", connectionName,
                "Registered", _gateways.IsEmpty ? "(none)" : string.Join(", ", _gateways.Keys.OrderBy(k => k, StringComparer.Ordinal))));
    }

    /// <inheritdoc />
    public IGenericResult Register(string connectionName, IConfigurationGateway gateway)
    {
        if (string.IsNullOrWhiteSpace(connectionName))
            return GenericResult.Failure(ConfigurationGatewayProviderLog.ConnectionNameMissing(_logger));

        if (gateway is null)
            return GenericResult.Failure(ConfigurationGatewayProviderLog.GatewayNull(_logger, connectionName));

        if (!_gateways.TryAdd(connectionName, gateway))
            return GenericResult.Failure(
                DataServiceResultCodes.ByName("ConfigurationGatewayAlreadyRegistered"),
                ResultDetails.Create("ConnectionName", connectionName));

        ConfigurationGatewayProviderLog.GatewayRegistered(_logger, connectionName, gateway.GetType().Name);
        return GenericResult.Success();
    }
}
