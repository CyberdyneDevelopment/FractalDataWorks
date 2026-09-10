using Fdw.Services.Abstractions;

namespace Fdw.Services.Quality;

/// <summary>Supplies the configured PromotionRequest members.</summary>
public interface IPromotionRequestConfigurationProvider
    : IDomainConfigurationProvider<IPromotionRequestImplementationConfiguration>
{
}
