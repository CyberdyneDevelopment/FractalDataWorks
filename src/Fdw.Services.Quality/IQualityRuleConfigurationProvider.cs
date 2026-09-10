using Fdw.Services.Abstractions;

namespace Fdw.Services.Quality;

/// <summary>Supplies the configured QualityRule members.</summary>
public interface IQualityRuleConfigurationProvider
    : IDomainConfigurationProvider<IQualityRuleImplementationConfiguration>
{
}
