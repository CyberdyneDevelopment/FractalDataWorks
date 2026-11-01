using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace Fdw.Roslyn.Commands.Logging;

/// <summary>
/// MessageLogging for <see cref="Workspace.Translators.ClearChangeLedgerTranslator"/>.
/// </summary>
[MessageLoggingTypeCode("RCMD")]
public static partial class ClearChangeLedgerTranslatorLog
{
    /// <summary>Trace: reporting the ledger clear about to be performed by the handler.</summary>
    [MessageLogging(EventId = 11159, Level = LogLevel.Trace,
        Message = "ClearChangeLedgerTranslator reporting ledger clear (reason='{reason}')")]
    public static partial IGenericMessage Clearing(ILogger logger, string reason);
}
