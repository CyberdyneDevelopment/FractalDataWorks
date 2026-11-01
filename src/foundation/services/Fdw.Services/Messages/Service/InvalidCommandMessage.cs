using System.Globalization;
using Fdw.Messages;
using Fdw.Messages.Attributes;
using Fdw.Services.Abstractions;

namespace Fdw.Services.Messages;

/// <summary>
/// CurrentMessage indicating that an invalid command was provided.
/// </summary>
[Message("InvalidCommand")]
public sealed class InvalidCommandMessage : ServiceMessage, IServiceMessage
{
    /// <summary>
    /// Initializes a new instance of the <see cref="InvalidCommandMessage"/> class.
    /// </summary>
    public InvalidCommandMessage()
        : base(1001, "InvalidCommand", MessageSeverity.Error,
               "Invalid command type: {0}", "INVALID_COMMAND")
    { }

    /// <summary>
    /// Initializes a new instance of the <see cref="InvalidCommandMessage"/> class with formatted message.
    /// The source generator will create ServiceMessages.InvalidCommand(commandType) method.
    /// </summary>
    /// <param name="commandType">The type of command that was invalid.</param>
    public InvalidCommandMessage(string commandType)
        : base(1001, "InvalidCommand", MessageSeverity.Error,
               string.Format(CultureInfo.InvariantCulture, "Invalid command type: {0}", commandType),
               "INVALID_COMMAND")
    { }

}
