using Fdw.Messages;
using Fdw.MessageLogging;
using Microsoft.Extensions.Logging;

namespace Fdw.Roslyn.Commands.Search.Logging;

/// <summary>
/// MessageLogging methods for FamilyTypeResolver.
/// EventId range: 9100-9109.
/// </summary>
[MessageLoggingTypeCode("SEARCH")]
public static partial class FamilyTypeResolverLog
{
    /// <summary>Trace: starting resolution.</summary>
    [MessageLogging(EventId = 11031, Level = LogLevel.Trace,
        Message = "Resolve start for type {typeName}")]
    public static partial IGenericMessage ResolveStart(ILogger logger, string typeName);

    /// <summary>Trace: fully-qualified attempt per project.</summary>
    [MessageLogging(EventId = 11032, Level = LogLevel.Trace,
        Message = "FQN attempt project={projectName} typeName={typeName}")]
    public static partial IGenericMessage FqnAttempt(ILogger logger, string projectName, string typeName);

    /// <summary>Debug: fully-qualified lookup hit.</summary>
    [MessageLogging(EventId = 11033, Level = LogLevel.Debug,
        Message = "FQN hit in project={projectName} for {typeName}")]
    public static partial IGenericMessage FqnHit(ILogger logger, string projectName, string typeName);

    /// <summary>Trace: fell through to simple-name pass.</summary>
    [MessageLogging(EventId = 11034, Level = LogLevel.Trace,
        Message = "FQN miss across all projects, falling back to simple-name pass for {simpleName}")]
    public static partial IGenericMessage SimpleNameFallback(ILogger logger, string simpleName);

    /// <summary>Trace: simple-name attempt in a single project.</summary>
    [MessageLogging(EventId = 11035, Level = LogLevel.Trace,
        Message = "Simple-name attempt project={projectName} simpleName={simpleName}")]
    public static partial IGenericMessage SimpleNameAttempt(ILogger logger, string projectName, string simpleName);

    /// <summary>Debug: simple-name lookup hit.</summary>
    [MessageLogging(EventId = 11036, Level = LogLevel.Debug,
        Message = "Simple-name hit in project={projectName} resolved to {fullName}")]
    public static partial IGenericMessage SimpleNameHit(ILogger logger, string projectName, string fullName);

    /// <summary>Warning: resolution failed entirely.</summary>
    [MessageLogging(EventId = 31001, Level = LogLevel.Warning,
        Message = "Type not found: {typeName} (tried FQN and simple-name across all projects)")]
    public static partial IGenericMessage NotFound(ILogger logger, string typeName);

    /// <summary>Trace: cancellation observed during resolution.</summary>
    [MessageLogging(EventId = 11037, Level = LogLevel.Trace,
        Message = "Resolution cancelled for {typeName}")]
    public static partial IGenericMessage Cancelled(ILogger logger, string typeName);
}
