using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics.CodeAnalysis;
using Fdw.Configuration;
using Fdw.Data;
using Fdw.Services.Etl.Abstractions;
using Fdw.Services.Etl.Transforms;

namespace Fdw.Services.Etl.Pipelines;

/// <summary>
/// Configuration for streaming pipelines — the "Streaming" ENGINE typed body of
/// <c>EtlPipelineConfiguration</c>. Persisted in <c>pipe.StreamingPipeline</c> as a type-specific child
/// of <c>pipe.EtlPipeline</c>.
/// </summary>
/// <remarks>
/// Why: a standalone typed body implementing <see cref="IEtlPipelineTypedConfiguration"/> (NOT a C#
/// subclass of EtlPipelineConfiguration) — C# header inheritance reintroduced the phantom-column mapper
/// bug. Properties use <c>{ get; set; }</c> to satisfy IOptions binding.
/// </remarks>
[ExcludeFromCodeCoverage]
[GenerateMapper]
[ManagedConfiguration(ServiceCategory = "Pipeline", ServiceType = "Streaming")]
public sealed partial class StreamingPipelineConfiguration : IEtlPipelineTypedConfiguration
{

    /// <inheritdoc/>
    public Guid Id { get; set; }

    /// <summary>Gets or sets the parent EtlPipeline's logical Id (FK to pipe.EtlPipeline.Id).</summary>
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
    /// Gets or sets the transforms to apply at runtime. NOT a column on pipe.StreamingPipeline — the
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
    /// Gets or sets the pipeline version tag.
    /// </summary>
    public string? PipelineVersion { get; set; }

    /// <summary>
    /// Gets or sets the buffer size for streaming operations.
    /// </summary>
    public int BufferSize { get; set; } = 1000;

    /// <summary>
    /// Gets or sets the flush interval in milliseconds.
    /// </summary>
    public int FlushIntervalMs { get; set; } = 5000;

    /// <summary>
    /// Gets or sets whether to use windowing for aggregations.
    /// </summary>
    public bool UseWindowing { get; set; }

    /// <summary>
    /// Gets or sets the window duration in seconds.
    /// </summary>
    public int WindowDurationSeconds { get; set; } = 60;

    /// <summary>
    /// Gets or sets the maximum records per second (rate limiting).
    /// </summary>
    public int? MaxRecordsPerSecond { get; set; }
}
