using Fdw.Messages;
using Fdw.MessageLogging;
using Microsoft.Extensions.Logging;

namespace Fdw.Roslyn.Commands.Search.Logging;

/// <summary>
/// MessageLogging methods for FindFamilyImplementationsTranslator.
/// EventId range: 9260-9289.
/// </summary>
[MessageLoggingTypeCode("SEARCH")]
public static partial class FindFamilyImplementationsTranslatorLog
{
    /// <summary>Info: Translate called.</summary>
    [MessageLogging(EventId = 11046, Level = LogLevel.Information,
        Message = "FindFamilyImplementations.Translate start root={rootTypeName} namespaceFilter={namespaceFilter} includeAbstract={includeAbstract}")]
    public static partial IGenericMessage TranslateStart(ILogger logger, string rootTypeName, string namespaceFilter, bool includeAbstract);

    /// <summary>Warning: RootTypeName parameter was empty.</summary>
    [MessageLogging(EventId = 21002, Level = LogLevel.Warning,
        Message = "FindFamilyImplementations validation failed: RootTypeName required")]
    public static partial IGenericMessage ValidationFailedRootRequired(ILogger logger);

    /// <summary>Warning: root not found.</summary>
    [MessageLogging(EventId = 31003, Level = LogLevel.Warning,
        Message = "FindFamilyImplementations root not found: {rootTypeName}")]
    public static partial IGenericMessage RootNotFound(ILogger logger, string rootTypeName);

    /// <summary>Debug: root resolved.</summary>
    [MessageLogging(EventId = 11047, Level = LogLevel.Debug,
        Message = "FindFamilyImplementations root resolved={rootName} contractKeyCount={contractKeyCount}")]
    public static partial IGenericMessage RootResolved(ILogger logger, string rootName, int contractKeyCount);

    /// <summary>Trace: per-project scan starting.</summary>
    [MessageLogging(EventId = 11048, Level = LogLevel.Trace,
        Message = "FindFamilyImplementations scanning project={projectName}")]
    public static partial IGenericMessage ProjectScanStart(ILogger logger, string projectName);

    /// <summary>Trace: project skipped (no compilation).</summary>
    [MessageLogging(EventId = 11049, Level = LogLevel.Trace,
        Message = "FindFamilyImplementations project={projectName} skipped (no compilation)")]
    public static partial IGenericMessage ProjectSkippedNoCompilation(ILogger logger, string projectName);

    /// <summary>Trace: type filtered out.</summary>
    [MessageLogging(EventId = 11050, Level = LogLevel.Trace,
        Message = "FindFamilyImplementations type={typeName} skipped reason={reason}")]
    public static partial IGenericMessage TypeFilteredOut(ILogger logger, string typeName, string reason);

    /// <summary>Trace: namespace filter rejected a candidate.</summary>
    [MessageLogging(EventId = 11051, Level = LogLevel.Trace,
        Message = "FindFamilyImplementations namespace-filter rejected type={typeName} namespace={ns}")]
    public static partial IGenericMessage NamespaceFilterRejected(ILogger logger, string typeName, string ns);

    /// <summary>Trace: implementation recorded.</summary>
    [MessageLogging(EventId = 11052, Level = LogLevel.Trace,
        Message = "FindFamilyImplementations match type={typeName} ns={ns} declaredMembers={declaredMembers} extraBeyondContract={extraBeyondContract}")]
    public static partial IGenericMessage ImplementationFound(ILogger logger, string typeName, string ns, int declaredMembers, int extraBeyondContract);

    /// <summary>Trace: cancellation observed.</summary>
    [MessageLogging(EventId = 11053, Level = LogLevel.Trace,
        Message = "FindFamilyImplementations cancellation requested")]
    public static partial IGenericMessage Cancelled(ILogger logger);

    /// <summary>Information: Translate completed.</summary>
    [MessageLogging(EventId = 11054, Level = LogLevel.Information,
        Message = "FindFamilyImplementations.Translate success root={rootName} matchCount={matchCount}")]
    public static partial IGenericMessage TranslateSuccess(ILogger logger, string rootName, int matchCount);
}
