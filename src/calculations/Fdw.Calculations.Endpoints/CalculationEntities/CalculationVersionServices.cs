using Fdw.Services.Authorization.Abstractions;
using Fdw.Services.Calculations;
using Fdw.Services.Multitenancy.Abstractions;

namespace Fdw.Calculations.Endpoints.CalculationEntities;

/// <summary>What the flows over the versions of a calculation read and ask (FDW-2560).</summary>
/// <param name="Calculations">Reads the calculations, which are the tenant's versions.</param>
/// <param name="Versions">Reads and writes the versions held at organizations and workspaces.</param>
/// <param name="Ancestry">Says which places are at or above a place.</param>
/// <param name="Authority">Says whether the caller holds a right at a place.</param>
internal sealed record CalculationVersionServices(
    ICalculationConfigurationProvider Calculations,
    ICalculationVersionConfigurationProvider Versions,
    IPlaceAncestry Ancestry,
    IScopeAuthority Authority);
