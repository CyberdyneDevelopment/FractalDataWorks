using System;
using System.Collections.Generic;

namespace Fdw.Calculations.Endpoints.CalculationEntities;

/// <summary>The versions of a calculation at the places the caller may read, nearest the caller first (FDW-2560).</summary>
/// <param name="Id">The identity of the calculation.</param>
/// <param name="Name">The name of the calculation.</param>
/// <param name="PlaceKind">The kind of place the versions were listed for: Workspace, Organization, or Tenant.</param>
/// <param name="PlaceId">The id of that place.</param>
/// <param name="Versions">The versions, nearest first; the first is the one that applies.</param>
public sealed record CalculationVersionsResponse(Guid Id, string Name, string PlaceKind, Guid PlaceId, IReadOnlyList<CalculationVersionResponse> Versions);
