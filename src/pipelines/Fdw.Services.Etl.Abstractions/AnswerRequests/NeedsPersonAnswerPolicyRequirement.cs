using Fdw.Collections.Attributes;

namespace Fdw.Services.Etl.Abstractions.AnswerRequests;

/// <summary>The person always answers.</summary>
[TypeOption(typeof(AnswerPolicyRequirements), "NeedsPerson")]
public sealed class NeedsPersonAnswerPolicyRequirement : AnswerPolicyRequirementBase
{
    /// <summary>Initializes a new instance of the <see cref="NeedsPersonAnswerPolicyRequirement"/> class.</summary>
    public NeedsPersonAnswerPolicyRequirement() : base("NeedsPerson", "Needs the person", "The person always answers; no grant covers the call.", needsPermission: false, outcomeWhenCovered: "NeedsPerson", outcomeWhenNot: "NeedsPerson")
    {
    }
}
