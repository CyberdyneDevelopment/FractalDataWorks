using Fdw.Services.Etl.Abstractions;

namespace Fdw.Services.Etl.Pipelines;

/// <summary>
/// Factory interface for creating batch copy pipelines.
/// </summary>
public interface IBatchCopyPipelineFactory : IEtlPipelineFactory<IEtlPipeline, BatchCopyPipelineConfiguration>
{
}
