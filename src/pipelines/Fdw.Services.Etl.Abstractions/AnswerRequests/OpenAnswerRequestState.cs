using Fdw.Collections.Attributes;

namespace Fdw.Services.Etl.Abstractions.AnswerRequests;

/// <summary>A request the person may answer.</summary>
[TypeOption(typeof(AnswerRequestStates), "Open")]
public sealed class OpenAnswerRequestState : AnswerRequestStateBase
{
    /// <summary>Initializes a new instance of the <see cref="OpenAnswerRequestState"/> class.</summary>
    public OpenAnswerRequestState() : base("Open", "Open", "The person may answer until it expires.", acceptsAnAnswer: true, owesTheStart: false, isReadable: true)
    {
    }
}
