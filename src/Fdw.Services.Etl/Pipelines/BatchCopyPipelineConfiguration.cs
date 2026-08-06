using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics.CodeAnalysis;
using Fdw.Configuration;
using Fdw.Data;
using Fdw.Services.Etl.Abstractions;
using Fdw.Services.Etl.Transforms;
using Fdw.Services.Pipelines.Abstractions.DataDestination;
using Fdw.Services.Pipelines.Abstractions.DataSource;

namespace Fdw.Services.Etl.Pipelines;

/// <summary>
/// Configuration for batch copy pipelines — the "BatchCopy" ENGINE typed body of
/// <c>EtlPipelineConfiguration</c>. Persisted in <c>pipe.BatchCopyPipeline</c> as a type-specific child
/// of <c>pipe.EtlPipeline</c>.
/// </summary>
/// <remarks>
/// Why: a standalone typed body implementing <see cref="IEtlPipelineTypedConfiguration"/> (NOT a C#
/// subclass of EtlPipelineConfiguration) — C# header inheritance reintroduced the phantom-column mapper
/// bug. Properties use <c>{ get; set; }</c> to satisfy IOptions binding.
/// </remarks>
[ExcludeFromCodeCoverage]
[GenerateMapper]
[ManagedConfiguration(ServiceCategory = "Pipeline", ServiceType = "BatchCopy")]
public sealed partial class BatchCopyPipelineConfiguration : IEtlPipelineTypedConfiguration
{

    /// <inheritdoc/>
    public Guid Id { get; set; }

    /// <summary>Gets or sets the parent EtlPipeline's logical Id (FK to pipe.EtlPipeline.Id).</summary>
    // Why: the keystone cascade stamps this from the EtlPipeline body's Id; the physical EtlPipelineRowId FK
    // column is NOT a POCO property (per the connection convention) so the save translator resolves it by
    // subquery on insert and the read JOIN uses the container's FK metadata. A POCO EtlPipelineRowId would
    // defeat the subquery and insert an empty RowId (FK_BatchCopyPipeline_EtlPipeline violation).
    public Guid EtlPipelineId { get; set; }

    /// <inheritdoc/>
    public string Name { get; set; } = string.Empty;

    /// <inheritdoc/>
    public string SectionName { get; set; } = string.Empty;

    /// <inheritdoc/>
    public string ServiceType { get; set; } = string.Empty;

    /// <inheritdoc/>
    public string? ServiceOptionType { get; set; }

    /// <summary>
    /// Gets or sets the transforms to apply at runtime. NOT a column on pipe.BatchCopyPipeline — the
    /// transforms are persisted on the ETL-kind body (<c>pipe.PipelineOperation</c> → pipe.EtlPipeline)
    /// and copied onto the engine body by the execution seam before the pipeline runs.
    /// </summary>
    [NotMapped]
    public IList<PipelineTransformConfiguration>? Transforms { get; set; }

    /// <summary>
    /// Gets or sets whether the pipeline is enabled.
    /// </summary>
    public bool IsEnabled { get; set; } = true;

    /// <summary>
    /// Gets or sets the source connection name.
    /// </summary>
    public string SourceConnectionName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the source data set identifier.
    /// </summary>
    public string SourceDataSet { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the destination connection name.
    /// </summary>
    public string DestinationConnectionName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the destination data set identifier.
    /// </summary>
    public string DestinationDataSet { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the batch size for processing.
    /// </summary>
    public int BatchSize { get; set; } = 1000;

    /// <summary>
    /// Gets or sets whether to continue processing on errors.
    /// </summary>
    public bool ContinueOnError { get; set; }

    /// <summary>
    /// Gets or sets the maximum number of errors before stopping.
    /// </summary>
    public int MaxErrors { get; set; } = 100;

    /// <summary>
    /// Gets or sets the secret manager name for credential resolution.
    /// </summary>
    public string? SecretManagerName { get; set; }

    /// <summary>
    /// Gets or sets the secret key name.
    /// </summary>
    public string? SecretKeyName { get; set; }

    /// <summary>
    /// Gets or sets the resiliency policy name for retry handling.
    /// </summary>
    public string? ResiliencyPolicyName { get; set; }

    /// <summary>
    /// Gets or sets the logical Id of the source DataSet.
    /// </summary>
    public Guid? SourceDataSetId { get; set; }


    /// <summary>
    /// Gets or sets the logical Id of the sink DataSet.
    /// </summary>
    public Guid? SinkDataSetId { get; set; }

    /// <summary>
    /// Gets or sets the container path for a connection-based source (ETL pattern).
    /// Required when SourceKind is Connection; ignored when SourceKind is DataSet.
    /// Format: schema.table (SQL), /api/path (HTTP), or file path.
    /// </summary>
    public string? SourceContainerPath { get; set; }

    /// <summary>
    /// Gets or sets the container path for a connection-based destination (ETL pattern).
    /// Required when DestinationKind is Connection; ignored when DestinationKind is DataSet.
    /// </summary>
    public string? DestinationContainerPath { get; set; }

    /// <summary>
    /// Gets or sets the resolved source kind discriminator (ETL vs ELT).
    /// Not a DB column — populated by the factory from SourceDataSet / SourceConnectionName
    /// at construction time. Null means the factory has not yet resolved the kind.
    /// </summary>
    // Why: [NotMapped] because this is a runtime discriminator computed from the configuration
    // fields (SourceDataSet vs SourceConnectionName), not a persisted column. The factory sets
    // it so the executor can branch on Kind without repeating the same if-else logic.
    [NotMapped]
    public IDataSourceKind? SourceKind { get; set; }

    /// <summary>
    /// Gets or sets the resolved destination kind discriminator (ETL vs ELT).
    /// Not a DB column — populated by the factory from DestinationDataSet / DestinationConnectionName
    /// at construction time. Null means the factory has not yet resolved the kind.
    /// </summary>
    // Why: same reason as SourceKind — computed from configuration fields, not persisted.
    [NotMapped]
    public IDataDestinationKind? DestinationKind { get; set; }

    /// <summary>
    /// Gets or sets the pipeline version tag.
    /// </summary>
    public string? PipelineVersion { get; set; }

    /// <summary>
    /// Gets or sets the maximum parallelism for batch processing.
    /// </summary>
    public int MaxParallelism { get; set; } = 1;

    /// <summary>
    /// Gets or sets the load mode (Append, Replace, Upsert).
    /// </summary>
    public string LoadMode { get; set; } = "Append";

    /// <summary>
    /// Gets or sets whether to truncate destination before load.
    /// </summary>
    public bool TruncateBeforeLoad { get; set; }
}
