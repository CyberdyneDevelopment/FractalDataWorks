using Fdw.Configuration;

namespace Fdw.Services.Hosting.Abstractions;

/// <summary>
/// The hosting domain configuration: names which implementation is configured and holds its settings.
/// </summary>
public interface IHostingConfiguration
    : IPlatformServiceConfiguration<IHostingImplementationConfiguration>
{
}
