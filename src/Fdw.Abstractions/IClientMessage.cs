namespace Fdw.Messages;

/// <summary>
/// A message that carries, beside its log text, a text written for the caller: a refusal of the caller's own input, or
/// of a state the caller can act on, said so they learn why.
/// </summary>
/// <remarks>
/// A <c>[MessageLogging]</c> method declares it with a <c>ClientMessage</c> template, and the generator builds its message
/// as one. A web host's status mapper answers a refused request with <see cref="ClientText"/> as the reason; any other
/// message is written for the log and the caller is given only its category's generic text.
/// </remarks>
public interface IClientMessage : IGenericMessage
{
    /// <summary>Gets the text written for the caller: the reason they were refused, in their terms.</summary>
    string ClientText { get; }
}
