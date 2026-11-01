namespace Fdw.Web.Calculations.Clients.Models;

/// <summary>
/// Specifies a field and sort direction for windowed calculation ordering.
/// </summary>
public sealed class WindowedOrderFieldPayload
{
    /// <summary>Gets or sets the name of the field to order by.</summary>
    public string FieldName { get; set; } = string.Empty;

    /// <summary>Gets or sets a value indicating whether to sort descending.</summary>
    public bool Descending { get; set; }
}
