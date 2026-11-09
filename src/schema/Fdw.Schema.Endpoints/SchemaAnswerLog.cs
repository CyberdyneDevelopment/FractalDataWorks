using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace Fdw.Schema.Endpoints;

/// <summary>The coded messages the schema endpoints answer a refusal or a fault with: one per reason, each with the client text it shows (FDW-2335).</summary>
[MessageLoggingTypeCode("SCHEMAANSWERS")]
public static partial class SchemaAnswerLog
{

    /// <summary>Logs that Source mappings was not found, or is not visible to the caller.</summary>
    [MessageLogging(EventId = 30001, Level = LogLevel.Warning, Message = "Source mappings was not found, or is not visible to the caller", ClientMessage = "Source mappings was not found")]
    public static partial IGenericMessage SaveSourceMappingsSourcemappingsNotFound(ILogger logger);

    /// <summary>Logs that Data set mappings was not found, or is not visible to the caller.</summary>
    [MessageLogging(EventId = 30002, Level = LogLevel.Warning, Message = "Data set mappings was not found, or is not visible to the caller", ClientMessage = "Data set mappings was not found")]
    public static partial IGenericMessage GetDataSetMappingsNotFound(ILogger logger);

    /// <summary>Logs that Source mappings was not found, or is not visible to the caller.</summary>
    [MessageLogging(EventId = 30003, Level = LogLevel.Warning, Message = "Source mappings was not found, or is not visible to the caller", ClientMessage = "Source mappings was not found")]
    public static partial IGenericMessage GetSourceMappingsNotFound(ILogger logger);

    /// <summary>Logs that Source mappings was not found, or is not visible to the caller.</summary>
    [MessageLogging(EventId = 30004, Level = LogLevel.Warning, Message = "Source mappings was not found, or is not visible to the caller", ClientMessage = "Source mappings was not found")]
    public static partial IGenericMessage GetSourceMappingsNotFound2(ILogger logger);

    /// <summary>Logs that Source mappings was not found, or is not visible to the caller.</summary>
    [MessageLogging(EventId = 30005, Level = LogLevel.Warning, Message = "Source mappings was not found, or is not visible to the caller", ClientMessage = "Source mappings was not found")]
    public static partial IGenericMessage SaveSourceMappingsNotFound(ILogger logger);
}
