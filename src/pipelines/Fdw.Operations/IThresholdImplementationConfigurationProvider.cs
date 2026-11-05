using Fdw.Operations.Configuration;
using Fdw.Services.Abstractions;

namespace Fdw.Operations;

/// <summary>Reads and writes what a threshold carries (FDW-3936).</summary>
public interface IThresholdImplementationConfigurationProvider
    : IImplementationConfigurationProvider<IThresholdImplementationConfiguration>
{
}
