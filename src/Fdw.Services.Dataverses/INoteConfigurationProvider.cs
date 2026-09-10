using Fdw.Services.Abstractions;

namespace Fdw.Services.Dataverses;

/// <summary>Supplies the configured Note members.</summary>
public interface INoteConfigurationProvider
    : IDomainConfigurationProvider<INoteImplementationConfiguration>
{
}
