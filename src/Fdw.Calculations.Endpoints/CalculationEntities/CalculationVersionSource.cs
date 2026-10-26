using Fdw.Services.Calculations;
using Fdw.Services.Calculations.Configuration;
using Fdw.Services.Workspaces;

namespace Fdw.Calculations.Endpoints.CalculationEntities;

/// <summary>Where the versions of calculations are read from, stated for the shared reads of versions held at places (FDW-2560).</summary>
internal static class CalculationVersionSource
{
    internal static VersionSource<CalculationVersionDomainConfiguration> Of(ICalculationVersionConfigurationProvider versions) => new(
        versions,
        nameof(CalculationVersionDomainConfiguration.BaseCalculationId),
        nameof(CalculationVersionDomainConfiguration.ScopeId),
        row => row.BaseCalculationId,
        row => row.ScopeKind,
        row => row.ScopeId);
}
