namespace Fdw.Messages;

/// <summary>
/// A message that carries a text written for the caller beside its log text (<see cref="IClientMessage"/>): what a
/// <c>[MessageLogging]</c> method with a <c>ClientMessage</c> template returns.
/// </summary>
public class ClientMessage : GenericMessage, IClientMessage
{
    /// <summary>Initializes a new instance of the <see cref="ClientMessage"/> class.</summary>
    /// <param name="severity">The message severity.</param>
    /// <param name="message">The message text, written for the log.</param>
    /// <param name="code">The message code.</param>
    /// <param name="source">The message source.</param>
    /// <param name="clientText">The text written for the caller.</param>
    public ClientMessage(MessageSeverity severity, string message, string? code, string? source, string clientText)
        : base(severity, message, code, source)
    {
        ClientText = clientText;
    }

    /// <inheritdoc />
    public string ClientText { get; }
}
