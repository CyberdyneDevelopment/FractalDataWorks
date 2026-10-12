using System;
using System.Diagnostics.CodeAnalysis;

namespace Fdw.Calculations.Endpoints.CalculationEntities;

/// <summary>
/// Summary DTO for calculation entity list responses.
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class CalculationEntitySummaryDto
{
    /// <summary>Gets or sets the unique identifier.</summary>
    public Guid Id { get; set; }

    /// <summary>Gets or sets the calculation entity name.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets or sets the optional description.</summary>
    public string? Description { get; set; }

    /// <summary>Gets or sets the calculation entity type name.</summary>
    public string CalculationEntityType { get; set; } = string.Empty;

    /// <summary>Gets or sets whether this entity is enabled.</summary>
    public bool IsEnabled { get; set; }
}
