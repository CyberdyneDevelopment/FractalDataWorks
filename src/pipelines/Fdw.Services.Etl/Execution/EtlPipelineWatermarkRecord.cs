using System;
using System.Diagnostics.CodeAnalysis;
using Fdw.Data;

namespace Fdw.Services.Etl.Execution;

/// <summary>
/// POCO for the <c>etl.PipelineWatermark</c> table (FDW-1108): when a pipeline last ran successfully, one row per
/// pipeline per tenant.
/// </summary>
/// <remarks>
/// <para>
/// Property names MUST match the SQL column names in <c>etl.PipelineWatermark</c> exactly. OpsDb has no row-level
/// security, so every read and write filters by <see cref="TenantId"/>.
/// </para>
/// <para>
/// The engine writes the row only at the end of a successful run, in place; a failed run never touches it. The row
/// is offered to every run as the reserved parameter <c>LastSuccessfulRun</c>.
/// </para>
/// </remarks>
[ExcludeFromCodeCoverage]
[GenerateMapper]
public sealed class EtlPipelineWatermarkRecord
{
    /// <summary>
    /// Gets or sets the unique identifier of the watermark row.
    /// </summary>
    public Guid Id { get; set; } = Guid.CreateVersion7();

    /// <summary>
    /// Gets or sets the logical Id of the pipeline (<c>pipe.Pipeline.Id</c>).
    /// </summary>
    public Guid PipelineId { get; set; }

    /// <summary>
    /// Gets or sets the tenant the pipeline runs in.
    /// </summary>
    public Guid TenantId { get; set; }

    /// <summary>
    /// Gets or sets when the last successful run of the pipeline started.
    /// </summary>
    public DateTimeOffset LastSuccessfulRun { get; set; }

    /// <summary>
    /// Gets or sets the execution that recorded the watermark.
    /// </summary>
    public Guid ExecutionId { get; set; }
}
