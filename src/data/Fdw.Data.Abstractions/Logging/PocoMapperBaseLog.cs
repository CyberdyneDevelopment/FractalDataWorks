using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace Fdw.Data.Abstractions.Logging;

/// <summary>
/// MessageLogging for <see cref="Mappers.PocoMappers.PocoMapperBase"/> construction.
/// </summary>
[MessageLoggingTypeCode("MAPPER")]
public static partial class PocoMapperBaseLog
{
    /// <summary>Traces a generated POCO mapper being constructed (compile-time discovery via [TypeOption]).</summary>
    [MessageLogging(EventId = 11005, Level = LogLevel.Trace,
        Message = "[PocoMapperBase] Registering mapper for '{typeFullName}' targeting '{targetTypeName}'")]
    public static partial IGenericMessage MapperRegistering(ILogger logger, string typeFullName, string targetTypeName);
}
