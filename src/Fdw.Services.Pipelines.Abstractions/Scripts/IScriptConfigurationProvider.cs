using Fdw.Services.Abstractions;

namespace Fdw.Services.Pipelines.Abstractions;

/// <summary>Supplies the stored scripts (FDW-1442).</summary>
public interface IScriptConfigurationProvider
    : IDomainConfigurationProvider<ISqlScriptImplementationConfiguration>
{
}
