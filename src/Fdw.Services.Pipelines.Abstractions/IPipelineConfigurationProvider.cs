using Fdw.Services.Abstractions;

namespace Fdw.Services.Pipelines.Abstractions;

/// <summary>
/// Resolves configured pipelines and routes each to the implementation provider that owns it.
/// </summary>
public interface IPipelineConfigurationProvider
    : IDomainConfigurationProvider<IPipelineImplementationConfiguration>
{
}
