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

    // ========================================================================
    // Parameter injection (91010-91011)
    // ========================================================================

    /// <summary>The name parameter was not supplied.</summary>
    /// <param name="logger">The logger.</param>
    /// <returns>The message.</returns>
    [MessageLogging(
        EventId = 91010,
        Level = LogLevel.Error,
        Message = "Parameter transform requires a 'name' parameter naming the run value to inject; none was supplied")]
    public static partial IGenericMessage ParameterNameMissing(ILogger logger);

    /// <summary>The named run value does not exist.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="name">The name that was asked for.</param>
    /// <returns>The message.</returns>
    [MessageLogging(
        EventId = 91011,
        Level = LogLevel.Error,
        Message = "Parameter transform was asked for '{name}', which is not a run value. Valid names are operatingDate and now")]
    public static partial IGenericMessage ParameterNameUnknown(ILogger logger, string name);
}
