using Fdw.Collections;

namespace Fdw.Services.Etl.Abstractions.AnswerRequests;

/// <summary>Base class for the options of <see cref="AnswerRequestKinds"/>, using the CRTP pattern.</summary>
public abstract class AnswerRequestKindBase : TypeOptionBase<AnswerRequestKindBase>, IAnswerRequestKind
{
    /// <summary>Initializes a new instance of the <see cref="AnswerRequestKindBase"/> class.</summary>
    /// <param name="name">The option name.</param>
    /// <param name="displayName">The name shown to a person.</param>
    /// <param name="description">What the option means.</param>
    /// <param name="answerMustBeAnOption">Whether the answer must be one of the options the request shows.</param>
    protected AnswerRequestKindBase(string name, string displayName, string description, bool answerMustBeAnOption) : base(name, displayName, description, "AnswerRequestKinds")
    {
        AnswerMustBeAnOption = answerMustBeAnOption;
    }

    /// <inheritdoc />
    public bool AnswerMustBeAnOption { get; }
}
