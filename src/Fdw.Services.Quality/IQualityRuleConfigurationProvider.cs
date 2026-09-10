using Fdw.Services.Quality.Configuration;
using Fdw.Services.Abstractions;

namespace Fdw.Services.Quality;

/// <summary>Supplies the configured QualityRule members.</summary>
public interface IQualityRuleConfigurationProvider
    : IDomainConfigurationProvider<IQualityRuleImplementationConfiguration>
{
}
