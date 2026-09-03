using System;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Data.Logging;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Services.Data;

/// <summary>Hands out the data gateway this framework ships.</summary>
public sealed class MainDataGatewayProvider : IDataGatewayProvider
{
    private readonly IDataGateway? _gateway;
    private readonly ILogger<MainDataGatewayProvider> _logger;

    /// <summary>Initializes a new instance of the <see cref="MainDataGatewayProvider"/> class.</summary>
    /// <param name="gateway">The gateway this provider hands out.</param>
    /// <param name="logger">The logger for this provider.</param>
    public MainDataGatewayProvider(
        IDataGateway? gateway = null,
        ILogger<MainDataGatewayProvider>? logger = null)
    {
        _gateway = gateway;
        _logger = logger ?? NullLogger<MainDataGatewayProvider>.Instance;
    }

    /// <inheritdoc />
    /// <remarks>
    /// One gateway is registered today, so any name reaches it -- the parameter exists for when a
    /// second implementation is registered, at which point this routes by it instead of ignoring it.
    /// </remarks>
    public IDataGateway ByName(string name)
        => _gateway
            ?? throw new InvalidOperationException(DataGatewayProviderLog.NoGatewaySupplied(_logger).Message);
}
