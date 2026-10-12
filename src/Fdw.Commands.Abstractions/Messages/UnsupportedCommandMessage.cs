using Fdw.Messages;

namespace Fdw.Commands.Abstractions.Messages;

/// <summary>
/// Message for when a command type is not supported.
/// </summary>
public sealed class UnsupportedCommandMessage : CommandMessage
{
    /// <summary>
    /// Gets the command type that is not supported.
    /// </summary>
    public string CommandType { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="UnsupportedCommandMessage"/> class.
    /// </summary>
    /// <param name="commandType">The unsupported command type.</param>
    public UnsupportedCommandMessage(string commandType)
        : base(1003, "UnsupportedCommand", MessageSeverity.Error,
               $"Command type '{commandType}' is not supported", "CMD_UNSUP")
    {
        CommandType = commandType;
    }
}