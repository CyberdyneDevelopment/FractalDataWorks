using System;
using System.Collections.Generic;
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
    /// caller's <c>orgId</c> claim. It therefore lives on the implementation row a read hands back,
    /// not on the four-column domain row.
    /// </remarks>
    Guid? OrgId { get; }

    /// <summary>Gets or sets what the pipeline is for, or <see langword="null"/> when nobody wrote one.</summary>
    /// <remarks>
    /// Kept in the descriptions table, not in a column of either pipeline table (FDW-7323): saving the pipeline writes it in the unit of the save, and reading
    /// the pipeline fills it unless the read states <c>WithoutFilledMembers</c>, which leaves it empty (not read). A pipeline saved with no text removes the one it had.
    /// </remarks>
    string? Description { get; set; }

    /// <summary>Gets the parameters of the pipeline that carry a description, none for an implementation that declares none (FDW-7323).</summary>
    /// <remarks>The provider keeps their descriptions in the descriptions table, owned by this pipeline, beside the pipeline's own.</remarks>
    IReadOnlyList<IDescribedPipelineParameter> DescribedParameters { get; }

    /// <summary>Gets when the engine's row for this version of the pipeline was written (FDW-1149).</summary>
    /// <remarks>
    /// A column of the engine's own row, read like the pipeline's other settings. A save writes a new version of the row, so
    /// this is the time of the last save and a caller can tell a pipeline it opened has since been saved again.
    /// </remarks>
    DateTimeOffset CreateDate { get; }

    /// <summary>Gets when the engine's row for this version of the pipeline was last modified (FDW-1149).</summary>
    DateTimeOffset ModifyDate { get; }
}
