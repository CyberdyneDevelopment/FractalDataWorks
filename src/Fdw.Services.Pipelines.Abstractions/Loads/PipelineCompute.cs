using System;
using System.Collections.Generic;

namespace Fdw.Services.Pipelines.Clients.Abstractions;

/// <summary>
/// What a Compute stage applies (its <c>compute</c>): the Built transformation, and which field of the record feeds each of
/// its inputs. The stage adds the transformation's kept results to each record as columns.
/// </summary>
public sealed class PipelineCompute
{
    /// <summary>Gets or sets the name of the Built transformation to apply. State this or <see cref="TransformationId"/>, one of the two.</summary>
    public string? Transformation { get; set; }

    /// <summary>Gets or sets the id of the Built transformation to apply. State this or <see cref="Transformation"/>, one of the two.</summary>
    public Guid? TransformationId { get; set; }

    /// <summary>Gets or sets which field of the record feeds each input the transformation declares; every declared input needs one.</summary>
    public IList<PipelineComputeInput> Inputs { get; set; } = [];

    /// <summary>Gets or sets where each table input of a transformation that reads and writes tables gets its rows; every declared table needs one.</summary>
    public IList<PipelineComputeTable> Tables { get; set; } = [];

    /// <summary>Gets or sets where each table output of such a transformation goes; every declared output needs one.</summary>
    public IList<PipelineComputeOutput> Outputs { get; set; } = [];

    /// <summary>Gets or sets the single values handed to the inputs of such a transformation; every declared input needs one.</summary>
    public IList<PipelineComputeScalar> Scalars { get; set; } = [];

    /// <summary>
    /// Gets or sets what the stage does with a row it cannot build: Fail, Skip or Reject. A stage that reads and writes tables
    /// must state it; there is no default.
    /// </summary>
    public string? OnRowFailure { get; set; }
}
