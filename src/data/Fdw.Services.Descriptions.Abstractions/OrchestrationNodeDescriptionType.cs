using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Descriptions.Abstractions;

/// <summary>An orchestration node's description: what the node is for, in the words of whoever made it (FDW-7323). Read by whoever holds <c>projects:read</c>, the permission the project and node routes ask. The node is the thing described, so the type has no owner: a node is removed with its own descriptions.</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(DescriptionTypes), OptionName)]
public sealed class OrchestrationNodeDescriptionType : DescriptionTypeBase
{
    /// <summary>The type's name, which the orchestration node provider writes and reads its descriptions under.</summary>
    public const string OptionName = "OrchestrationNode";

    /// <summary>Initializes a new instance of the <see cref="OrchestrationNodeDescriptionType"/> class.</summary>
    public OrchestrationNodeDescriptionType()
        : base(OptionName, "projects:read", hasOwner: false)
    {
    }
}
