using Fdw.Messages;
using Fdw.Messages.Attributes;

namespace Fdw.Services.Connections.Abstractions.Messages;

/// <summary>
/// Message indicating that command execution failed.
/// </summary>
// Why: pure message DTO; ctor only forwards literal id/severity/text to the base template, no logic
[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
[Message("ExecutionFailed")]
[MessageOption(typeof(ConnectionMessageCollectionBase))]
public sealed class ExecutionFailedMessage : ConnectionMessage
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ExecutionFailedMessage"/> class.
    /// </summary>
    public ExecutionFailedMessage()
        : base(
            id: 3012,
            name: "ExecutionFailed",
            severity: MessageSeverity.Error,
            message: "Command execution failed",
            code: "CONN_EXECUTION_FAILED")
    {
    }
}
