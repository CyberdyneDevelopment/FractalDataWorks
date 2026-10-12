using Fdw.Messages;
using Fdw.Messages.Attributes;
using Fdw.Services.Abstractions;

namespace Fdw.Services.Messages;

/// <summary>
/// CurrentMessage indicating that an invalid command type was provided.
/// </summary>
[Message("InvalidCommandType")]
public sealed class InvalidCommandTypeMessage : ServiceMessage, IServiceMessage
{
    /// <summary>
    /// Initializes a new instance of the <see cref="InvalidCommandTypeMessage"/> class.
    /// </summary>
    public InvalidCommandTypeMessage()
        : base(1001, "InvalidCommandType", MessageSeverity.Error,
               "Invalid command type provided", "INVALID_COMMAND_TYPE")
    { }

    /// <summary>
    /// Initializes a new instance with the invalid command type name.
    /// </summary>
    /// <param name="commandType">The name of the invalid command type.</param>
    public InvalidCommandTypeMessage(string commandType)
        : base(1001, "InvalidCommandType", MessageSeverity.Error,
               $"Invalid command type: {commandType}", "INVALID_COMMAND_TYPE")
    { }
}