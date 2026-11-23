using Fdw.Collections;

namespace Fdw.Services.Pipelines.Abstractions.Runs;

/// <summary>How a pipeline run is carried out once it is started (FDW-6131): queued for the host's executor, or run at once in the host that took the request.</summary>
public interface IPipelineRunMode : ITypeOption<int, IPipelineRunMode>
{
}
