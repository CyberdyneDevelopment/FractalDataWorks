using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Data.Logging;

/// <summary>MessageLogging for <see cref="DataGatewayProvider"/>.</summary>
public static partial class DataGatewayProviderLog
{
    /// <summary>Nothing registered a way to reach the gateway.</summary>
    /// <param name="logger">The logger.</param>
    /// <returns>The logged message.</returns>
    [MessageLogging(EventId = 61033, Level = LogLevel.Error,
        Message = "No data gateway was supplied to the provider, so nothing can read the data plane.")]
    public static partial IGenericMessage NoGatewaySupplied(ILogger logger);

    /// <summary>A command arrived with no address to route it by.</summary>
    /// <param name="logger">The logger.</param>
    /// <returns>The logged message.</returns>
    [MessageLogging(EventId = 61034, Level = LogLevel.Error,
        Message = "A data gateway routes by DataStoreTarget or DataSetTarget. A bare command carries "
                + "no address, so there is nothing to route it to.")]
    public static partial IGenericMessage CommandCarriesNoAddress(ILogger logger);
}
