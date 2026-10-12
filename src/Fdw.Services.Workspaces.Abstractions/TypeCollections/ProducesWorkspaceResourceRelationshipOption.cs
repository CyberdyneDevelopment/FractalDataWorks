using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Workspaces.Abstractions;

/// <summary>The workspace generates this resource as an output.</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(WorkspaceResourceRelationships), "Produces")]
public sealed class ProducesWorkspaceResourceRelationshipOption : WorkspaceResourceRelationshipBase
{
    /// <summary>Initializes a new instance of the <see cref="ProducesWorkspaceResourceRelationshipOption"/> class.</summary>
    public ProducesWorkspaceResourceRelationshipOption() : base("Produces")
    {
    }
}
