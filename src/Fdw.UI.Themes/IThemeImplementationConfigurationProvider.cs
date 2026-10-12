using Fdw.Services.Abstractions;
using Fdw.UI.Themes.Configuration;

namespace Fdw.UI.Themes;

/// <summary>Supplies the Theme implementation's own configuration to the domain that registers it.</summary>
public interface IThemeImplementationConfigurationProvider
    : IImplementationConfigurationProvider<IThemeImplementationConfiguration>
{
}
