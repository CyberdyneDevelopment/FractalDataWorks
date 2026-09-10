using Fdw.Services.Abstractions;
using Fdw.Services.Calculations.Configuration;

namespace Fdw.Services.Calculations;

/// <summary>Supplies the Windowed calculation's own configuration to the domain that registers it.</summary>
public interface IWindowedCalculationConfigurationProvider
    : IImplementationConfigurationProvider<ICalculationEntityImplementationConfiguration>
{
}
