using Fdw.Services.Abstractions;
using Fdw.Services.Calculations.Configuration;

namespace Fdw.Services.Calculations;

/// <summary>Supplies the configured calculation entities.</summary>
public interface ICalculationConfigurationProvider
    : IDomainConfigurationProvider<ICalculationEntityImplementationConfiguration>
{
}
