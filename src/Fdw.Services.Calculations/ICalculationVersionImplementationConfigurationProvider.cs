using Fdw.Services.Abstractions;
using Fdw.Services.Calculations.Configuration;

namespace Fdw.Services.Calculations;

/// <summary>Reads and writes the stored form of the versions of calculations (FDW-2560).</summary>
public interface ICalculationVersionImplementationConfigurationProvider
    : IImplementationConfigurationProvider<ICalculationVersionImplementationConfiguration>
{
}
