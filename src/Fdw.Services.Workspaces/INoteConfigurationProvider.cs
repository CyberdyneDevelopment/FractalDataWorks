using Fdw.Services.Abstractions;

namespace Fdw.Services.Workspaces;

/// <summary>Supplies the configured Note members.</summary>
public interface INoteConfigurationProvider
    : IDomainConfigurationProvider<INoteImplementationConfiguration>
{
}
