using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace Fdw.Roslyn.Commands.Logging;

/// <summary>
/// MessageLogging for <see cref="Workspace.Translators.WriteMigrationGuideTranslator"/>.
/// </summary>
[MessageLoggingTypeCode("RCMD")]
public static partial class WriteMigrationGuideTranslatorLog
{
    /// <summary>Trace: migration-guide write starting.</summary>
    [MessageLogging(EventId = 11181, Level = LogLevel.Trace,
        Message = "WriteMigrationGuideTranslator writing migration guide to '{outputPath}'")]
    public static partial IGenericMessage Writing(ILogger logger, string outputPath);

    /// <summary>Error: no session change ledger is available.</summary>
    /// <remarks>Mirrors <c>RoslynResultCodes.LedgerNotAvailable</c> (70000).</remarks>
    [MessageLogging(EventId = 70000, Level = LogLevel.Error,
        Message = "WriteMigrationGuideTranslator: no change ledger is available")]
    public static partial IGenericMessage LedgerNotAvailable(ILogger logger);

    /// <summary>Error: OutputPath was not supplied.</summary>
    /// <remarks>Mirrors <c>RoslynResultCodes.OutputPathRequired</c> (21023).</remarks>
    [MessageLogging(EventId = 21023, Level = LogLevel.Error,
        Message = "WriteMigrationGuideTranslator: OutputPath is required")]
    public static partial IGenericMessage OutputPathRequired(ILogger logger);

    /// <summary>Error: a relative OutputPath was given but the solution has no file path to resolve against.</summary>
    /// <remarks>Mirrors <c>RoslynResultCodes.RelativeOutputPathNeedsSolutionPath</c> (31026).</remarks>
    [MessageLogging(EventId = 31026, Level = LogLevel.Error,
        Message = "WriteMigrationGuideTranslator: relative path '{outputPath}' needs a solution path to resolve against")]
    public static partial IGenericMessage RelativeOutputPathNeedsSolutionPath(ILogger logger, string outputPath);

    /// <summary>Warning: the ledger's own write failed; its failure result is forwarded unchanged.</summary>
    [MessageLogging(EventId = 71101, Level = LogLevel.Warning,
        Message = "WriteMigrationGuideTranslator: writing migration guide to '{outputPath}' failed")]
    public static partial IGenericMessage WriteFailed(ILogger logger, string outputPath);

    /// <summary>Information: the migration guide was written.</summary>
    [MessageLogging(EventId = 11182, Level = LogLevel.Information,
        Message = "WriteMigrationGuideTranslator wrote migration guide to '{outputPath}' with {entryCount} entries")]
    public static partial IGenericMessage Written(ILogger logger, string outputPath, int entryCount);
}
