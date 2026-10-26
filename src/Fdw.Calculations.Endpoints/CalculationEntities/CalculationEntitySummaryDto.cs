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

    /// <summary>
    /// Gets or sets how many versions of the calculation the caller may read, counting the calculation itself as one (FDW-2560). It is 1 when no
    /// organization or workspace the caller may read has written its own description for it.
    /// </summary>
    public int VersionCount { get; set; }
}
