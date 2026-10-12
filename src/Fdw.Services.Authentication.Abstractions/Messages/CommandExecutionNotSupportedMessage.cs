using Fdw.Messages;
using Fdw.Messages.Attributes;
using Fdw.Services.Abstractions;

namespace Fdw.Services.Authentication.Abstractions.Messages;

/// <summary>
/// CurrentMessage indicating that command-based execution is not supported.
/// </summary>
[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
[Message("CommandExecutionNotSupported")]
[MessageOption(typeof(AuthenticationMessageCollectionBase))]
public sealed class CommandExecutionNotSupportedMessage : AuthenticationMessage, IServiceMessage
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CommandExecutionNotSupportedMessage"/> class.
    /// </summary>
    public CommandExecutionNotSupportedMessage()
        : base(2004, "CommandExecutionNotSupported", MessageSeverity.Error,
               "Authentication service does not support command-based execution. Use direct methods instead.", "AUTH_CMD_NOT_SUPPORTED")
    { }
}
