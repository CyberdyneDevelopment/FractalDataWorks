using Fdw.Collections.Attributes;

namespace Fdw.Services.Etl.Abstractions.AnswerRequests;

/// <summary>A question the person answers in words.</summary>
[TypeOption(typeof(AnswerRequestKinds), "Question")]
public sealed class QuestionAnswerRequestKind : AnswerRequestKindBase
{
    /// <summary>Initializes a new instance of the <see cref="QuestionAnswerRequestKind"/> class.</summary>
    public QuestionAnswerRequestKind() : base("Question", "Question", "The person answers in words; any text that is not blank is an answer.", answerMustBeAnOption: false)
    {
    }
}
