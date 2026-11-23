using Fdw.Collections.Attributes;

namespace Fdw.Services.Etl.Abstractions.AnswerRequests;

/// <summary>The call goes on with no request.</summary>
[TypeOption(typeof(AnswerPolicyRequirements), "Automatic")]
public sealed class AutomaticAnswerPolicyRequirement : AnswerPolicyRequirementBase
{
    /// <summary>Initializes a new instance of the <see cref="AutomaticAnswerPolicyRequirement"/> class.</summary>
    public AutomaticAnswerPolicyRequirement() : base("Automatic", "Automatic", "The call goes on with no request.", needsPermission: false, outcomeWhenCovered: "Automatic", outcomeWhenNot: "Automatic")
    {
    }
}
