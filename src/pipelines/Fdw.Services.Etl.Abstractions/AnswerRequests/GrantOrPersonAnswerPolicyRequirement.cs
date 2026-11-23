using Fdw.Collections.Attributes;

namespace Fdw.Services.Etl.Abstractions.AnswerRequests;

/// <summary>A stored grant covers the call when it allows the stated permission; otherwise the person answers.</summary>
[TypeOption(typeof(AnswerPolicyRequirements), "GrantOrPerson")]
public sealed class GrantOrPersonAnswerPolicyRequirement : AnswerPolicyRequirementBase
{
    /// <summary>Initializes a new instance of the <see cref="GrantOrPersonAnswerPolicyRequirement"/> class.</summary>
    public GrantOrPersonAnswerPolicyRequirement() : base("GrantOrPerson", "Grant or the person", "A stored grant that allows the stated permission covers the call; otherwise the person answers.", needsPermission: true, outcomeWhenCovered: "Granted", outcomeWhenNot: "NeedsPerson")
    {
    }
}
