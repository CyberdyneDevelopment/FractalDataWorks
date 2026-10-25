using System;
using System.Diagnostics.CodeAnalysis;

namespace Fdw.Calculations.Endpoints.CalculationEntities;

/// <summary>Request naming a calculation whose versions are listed (FDW-2560).</summary>
[ExcludeFromCodeCoverage]
public class CalculationVersionsRequest
{
    /// <summary>Gets or sets the identity of the calculation.</summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the workspace the versions are listed for. Left out, they are listed for the organization the caller acts in, or for the tenant when
    /// they act in none. A workspace of another tenant is refused as not found.
    /// </summary>
    public Guid? WorkspaceId { get; set; }
}
