using System;
using System.Diagnostics.CodeAnalysis;

namespace Fdw.Calculations.Endpoints.CalculationEntities;

/// <summary>Request naming a calculation and the place a version of it is held at (FDW-2560).</summary>
[ExcludeFromCodeCoverage]
public class CalculationVersionPlaceRequest : CalculationVersionsRequest
{
    /// <summary>Gets or sets the kind of place: Organization or Workspace.</summary>
    public string PlaceKind { get; set; } = string.Empty;

    /// <summary>Gets or sets the id of the organization or workspace.</summary>
    public Guid PlaceId { get; set; }
}
