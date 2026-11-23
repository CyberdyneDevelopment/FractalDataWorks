using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Etl.Abstractions;

/// <summary>The output's rows replace the stage's own rows.</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(ComputeOutputRoutes), "Replace")]
public sealed class ReplaceComputeOutputRoute : ComputeOutputRouteBase
{
    /// <summary>Initializes a new instance of the <see cref="ReplaceComputeOutputRoute"/> class.</summary>
    public ReplaceComputeOutputRoute()
        : base("Replace", false, "a Replace route has no name")
    {
    }
}
