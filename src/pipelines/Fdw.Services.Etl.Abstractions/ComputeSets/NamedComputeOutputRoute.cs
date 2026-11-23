using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Etl.Abstractions;

/// <summary>The output's rows are kept under a name.</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(ComputeOutputRoutes), "Named")]
public sealed class NamedComputeOutputRoute : ComputeOutputRouteBase
{
    /// <summary>Initializes a new instance of the <see cref="NamedComputeOutputRoute"/> class.</summary>
    public NamedComputeOutputRoute()
        : base("Named", true, "a Named route states the name it keeps the rows under")
    {
    }
}
