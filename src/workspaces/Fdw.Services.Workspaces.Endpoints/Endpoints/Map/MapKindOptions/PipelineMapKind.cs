using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Workspaces.Endpoints;

/// <summary>A pipeline.</summary>
[TypeOption(typeof(MapKindOptions), "Pipeline")]
[ExcludeFromCodeCoverage]
public sealed class PipelineMapKind : MapKindOptionBase
{
    /// <summary>Initializes a new instance of the <see cref="PipelineMapKind"/> class.</summary>
    public PipelineMapKind()
        : base("Pipeline")
    {
    }
}
