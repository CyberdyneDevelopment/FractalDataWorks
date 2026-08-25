using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace Fdw.Data.Transformations;

/// <summary>
/// Static logger class for field transformer operations.
/// EventId range: 5431-5461 (DataSet sub-range).
/// </summary>
[MessageLoggingTypeCode("DATASETS")]
public static partial class FieldTransformerLog
{
    // ========================================================================
    // FromUnixMilliseconds (5431-5432)
    // ========================================================================

    /// <summary>
    /// Logs that the input value is null when a numeric epoch-milliseconds value is required.
    /// </summary>
    [MessageLogging(
        EventId = 21000,
        Level = LogLevel.Error,
        Message = "FromUnixMilliseconds: input value is null; a numeric epoch-milliseconds value is required")]
    public static partial IGenericMessage InputIsNull(ILogger logger);

    /// <summary>
    /// Logs that the input value cannot be converted to a long epoch-milliseconds value.
    /// </summary>
    [MessageLogging(
        EventId = 91005,
        Level = LogLevel.Error,
        Message = "FromUnixMilliseconds: cannot convert input of type '{inputTypeName}' to long; provide a numeric epoch-milliseconds value")]
    public static partial IGenericMessage InputNotConvertibleToLong(ILogger logger, string inputTypeName);
}
