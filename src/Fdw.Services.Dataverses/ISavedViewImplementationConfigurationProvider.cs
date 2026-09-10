using Fdw.Services.Abstractions;

namespace Fdw.Services.Dataverses;

/// <summary>Supplies the SavedView implementation's own configuration to the domain that registers it.</summary>
public interface ISavedViewImplementationConfigurationProvider
    : IImplementationConfigurationProvider<ISavedViewImplementationConfiguration>
{
}
