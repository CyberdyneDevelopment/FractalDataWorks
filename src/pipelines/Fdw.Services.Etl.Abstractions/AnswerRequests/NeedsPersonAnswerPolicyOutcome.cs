using Fdw.Collections.Attributes;

namespace Fdw.Services.Etl.Abstractions.AnswerRequests;

/// <summary>The call needs the person.</summary>
[TypeOption(typeof(AnswerPolicyOutcomes), "NeedsPerson")]
public sealed class NeedsPersonAnswerPolicyOutcome : AnswerPolicyOutcomeBase
{
    /// <summary>Initializes a new instance of the <see cref="NeedsPersonAnswerPolicyOutcome"/> class.</summary>
    public NeedsPersonAnswerPolicyOutcome() : base("NeedsPerson", "Needs the person", "The call waits for the person to answer a request.", callGoesOn: false, refusesTheCall: false, keepsARequest: true)
    {
    }
}
