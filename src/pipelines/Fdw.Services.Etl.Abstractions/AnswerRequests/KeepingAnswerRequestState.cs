using Fdw.Collections.Attributes;

namespace Fdw.Services.Etl.Abstractions.AnswerRequests;

/// <summary>A request being kept: written, its parts not yet.</summary>
[TypeOption(typeof(AnswerRequestStates), "Keeping")]
public sealed class KeepingAnswerRequestState : AnswerRequestStateBase
{
    /// <summary>Initializes a new instance of the <see cref="KeepingAnswerRequestState"/> class.</summary>
    public KeepingAnswerRequestState() : base("Keeping", "Being kept", "The row is written and its parts are being written; nobody reads or answers it yet.", acceptsAnAnswer: false, owesTheStart: false, isReadable: false)
    {
    }
}
