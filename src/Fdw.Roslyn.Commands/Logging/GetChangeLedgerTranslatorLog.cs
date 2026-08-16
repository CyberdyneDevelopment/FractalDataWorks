using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace Fdw.Roslyn.Commands.Logging;

/// <summary>
/// MessageLogging for <see cref="Workspace.Translators.GetChangeLedgerTranslator"/>.
/// </summary>
[MessageLoggingTypeCode("RCMD")]
public static partial class GetChangeLedgerTranslatorLog
{
    /// <summary>Trace: change-ledger retrieval starting.</summary>
    [MessageLogging(EventId = 11169, Level = LogLevel.Trace,
        Message = "GetChangeLedgerTranslator retrieving the change ledger")]
    public static partial IGenericMessage Getting(ILogger logger);

    /// <summary>Error: the session has no change ledger available.</summary>
    /// <remarks>Mirrors <c>RoslynResultCodes.LedgerNotAvailable</c> (70000).</remarks>
    [MessageLogging(EventId = 70000, Level = LogLevel.Error,
        Message = "GetChangeLedgerTranslator: no change ledger is available")]
    public static partial IGenericMessage LedgerNotAvailable(ILogger logger);

    /// <summary>Information: retrieval completed.</summary>
    [MessageLogging(EventId = 11170, Level = LogLevel.Information,
        Message = "GetChangeLedgerTranslator retrieved {entryCount} ledger entry/entries")]
    public static partial IGenericMessage Retrieved(ILogger logger, int entryCount);
}
