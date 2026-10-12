using System;

namespace Fdw.Web.Calculations.Clients.CalculationEntities;

/// <summary>
/// Summary of a calculation entity returned by the list endpoint.
/// </summary>
public sealed class CalculationEntitySummaryModel
{
    /// <summary>
    /// Gets or sets the unique identifier of the calculation entity.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the calculation entity name.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the optional description.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Gets or sets the calculation entity type name (e.g. "Formula", "Windowed").
    /// </summary>
    public string CalculationEntityType { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets whether this entity is enabled.
    /// </summary>
    public bool IsEnabled { get; set; }
}
