using Fdw.Collections;

namespace Fdw.Services.Etl.Abstractions.TriggerSources;

/// <summary>
/// What caused an ETL run to start.
/// </summary>
/// <remarks>
/// The concept already travels the execution path as a bare string on
/// <c>TriggerRequest</c>, <c>PipelineExecutionRequest</c> and <c>OrchestrationNodeExecutionRequest</c>.
/// Naming it as a collection gives those strings a closed set to be validated against, and a place
/// for a downstream package to add a source the framework does not ship.
/// </remarks>
public interface ITriggerSource : ITypeOption<int>
{
}
