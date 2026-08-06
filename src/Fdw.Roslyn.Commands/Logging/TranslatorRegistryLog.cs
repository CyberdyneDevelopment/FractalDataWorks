using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace Fdw.Roslyn.Commands.Logging;

/// <summary>
/// MessageLogging for translator registration — the point where a translator becomes reachable AND
/// gains a real logger.
/// </summary>
/// <remarks>
/// Registration is the choke point every executable translator passes through, so it is also the only
/// place that can say, per translator, whether logging was actually wired. Everything here answers a
/// question that previously required reading source: which translators exist, which command type each
/// claims, which one won a contested key, and which ones can never log at all.
/// </remarks>
[MessageLoggingTypeCode("REGISTRY")]
public static partial class TranslatorRegistryLog
{
    /// <summary>Trace: a translator was registered and given a logger.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="translator">The translator's CLR type name.</param>
    /// <param name="commandType">The command type it is keyed under.</param>
    /// <returns>The structured message.</returns>
    /// <remarks>
    /// Trace because a full catalogue is ~100 lines at startup. At Trace, "is my new command registered"
    /// is one grep instead of a debugger.
    /// </remarks>
    [MessageLogging(EventId = 12520, Level = LogLevel.Trace,
        Message = "Registered translator {translator} for command type {commandType}")]
    public static partial IGenericMessage TranslatorRegistered(ILogger logger, string translator, string commandType);

    /// <summary>Warning: a second translator claimed a command type already registered.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="commandType">The contested command type.</param>
    /// <param name="previous">The translator being displaced.</param>
    /// <param name="replacement">The translator taking the key.</param>
    /// <returns>The structured message.</returns>
    /// <remarks>
    /// Last writer wins, silently, and the loser simply never runs again. When a command starts
    /// behaving like a different command, this line is the answer; without it the only evidence is that
    /// the catalogue count is smaller than the number of translator types.
    /// </remarks>
    [MessageLogging(EventId = 12521, Level = LogLevel.Warning,
        Message = "Translator for {commandType} replaced: {previous} -> {replacement}")]
    public static partial IGenericMessage TranslatorReplaced(ILogger logger, string commandType, string previous, string replacement);

    /// <summary>Warning: a registered translator cannot be given a logger.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="translator">The translator's CLR type name.</param>
    /// <returns>The structured message.</returns>
    /// <remarks>
    /// Category 6 — this is a wiring fault, not a request fault. A translator that does not derive from
    /// <c>RoslynCommandTranslatorBase</c> has no logger surface to set, so it will run silently forever.
    /// That is legal (the interface is what the registry requires) but it is worth knowing about,
    /// because "that command produces no logs" otherwise looks identical to "logging is broken".
    /// </remarks>
    [MessageLogging(EventId = 61040, Level = LogLevel.Warning,
        Message = "Translator {translator} does not derive from RoslynCommandTranslatorBase, so it cannot be given a logger and will run silently")]
    public static partial IGenericMessage TranslatorCannotReceiveLogger(ILogger logger, string translator);
}
