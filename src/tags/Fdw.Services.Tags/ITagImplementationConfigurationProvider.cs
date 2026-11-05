using Fdw.Services.Abstractions;
using Fdw.Services.Tags.Configuration;

namespace Fdw.Services.Tags;

/// <summary>Supplies the tag implementation's own configuration to the domain that registers it.</summary>
public interface ITagImplementationConfigurationProvider
    : IImplementationConfigurationProvider<ITagImplementationConfiguration>
{
}
