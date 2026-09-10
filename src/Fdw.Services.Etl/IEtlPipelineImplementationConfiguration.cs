using System;
using Fdw.Services.Pipelines.Abstractions;

namespace Fdw.Services.Etl;

/// <summary>
/// The contract every EtlPipeline implementation carries.
/// </summary>
/// <remarks>
/// The marker is what keeps the domain closed: only a configuration carrying it can be
/// registered against this domain or handed back by a read of it.
/// <para>
/// It also carries <see cref="IPipelineImplementationConfiguration"/>, because an ETL pipeline is
/// what the Pipeline domain's "Etl" row names: <c>pipe.EtlPipeline</c> hangs from
/// <c>pipe.Pipeline</c> by PipelineId, and is itself the domain the engines hang from. One record
/// therefore answers both reads -- through Pipeline as its implementation, and through EtlPipeline
/// as its own domain's.
/// </para>
/// <para>
/// The linkage members are here rather than behind a per-engine cast so a reader through the domain
/// sees them on whatever engine the row named. Read-only: the engines expose them as
/// <c>{ get; set; }</c> and every writer builds the concrete record, so nothing writes through this.
/// </para>
/// </remarks>
public interface IEtlPipelineImplementationConfiguration : IPipelineImplementationConfiguration
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
