using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Services.Universes.Abstractions;

namespace Fdw.Services.Pipelines;

/// <summary>
/// A pipeline attached to a universe.
/// </summary>
/// <remarks>
/// Declared here rather than in the universes package because this package owns the pipeline.
/// A host that has not referenced pipelines cannot attach one to a universe.
/// </remarks>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(UniverseResourceKinds), "Pipeline")]
public sealed class PipelineResourceKind : UniverseResourceKindBase
{
    /// <summary>Initializes a new instance of the <see cref="PipelineResourceKind"/> class.</summary>
    /// <remarks>
    /// Why <c>canBeOwned: true</c>: a pipeline is a component of the project that built it, in the
    /// same sense as a sketched data set — it exists to move that project's data and has no life
    /// once the project is archived.
    /// </remarks>
    public PipelineResourceKind()
        : base("Pipeline", canBeOwned: true)
    {
    }
}
