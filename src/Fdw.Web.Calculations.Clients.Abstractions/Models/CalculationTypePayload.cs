using System;

namespace Fdw.Web.Calculations.Clients.Models;

/// <summary>
/// Represents one entry in the unified calculation catalog (codified + configured), tagged with the
/// source that owns it.
/// </summary>
public sealed class CalculationTypePayload
{
    /// <summary>
    /// Gets or sets the name of the calculation type.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the display-friendly name.
    /// </summary>
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the description of the calculation type. Null when the source has none.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Gets or sets the name of the <c>CalculationSourceTypes</c> option that owns this entry
    /// (e.g. "Default", "Configuration").
    /// </summary>
    public string CalculationSource { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the <c>calc.CalculationEntity</c> id backing this entry, when configured.
    /// </summary>
    public Guid? CalculationEntityId { get; set; }

    /// <summary>
    /// Gets or sets the codified <c>CalculationTypes</c> operator id backing this entry, when code-defined.
    /// </summary>
    public int? OperatorId { get; set; }
}
