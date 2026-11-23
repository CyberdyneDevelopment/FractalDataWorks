using Fdw.Collections.Attributes;

namespace Fdw.Services.Etl.Abstractions.AnswerRequests;

/// <summary>The call goes on by itself.</summary>
[TypeOption(typeof(AnswerPolicyOutcomes), "Automatic")]
public sealed class AutomaticAnswerPolicyOutcome : AnswerPolicyOutcomeBase
{
    /// <summary>Initializes a new instance of the <see cref="AutomaticAnswerPolicyOutcome"/> class.</summary>
    public AutomaticAnswerPolicyOutcome() : base("Automatic", "Automatic", "The policy lets the call go on with no request.", callGoesOn: true, refusesTheCall: false, keepsARequest: false)
    {
    }
}
