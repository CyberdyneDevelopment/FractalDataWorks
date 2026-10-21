using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Data.Etl;

/// <summary>Message logging for what the ETL data gateway adds to the data gateway (FDW-1952).</summary>
[MessageLoggingTypeCode("ETLDG")]
public static partial class EtlDataGatewayLogger
{
    /// <summary>Logs a command that asked for another physical address and was refused, because the declared container cannot take one.</summary>
    [MessageLogging(
        EventId = 90004,
        Level = LogLevel.Error,
        Message = "Data store '{dataStore}' cannot be read at another path or name: {reason}")]
    public static partial IGenericMessage AddressOverrideRefused(ILogger logger, string dataStore, string reason);
}
