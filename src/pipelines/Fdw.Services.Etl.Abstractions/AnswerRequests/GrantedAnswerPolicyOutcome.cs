using Fdw.Collections.Attributes;

namespace Fdw.Services.Etl.Abstractions.AnswerRequests;

/// <summary>The call goes on under a stored grant.</summary>
[TypeOption(typeof(AnswerPolicyOutcomes), "Granted")]
public sealed class GrantedAnswerPolicyOutcome : AnswerPolicyOutcomeBase
{
    /// <summary>Initializes a new instance of the <see cref="GrantedAnswerPolicyOutcome"/> class.</summary>
    public GrantedAnswerPolicyOutcome() : base("Granted", "Granted", "A stored grant that has not ended allows the call.", callGoesOn: true, refusesTheCall: false, keepsARequest: false)
    {
    }
}
