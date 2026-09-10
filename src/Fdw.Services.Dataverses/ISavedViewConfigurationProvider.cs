using Fdw.Services.Abstractions;

namespace Fdw.Services.Dataverses;

/// <summary>Supplies the configured SavedView members.</summary>
public interface ISavedViewConfigurationProvider
    : IDomainConfigurationProvider<ISavedViewImplementationConfiguration>
{
}
