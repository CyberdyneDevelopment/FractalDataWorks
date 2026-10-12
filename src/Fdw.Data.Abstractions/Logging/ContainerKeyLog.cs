using System;
using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace Fdw.Data.Abstractions.Logging;

/// <summary>
/// MessageLogging for declaring and removing a container key.
/// </summary>
/// <remarks>
/// Why: each condition here shares its EventId with the paired <c>ContainerKeyResultCodeBase</c>
/// TypeOption in <c>Fdw.Data.Abstractions.Results</c> — same number, same meaning. The number is
/// the identity; the CONTAINERKEY prefix says which package raised it. There is no separate
/// logging number.
/// </remarks>
[MessageLoggingTypeCode("CONTAINERKEY")]
public static partial class ContainerKeyLog
{
    /// <summary>Traces entry into declaring a key.</summary>
    [MessageLogging(EventId = 11000, Level = LogLevel.Trace,
        Message = "[ContainerKey] Declaring key '{keyName}' on container '{container}'")]
    public static partial IGenericMessage CreatingKey(ILogger logger, string keyName, string container);

    /// <summary>Logs a key that was declared.</summary>
    [MessageLogging(EventId = 11001, Level = LogLevel.Information,
        Message = "[ContainerKey] Declared key '{keyName}' on container '{container}'")]
    public static partial IGenericMessage KeyCreated(ILogger logger, string keyName, string container);

    /// <summary>Traces entry into removing a key.</summary>
    [MessageLogging(EventId = 11002, Level = LogLevel.Trace,
        Message = "[ContainerKey] Removing key '{keyName}' from container '{container}'")]
    public static partial IGenericMessage DeletingKey(ILogger logger, string keyName, string container);

    /// <summary>Logs a key that was removed.</summary>
    [MessageLogging(EventId = 11003, Level = LogLevel.Information,
        Message = "[ContainerKey] Removed key '{keyName}' from container '{container}'")]
    public static partial IGenericMessage DeletedKey(ILogger logger, string keyName, string container);

    /// <summary>Logs a key declared with no fields.</summary>
    [MessageLogging(EventId = 21000, Level = LogLevel.Warning,
        Message = "[ContainerKey] Key '{keyName}' on container '{container}' declares no fields")]
    public static partial IGenericMessage KeyDeclaresNoFields(ILogger logger, string keyName, string container);

    /// <summary>Logs a referencing key that names no referenced container.</summary>
    [MessageLogging(EventId = 21001, Level = LogLevel.Warning,
        Message = "[ContainerKey] Key '{keyName}' on container '{container}' references nothing")]
    public static partial IGenericMessage ForeignKeyMissingReference(ILogger logger, string keyName, string container);

    /// <summary>Logs a key declared against a container that does not exist.</summary>
    [MessageLogging(EventId = 31000, Level = LogLevel.Warning,
        Message = "[ContainerKey] Container '{container}' was not found")]
    public static partial IGenericMessage KeyContainerNotFound(ILogger logger, string container);

    /// <summary>Logs a key field naming a field the container does not declare.</summary>
    [MessageLogging(EventId = 31001, Level = LogLevel.Warning,
        Message = "[ContainerKey] Key field '{fieldName}' is not declared on container '{container}'")]
    public static partial IGenericMessage KeyFieldNotDeclared(ILogger logger, string fieldName, string container);

    /// <summary>Logs a second primary key declared on one container.</summary>
    [MessageLogging(EventId = 41000, Level = LogLevel.Warning,
        Message = "[ContainerKey] Container '{container}' already declares a primary key")]
    public static partial IGenericMessage ContainerAlreadyHasPrimaryKey(ILogger logger, string container);
}
