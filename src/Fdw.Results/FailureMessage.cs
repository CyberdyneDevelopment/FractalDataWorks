using Fdw.Messages;

namespace Fdw.Results;

/// <summary>
/// The message of a failed result that says why it failed: its code, the number in that code, and the message itself (FDW-3302).
/// </summary>
public sealed class FailureMessage
{
    /// <summary>Initializes a new instance of the <see cref="FailureMessage"/> class.</summary>
    /// <param name="code">The message's code, in the generator's "PREFIX-NNNNN" form.</param>
    /// <param name="number">The number in the code; its failure category is the number divided by 10000.</param>
    /// <param name="message">The message.</param>
    public FailureMessage(string code, int number, IGenericMessage message)
    {
        Code = code;
        Number = number;
        Message = message;
    }

    /// <summary>Gets the message's code, in the generator's "PREFIX-NNNNN" form.</summary>
    public string Code { get; }

    /// <summary>Gets the number in the code; its failure category is the number divided by 10000.</summary>
    public int Number { get; }

    /// <summary>Gets the message.</summary>
    public IGenericMessage Message { get; }
}
