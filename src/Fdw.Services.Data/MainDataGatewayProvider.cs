using System;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Data.Logging;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Services.Data;

/// <summary>Hands out the data gateway this framework ships.</summary>
/// <remarks>
/// Why this is safe to register singleton: it holds no gateway. Every ask calls
/// <see cref="IDataGatewayFactory"/>, which builds a fresh <see cref="IDataGateway"/> on every
/// call — nothing here is ever captured across asks, so there is nothing to be captive.
/// </remarks>
public sealed class MainDataGatewayProvider : IDataGatewayProvider
{
    private readonly IDataGatewayFactory _factory;
    private readonly MainDataGatewayConfiguration _configuration;
    private readonly ILogger<MainDataGatewayProvider> _logger;

    /// <summary>Initializes a new instance of the <see cref="MainDataGatewayProvider"/> class.</summary>
    /// <param name="factory">Builds a fresh gateway on every ask.</param>
    /// <param name="configuration">The row this framework's own implementation reads.</param>
    /// <param name="logger">The logger for this provider.</param>
    public MainDataGatewayProvider(
        IDataGatewayFactory factory,
        MainDataGatewayConfiguration configuration,
        ILogger<MainDataGatewayProvider>? logger = null)
    {
        _factory = factory;
        _configuration = configuration;
        _logger = logger ?? NullLogger<MainDataGatewayProvider>.Instance;
    }

    /// <inheritdoc />
    /// <remarks>
    /// One implementation is registered today, so any name reaches it -- the parameter exists for
    /// when a second implementation is registered, at which point this routes by it instead of
    /// ignoring it.
    /// </remarks>
    public IDataGateway ByName(string name)
    {
        var result = _factory.Create<IDataGateway>(_configuration);
        return result.IsSuccess && result.Value is not null
            ? result.Value
            : throw new InvalidOperationException(DataGatewayProviderLog.NoGatewaySupplied(_logger).Message);
    }
}
