using Fdw.Services.Abstractions;

namespace Fdw.Services.Workspaces;

/// <summary>Supplies the Note implementation's own configuration to the domain that registers it.</summary>
public interface INoteImplementationConfigurationProvider
    : IImplementationConfigurationProvider<INoteImplementationConfiguration>
{
}
