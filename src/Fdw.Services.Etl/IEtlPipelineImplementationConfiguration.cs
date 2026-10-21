using Fdw.Services.Pipelines.Abstractions;

namespace Fdw.Services.Etl;

/// <summary>
/// The marker every EtlPipeline implementation carries.
/// </summary>
/// <remarks>
/// The marker is what keeps the domain closed: only a configuration carrying it can be registered against this domain or handed back
/// by a read of it. It states no member. What a pipeline reads and writes is read from the pipeline itself, the one way the whole
/// workspace reads it (<see cref="Graphs.GraphPipelineUses"/>), not from fields every engine would have to share (FDW-1885).
/// <para>
/// It also carries <see cref="IPipelineImplementationConfiguration"/>, because an ETL pipeline is what the Pipeline domain's "Etl" row
/// names: <c>pipe.EtlPipeline</c> hangs from <c>pipe.Pipeline</c> by PipelineId. One record therefore answers both reads.
/// </para>
/// </remarks>
public interface IEtlPipelineImplementationConfiguration : IPipelineImplementationConfiguration, IEnablablePipelineConfiguration
{
}
