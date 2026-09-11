using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics.CodeAnalysis;
using Fdw.Configuration;
using Fdw.Data;
using Fdw.Services.Etl.Transforms;
using Fdw.Services.Pipelines.Abstractions.DataDestination;
using Fdw.Services.Pipelines.Abstractions.DataSource;

namespace Fdw.Services.Etl.Pipelines;

/// <summary>
/// The "BatchCopy" implementation of the EtlPipeline domain. Persisted in <c>pipe.BatchCopyPipeline</c>,
/// hanging from its <c>pipe.EtlPipeline</c> domain row by <see cref="EtlPipelineId"/>.
/// </summary>
/// <remarks>
/// Why it is not a C# subclass of anything: header inheritance reintroduced the phantom-column
/// mapper bug. Properties use <c>{ get; set; }</c> to satisfy IOptions binding.
/// </remarks>
[ExcludeFromCodeCoverage]
[GenerateMapper]
[ManagedConfiguration(ServiceCategory = "Pipeline", ServiceType = "BatchCopy")]
public sealed partial class BatchCopyPipelineConfiguration : IEtlPipelineImplementationConfiguration
{

    /// <inheritdoc/>
    public Guid Id { get; set; }

    /// <summary>Gets or sets the parent EtlPipeline's logical Id (FK to pipe.EtlPipeline.Id).</summary>
    public Guid EtlPipelineId { get; set; }

    /// <inheritdoc/>
    /// <remarks>Set by the domain provider from the domain row; never persisted.</remarks>
    public string Name { get; set; } = string.Empty;

    /// <inheritdoc/>
    /// <remarks>Set by the domain provider from the domain row; never persisted.</remarks>
    public string Domain { get; set; } = string.Empty;

    /// <inheritdoc/>
    /// <remarks>Set by the domain provider from the domain row; never persisted.</remarks>
    public string Implementation { get; set; } = string.Empty;

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

    /// <summary>Gets or sets the Id of the org that owns the pipeline.</summary>
    public Guid? OrgId { get; set; }


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
    [NotMapped]
    public IDataSourceKind? SourceKind { get; set; }

    /// <summary>
    /// Gets or sets the resolved destination kind discriminator (ETL vs ELT).
    /// Not a DB column — populated by the factory from DestinationDataSet / DestinationConnectionName
    /// at construction time. Null means the factory has not yet resolved the kind.
    /// </summary>
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
