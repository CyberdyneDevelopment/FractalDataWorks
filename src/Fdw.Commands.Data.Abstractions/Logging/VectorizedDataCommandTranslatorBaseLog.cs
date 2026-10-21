using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace Fdw.Commands.Data.Abstractions.Logging;

/// <summary>
/// MessageLogging for <see cref="VectorizedDataCommandTranslatorBase{TCommand}"/>.
/// </summary>
[MessageLoggingTypeCode("DATAABSTRACTIONS")]
public static partial class VectorizedDataCommandTranslatorBaseLog
{
    // Why Trace: once per translator, as its collection is built at start.
    /// <summary>A vectorized translator is being made.</summary>
    [MessageLogging(EventId = 11006, Level = LogLevel.Trace,
        Message = "[VectorizedDataCommandTranslatorBase] Initializing vectorized translator '{name}'")]
    public static partial IGenericMessage TranslatorInitializing(ILogger logger, string name);
}
