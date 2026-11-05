using Fdw.Operations.Configuration;
using Fdw.Services.Abstractions;

namespace Fdw.Operations;

/// <summary>The thresholds of a tenant, by name or Id (FDW-3936): the domain provider; the implementation provider is registered under each threshold type.</summary>
public interface IThresholdConfigurationProvider
    : IDomainConfigurationProvider<IThresholdImplementationConfiguration>
{
}
