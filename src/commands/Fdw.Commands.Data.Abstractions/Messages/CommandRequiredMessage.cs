using Fdw.Messages;
using Fdw.Messages.Attributes;

namespace Fdw.Commands.Data.Abstractions;

/// <summary>
/// Message indicating that a command is required.
/// </summary>
[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
[Message("CommandRequired")]
public sealed class CommandRequiredMessage : DataCommandMessage
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CommandRequiredMessage"/> class.
    /// </summary>
    public CommandRequiredMessage()
        : base(
            id: 1,
            name: "CommandRequired",
            severity: MessageSeverity.Error,
            message: "Command is required",
            code: "DATACMD_001")
    {
    }
}
