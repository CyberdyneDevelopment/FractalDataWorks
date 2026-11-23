using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Descriptions.Abstractions;

/// <summary>An escalation policy's description: when the policy applies and why, in the words of whoever made it (FDW-7205). Read by whoever holds <c>escalation:read</c>, the permission the escalation alerts list asks. The policy is the thing described, so the type has no owner: a policy is removed with its own descriptions.</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(DescriptionTypes), OptionName)]
public sealed class EscalationPolicyDescriptionType : DescriptionTypeBase
{
    /// <summary>The type's name, which the escalation provider writes and reads its descriptions under.</summary>
    public const string OptionName = "EscalationPolicy";

    /// <summary>Initializes a new instance of the <see cref="EscalationPolicyDescriptionType"/> class.</summary>
    public EscalationPolicyDescriptionType()
        : base(OptionName, "escalation:read", hasOwner: false)
    {
    }
}
