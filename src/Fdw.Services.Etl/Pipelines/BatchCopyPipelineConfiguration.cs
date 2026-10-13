using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics.CodeAnalysis;
using Fdw.Configuration;
using Fdw.Data;
using Fdw.Services.Etl.Pipelines.Loads;
using Fdw.Services.Etl.Transforms;
using Fdw.Services.Pipelines.Abstractions.DataDestination;
using Fdw.Services.Pipelines.Abstractions.DataSource;

namespace Fdw.Services.Etl.Pipelines;

/// <summary>
/// The "BatchCopy" implementation of the EtlPipeline domain. Persisted in <c>pipe.BatchCopyPipelineConfiguration</c>,
/// hanging from its <c>pipe.Pipeline</c> domain row by <c>PipelineRowId</c>.
/// </summary>
/// <remarks>
/// Why it is not a C# subclass of anything: header inheritance reintroduced the phantom-column
/// mapper bug. Properties use <c>{ get; set; }</c> to satisfy IOptions binding.
/// </remarks>
[ExcludeFromCodeCoverage]
[GenerateMapper]
[ManagedConfiguration(ServiceCategory = "Pipeline", ServiceType = "BatchCopy")]
public sealed partial class BatchCopyPipelineConfiguration : IEtlPipelineImplementationConfiguration, IDataStoreSourcedPipelineConfiguration, IDataStoreDestinedPipelineConfiguration
{

    /// <inheritdoc/>
    public Guid Id { get; set; }

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
    /// Gets or sets the transforms to apply at runtime, in ExecutionOrder. Rows live in
    /// <c>pipe.PipelineTransform</c>, each holding this implementation row's RowId in
    /// <c>BatchCopyPipelineConfigurationRowId</c>, the way a connection's properties hang from theirs.
    /// </summary>
    [NotMapped]
    public IList<PipelineTransformConfiguration>? Transforms { get; set; }

    /// <summary>
    /// Gets or sets whether the pipeline is enabled.
    /// </summary>
    public bool IsEnabled { get; set; } = true;

    /// <summary>
    /// Gets or sets the source data set identifier.
    /// </summary>
    public string SourceDataSet { get; set; } = string.Empty;

    /// <inheritdoc />
    /// <remarks>Maps to <c>pipe.BatchCopyPipelineConfiguration.SourceDataStoreName</c> (FDW-1103).</remarks>
    public string? SourceDataStoreName { get; set; }

    /// <inheritdoc />
    /// <remarks>Maps to <c>pipe.BatchCopyPipelineConfiguration.SourceDataStorePath</c> (FDW-1103).</remarks>
    public string? SourceDataStorePath { get; set; }

    /// <inheritdoc />
    /// <remarks>Maps to <c>pipe.BatchCopyPipelineConfiguration.SourceContainerName</c> (FDW-1103).</remarks>
    public string? SourceContainerName { get; set; }

    /// <summary>
    /// Gets or sets the destination data set identifier.
    /// </summary>
    public string DestinationDataSet { get; set; } = string.Empty;

    /// <inheritdoc />
    /// <remarks>Maps to <c>pipe.BatchCopyPipelineConfiguration.DestinationDataStoreName</c> (FDW-1269).</remarks>
    public string? DestinationDataStoreName { get; set; }

    /// <inheritdoc />
    /// <remarks>Maps to <c>pipe.BatchCopyPipelineConfiguration.DestinationDataStorePath</c> (FDW-1269).</remarks>
    public string? DestinationDataStorePath { get; set; }

    /// <inheritdoc />
    /// <remarks>Maps to <c>pipe.BatchCopyPipelineConfiguration.DestinationContainerName</c> (FDW-1269).</remarks>
    public string? DestinationContainerName { get; set; }

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

    /// <inheritdoc/>
    public string? Description { get; set; }

    /// <inheritdoc/>
    public DateTimeOffset CreateDate { get; set; }

    /// <inheritdoc/>
    public DateTimeOffset ModifyDate { get; set; }


    /// <summary>
    /// Gets or sets the logical Id of the sink DataSet.
    /// </summary>
    public Guid? SinkDataSetId { get; set; }

    /// <summary>
    /// Gets or sets the resolved source kind discriminator (ETL vs ELT).
    /// Not a DB column — populated by the factory from SourceDataSet / SourceDataStoreName
    /// at construction time. Null means the factory has not yet resolved the kind.
    /// </summary>
    [NotMapped]
    public IDataSourceKind? SourceKind { get; set; }

    /// <summary>
    /// Gets or sets the resolved destination kind discriminator (ETL vs ELT).
    /// Not a DB column — populated by the factory from DestinationDataSet / DestinationDataStoreName
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
    /// Gets or sets the load mode (resolved against <c>LoadModes</c>: Append, Replace, Upsert), or <see langword="null"/>
    /// when the pipeline states load steps instead (FDW-1108).
    /// </summary>
    /// <remarks>
    /// Why no default: a pipeline states a load mode or load steps, never both, and a silent <c>"Append"</c> would hide
    /// which. <c>PipelineLoadValidator</c> refuses a pipeline that states neither or both.
    /// </remarks>
    public string? LoadMode { get; set; }

    /// <summary>
    /// Gets or sets whether to truncate destination before load.
    /// </summary>
    public bool TruncateBeforeLoad { get; set; }

    /// <summary>
    /// Gets or sets the load rows of the pipeline (FDW-1108): when the pipeline has no load steps, the one row of its
    /// own load (step order <see langword="null"/>) carrying the load options and the value sources of the implicit
    /// load; otherwise its load steps, step order 1 and up, run in one unit of work. The two never coexist. Rows live
    /// in <c>pipe.PipelineLoadStep</c>, each holding this implementation row's RowId in
    /// <c>BatchCopyPipelineConfigurationRowId</c>, the way <see cref="Transforms"/> do.
    /// </summary>
    [NotMapped]
    public IList<PipelineLoadStepConfiguration>? LoadSteps { get; set; }

    /// <summary>
    /// Gets or sets the run parameters the pipeline declares (FDW-1108): the trigger supplies a value by name and
    /// sources and transforms reference it. Rows live in <c>pipe.PipelineRunParameter</c>.
    /// </summary>
    [NotMapped]
    public IList<PipelineRunParameterConfiguration>? RunParameters { get; set; }
}
