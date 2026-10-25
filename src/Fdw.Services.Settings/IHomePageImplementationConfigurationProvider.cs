using Fdw.Services.Abstractions;
using Fdw.Services.Settings.Configuration;

namespace Fdw.Services.Settings;

/// <summary>Supplies the home page implementation of the HomePage domain (FDW-2325).</summary>
public interface IHomePageImplementationConfigurationProvider
    : IImplementationConfigurationProvider<IHomePageImplementationConfiguration>
{
}
