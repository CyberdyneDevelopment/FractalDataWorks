using System;
using System.Collections.Generic;

namespace Fdw.Services.Etl.Abstractions;

/// <summary>
/// What a request states for a Compute stage beyond the members every stage has: the Built transformation it applies and
/// which field of the record feeds each of its inputs.
/// </summary>
/// <remarks>
/// FDW-1209. The stage reads the transformation by id; the wire may carry the name, and the API resolves it to the id when
/// the pipeline is saved. A request that states neither is refused.
/// </remarks>
public interface ITransformComputeSpec
{
    /// <summary>Gets the name of the Built transformation a Compute stage applies; null when the request states none. The API resolves it to an id when the pipeline is saved.</summary>
    string? ComputeTransformation { get; }

    /// <summary>Gets the id of the Built transformation a Compute stage applies; null when the request states none.</summary>
    Guid? ComputeTransformationId { get; }

    /// <summary>Gets which field of the record feeds each input of the transformation; empty when the request states none.</summary>
    IReadOnlyList<ComputeInputSpec> ComputeInputs { get; }

    /// <summary>Gets where each table input of the transformation gets its rows; empty for a transformation that reads one record at a time.</summary>
    IReadOnlyList<ComputeTableSpec> ComputeTables { get; }

    /// <summary>Gets where each table output of the transformation goes; empty when it has none.</summary>
    IReadOnlyList<ComputeOutputSpec> ComputeOutputs { get; }

    /// <summary>Gets the single values handed to inputs of the transformation; empty when it needs none.</summary>
    IReadOnlyList<ComputeScalarSpec> ComputeScalars { get; }

    /// <summary>Gets what the stage does with a row it cannot build (a <see cref="RowFailureActions"/> name); null when the request states none.</summary>
    string? ComputeOnRowFailure { get; }
}
