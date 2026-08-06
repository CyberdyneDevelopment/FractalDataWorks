using Fdw.Messages;
using Fdw.Messages.Attributes;
using Fdw.Services.Abstractions;

namespace Fdw.Services.SecretManagers.Abstractions.Messages;

/// <summary>
/// CurrentMessage indicating that the command was null.
/// </summary>
// Why: pure message DTO; ctor only forwards literal id/severity/text to the base template, no logic
[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
[Message("CommandNull")]
[MessageOption(typeof(SecretManagerMessageCollectionBase))]
public sealed class CommandNullMessage : SecretManagerMessage, IServiceMessage
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CommandNullMessage"/> class.
    /// </summary>
    public CommandNullMessage()
        : base(1001, "CommandNull", MessageSeverity.Error,
               "Command cannot be null.", "SM_CMD_NULL")
    { }
}
