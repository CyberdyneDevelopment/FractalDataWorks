using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Fdw.Configuration;
using Fdw.Data;

namespace Fdw.Services.Etl.Transforms;

/// <summary>
/// One path of a Branch stage: a name, the condition that routes a record to it, and the stages a routed record passes through.
/// </summary>
/// <remarks>
/// FDW-1120 (pipeline loads item 11, phase 2). Ordered by <see cref="Ordinal"/> (counted from 1): a record takes the first path
/// whose condition is true. A cascade child of pipe.PipelineTransform (the Branch stage). The stages of the path are rows of
/// pipe.PipelineTransform too, owned by this path alone (their pipeline keys are null), so a stage tree needs no key from a
/// stage to its parent stage: each hop of the tree is stage, then path, then stage.
/// </remarks>
[ExcludeFromCodeCoverage]
[GenerateMapper]
[ManagedConfiguration(ServiceCategory = "Pipeline")]
public sealed partial class PipelineTransformPathConfiguration : IGenericConfiguration
{
    /// <summary>Gets or sets the business identifier of the path.</summary>
    public Guid Id { get; set; } = Guid.CreateVersion7();

    /// <summary>Gets or sets the path's name, unique within its Branch and free of '.'.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets or sets the condition a record must meet to take this path, in the pipeline's condition language.</summary>
    public string Condition { get; set; } = string.Empty;

    /// <summary>Gets or sets the path's position among the Branch's paths, counted from 1.</summary>
    public int Ordinal { get; set; }

    /// <summary>
    /// Gets or sets the stages of the path, run in <see cref="PipelineTransformConfiguration.ExecutionOrder"/> order on the
    /// records the path took. Cascade child (pipe.PipelineTransform); may be empty, which passes the records through.
    /// </summary>
    public IList<PipelineTransformConfiguration> Stages { get; set; } = [];
}
