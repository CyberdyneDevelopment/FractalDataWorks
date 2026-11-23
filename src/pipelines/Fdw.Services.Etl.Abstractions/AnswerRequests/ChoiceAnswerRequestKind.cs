using Fdw.Collections.Attributes;

namespace Fdw.Services.Etl.Abstractions.AnswerRequests;

/// <summary>A question the person answers with one of the options the request shows.</summary>
[TypeOption(typeof(AnswerRequestKinds), "Choice")]
public sealed class ChoiceAnswerRequestKind : AnswerRequestKindBase
{
    /// <summary>Initializes a new instance of the <see cref="ChoiceAnswerRequestKind"/> class.</summary>
    public ChoiceAnswerRequestKind() : base("Choice", "Choice", "The person answers with one of the options the request shows.", answerMustBeAnOption: true)
    {
    }
}
