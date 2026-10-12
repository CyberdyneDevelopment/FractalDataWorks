using System;

namespace Fdw.Services.Data.Clients.Models;

/// <summary>
/// Editor-state payload for a calculated field during in-place workbench composition.
/// </summary>
public sealed class DataSetCalculationEditorPayload
{
    /// <summary>Gets or sets the unique identifier for this calculation.</summary>
    public Guid CalculationId { get; set; }

    /// <summary>Gets or sets the calculated field name.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets or sets the formula expression that produces the field value.</summary>
    public string Formula { get; set; } = string.Empty;

    /// <summary>Gets or sets the output data type of the calculated field.</summary>
    public string DataType { get; set; } = string.Empty;

    /// <summary>Gets or sets an optional description of what the calculation computes.</summary>
    public string? Description { get; set; }

    /// <summary>Gets or sets whether this calculation can be removed from the working set.</summary>
    public bool CanRemove { get; set; }
}
