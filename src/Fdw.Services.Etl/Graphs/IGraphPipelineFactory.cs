using Fdw.Services.Etl.Abstractions;

namespace Fdw.Services.Etl.Graphs;

/// <summary>
/// Factory interface for creating batch copy pipelines.
/// </summary>
public interface IGraphPipelineFactory : IEtlPipelineFactory<IEtlPipeline, GraphPipelineConfiguration>
{
}
