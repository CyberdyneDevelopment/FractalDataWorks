using Fdw.Collections.Attributes;

namespace Fdw.Services.Etl.Abstractions.AnswerRequests;

/// <summary>A request whose answer was taken and whose continuation has not started.</summary>
[TypeOption(typeof(AnswerRequestStates), "Answered")]
public sealed class AnsweredAnswerRequestState : AnswerRequestStateBase
{
    /// <summary>Initializes a new instance of the <see cref="AnsweredAnswerRequestState"/> class.</summary>
    public AnsweredAnswerRequestState() : base("Answered", "Answered", "The answer is taken; the start of the continuation is owed.", acceptsAnAnswer: false, owesTheStart: true, isReadable: true)
    {
    }
}
