using Fdw.Services.Abstractions;

namespace Fdw.UI.Themes;

/// <summary>Supplies the configured Theme members.</summary>
public interface IThemeConfigurationProvider
    
    
    : IDomainConfigurationProvider<IThemeImplementationConfiguration>
{
}
