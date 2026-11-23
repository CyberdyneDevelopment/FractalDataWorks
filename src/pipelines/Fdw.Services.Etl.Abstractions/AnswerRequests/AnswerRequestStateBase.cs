using Fdw.Collections;

namespace Fdw.Services.Etl.Abstractions.AnswerRequests;

/// <summary>Base class for the options of <see cref="AnswerRequestStates"/>, using the CRTP pattern.</summary>
public abstract class AnswerRequestStateBase : TypeOptionBase<AnswerRequestStateBase>, IAnswerRequestState
{
    /// <summary>Initializes a new instance of the <see cref="AnswerRequestStateBase"/> class.</summary>
    /// <param name="name">The option name.</param>
    /// <param name="displayName">The name shown to a person.</param>
    /// <param name="description">What the option means.</param>
    /// <param name="acceptsAnAnswer">Whether a request in this state may still be answered.</param>
    /// <param name="owesTheStart">Whether a request in this state has an answer whose continuation has not started.</param>
    /// <param name="isReadable">Whether the person is shown a request in this state.</param>
    protected AnswerRequestStateBase(string name, string displayName, string description, bool acceptsAnAnswer, bool owesTheStart, bool isReadable) : base(name, displayName, description, "AnswerRequestStates")
    {
        AcceptsAnAnswer = acceptsAnAnswer;
        OwesTheStart = owesTheStart;
        IsReadable = isReadable;
    }

    /// <inheritdoc />
    public bool AcceptsAnAnswer { get; }

    /// <inheritdoc />
    public bool OwesTheStart { get; }

    /// <inheritdoc />
    public bool IsReadable { get; }
}
