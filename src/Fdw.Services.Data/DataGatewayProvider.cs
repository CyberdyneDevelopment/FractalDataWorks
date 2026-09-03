using Fdw.Results;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Data.Logging;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Services.Data;

/// <summary>Hands out the data gateway.</summary>
public sealed class DataGatewayProvider : IDataGatewayProvider
{
    private readonly IDataGateway? _gateway;
    private readonly ILogger<DataGatewayProvider> _logger;

    /// <summary>Initializes a new instance of the <see cref="DataGatewayProvider"/> class.</summary>
    /// <param name="gateway">The gateway this provider hands out.</param>
    /// <param name="logger">The logger for this provider.</param>
    public DataGatewayProvider(
        IDataGateway? gateway = null,
        ILogger<DataGatewayProvider>? logger = null)
    {
        _gateway = gateway;
        _logger = logger ?? NullLogger<DataGatewayProvider>.Instance;
    }

    /// <inheritdoc />
    public IGenericResult<IDataGateway> Get()
        => _gateway is null
            ? GenericResult<IDataGateway>.Failure(DataGatewayProviderLog.NoGatewaySupplied(_logger))
            : GenericResult<IDataGateway>.Success(_gateway);
}
