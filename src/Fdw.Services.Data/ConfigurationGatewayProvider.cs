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
    private readonly Func<string, IGenericResult<IConfigurationGateway>>? _build;

    /// <summary>Initializes a new instance of the <see cref="ConfigurationGatewayProvider"/> class.</summary>
    /// <param name="build">Builds the gateway for a declared connection, on first request.</param>
    /// <param name="logger">The logger.</param>
    /// <remarks>
    /// Why the gateways are built on first request rather than when the domain registers: a gateway
    /// needs its connection's factory, and registration runs collections in category order,
    /// which puts ConfigurationGateway ahead of Connection. Building on demand means registration
    /// has finished before any gateway is needed, so no host has to know that order.
    /// </remarks>
    public ConfigurationGatewayProvider(
        Func<string, IGenericResult<IConfigurationGateway>>? build = null,
        ILogger<ConfigurationGatewayProvider>? logger = null)
    {
        _build = build;
        _logger = logger ?? NullLogger<ConfigurationGatewayProvider>.Instance;
    }

    /// <inheritdoc />
    public IGenericResult<IConfigurationGateway> Get(string connectionName)
    {
        if (string.IsNullOrWhiteSpace(connectionName))
            return GenericResult<IConfigurationGateway>.Failure(
                ConfigurationGatewayProviderLog.ConnectionNameMissing(_logger));

        if (_gateways.TryGetValue(connectionName, out var gateway))
            return GenericResult<IConfigurationGateway>.Success(gateway);

        if (_build is not null)
        {
            var built = _build(connectionName);
            if (built.IsSuccess && built.Value is not null)
            {
                _gateways.TryAdd(connectionName, built.Value);
                return GenericResult<IConfigurationGateway>.Success(_gateways[connectionName]);
            }

            if (built.IsFailure)
                return built;
        }

        return
            GenericResult<IConfigurationGateway>.Failure(
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
