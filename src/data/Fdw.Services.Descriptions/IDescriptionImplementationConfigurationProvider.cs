using Fdw.Services.Abstractions;
using Fdw.Services.Descriptions.Configuration;

namespace Fdw.Services.Descriptions;

/// <summary>Supplies the description implementation's own configuration to the domain that registers it.</summary>
public interface IDescriptionImplementationConfigurationProvider
    : IImplementationConfigurationProvider<IDescriptionImplementationConfiguration>
{
}
