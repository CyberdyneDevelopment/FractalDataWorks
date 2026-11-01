using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace Fdw.Data.Abstractions.Logging;

/// <summary>
/// MessageLogging for <see cref="DataMapperBase{TSource, TTarget}"/> CLR-bridge mapping.
/// </summary>
[MessageLoggingTypeCode("MAPPER")]
public static partial class DataMapperBaseLog
{
    /// <summary>Traces a value being mapped via the two-step CLR bridge (Source → CLR → Target).</summary>
    [MessageLogging(EventId = 11003, Level = LogLevel.Trace,
        Message = "[DataMapperBase] Mapping via CLR bridge from '{sourceConverter}' to '{targetConverter}'")]
    public static partial IGenericMessage MappingViaClr(ILogger logger, string sourceConverter, string targetConverter);
}
