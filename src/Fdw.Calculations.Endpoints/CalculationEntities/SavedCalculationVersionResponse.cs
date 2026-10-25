using System;

namespace Fdw.Calculations.Endpoints.CalculationEntities;

/// <summary>A version of a calculation as it was saved at a place (FDW-2560).</summary>
/// <param name="PlaceKind">Organization or Workspace.</param>
/// <param name="PlaceId">The id of the organization or workspace.</param>
/// <param name="Description">The description held at that place.</param>
public sealed record SavedCalculationVersionResponse(string PlaceKind, Guid PlaceId, string Description);
