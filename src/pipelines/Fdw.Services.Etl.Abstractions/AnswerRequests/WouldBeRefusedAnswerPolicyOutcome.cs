using Fdw.Collections.Attributes;

namespace Fdw.Services.Etl.Abstractions.AnswerRequests;

/// <summary>The call is refused.</summary>
[TypeOption(typeof(AnswerPolicyOutcomes), "WouldBeRefused")]
public sealed class WouldBeRefusedAnswerPolicyOutcome : AnswerPolicyOutcomeBase
{
    /// <summary>Initializes a new instance of the <see cref="WouldBeRefusedAnswerPolicyOutcome"/> class.</summary>
    public WouldBeRefusedAnswerPolicyOutcome() : base("WouldBeRefused", "Would be refused", "The policy refuses the call.", callGoesOn: false, refusesTheCall: true, keepsARequest: false)
    {
    }
}
