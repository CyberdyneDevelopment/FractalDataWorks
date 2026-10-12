using Fdw.Messages;
using Fdw.MessageLogging;
using Microsoft.Extensions.Logging;

namespace Fdw.Roslyn.Commands.Search.Logging;

/// <summary>
/// MessageLogging methods for AnalyzeFamilyDriftTranslator.
/// EventId range: 9300-9349.
/// </summary>
[MessageLoggingTypeCode("SEARCH")]
public static partial class AnalyzeFamilyDriftTranslatorLog
{
    /// <summary>Info: Translate called.</summary>
    [MessageLogging(EventId = 11000, Level = LogLevel.Information,
        Message = "AnalyzeFamilyDrift.Translate start root={rootTypeName} namespaceFilter={namespaceFilter} includeExtensionMethods={includeExtensionMethods}")]
    public static partial IGenericMessage TranslateStart(ILogger logger, string rootTypeName, string namespaceFilter, bool includeExtensionMethods);

    /// <summary>Warning: RootTypeName parameter was empty.</summary>
    [MessageLogging(EventId = 21000, Level = LogLevel.Warning,
        Message = "AnalyzeFamilyDrift validation failed: RootTypeName required")]
    public static partial IGenericMessage ValidationFailedRootRequired(ILogger logger);

    /// <summary>Warning: root not found.</summary>
    [MessageLogging(EventId = 31000, Level = LogLevel.Warning,
        Message = "AnalyzeFamilyDrift root not found: {rootTypeName}")]
    public static partial IGenericMessage RootNotFound(ILogger logger, string rootTypeName);

    /// <summary>Debug: root resolved.</summary>
    [MessageLogging(EventId = 11001, Level = LogLevel.Debug,
        Message = "AnalyzeFamilyDrift root resolved={rootName}")]
    public static partial IGenericMessage RootResolved(ILogger logger, string rootName);

    /// <summary>Trace: starting CollectImplementations.</summary>
    [MessageLogging(EventId = 11002, Level = LogLevel.Trace,
        Message = "AnalyzeFamilyDrift CollectImplementations start namespaceFilter={namespaceFilter}")]
    public static partial IGenericMessage CollectImplStart(ILogger logger, string namespaceFilter);

    /// <summary>Trace: per-project scan during CollectImplementations.</summary>
    [MessageLogging(EventId = 11003, Level = LogLevel.Trace,
        Message = "AnalyzeFamilyDrift CollectImplementations project={projectName}")]
    public static partial IGenericMessage CollectImplProject(ILogger logger, string projectName);

    /// <summary>Trace: a candidate was filtered out during impl collection.</summary>
    [MessageLogging(EventId = 11004, Level = LogLevel.Trace,
        Message = "AnalyzeFamilyDrift CollectImplementations type={typeName} skipped reason={reason}")]
    public static partial IGenericMessage CollectImplSkipped(ILogger logger, string typeName, string reason);

    /// <summary>Trace: implementation accepted.</summary>
    [MessageLogging(EventId = 11005, Level = LogLevel.Trace,
        Message = "AnalyzeFamilyDrift CollectImplementations accepted type={typeName} ns={ns}")]
    public static partial IGenericMessage CollectImplAccepted(ILogger logger, string typeName, string ns);

    /// <summary>Debug: CollectImplementations finished.</summary>
    [MessageLogging(EventId = 11006, Level = LogLevel.Debug,
        Message = "AnalyzeFamilyDrift CollectImplementations done implementationCount={implementationCount}")]
    public static partial IGenericMessage CollectImplDone(ILogger logger, int implementationCount);

    /// <summary>Trace: starting ComputeDrift.</summary>
    [MessageLogging(EventId = 11007, Level = LogLevel.Trace,
        Message = "AnalyzeFamilyDrift ComputeDrift start implementationCount={implementationCount}")]
    public static partial IGenericMessage ComputeDriftStart(ILogger logger, int implementationCount);

    /// <summary>Trace: a member was added to the presence map.</summary>
    [MessageLogging(EventId = 11008, Level = LogLevel.Trace,
        Message = "AnalyzeFamilyDrift presence add memberKey={memberKey} impl={implName}")]
    public static partial IGenericMessage MemberPresenceAdded(ILogger logger, string memberKey, string implName);

    /// <summary>Trace: a member was bucketed.</summary>
    [MessageLogging(EventId = 11009, Level = LogLevel.Trace,
        Message = "AnalyzeFamilyDrift bucket member={memberName} bucket={bucket} presentCount={presentCount} totalCount={totalCount}")]
    public static partial IGenericMessage MemberBucketed(ILogger logger, string memberName, string bucket, int presentCount, int totalCount);

    /// <summary>Debug: ComputeDrift finished.</summary>
    [MessageLogging(EventId = 11010, Level = LogLevel.Debug,
        Message = "AnalyzeFamilyDrift ComputeDrift done driftMemberCount={driftMemberCount}")]
    public static partial IGenericMessage ComputeDriftDone(ILogger logger, int driftMemberCount);

    /// <summary>Trace: starting CollectExtensionMethods.</summary>
    [MessageLogging(EventId = 11011, Level = LogLevel.Trace,
        Message = "AnalyzeFamilyDrift CollectExtensionMethods start")]
    public static partial IGenericMessage CollectExtStart(ILogger logger);

    /// <summary>Trace: per-project scan during CollectExtensionMethods.</summary>
    [MessageLogging(EventId = 11012, Level = LogLevel.Trace,
        Message = "AnalyzeFamilyDrift CollectExtensionMethods project={projectName}")]
    public static partial IGenericMessage CollectExtProject(ILogger logger, string projectName);

    /// <summary>Trace: extension method skipped.</summary>
    [MessageLogging(EventId = 11013, Level = LogLevel.Trace,
        Message = "AnalyzeFamilyDrift CollectExtensionMethods skip method={methodName} reason={reason}")]
    public static partial IGenericMessage CollectExtSkipped(ILogger logger, string methodName, string reason);

    /// <summary>Trace: extension method accepted.</summary>
    [MessageLogging(EventId = 11014, Level = LogLevel.Trace,
        Message = "AnalyzeFamilyDrift CollectExtensionMethods accepted method={methodName} owningClass={owningClass}")]
    public static partial IGenericMessage CollectExtAccepted(ILogger logger, string methodName, string owningClass);

    /// <summary>Debug: CollectExtensionMethods finished.</summary>
    [MessageLogging(EventId = 11015, Level = LogLevel.Debug,
        Message = "AnalyzeFamilyDrift CollectExtensionMethods done extensionMethodCount={extensionMethodCount}")]
    public static partial IGenericMessage CollectExtDone(ILogger logger, int extensionMethodCount);

    /// <summary>Trace: cancellation observed.</summary>
    [MessageLogging(EventId = 11016, Level = LogLevel.Trace,
        Message = "AnalyzeFamilyDrift cancellation requested")]
    public static partial IGenericMessage Cancelled(ILogger logger);

    /// <summary>Information: Translate completed.</summary>
    [MessageLogging(EventId = 11017, Level = LogLevel.Information,
        Message = "AnalyzeFamilyDrift.Translate success root={rootName} implementationCount={implementationCount} driftMemberCount={driftMemberCount} extensionMethodCount={extensionMethodCount}")]
    public static partial IGenericMessage TranslateSuccess(ILogger logger, string rootName, int implementationCount, int driftMemberCount, int extensionMethodCount);
}
