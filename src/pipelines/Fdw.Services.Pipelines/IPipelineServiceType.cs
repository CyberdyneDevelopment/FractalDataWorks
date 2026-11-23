using Fdw.ServiceTypes;

namespace Fdw.Services.Pipelines;

/// <summary>
/// Interface for the options of <see cref="PipelineServiceTypes"/>, the services a host gives pipelines; distinct from
/// <see cref="Abstractions.IPipelineType"/>, an implementation of the Pipeline domain (FDW-6244).
/// </summary>
public interface IPipelineServiceType : IServiceType
{
}
