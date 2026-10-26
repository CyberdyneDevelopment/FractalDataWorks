using System;

namespace Fdw.Calculations.Endpoints.CalculationEntities;

/// <summary>One version of a calculation, at a place the caller may read (FDW-2560).</summary>
/// <param name="PlaceKind">Tenant, Organization or Workspace.</param>
/// <param name="PlaceId">The id of the tenant, organization or workspace.</param>
/// <param name="Depth">How far above the caller's own place this place is: 0 for the caller's own place.</param>
/// <param name="Description">The description held at that place.</param>
/// <param name="Label">What to show: the calculation's name, followed for an organization or a workspace by its name in parentheses.</param>
/// <param name="IsBase">True for the calculation itself, which is the tenant's version.</param>
/// <param name="Applies">True for the nearest version to the caller, which is the one that applies to them.</param>
public sealed record CalculationVersionResponse(string PlaceKind, Guid PlaceId, int Depth, string? Description, string Label, bool IsBase, bool Applies);
