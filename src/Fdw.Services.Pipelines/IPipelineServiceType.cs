using Fdw.ServiceTypes;

namespace Fdw.Services.Pipelines;

/// <summary>
/// Interface for pipeline-service domain service types (the gateway-backed pipeline
/// configuration provider domain, distinct from the EtlPipelineTypes engine collection).
/// </summary>
public interface IPipelineServiceType : IServiceType
{
}
