using Fdw.Services.Abstractions;
using Fdw.Services.Calculations.Configuration;
using Fdw.Services.Data.Abstractions;

namespace Fdw.Services.Calculations;

/// <summary>Reads and writes the versions of calculations held at organizations and workspaces (FDW-2560).</summary>
public interface ICalculationVersionConfigurationProvider
    : IDomainConfigurationProvider<ICalculationVersionImplementationConfiguration>,
      IDomainRecordProvider<CalculationVersionDomainConfiguration>
{
}
