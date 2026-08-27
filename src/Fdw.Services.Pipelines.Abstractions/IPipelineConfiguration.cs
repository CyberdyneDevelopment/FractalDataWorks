using Fdw.Configuration;

namespace Fdw.Services.Pipelines.Abstractions;

/// <summary>
/// One configured pipeline — the domain record, naming which implementation it is and holding that
/// implementation's own configuration.
/// </summary>
public interface IPipelineConfiguration
    : IPlatformServiceConfiguration<IPipelineImplementationConfiguration>
{
}
