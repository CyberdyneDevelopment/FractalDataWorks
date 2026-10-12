using System.Collections.Generic;
using Fdw.Services.Etl.Abstractions;

namespace Fdw.Services.Etl.Transforms;

/// <summary>
/// A stage kind that holds stages of its own, grouped in named paths (Branch).
/// </summary>
/// <remarks>
/// FDW-1120. The stage mapper uses this to map the stages of each path the way it maps the stages of a pipeline, without
/// knowing which kinds are composite: it asks the kind for the paths a request states and hands the mapped stages back.
/// A kind that implements it also implements <see cref="INestedStageShape"/>, which states how deep stages may nest.
/// </remarks>
public interface ICompositeTransformType
{
    /// <summary>Gets the paths a request states for a stage of this kind, each with the stage requests it holds.</summary>
    /// <param name="spec">The stage request.</param>
    /// <returns>The stated paths, in the order they were stated; empty when the request states none.</returns>
    IReadOnlyList<ITransformPathSpec> PathsOf(ITransformOperationSpec spec);

    /// <summary>Gives a path of a mapped stage the stages mapped from its request.</summary>
    /// <param name="stage">The mapped stage, whose paths <c>MapSpecToConfiguration</c> already filled in.</param>
    /// <param name="pathName">The name of the path the stages belong to.</param>
    /// <param name="stages">The mapped stages, in the order they were stated.</param>
    void Attach(PipelineTransformConfiguration stage, string pathName, List<PipelineTransformConfiguration> stages);
}
