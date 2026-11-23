using Fdw.Collections.Attributes;

namespace Fdw.Services.Etl.Abstractions.AnswerRequests;

/// <summary>The call is refused.</summary>
[TypeOption(typeof(AnswerPolicyRequirements), "Refused")]
public sealed class RefusedAnswerPolicyRequirement : AnswerPolicyRequirementBase
{
    /// <summary>Initializes a new instance of the <see cref="RefusedAnswerPolicyRequirement"/> class.</summary>
    public RefusedAnswerPolicyRequirement() : base("Refused", "Refused", "The call is refused.", needsPermission: false, outcomeWhenCovered: "WouldBeRefused", outcomeWhenNot: "WouldBeRefused")
    {
    }
}
