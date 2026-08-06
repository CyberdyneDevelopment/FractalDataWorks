using System;
using System.Diagnostics.CodeAnalysis;

namespace Fdw.Calculations.Endpoints.CalculationEntities;

/// <summary>
/// Request to execute a calculation entity.
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class ExecuteCalculationEntityRequest
{
    /// <summary>Gets or sets the calculation entity ID to execute.</summary>
    public Guid Id { get; set; }
}
