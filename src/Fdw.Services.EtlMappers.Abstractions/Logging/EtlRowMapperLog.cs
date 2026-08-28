using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.EtlMappers.Abstractions.Logging;

/// <summary>
/// MessageLogging for ETL row mapper operations.
/// EventId range: 8300-8399
/// </summary>
[MessageLoggingTypeCode("ABSTRACTIONS7")]
public static partial class EtlRowMapperLog
{
    #region Initialization (8300-8309)

    /// <summary>
    /// Logs mapper initialization start.
    /// </summary>
    [MessageLogging(
        EventId = 11000,
        Level = LogLevel.Debug,
        Message = "Initializing mapper '{mapperType}' for schema with {fieldCount} fields")]
    public static partial IGenericMessage MapperInitializing(
        ILogger logger,
        string mapperType,
        int fieldCount);

    /// <summary>
    /// Logs mapper compilation complete.
    /// </summary>
    [MessageLogging(
        EventId = 11001,
        Level = LogLevel.Debug,
        Message = "Mapper '{mapperType}' compiled in {elapsedMs}ms")]
    public static partial IGenericMessage MapperCompiled(
        ILogger logger,
        string mapperType,
        double elapsedMs);

    /// <summary>
    /// Logs mapper fallback scenario.
    /// </summary>
    [MessageLogging(
        EventId = 91000,
        Level = LogLevel.Warning,
        Message = "Mapper '{mapperType}' fallback: {reason}")]
    public static partial IGenericMessage MapperFallback(
        ILogger logger,
        string mapperType,
        string reason);

    /// <summary>
    /// Logs that a field was not found in the result set during ordinal lookup; ordinal set to -1.
    /// </summary>
    [MessageLogging(
        EventId = 11002,
        Level = LogLevel.Error,
        Message = "Field '{fieldName}' not found in result set during mapper initialization; ordinal set to -1")]
    public static partial IGenericMessage FieldOrdinalNotFound(
        ILogger logger,
        string fieldName);

    #endregion

    #region Errors (8310-8319)

    /// <summary>
    /// Logs mapper initialization failure.
    /// </summary>
    [MessageLogging(
        EventId = 91001,
        Level = LogLevel.Error,
        Message = "Mapper initialization failed: {error}")]
    public static partial IGenericMessage MapperInitializationFailed(
        ILogger logger,
        string error);

    /// <summary>
    /// Logs mapper creation failure.
    /// </summary>
    [MessageLogging(
        EventId = 91002,
        Level = LogLevel.Error,
        Message = "Failed to create mapper '{mapperType}': {error}")]
    public static partial IGenericMessage MapperCreationFailed(
        ILogger logger,
        string mapperType,
        string error);

    /// <summary>
    /// Logs row mapping failure.
    /// </summary>
    [MessageLogging(
        EventId = 91003,
        Level = LogLevel.Error,
        Message = "Failed to map row: {error}")]
    public static partial IGenericMessage RowMappingFailed(
        ILogger logger,
        string error);

    #endregion

    #region Provider (8320-8329)

    /// <summary>
    /// Logs mapper type registration.
    /// </summary>
    [MessageLogging(
        EventId = 11003,
        Level = LogLevel.Debug,
        Message = "Registered mapper type '{mapperType}' with factory '{factoryType}'")]
    public static partial IGenericMessage MapperTypeRegistered(
        ILogger logger,
        string mapperType,
        string factoryType);

    /// <summary>
    /// Logs provider initialization.
    /// </summary>
    [MessageLogging(
        EventId = 11004,
        Level = LogLevel.Information,
        Message = "ETL row mapper provider initialized with {mapperCount} mapper types")]
    public static partial IGenericMessage ProviderInitialized(
        ILogger logger,
        int mapperCount);

    #endregion
}
