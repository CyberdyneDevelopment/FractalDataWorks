using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace Fdw.Data.Abstractions.Logging;

/// <summary>
/// MessageLogging for <see cref="DefaultDataMapper{TSource, TTarget}"/> — the fallback mapper used
/// when no explicit mapper is registered for a source/target converter pair.
/// </summary>
[MessageLoggingTypeCode("MAPPER")]
public static partial class DefaultDataMapperLog
{
    /// <summary>
    /// Logs (at Debug) that no explicit mapper was registered for the pair, so the default,
    /// unoptimized CLR bridge is being used — a branch decision that changes mapping cost.
    /// </summary>
    [MessageLogging(EventId = 11004, Level = LogLevel.Debug,
        Message = "[DefaultDataMapper] No explicit mapper registered for '{sourceConverter}' -> '{targetConverter}'; using default CLR bridge")]
    public static partial IGenericMessage UsingDefaultMapper(ILogger logger, string sourceConverter, string targetConverter);
}
