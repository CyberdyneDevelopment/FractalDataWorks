using Fdw.Services.Etl.Abstractions;

namespace Fdw.Services.Etl.Pipelines;

/// <summary>
/// Factory interface for creating streaming pipelines.
/// </summary>
public interface IStreamingPipelineFactory : IEtlPipelineFactory<IEtlPipeline, StreamingPipelineConfiguration>
{
}
