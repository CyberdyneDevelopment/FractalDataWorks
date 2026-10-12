using System;
using Fdw.Configuration;

namespace Fdw.Services.Pipelines.Abstractions;

/// <summary>
/// The contract every Pipeline implementation carries.
/// </summary>
/// <remarks>
/// The marker is what keeps the domain closed: only a configuration carrying it can be registered
/// against this domain or handed back by a read of it. A <c>pipe.Pipeline</c> domain row names the
/// implementation it is, and that implementation's own row lives in its own table --
/// <c>pipe.EtlPipeline</c> for "Etl" -- linked by a <c>PipelineId</c>/<c>PipelineRowId</c> foreign
/// key. A read through the domain is already dispatched, so a caller is handed the implementation
/// rather than something to unwrap.
/// </remarks>
public interface IPipelineImplementationConfiguration : IImplementationConfiguration
{
    /// <summary>Gets the Id of the org that owns the pipeline, or <see langword="null"/> for an unowned one.</summary>
    /// <remarks>
    /// Payload, not an ambient RLS column: the ETL background service reads it off a dispatched read
    /// to key the realtime firehose (<c>org:{OrgId}:pipeline-updates</c>), which the hub joins from the
    /// caller's <c>org_id</c> claim. It therefore lives on the implementation row a read hands back,
    /// not on the four-column domain row.
    /// </remarks>
    Guid? OrgId { get; }

    /// <summary>Gets what the pipeline is for, or <see langword="null"/> when nobody wrote one.</summary>
    /// <remarks>
    /// A column of the engine's own row, like every other setting of the pipeline, so saving the pipeline
    /// stores it and reading the pipeline returns it. It is deliberately not a column of the shared
    /// <c>pipe.Pipeline</c> domain row: a domain row that lists a field is re-versioned when that field changes,
    /// and two tables cite <c>pipe.Pipeline(Id)</c> with a foreign key, which needs that row written once.
    /// </remarks>
    string? Description { get; }

    /// <summary>Gets when the engine's row for this version of the pipeline was written (FDW-1149).</summary>
    /// <remarks>
    /// A column of the engine's own row, read like the pipeline's other settings. A save writes a new version of the row, so
    /// this is the time of the last save and a caller can tell a pipeline it opened has since been saved again.
    /// </remarks>
    DateTimeOffset CreateDate { get; }

    /// <summary>Gets when the engine's row for this version of the pipeline was last modified (FDW-1149).</summary>
    DateTimeOffset ModifyDate { get; }
}
