using Fdw.Collections.Attributes;

namespace Fdw.Services.Etl.Abstractions.AnswerRequests;

/// <summary>A request that passed its end with no answer.</summary>
[TypeOption(typeof(AnswerRequestStates), "Expired")]
public sealed class ExpiredAnswerRequestState : AnswerRequestStateBase
{
    /// <summary>Initializes a new instance of the <see cref="ExpiredAnswerRequestState"/> class.</summary>
    public ExpiredAnswerRequestState() : base("Expired", "Expired", "The time the policy allowed passed with no answer; it starts nothing.", acceptsAnAnswer: false, owesTheStart: false, isReadable: true)
    {
    }
}
