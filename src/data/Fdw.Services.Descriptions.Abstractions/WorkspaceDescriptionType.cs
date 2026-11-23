using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Descriptions.Abstractions;

/// <summary>A workspace's description: what the workspace is for, shown beside its name (FDW-6208). Read by whoever holds <c>workspaces:read</c>.</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(DescriptionTypes), OptionName)]
public sealed class WorkspaceDescriptionType : DescriptionTypeBase
{
    /// <summary>The type's name, which the workspace provider writes and reads its descriptions under.</summary>
    public const string OptionName = "Workspace";

    /// <summary>Initializes a new instance of the <see cref="WorkspaceDescriptionType"/> class.</summary>
    public WorkspaceDescriptionType()
        : base(OptionName, "workspaces:read", hasOwner: false)
    {
    }
}
