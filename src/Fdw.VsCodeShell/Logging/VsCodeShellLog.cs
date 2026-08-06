using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace Fdw.VsCodeShell.Logging;

/// <summary>
/// Message logging for the VS Code shell's command dispatch surface.
/// EventId ranges follow the result-code categories: 32xxx NotFound, 92xxx Internal.
/// </summary>
[MessageLoggingTypeCode("VSCODE")]
public static partial class VsCodeShellLog
{
    /// <summary>
    /// Logs a POST to /vscode/commands/{id} for a command no option declares.
    /// </summary>
    [MessageLogging(
        EventId = 32000,
        Level = LogLevel.Warning,
        Message = "No VS Code command '{commandId}' is declared by any registered option",
        TypeCode = new[] { 'V', 'S', 'C', 'O', 'D', 'E' })]
    public static partial IGenericMessage UnknownCommand(
        ILogger logger,
        string commandId);

    /// <summary>
    /// Logs a declared command whose handler could not be resolved from DI.
    /// </summary>
    /// <remarks>
    /// Should be unreachable: an option registers its own handler in the option's Register phase, so a
    /// declared command always has one. Reaching this means the collection was registered without
    /// Register(...) having run, which is a host wiring fault worth surfacing loudly.
    /// </remarks>
    [MessageLogging(
        EventId = 92000,
        Level = LogLevel.Error,
        Message = "VS Code command '{commandId}' is declared but no handler is registered for it — was VsCodeCommandTypes.Register called?",
        TypeCode = new[] { 'V', 'S', 'C', 'O', 'D', 'E' })]
    public static partial IGenericMessage HandlerNotRegistered(
        ILogger logger,
        string commandId);

    /// <summary>
    /// Logs a handler that returned a non-success result.
    /// </summary>
    [MessageLogging(
        EventId = 92001,
        Level = LogLevel.Error,
        Message = "VS Code command '{commandId}' handler returned a failure",
        TypeCode = new[] { 'V', 'S', 'C', 'O', 'D', 'E' })]
    public static partial IGenericMessage HandlerFailed(
        ILogger logger,
        string commandId);
}
