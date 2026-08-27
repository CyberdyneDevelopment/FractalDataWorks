using System;
using Fdw.Configuration;

namespace Fdw.Services.Etl.Abstractions;

/// <summary>
/// Typed body for an ETL ENGINE (e.g. <c>BatchCopyPipelineConfiguration</c>,
/// <c>StreamingPipelineConfiguration</c>). The ETL-kind body <c>EtlPipelineConfiguration</c> carries
/// an <c>IEtlPipelineTypedConfiguration? Configuration</c> property whose runtime type is the engine
/// body selected by the kind body's <c>ServiceOptionType</c> discriminator (e.g. "BatchCopy").
/// </summary>
/// <remarks>
/// Why: a bare <see cref="IGenericConfiguration"/>-typed <c>Configuration</c> property does NOT trigger
/// the generated mapper's <c>GetTypedBody</c>/<c>SetTypedBody</c>. This is the trigger for the
/// second typed-body level of the pipeline chain. The engine body is persisted in its own table
/// (<c>pipe.BatchCopyPipeline</c>/<c>pipe.StreamingPipeline</c>) and linked to the parent
/// <c>pipe.EtlPipeline</c> row via an <c>EtlPipelineId</c>/<c>EtlPipelineRowId</c> foreign key.
/// </remarks>
/// <remarks>
/// Why: the linkage members (<see cref="SourceDataSet"/>, <see cref="DestinationDataSet"/>,
/// <see cref="SourceConnectionName"/>, <see cref="DestinationConnectionName"/>, <see cref="IsEnabled"/>,
/// <see cref="SourceDataSetId"/>, <see cref="SinkDataSetId"/>) are promoted onto this shared interface
/// rather than read via a per-engine cast (`is BatchCopyPipelineConfiguration`) so the lineage graph
/// builder can dot-walk any current or future engine polymorphically. Both existing engines already
/// expose these getters, so this promotion is non-breaking.
/// </remarks>
public interface IEtlPipelineTypedConfiguration : IImplementationConfiguration
{
    /// <summary>Gets a value indicating whether the pipeline is enabled.</summary>
    bool IsEnabled { get; }

    /// <summary>Gets the source connection name (populated when the source is a Connection).</summary>
    string SourceConnectionName { get; }

    /// <summary>Gets the source data set name (populated when the source is a DataSet).</summary>
    string SourceDataSet { get; }

    /// <summary>Gets the destination connection name (populated when the destination is a Connection).</summary>
    string DestinationConnectionName { get; }

    /// <summary>Gets the destination data set name (populated when the destination is a DataSet).</summary>
    string DestinationDataSet { get; }

    /// <summary>Gets the logical Id of the source DataSet, if resolved.</summary>
    Guid? SourceDataSetId { get; }

    /// <summary>Gets the logical Id of the sink DataSet, if resolved.</summary>
    Guid? SinkDataSetId { get; }
}
