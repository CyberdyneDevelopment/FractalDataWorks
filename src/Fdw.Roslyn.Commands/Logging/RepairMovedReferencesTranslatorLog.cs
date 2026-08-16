using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace Fdw.Roslyn.Commands.Logging;

/// <summary>
/// MessageLogging for <see cref="Workspace.Translators.RepairMovedReferencesTranslator"/>.
/// </summary>
[MessageLoggingTypeCode("RCMD")]
public static partial class RepairMovedReferencesTranslatorLog
{
    /// <summary>Trace: reference repair starting.</summary>
    [MessageLogging(EventId = 11173, Level = LogLevel.Trace,
        Message = "RepairMovedReferencesTranslator repairing references (scope='{scope}', dryRun={dryRun})")]
    public static partial IGenericMessage Repairing(ILogger logger, string scope, bool dryRun);

    /// <summary>Error: the command argument was null.</summary>
    /// <remarks>Mirrors <c>RoslynResultCodes.CommandCannotBeNull</c> (21000).</remarks>
    [MessageLogging(EventId = 21000, Level = LogLevel.Error,
        Message = "RepairMovedReferencesTranslator: command was null")]
    public static partial IGenericMessage CommandCannotBeNull(ILogger logger);

    /// <summary>Error: a relative output/guide/plan path was given but the solution has no file path to resolve against.</summary>
    /// <remarks>Mirrors <c>RoslynResultCodes.RelativeOutputPathNeedsSolutionPath</c> (31026).</remarks>
    [MessageLogging(EventId = 31026, Level = LogLevel.Error,
        Message = "RepairMovedReferencesTranslator: relative path '{outputPath}' needs a solution path to resolve against")]
    public static partial IGenericMessage RelativeOutputPathNeedsSolutionPath(ILogger logger, string outputPath);

    /// <summary>Error: the migration guide at the given path does not exist or has no usable content.</summary>
    /// <remarks>Mirrors <c>RoslynResultCodes.MigrationGuideNotUsable</c> (31028).</remarks>
    [MessageLogging(EventId = 31028, Level = LogLevel.Error,
        Message = "RepairMovedReferencesTranslator: migration guide '{guidePath}' is not usable — {problem}")]
    public static partial IGenericMessage MigrationGuideNotUsable(ILogger logger, string guidePath, string problem);

    /// <summary>Error: no session change ledger and no guide path were supplied.</summary>
    /// <remarks>Mirrors <c>RoslynResultCodes.LedgerNotAvailable</c> (70000).</remarks>
    [MessageLogging(EventId = 70000, Level = LogLevel.Error,
        Message = "RepairMovedReferencesTranslator: no change ledger is available")]
    public static partial IGenericMessage LedgerNotAvailable(ILogger logger);

    /// <summary>Error: no unresolved-reference errors were found in scope.</summary>
    /// <remarks>Mirrors <c>RoslynResultCodes.NoReferenceErrorsFound</c> (31025).</remarks>
    [MessageLogging(EventId = 31025, Level = LogLevel.Error,
        Message = "RepairMovedReferencesTranslator: no reference errors found (scope='{scope}')")]
    public static partial IGenericMessage NoReferenceErrorsFound(ILogger logger, string scope);

    /// <summary>Error: the repair plan file at the given path does not exist.</summary>
    /// <remarks>Mirrors <c>RoslynResultCodes.RepairPlanNotFound</c> (31027).</remarks>
    [MessageLogging(EventId = 31027, Level = LogLevel.Error,
        Message = "RepairMovedReferencesTranslator: repair plan not found at '{planPath}'")]
    public static partial IGenericMessage RepairPlanNotFound(ILogger logger, string planPath);

    /// <summary>Information: repair completed.</summary>
    [MessageLogging(EventId = 11174, Level = LogLevel.Information,
        Message = "RepairMovedReferencesTranslator repaired {repairedCount} of {errorsExamined} error(s); {unresolvedCount} unresolved (dryRun={dryRun})")]
    public static partial IGenericMessage Repaired(ILogger logger, int errorsExamined, int repairedCount, int unresolvedCount, bool dryRun);
}
