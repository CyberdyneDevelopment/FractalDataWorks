using System;
using System.Diagnostics.CodeAnalysis;

namespace Fdw.Calculations.Endpoints.CalculationEntities;

/// <summary>
/// Request with a calculation entity ID.
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class CalculationEntityIdRequest
{
    /// <summary>Gets or sets the calculation entity ID.</summary>
    public Guid Id { get; set; }
}
