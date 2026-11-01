using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace Fdw.Data.OData.Logging;

/// <summary>
/// Message logging for <see cref="Fdw.Data.OData.ODataCommandTranslatorBase"/> — the shared
/// OData $filter/$orderby/$select/paging query-string builders used by every REST translator.
/// </summary>
[MessageLoggingTypeCode("REST")]
public static partial class ODataCommandTranslatorBaseLog
{
    /// <summary>
    /// Logs the unrecoverable state where a filter expression tree contains a node that is
    /// neither <c>FilterCondition</c> nor <c>FilterGroup</c> — corrupt/unsupported filter input
    /// the OData builder cannot proceed past.
    /// </summary>
    [MessageLogging(
        EventId = 62000,
        Level = LogLevel.Critical,
        Message = "OData filter builder encountered an unknown filter node type '{nodeType}'")]
    public static partial IGenericMessage UnknownFilterNodeType(
        ILogger logger,
        string nodeType);
}
