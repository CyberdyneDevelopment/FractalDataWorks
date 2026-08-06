using Fdw.Messages;
using Fdw.MessageLogging;
using Microsoft.Extensions.Logging;

namespace Fdw.Roslyn.Commands.Search.Logging;

/// <summary>
/// MessageLogging methods for FindFamilyDerivedTypesTranslator.
/// EventId range: 9220-9249.
/// </summary>
[MessageLoggingTypeCode("SEARCH")]
public static partial class FindFamilyDerivedTypesTranslatorLog
{
    /// <summary>Info: Translate called.</summary>
    [MessageLogging(EventId = 11038, Level = LogLevel.Information,
        Message = "FindFamilyDerivedTypes.Translate start root={rootTypeName}")]
    public static partial IGenericMessage TranslateStart(ILogger logger, string rootTypeName);

    /// <summary>Warning: RootTypeName parameter was empty.</summary>
    [MessageLogging(EventId = 21001, Level = LogLevel.Warning,
        Message = "FindFamilyDerivedTypes validation failed: RootTypeName required")]
    public static partial IGenericMessage ValidationFailedRootRequired(ILogger logger);

    /// <summary>Warning: root type could not be resolved.</summary>
    [MessageLogging(EventId = 31002, Level = LogLevel.Warning,
        Message = "FindFamilyDerivedTypes root not found: {rootTypeName}")]
    public static partial IGenericMessage RootNotFound(ILogger logger, string rootTypeName);

    /// <summary>Debug: root resolved, computed root contract keys.</summary>
    [MessageLogging(EventId = 11039, Level = LogLevel.Debug,
        Message = "FindFamilyDerivedTypes root resolved={rootName} contractKeyCount={contractKeyCount}")]
    public static partial IGenericMessage RootResolved(ILogger logger, string rootName, int contractKeyCount);

    /// <summary>Trace: starting per-project scan.</summary>
    [MessageLogging(EventId = 11040, Level = LogLevel.Trace,
        Message = "FindFamilyDerivedTypes scanning project={projectName}")]
    public static partial IGenericMessage ProjectScanStart(ILogger logger, string projectName);

    /// <summary>Trace: skipped a project because compilation was unavailable.</summary>
    [MessageLogging(EventId = 11041, Level = LogLevel.Trace,
        Message = "FindFamilyDerivedTypes project={projectName} skipped (no compilation)")]
    public static partial IGenericMessage ProjectSkippedNoCompilation(ILogger logger, string projectName);

    /// <summary>Trace: a candidate type was filtered out.</summary>
    [MessageLogging(EventId = 11042, Level = LogLevel.Trace,
        Message = "FindFamilyDerivedTypes type={typeName} skipped reason={reason}")]
    public static partial IGenericMessage TypeFilteredOut(ILogger logger, string typeName, string reason);

    /// <summary>Trace: found a derived type, recording it.</summary>
    [MessageLogging(EventId = 11043, Level = LogLevel.Trace,
        Message = "FindFamilyDerivedTypes match type={typeName} kind={kind} extraMemberCount={extraMemberCount}")]
    public static partial IGenericMessage DerivedMatchFound(ILogger logger, string typeName, string kind, int extraMemberCount);

    /// <summary>Trace: cancellation observed.</summary>
    [MessageLogging(EventId = 11044, Level = LogLevel.Trace,
        Message = "FindFamilyDerivedTypes cancellation requested")]
    public static partial IGenericMessage Cancelled(ILogger logger);

    /// <summary>Information: Translate completed.</summary>
    [MessageLogging(EventId = 11045, Level = LogLevel.Information,
        Message = "FindFamilyDerivedTypes.Translate success root={rootName} matchCount={matchCount}")]
    public static partial IGenericMessage TranslateSuccess(ILogger logger, string rootName, int matchCount);
}
