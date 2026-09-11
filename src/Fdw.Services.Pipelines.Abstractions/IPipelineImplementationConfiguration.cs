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
}
