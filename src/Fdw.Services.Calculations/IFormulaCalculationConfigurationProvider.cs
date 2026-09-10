using Fdw.Services.Abstractions;
using Fdw.Services.Calculations.Configuration;

namespace Fdw.Services.Calculations;

/// <summary>Supplies the Formula calculation's own configuration to the domain that registers it.</summary>
public interface IFormulaCalculationConfigurationProvider
    : IImplementationConfigurationProvider<ICalculationEntityImplementationConfiguration>
{
}
