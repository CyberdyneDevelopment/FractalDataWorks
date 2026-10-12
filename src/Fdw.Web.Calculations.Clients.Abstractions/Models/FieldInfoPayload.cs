namespace Fdw.Web.Calculations.Clients.Models;

/// <summary>
/// Lightweight field descriptor used in bulk DataSet field enumeration for formula autocomplete.
/// </summary>
public sealed class FieldInfoPayload
{
    /// <summary>
    /// Gets or sets the field name.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the data type of the field (e.g., "string", "decimal", "datetime").
    /// </summary>
    public string DataType { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the optional description of the field.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the field value is derived from a calculation.
    /// </summary>
    public bool IsCalculated { get; set; }
}
