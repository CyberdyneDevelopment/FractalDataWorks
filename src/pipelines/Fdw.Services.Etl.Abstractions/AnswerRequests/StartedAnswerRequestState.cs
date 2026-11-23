using Fdw.Collections.Attributes;

namespace Fdw.Services.Etl.Abstractions.AnswerRequests;

/// <summary>A request whose continuation was started.</summary>
[TypeOption(typeof(AnswerRequestStates), "Started")]
public sealed class StartedAnswerRequestState : AnswerRequestStateBase
{
    /// <summary>Initializes a new instance of the <see cref="StartedAnswerRequestState"/> class.</summary>
    public StartedAnswerRequestState() : base("Started", "Started", "The continuation was started from the answer.", acceptsAnAnswer: false, owesTheStart: false, isReadable: true)
    {
    }
}
