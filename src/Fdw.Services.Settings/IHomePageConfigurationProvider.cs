using Fdw.Services.Abstractions;
using Fdw.Services.Settings.Configuration;

namespace Fdw.Services.Settings;

/// <summary>Supplies the home pages (FDW-2325).</summary>
public interface IHomePageConfigurationProvider
    : IDomainConfigurationProvider<IHomePageImplementationConfiguration>
{
}
